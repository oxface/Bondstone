using Bondstone.Persistence;

namespace Bondstone.EntityFrameworkCore.Inbox;

public sealed class InboxMessageEntity
{
    private InboxMessageEntity(
        Guid messageId,
        string moduleName,
        string handlerIdentity,
        DateTimeOffset receivedAtUtc,
        DateTimeOffset? processedAtUtc)
    {
        MessageId = messageId;
        ModuleName = moduleName;
        HandlerIdentity = handlerIdentity;
        ReceivedAtUtc = receivedAtUtc;
        ProcessedAtUtc = processedAtUtc;
    }

    private InboxMessageEntity()
    {
    }

    public Guid MessageId { get; private set; }

    public string ModuleName { get; private set; } = string.Empty;

    public string HandlerIdentity { get; private set; } = string.Empty;

    public DateTimeOffset ReceivedAtUtc { get; private set; }

    public DateTimeOffset? ProcessedAtUtc { get; private set; }

    public static InboxMessageEntity FromRecord(DurableInboxRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);

        return new InboxMessageEntity(
            record.Key.MessageId,
            record.Key.ModuleName,
            record.Key.HandlerIdentity,
            record.ReceivedAtUtc,
            record.ProcessedAtUtc);
    }

    public DurableInboxRecord ToRecord()
    {
        return new DurableInboxRecord(
            new DurableInboxMessageKey(MessageId, ModuleName, HandlerIdentity),
            ReceivedAtUtc,
            ProcessedAtUtc);
    }
}
