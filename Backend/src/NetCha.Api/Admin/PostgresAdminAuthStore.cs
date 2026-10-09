using Microsoft.Extensions.Options;
using NetCha.Api.Configuration;
using Npgsql;

namespace NetCha.Api.Admin;

public sealed class PostgresAdminAuthStore(NpgsqlDataSource dataSource, IOptions<AdminOptions> options, TimeProvider timeProvider) : IAdminAuthStore
{
    public async Task InitializeAsync(string? bootstrapPassword, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(bootstrapPassword)) return;
        await using var command = dataSource.CreateCommand("""
            INSERT INTO netcha_private.admin_users (username, password_hash)
            SELECT 'admin', $1
            WHERE NOT EXISTS (SELECT 1 FROM netcha_private.admin_users)
            ON CONFLICT (username) DO NOTHING
            """);
        command.Parameters.AddWithValue(AdminPassword.Hash(bootstrapPassword));
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<AdminSession?> LoginAsync(string username, string password, CancellationToken cancellationToken)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var find = new NpgsqlCommand("SELECT id, username, password_hash, created_at, last_login_at FROM netcha_private.admin_users WHERE lower(username) = lower($1) AND is_active", connection);
        find.Parameters.AddWithValue(username.Trim());
        await using var reader = await find.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken)) return null;
        var id = reader.GetInt64(0);
        var canonicalUsername = reader.GetString(1);
        var hash = reader.GetString(2);
        var createdAt = reader.GetFieldValue<DateTimeOffset>(3);
        DateTimeOffset? previousLogin = reader.IsDBNull(4) ? null : reader.GetFieldValue<DateTimeOffset>(4);
        await reader.DisposeAsync();
        if (!AdminPassword.Verify(password, hash)) return null;

        var now = timeProvider.GetUtcNow();
        var expiresAt = now.AddHours(options.Value.SessionHours);
        var token = AdminPassword.NewToken();
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        await using var update = new NpgsqlCommand("UPDATE netcha_private.admin_users SET last_login_at = $1, updated_at = $1 WHERE id = $2", connection, transaction);
        update.Parameters.AddWithValue(now);
        update.Parameters.AddWithValue(id);
        await update.ExecuteNonQueryAsync(cancellationToken);
        await using var insert = new NpgsqlCommand("INSERT INTO netcha_private.admin_sessions (token_hash, user_id, expires_at) VALUES ($1, $2, $3)", connection, transaction);
        insert.Parameters.AddWithValue(AdminPassword.TokenHash(token));
        insert.Parameters.AddWithValue(id);
        insert.Parameters.AddWithValue(expiresAt);
        await insert.ExecuteNonQueryAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return new AdminSession(token, expiresAt, new AdminUser(id, canonicalUsername, createdAt, previousLogin));
    }

    public async Task<AdminUser?> ValidateSessionAsync(string token, CancellationToken cancellationToken)
    {
        await using var command = dataSource.CreateCommand("""
            SELECT u.id, u.username, u.created_at, u.last_login_at
            FROM netcha_private.admin_sessions s
            JOIN netcha_private.admin_users u ON u.id = s.user_id
            WHERE s.token_hash = $1 AND s.revoked_at IS NULL AND s.expires_at > NOW() AND u.is_active
            """);
        command.Parameters.AddWithValue(AdminPassword.TokenHash(token));
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken)) return null;
        return new AdminUser(reader.GetInt64(0), reader.GetString(1), reader.GetFieldValue<DateTimeOffset>(2), reader.IsDBNull(3) ? null : reader.GetFieldValue<DateTimeOffset>(3));
    }

    public async Task<bool> ChangePasswordAsync(long userId, string currentPassword, string newPassword, CancellationToken cancellationToken)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var find = new NpgsqlCommand("SELECT password_hash FROM netcha_private.admin_users WHERE id = $1 AND is_active", connection);
        find.Parameters.AddWithValue(userId);
        var existing = (string?)await find.ExecuteScalarAsync(cancellationToken);
        if (existing is null || !AdminPassword.Verify(currentPassword, existing)) return false;
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        await using var update = new NpgsqlCommand("UPDATE netcha_private.admin_users SET password_hash = $1, updated_at = NOW() WHERE id = $2", connection, transaction);
        update.Parameters.AddWithValue(AdminPassword.Hash(newPassword));
        update.Parameters.AddWithValue(userId);
        await update.ExecuteNonQueryAsync(cancellationToken);
        await using var revoke = new NpgsqlCommand("UPDATE netcha_private.admin_sessions SET revoked_at = NOW() WHERE user_id = $1 AND revoked_at IS NULL", connection, transaction);
        revoke.Parameters.AddWithValue(userId);
        await revoke.ExecuteNonQueryAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    public async Task LogoutAsync(string token, CancellationToken cancellationToken)
    {
        await using var command = dataSource.CreateCommand("UPDATE netcha_private.admin_sessions SET revoked_at = NOW() WHERE token_hash = $1 AND revoked_at IS NULL");
        command.Parameters.AddWithValue(AdminPassword.TokenHash(token));
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}

