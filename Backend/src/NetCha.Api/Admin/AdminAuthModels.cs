using System.Security.Cryptography;

namespace NetCha.Api.Admin;

public sealed record AdminLoginRequest(string Username, string Password);
public sealed record AdminLoginResponse(string Token, DateTimeOffset ExpiresAt, AdminUser User);
public sealed record AdminUser(long Id, string Username, DateTimeOffset CreatedAt, DateTimeOffset? LastLoginAt);
public sealed record AdminSession(string Token, DateTimeOffset ExpiresAt, AdminUser User);
public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);

public interface IAdminAuthStore
{
    Task InitializeAsync(string? bootstrapPassword, CancellationToken cancellationToken);
    Task<AdminSession?> LoginAsync(string username, string password, CancellationToken cancellationToken);
    Task<AdminUser?> ValidateSessionAsync(string token, CancellationToken cancellationToken);
    Task<bool> ChangePasswordAsync(long userId, string currentPassword, string newPassword, CancellationToken cancellationToken);
    Task LogoutAsync(string token, CancellationToken cancellationToken);
}

public static class AdminPassword
{
    private const int Iterations = 210_000;
    private const int SaltSize = 16;
    private const int HashSize = 32;

    public static string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, HashSize);
        return $"pbkdf2-sha256${Iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
    }

    public static bool Verify(string password, string encoded)
    {
        var parts = encoded.Split('$');
        if (parts.Length != 4 || parts[0] != "pbkdf2-sha256" || !int.TryParse(parts[1], out var iterations)) return false;
        try
        {
            var salt = Convert.FromBase64String(parts[2]);
            var expected = Convert.FromBase64String(parts[3]);
            var actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, expected.Length);
            return CryptographicOperations.FixedTimeEquals(actual, expected);
        }
        catch (FormatException) { return false; }
    }

    public static string NewToken() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
    public static string TokenHash(string token) => Convert.ToHexStringLower(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(token)));
}

