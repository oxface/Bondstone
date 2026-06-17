namespace Bondstone.Persistence;

public interface IDurableIncomingInboxClaimer
{
    ValueTask<IReadOnlyList<DurableIncomingInboxRecord>> ClaimAsync(
        string claimedBy,
        TimeSpan leaseDuration,
        int maxCount = 100,
        CancellationToken ct = default);
}
