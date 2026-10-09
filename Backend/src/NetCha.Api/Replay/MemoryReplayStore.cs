using System.Collections.Concurrent;

namespace NetCha.Api.Replay;

public sealed class MemoryReplayStore(TimeProvider timeProvider) : IReplayStore
{
    private readonly ConcurrentDictionary<string, DateTimeOffset> _consumed = new();
    private int _operations;

    public Task<bool> TryConsumeAsync(string replayKey, DateTimeOffset expiresAt, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (Interlocked.Increment(ref _operations) % 128 == 0)
        {
            var now = timeProvider.GetUtcNow();
            foreach (var item in _consumed.Where(x => x.Value <= now))
                _consumed.TryRemove(item.Key, out _);
        }

        return Task.FromResult(_consumed.TryAdd(replayKey, expiresAt));
    }
}
