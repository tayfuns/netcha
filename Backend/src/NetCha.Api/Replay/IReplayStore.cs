namespace NetCha.Api.Replay;

public interface IReplayStore
{
    Task<bool> TryConsumeAsync(string replayKey, DateTimeOffset expiresAt, CancellationToken cancellationToken);
}
