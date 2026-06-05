using Bondstone.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Bondstone.EntityFrameworkCore.Inbox;

public sealed class EntityFrameworkCoreDurableInboxStore<TDbContext>(
    TDbContext context)
    : IDurableInboxStore
    where TDbContext : DbContext
{
    public async ValueTask<DurableInboxRecord?> GetAsync(
        DurableInboxMessageKey key,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(key);

        InboxMessageEntity? entity = await FindAsync(key, cancellationToken);
        return entity?.ToRecord();
    }

    public ValueTask AddAsync(
        DurableInboxRecord record,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(record);

        context.Set<InboxMessageEntity>().Add(InboxMessageEntity.FromRecord(record));
        return ValueTask.CompletedTask;
    }

    public async ValueTask MarkProcessedAsync(
        DurableInboxMessageKey key,
        DateTimeOffset processedAtUtc,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(key);

        InboxMessageEntity entity = await FindAsync(key, cancellationToken)
            ?? throw new InvalidOperationException("Inbox message was not found.");

        entity.MarkProcessed(processedAtUtc);
    }

    private ValueTask<InboxMessageEntity?> FindAsync(
        DurableInboxMessageKey key,
        CancellationToken cancellationToken)
    {
        return context
            .Set<InboxMessageEntity>()
            .FindAsync(
                [key.ModuleName, key.MessageId, key.HandlerIdentity],
                cancellationToken);
    }
}
