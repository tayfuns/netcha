using System.Collections.Concurrent;

namespace NetCha.Api.Audit;

public sealed class MemoryAuditRepository(TimeProvider timeProvider) : IAuditRepository
{
    private readonly ConcurrentQueue<AuditEntry> _entries = new();
    private long _id;

    public Task RecordAsync(bool succeeded, string? error, string? clientIp, string? userAgent, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _entries.Enqueue(new AuditEntry(Interlocked.Increment(ref _id), timeProvider.GetUtcNow(), succeeded, error, clientIp, userAgent));
        while (_entries.Count > 10_000) _entries.TryDequeue(out _);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<AuditEntry>> ListAsync(int limit, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        IReadOnlyList<AuditEntry> result = _entries.Reverse().Take(limit).ToArray();
        return Task.FromResult(result);
    }

    public Task<AuditStats> GetStatsAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var snapshot = _entries.ToArray();
        return Task.FromResult(new AuditStats(snapshot.LongLength, snapshot.LongCount(x => x.Succeeded), snapshot.LongCount(x => !x.Succeeded), snapshot.LastOrDefault()?.OccurredAt));
    }
}
