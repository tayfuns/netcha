using NetCha.Api.Replay;

namespace NetCha.Api.Tests;

public sealed class MemoryReplayStoreTests
{
    [Fact]
    public async Task A_challenge_can_only_be_consumed_once()
    {
        var store = new MemoryReplayStore(TimeProvider.System);
        var expiry = DateTimeOffset.UtcNow.AddMinutes(1);

        Assert.True(await store.TryConsumeAsync("same-key", expiry, default));
        Assert.False(await store.TryConsumeAsync("same-key", expiry, default));
    }
}
