using System.Security.Cryptography;
using System.Text;

namespace NetCha.Api.Captcha;

internal static class CaptchaCrypto
{
    internal static string Sha256Hex(string value) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

    internal static string HmacSha256Hex(string value, ReadOnlySpan<byte> key) =>
        Convert.ToHexStringLower(HMACSHA256.HashData(key, Encoding.UTF8.GetBytes(value)));

    internal static bool FixedTimeHexEquals(string expected, string supplied)
    {
        try
        {
            return CryptographicOperations.FixedTimeEquals(
                Convert.FromHexString(expected),
                Convert.FromHexString(supplied));
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
