namespace Bondstone.Persistence;

public interface IDurableInboxStore
{
    ValueTask<DurableInboxRecord?> GetAsync(
        DurableInboxMessageKey key,
        CancellationToken cancellationToken = default);

    ValueTask AddAsync(
        DurableInboxRecord record,
        CancellationToken cancellationToken = default);

    ValueTask MarkProcessedAsync(
        DurableInboxMessageKey key,
        DateTimeOffset processedAtUtc,
        CancellationToken cancellationToken = default);
}
