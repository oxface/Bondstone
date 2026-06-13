using Bondstone.Persistence;

namespace Bondstone.Modules;

public sealed class ModuleEventSubscriberReceiveContext
{
    public ModuleEventSubscriberReceiveContext(
        DurableInboxRecord inboxRecord,
        Guid? durableOperationId = null)
    {
        InboxRecord = inboxRecord ?? throw new ArgumentNullException(nameof(inboxRecord));

        if (durableOperationId == Guid.Empty)
        {
            throw new ArgumentException(
                "Durable operation id must not be empty.",
                nameof(durableOperationId));
        }

        DurableOperationId = durableOperationId;
    }

    public DurableInboxRecord InboxRecord { get; }

    public Guid? DurableOperationId { get; }
}
