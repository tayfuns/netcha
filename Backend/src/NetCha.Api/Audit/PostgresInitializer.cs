using Microsoft.Extensions.Options;
using NetCha.Api.Configuration;
using Npgsql;

namespace NetCha.Api.Audit;

public sealed class PostgresInitializer(
    NpgsqlDataSource dataSource,
    IOptions<PersistenceOptions> options,
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
            """;
        try
        {
            await using var command = dataSource.CreateCommand(sql);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (Exception exception) when (options.Value.FallbackToMemory)
        {
            logger.LogError(exception, "PostgreSQL initialization failed. Runtime operations may fail; remove the connection string to use memory persistence.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
