namespace Bondstone.Persistence;

public interface IDurableIncomingInboxDispatcher
{
    ValueTask<DurableIncomingInboxProcessingResult> ProcessAsync(
        string claimedBy,
        TimeSpan leaseDuration,
        int maxCount = 100,
        CancellationToken ct = default);
}
