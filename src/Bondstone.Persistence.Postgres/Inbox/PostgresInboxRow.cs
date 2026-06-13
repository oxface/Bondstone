using Bondstone.Persistence;

namespace Bondstone.Persistence.Postgres.Inbox;

internal class PostgresInboxRow
{
    public Guid MessageId { get; init; }

    public string ModuleName { get; init; } = string.Empty;

    public string HandlerIdentity { get; init; } = string.Empty;

    public DateTimeOffset ReceivedAtUtc { get; init; }

    public DateTimeOffset? ProcessedAtUtc { get; init; }

    public DurableInboxRecord ToRecord()
    {
        return new DurableInboxRecord(
            new DurableInboxMessageKey(MessageId, ModuleName, HandlerIdentity),
            ReceivedAtUtc,
            ProcessedAtUtc);
    }
}
