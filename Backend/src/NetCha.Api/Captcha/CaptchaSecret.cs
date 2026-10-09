using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using NetCha.Api.Configuration;

namespace NetCha.Api.Captcha;

public interface ICaptchaSecret
{
    ReadOnlyMemory<byte> Value { get; }
}

public sealed class CaptchaSecret : ICaptchaSecret
{
    public CaptchaSecret(IOptions<CaptchaOptions> options, IHostEnvironment environment, ILogger<CaptchaSecret> logger)
    {
        var configured = options.Value.HmacKey;
        if (!string.IsNullOrWhiteSpace(configured))
        {
            Value = Encoding.UTF8.GetBytes(configured);
            if (Value.Length < 32)
                throw new InvalidOperationException("Captcha:HmacKey must contain at least 32 UTF-8 bytes.");
            return;
        }

        if (!environment.IsDevelopment() && !environment.IsEnvironment("Testing"))
            throw new InvalidOperationException("Captcha:HmacKey is required outside Development/Testing.");

        Value = RandomNumberGenerator.GetBytes(32);
        logger.LogWarning("Captcha:HmacKey is not configured. Using an ephemeral development key; challenges will not survive a restart.");
    }

    public ReadOnlyMemory<byte> Value { get; }
}
