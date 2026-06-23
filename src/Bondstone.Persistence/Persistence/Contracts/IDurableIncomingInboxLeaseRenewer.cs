namespace Bondstone.Persistence;

public interface IDurableIncomingInboxLeaseRenewer
{
    ValueTask<bool> RenewAsync(
        DurableIncomingInboxKey key,
        string claimedBy,
        TimeSpan leaseDuration,
        CancellationToken ct = default);
}
