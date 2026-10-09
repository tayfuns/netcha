using Microsoft.Extensions.Options;
using NetCha.Api.Configuration;

namespace NetCha.Api.Admin;

public sealed class MemoryAdminAuthStore(IOptions<AdminOptions> options, TimeProvider timeProvider) : IAdminAuthStore
{
    private readonly object _gate = new();
    private readonly Dictionary<string, (long UserId, DateTimeOffset ExpiresAt)> _sessions = [];
    private string? _passwordHash;
    private DateTimeOffset? _lastLoginAt;
    private readonly DateTimeOffset _createdAt = timeProvider.GetUtcNow();

    public Task InitializeAsync(string? bootstrapPassword, CancellationToken cancellationToken)
    {
        lock (_gate)
            if (_passwordHash is null && !string.IsNullOrWhiteSpace(bootstrapPassword)) _passwordHash = AdminPassword.Hash(bootstrapPassword);
        return Task.CompletedTask;
    }

    public Task<AdminSession?> LoginAsync(string username, string password, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            if (_passwordHash is null && !string.IsNullOrWhiteSpace(options.Value.ApiKey)) _passwordHash = AdminPassword.Hash(options.Value.ApiKey);
            if (_passwordHash is null || !string.Equals(username, "admin", StringComparison.OrdinalIgnoreCase) || !AdminPassword.Verify(password, _passwordHash))
                return Task.FromResult<AdminSession?>(null);
            var now = timeProvider.GetUtcNow();
            var expiresAt = now.AddHours(options.Value.SessionHours);
            var token = AdminPassword.NewToken();
            _lastLoginAt = now;
            _sessions[AdminPassword.TokenHash(token)] = (1, expiresAt);
            return Task.FromResult<AdminSession?>(new AdminSession(token, expiresAt, User()));
        }
    }

    public Task<AdminUser?> ValidateSessionAsync(string token, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            var valid = _sessions.TryGetValue(AdminPassword.TokenHash(token), out var session) && session.ExpiresAt > timeProvider.GetUtcNow();
            return Task.FromResult<AdminUser?>(valid ? User() : null);
        }
    }

    public Task<bool> ChangePasswordAsync(long userId, string currentPassword, string newPassword, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            if (userId != 1 || _passwordHash is null || !AdminPassword.Verify(currentPassword, _passwordHash)) return Task.FromResult(false);
            _passwordHash = AdminPassword.Hash(newPassword);
            _sessions.Clear();
            return Task.FromResult(true);
        }
    }

    public Task LogoutAsync(string token, CancellationToken cancellationToken)
    {
        lock (_gate) _sessions.Remove(AdminPassword.TokenHash(token));
        return Task.CompletedTask;
    }

    private AdminUser User() => new(1, "admin", _createdAt, _lastLoginAt);
}

