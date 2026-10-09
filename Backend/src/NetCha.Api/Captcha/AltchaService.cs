using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using NetCha.Api.Configuration;

namespace NetCha.Api.Captcha;

public interface IAltchaService
{
    AltchaChallenge CreateChallenge();
    CaptchaVerificationResult Verify(string? encodedPayload);
}

public sealed class AltchaService(
    IOptions<CaptchaOptions> options,
    ICaptchaSecret secret,
    TimeProvider timeProvider) : IAltchaService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly CaptchaOptions _options = options.Value;

    public AltchaChallenge CreateChallenge()
    {
        var number = RandomNumberGenerator.GetInt32(_options.MaxNumber + 1);
        var expires = timeProvider.GetUtcNow().AddSeconds(_options.ExpiresInSeconds).ToUnixTimeSeconds();
        var nonce = Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(16));
        var salt = $"expires={expires}&nonce={nonce}";
        var challenge = CaptchaCrypto.Sha256Hex(salt + number);
        var signature = CaptchaCrypto.HmacSha256Hex(challenge, secret.Value.Span);
        return new AltchaChallenge("SHA-256", challenge, salt, signature);
    }

    public CaptchaVerificationResult Verify(string? encodedPayload)
    {
        if (string.IsNullOrWhiteSpace(encodedPayload))
            return Invalid("payload_required");

        AltchaPayload? payload;
        try
        {
            payload = JsonSerializer.Deserialize<AltchaPayload>(Convert.FromBase64String(encodedPayload), JsonOptions);
        }
        catch (Exception exception) when (exception is FormatException or JsonException)
        {
            return Invalid("payload_invalid");
        }

        if (payload is null || !string.Equals(payload.Algorithm, "SHA-256", StringComparison.OrdinalIgnoreCase))
            return Invalid("algorithm_unsupported");
        if (payload.Number is < 0 || payload.Number > _options.MaxNumber)
            return Invalid("number_out_of_range");
        if (string.IsNullOrWhiteSpace(payload.Challenge) || string.IsNullOrWhiteSpace(payload.Signature) || string.IsNullOrWhiteSpace(payload.Salt))
            return Invalid("payload_invalid");

        var expectedSignature = CaptchaCrypto.HmacSha256Hex(payload.Challenge, secret.Value.Span);
        if (!CaptchaCrypto.FixedTimeHexEquals(expectedSignature, payload.Signature))
            return Invalid("signature_invalid");

        var expectedChallenge = CaptchaCrypto.Sha256Hex(payload.Salt + payload.Number);
        if (!CaptchaCrypto.FixedTimeHexEquals(expectedChallenge, payload.Challenge))
            return Invalid("solution_invalid");

        var expiry = ReadExpiry(payload.Salt);
        if (expiry is null)
            return Invalid("expiry_missing");
        if (expiry <= timeProvider.GetUtcNow())
            return Invalid("challenge_expired");

        var replayKey = CaptchaCrypto.Sha256Hex(payload.Challenge + ":" + payload.Signature);
        return new CaptchaVerificationResult(true, null, replayKey, expiry);
    }

    private static DateTimeOffset? ReadExpiry(string salt)
    {
        foreach (var item in salt.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = item.Split('=', 2);
            if (parts.Length == 2 && parts[0] == "expires" && long.TryParse(parts[1], out var seconds))
            {
                try { return DateTimeOffset.FromUnixTimeSeconds(seconds); }
                catch (ArgumentOutOfRangeException) { return null; }
            }
        }
        return null;
    }

    private static CaptchaVerificationResult Invalid(string error) => new(false, error, null, null);
}
