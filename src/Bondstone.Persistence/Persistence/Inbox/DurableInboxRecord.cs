namespace Bondstone.Persistence;

public sealed record DurableInboxRecord
{
    public DurableInboxRecord(
        DurableInboxMessageKey key,
        DateTimeOffset receivedAtUtc,
        DateTimeOffset? processedAtUtc = null)
    {
        ArgumentNullException.ThrowIfNull(key);
        ValidateUtcTimestamp(receivedAtUtc, nameof(receivedAtUtc), "Received timestamp");

        if (processedAtUtc is not null)
        {
            ValidateUtcTimestamp(processedAtUtc.Value, nameof(processedAtUtc), "Processed timestamp");

            if (processedAtUtc < receivedAtUtc)
            {
                throw new ArgumentException(
                    "Processed timestamp must not be earlier than received timestamp.",
                    nameof(processedAtUtc));
            }
        }

        Key = key;
        ReceivedAtUtc = receivedAtUtc;
        ProcessedAtUtc = processedAtUtc;
    }

    public DurableInboxMessageKey Key { get; }

    public DateTimeOffset ReceivedAtUtc { get; }

    public DateTimeOffset? ProcessedAtUtc { get; }

    public bool IsProcessed => ProcessedAtUtc is not null;

    public DurableInboxRecord MarkProcessed(DateTimeOffset processedAtUtc)
    {
        return new DurableInboxRecord(Key, ReceivedAtUtc, processedAtUtc);
    }

    private static void ValidateUtcTimestamp(
        DateTimeOffset value,
        string parameterName,
        string valueName)
    {
        if (value == default)
        {
            throw new ArgumentException($"{valueName} must not be the default value.", parameterName);
        }

        if (value.Offset != TimeSpan.Zero)
        {
            throw new ArgumentException($"{valueName} must use UTC offset.", parameterName);
        }
    }
}
