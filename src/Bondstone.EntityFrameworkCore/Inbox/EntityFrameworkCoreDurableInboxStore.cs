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
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(key);

        InboxMessageEntity? entity = await FindAsync(key, ct);
        return entity?.ToRecord();
    }

    public ValueTask AddAsync(
        DurableInboxRecord record,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(record);

        context.Set<InboxMessageEntity>().Add(InboxMessageEntity.FromRecord(record));
        return ValueTask.CompletedTask;
    }

    public async ValueTask MarkProcessedAsync(
        DurableInboxMessageKey key,
        DateTimeOffset processedAtUtc,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(key);

        InboxMessageEntity entity = await FindAsync(key, ct)
            ?? throw new InvalidOperationException("Inbox message was not found.");

        entity.MarkProcessed(processedAtUtc);
    }

    private ValueTask<InboxMessageEntity?> FindAsync(
        DurableInboxMessageKey key,
        CancellationToken ct)
    {
        return context
            .Set<InboxMessageEntity>()
            .FindAsync(
                [key.ModuleName, key.MessageId, key.HandlerIdentity],
                ct);
    }
}
