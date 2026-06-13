namespace Bondstone.Persistence;

public interface IDurableOutboxClaimer
{
    ValueTask<IReadOnlyList<DurableOutboxRecord>> ClaimAsync(
        string claimedBy,
        TimeSpan leaseDuration,
        int maxCount = 100,
        CancellationToken ct = default);
}
