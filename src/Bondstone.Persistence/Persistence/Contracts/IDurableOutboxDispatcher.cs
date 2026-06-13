namespace Bondstone.Persistence;

public interface IDurableOutboxDispatcher
{
    ValueTask<DurableOutboxDispatchResult> DispatchAsync(
        string claimedBy,
        TimeSpan leaseDuration,
        int maxCount = 100,
        CancellationToken ct = default);
}
