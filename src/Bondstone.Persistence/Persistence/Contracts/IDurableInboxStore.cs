namespace Bondstone.Persistence;

public interface IDurableInboxStore
{
    ValueTask<DurableInboxRecord?> GetAsync(
        DurableInboxMessageKey key,
        CancellationToken ct = default);

    ValueTask AddAsync(
        DurableInboxRecord record,
        CancellationToken ct = default);

    ValueTask MarkProcessedAsync(
        DurableInboxMessageKey key,
        DateTimeOffset processedAtUtc,
        CancellationToken ct = default);
}
