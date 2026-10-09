namespace NetCha.Api.Audit;

public sealed record AuditEntry(
    long Id,
    DateTimeOffset OccurredAt,
    bool Succeeded,
    string? Error,
    string? ClientIp,
    string? UserAgent);

public sealed record AuditStats(long Total, long Succeeded, long Failed, DateTimeOffset? LastAttemptAt);

public interface IAuditRepository
{
    Task RecordAsync(bool succeeded, string? error, string? clientIp, string? userAgent, CancellationToken cancellationToken);
    Task<IReadOnlyList<AuditEntry>> ListAsync(int limit, CancellationToken cancellationToken);
    Task<AuditStats> GetStatsAsync(CancellationToken cancellationToken);
}
