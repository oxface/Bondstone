using Bondstone.Messaging;
using Bondstone.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Bondstone.EntityFrameworkCore.Outbox;

public sealed class EntityFrameworkCoreDurableOutboxWriter<TDbContext>(
    TDbContext context,
    TimeProvider? timeProvider = null)
    : IDurableOutboxWriter
    where TDbContext : DbContext
{
    private readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;

    public ValueTask WriteAsync(
        DurableMessageEnvelope envelope,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(envelope);

        var record = new DurableOutboxRecord(
            envelope,
            _timeProvider.GetUtcNow());

        context.Set<OutboxMessageEntity>().Add(OutboxMessageEntity.FromRecord(record));
        return ValueTask.CompletedTask;
    }
}
