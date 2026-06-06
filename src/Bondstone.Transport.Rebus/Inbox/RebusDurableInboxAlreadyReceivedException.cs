using Bondstone.Persistence;

namespace Bondstone.Transport.Rebus.Inbox;

public sealed class RebusDurableInboxAlreadyReceivedException : InvalidOperationException
{
    public RebusDurableInboxAlreadyReceivedException(DurableInboxHandleResult result)
        : base(CreateMessage(result))
    {
        Result = result;
    }

    public DurableInboxHandleResult Result { get; }

    private static string CreateMessage(DurableInboxHandleResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        if (result.Status != DurableInboxHandleStatus.AlreadyReceived)
        {
            throw new ArgumentException(
                "Inbox handle result must be AlreadyReceived.",
                nameof(result));
        }

        DurableInboxMessageKey key = result.Record.Key;
        return "Rebus durable inbox message was already received but not processed. "
            + $"MessageId='{key.MessageId:D}', Module='{key.ModuleName}', Handler='{key.HandlerIdentity}'.";
    }
}
