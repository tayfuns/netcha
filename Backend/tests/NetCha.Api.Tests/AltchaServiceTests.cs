using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using NetCha.Api.Captcha;
using NetCha.Api.Configuration;

namespace NetCha.Api.Tests;

public sealed class AltchaServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 9, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Created_challenge_can_be_solved_and_verified()
    {
        var service = CreateService();
        var challenge = service.CreateChallenge();
        var payload = EncodeSolvedPayload(challenge, 5_000);

        var result = service.Verify(payload);

        Assert.True(result.IsValid);
        Assert.NotNull(result.ReplayKey);
        Assert.True(result.ExpiresAt > Now);
    }

    [Fact]
    public void Tampered_signature_is_rejected()
    {
        var service = CreateService();
        var challenge = service.CreateChallenge();
        var payload = new AltchaPayload(challenge.Algorithm, challenge.Challenge, 0, challenge.Salt, new string('0', 64));

        var result = service.Verify(Convert.ToBase64String(JsonSerializer.SerializeToUtf8Bytes(payload)));

        Assert.False(result.IsValid);
        Assert.Equal("signature_invalid", result.Error);
    }

    [Fact]
    public void Expired_challenge_is_rejected()
    {
        var challenge = CreateService().CreateChallenge();
        var payload = EncodeSolvedPayload(challenge, 5_000);

        var result = CreateService(Now.AddMinutes(2)).Verify(payload);

        Assert.False(result.IsValid);
        Assert.Equal("challenge_expired", result.Error);
    }

    [Theory]
    [InlineData(null, "payload_required")]
    [InlineData("not-base64", "payload_invalid")]
    public void Malformed_payload_is_rejected(string? payload, string expectedError)
    {
        var result = CreateService().Verify(payload);
        Assert.False(result.IsValid);
        Assert.Equal(expectedError, result.Error);
    }

    private static AltchaService CreateService(DateTimeOffset? now = null) => new(
        Options.Create(new CaptchaOptions { MaxNumber = 5_000, ExpiresInSeconds = 60 }),
        new TestSecret(),
        new FixedTimeProvider(now ?? Now));

    internal static string EncodeSolvedPayload(AltchaChallenge challenge, int maxNumber)
    {
        for (var number = 0; number <= maxNumber; number++)
        {
            var candidate = Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(challenge.Salt + number)));
            if (candidate == challenge.Challenge)
            {
                var payload = new AltchaPayload(challenge.Algorithm, challenge.Challenge, number, challenge.Salt, challenge.Signature);
                return Convert.ToBase64String(JsonSerializer.SerializeToUtf8Bytes(payload));
            }
        }
        throw new InvalidOperationException("Challenge was not solved within the configured range.");
    }

    private sealed class TestSecret : ICaptchaSecret
    {
        public ReadOnlyMemory<byte> Value { get; } = Encoding.UTF8.GetBytes("unit-test-key-with-at-least-32-bytes");
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
