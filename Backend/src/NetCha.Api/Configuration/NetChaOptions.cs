using System.ComponentModel.DataAnnotations;

namespace NetCha.Api.Configuration;

public sealed class CaptchaOptions
{
    public const string SectionName = "Captcha";
    public string? HmacKey { get; set; }

    [Range(1_000, 1_000_000)]
    public int MaxNumber { get; set; } = 100_000;

    [Range(30, 3_600)]
    public int ExpiresInSeconds { get; set; } = 300;
}

public sealed class RedisOptions
{
    public const string SectionName = "Redis";
    public string? ConnectionString { get; set; }
    public bool FallbackToMemory { get; set; } = true;
    public string KeyPrefix { get; set; } = "netcha";
}

public sealed class PersistenceOptions
{
    public const string SectionName = "Persistence";
    public string? ConnectionString { get; set; }
    public bool AutoMigrate { get; set; } = true;
    public bool FallbackToMemory { get; set; } = true;
}

public sealed class AdminOptions
{
    public const string SectionName = "Admin";
    public string? ApiKey { get; set; }
}

public sealed record EffectiveCaptchaSettings(int MaxNumber, int ExpiresInSeconds);
