using Microsoft.Extensions.Options;
using NetCha.Api.Configuration;
using Npgsql;

namespace NetCha.Api.Audit;

public sealed class ResilientAuditRepository(
    PostgresAuditRepository postgres,
    MemoryAuditRepository memory,
    IOptions<PersistenceOptions> options,
    ILogger<ResilientAuditRepository> logger) : IAuditRepository
{
    public Task RecordAsync(bool succeeded, string? error, string? clientIp, string? userAgent, CancellationToken cancellationToken) =>
        ExecuteAsync(
            () => postgres.RecordAsync(succeeded, error, clientIp, userAgent, cancellationToken),
            () => memory.RecordAsync(succeeded, error, clientIp, userAgent, cancellationToken));

    public Task<IReadOnlyList<AuditEntry>> ListAsync(int limit, CancellationToken cancellationToken) =>
        ExecuteAsync(
            () => postgres.ListAsync(limit, cancellationToken),
            () => memory.ListAsync(limit, cancellationToken));

    public Task<AuditStats> GetStatsAsync(CancellationToken cancellationToken) =>
        ExecuteAsync(
            () => postgres.GetStatsAsync(cancellationToken),
            () => memory.GetStatsAsync(cancellationToken));

    private async Task ExecuteAsync(Func<Task> primary, Func<Task> fallback)
    {
        try { await primary(); }
        catch (Exception exception) when (ShouldFallback(exception))
        {
            logger.LogWarning(exception, "PostgreSQL unavailable; audit persistence is using this instance's memory fallback.");
            await fallback();
        }
    }

    private async Task<T> ExecuteAsync<T>(Func<Task<T>> primary, Func<Task<T>> fallback)
    {
        try { return await primary(); }
        catch (Exception exception) when (ShouldFallback(exception))
        {
            logger.LogWarning(exception, "PostgreSQL unavailable; audit reads are using this instance's memory fallback.");
            return await fallback();
        }
    }

    private bool ShouldFallback(Exception exception) => options.Value.FallbackToMemory &&
        exception is NpgsqlException or TimeoutException;
}
