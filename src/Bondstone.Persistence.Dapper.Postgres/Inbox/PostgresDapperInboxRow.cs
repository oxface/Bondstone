using Bondstone.Persistence;

namespace Bondstone.Persistence.Dapper.Postgres.Inbox;

internal class PostgresDapperInboxRow
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
