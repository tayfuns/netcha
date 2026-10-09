using System.Text.Json.Serialization;

namespace NetCha.Api.Captcha;

public sealed record AltchaChallenge(
    [property: JsonPropertyName("algorithm")] string Algorithm,
    [property: JsonPropertyName("challenge")] string Challenge,
    [property: JsonPropertyName("salt")] string Salt,
    [property: JsonPropertyName("signature")] string Signature);

public sealed record AltchaPayload(
    [property: JsonPropertyName("algorithm")] string Algorithm,
    [property: JsonPropertyName("challenge")] string Challenge,
    [property: JsonPropertyName("number")] long Number,
    [property: JsonPropertyName("salt")] string Salt,
    [property: JsonPropertyName("signature")] string Signature);

public sealed record VerifyCaptchaRequest(string? Payload);
public sealed record VerifyCaptchaResponse(bool Verified, string? Error = null);
public sealed record CaptchaVerificationResult(bool IsValid, string? Error, string? ReplayKey, DateTimeOffset? ExpiresAt);
