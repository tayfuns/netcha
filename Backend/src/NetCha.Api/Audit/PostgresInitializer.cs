using Microsoft.Extensions.Options;
using NetCha.Api.Admin;
using NetCha.Api.Configuration;
using Npgsql;

namespace NetCha.Api.Audit;

public sealed class PostgresInitializer(
    NpgsqlDataSource dataSource,
    IOptions<PersistenceOptions> options,
    IOptions<AdminOptions> adminOptions,
    IAdminAuthStore adminAuthStore,
    ILogger<PostgresInitializer> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!options.Value.AutoMigrate) return;
        const string sql = """
            CREATE TABLE IF NOT EXISTS captcha_audit (
                id BIGSERIAL PRIMARY KEY,
                occurred_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                succeeded BOOLEAN NOT NULL,
                error_code TEXT NULL,
                client_ip TEXT NULL,
                user_agent TEXT NULL
            );
            CREATE INDEX IF NOT EXISTS ix_captcha_audit_occurred_at ON captcha_audit (occurred_at DESC);
            CREATE SCHEMA IF NOT EXISTS netcha_private;
            REVOKE ALL ON SCHEMA netcha_private FROM PUBLIC;
            CREATE TABLE IF NOT EXISTS netcha_private.admin_users (
                id BIGSERIAL PRIMARY KEY,
                username TEXT NOT NULL UNIQUE,
                password_hash TEXT NOT NULL,
                is_active BOOLEAN NOT NULL DEFAULT TRUE,
                created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                last_login_at TIMESTAMPTZ NULL
            );
            CREATE TABLE IF NOT EXISTS netcha_private.admin_sessions (
                token_hash CHAR(64) PRIMARY KEY,
                user_id BIGINT NOT NULL REFERENCES netcha_private.admin_users(id) ON DELETE CASCADE,
                created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                expires_at TIMESTAMPTZ NOT NULL,
                revoked_at TIMESTAMPTZ NULL
            );
            CREATE INDEX IF NOT EXISTS ix_admin_sessions_user_id ON netcha_private.admin_sessions (user_id);
            CREATE INDEX IF NOT EXISTS ix_admin_sessions_expires_at ON netcha_private.admin_sessions (expires_at);
            """;
        try
        {
            await using var command = dataSource.CreateCommand(sql);
            await command.ExecuteNonQueryAsync(cancellationToken);
            await adminAuthStore.InitializeAsync(adminOptions.Value.ApiKey, cancellationToken);
        }
        catch (Exception exception) when (options.Value.FallbackToMemory)
        {
            logger.LogError(exception, "PostgreSQL initialization failed. Runtime operations may fail; remove the connection string to use memory persistence.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
