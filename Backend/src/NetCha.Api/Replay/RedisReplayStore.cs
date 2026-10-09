using Microsoft.Extensions.Options;
using NetCha.Api.Configuration;
using StackExchange.Redis;

namespace NetCha.Api.Replay;

public sealed class RedisReplayStore(
    IConnectionMultiplexer redis,
    IOptions<RedisOptions> options,
    MemoryReplayStore fallback,
    TimeProvider timeProvider,
    ILogger<RedisReplayStore> logger) : IReplayStore
{
    public async Task<bool> TryConsumeAsync(string replayKey, DateTimeOffset expiresAt, CancellationToken cancellationToken)
    {
        var ttl = expiresAt - timeProvider.GetUtcNow();
        if (ttl <= TimeSpan.Zero) return false;
        try
        {
            return await redis.GetDatabase().StringSetAsync(
                $"{options.Value.KeyPrefix}:replay:{replayKey}", "1", ttl, When.NotExists);
        }
        catch (RedisException exception) when (options.Value.FallbackToMemory)
        {
            logger.LogWarning(exception, "Redis unavailable; replay protection is using this instance's memory fallback.");
            return await fallback.TryConsumeAsync(replayKey, expiresAt, cancellationToken);
        }
    }
}
