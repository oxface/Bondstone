namespace Bondstone.Persistence;

public interface IDurableOutboxLeaseRenewer
{
    ValueTask<bool> RenewAsync(
        Guid messageId,
        string claimedBy,
        TimeSpan leaseDuration,
        CancellationToken cancellationToken = default);
}
