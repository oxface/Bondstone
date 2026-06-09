namespace Bondstone.Persistence;

public sealed class DurableInboxAlreadyReceivedException : InvalidOperationException
{
    public DurableInboxAlreadyReceivedException(DurableInboxHandleResult result)
        : base("Durable inbox message was already received but has not been processed.")
    {
        ArgumentNullException.ThrowIfNull(result);
        if (result.Status != DurableInboxHandleStatus.AlreadyReceived)
        {
            throw new ArgumentException(
                "Inbox result must have AlreadyReceived status.",
                nameof(result));
        }

        Result = result;
    }

    public DurableInboxHandleResult Result { get; }
}
