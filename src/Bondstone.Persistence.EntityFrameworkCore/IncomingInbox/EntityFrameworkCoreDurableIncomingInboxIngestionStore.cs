using Bondstone.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Bondstone.Persistence.EntityFrameworkCore.IncomingInbox;

public sealed class EntityFrameworkCoreDurableIncomingInboxIngestionStore<TDbContext>(
    TDbContext context)
    : IDurableIncomingInboxIngestionStore
    where TDbContext : DbContext
{
    public async ValueTask<DurableIncomingInboxIngestionResult> IngestAsync(
        DurableIncomingInboxRecord record,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(record);
        ValidateIncomingInboxMapping();

        IncomingInboxMessageEntity? existing = await FindAsync(record.Key, ct);
        if (existing is not null)
        {
            return new DurableIncomingInboxIngestionResult(
                DurableIncomingInboxIngestionStatus.AlreadyIngested,
                existing.ToRecord());
        }

        if (record.State.Status != DurableIncomingInboxStatus.Pending)
        {
            throw new ArgumentException(
                "Durable incoming inbox ingestion records must start in pending state.",
                nameof(record));
        }

        context.Set<IncomingInboxMessageEntity>().Add(IncomingInboxMessageEntity.FromRecord(record));

        return new DurableIncomingInboxIngestionResult(
            DurableIncomingInboxIngestionStatus.Ingested,
            record);
    }

    private ValueTask<IncomingInboxMessageEntity?> FindAsync(
        DurableIncomingInboxKey key,
        CancellationToken ct)
    {
        return context
            .Set<IncomingInboxMessageEntity>()
            .FindAsync(
                [key.ReceiverModule, key.MessageId, key.HandlerIdentity],
                ct);
    }

    private void ValidateIncomingInboxMapping()
    {
        if (context.Model.FindEntityType(typeof(IncomingInboxMessageEntity)) is not null)
        {
            return;
        }

        throw new InvalidOperationException(
            $"DbContext '{context.GetType().FullName}' is missing the Bondstone EF Core incoming inbox mapping. Map the durable incoming inbox explicitly with ApplyBondstoneIncomingInbox().");
    }
}
