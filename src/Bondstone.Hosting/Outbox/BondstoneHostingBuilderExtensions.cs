using Bondstone.Configuration;

namespace Bondstone.Hosting.Outbox;

public static class BondstoneHostingBuilderExtensions
{
    public static BondstoneOutboxBuilder UseDurableDispatcher(
        this BondstoneOutboxBuilder outbox)
    {
        ArgumentNullException.ThrowIfNull(outbox);

        outbox.Services.AddBondstoneDurableOutboxDispatcher();
        outbox.MarkDispatcher("Default durable outbox dispatcher");

        return outbox;
    }

    public static BondstoneOutboxBuilder UseWorker(
        this BondstoneOutboxBuilder outbox,
        Action<DurableOutboxWorkerOptions>? configureOptions = null)
    {
        ArgumentNullException.ThrowIfNull(outbox);

        outbox.Services.AddBondstoneDurableOutboxWorker(configureOptions);
        outbox.MarkDispatcher("Default durable outbox dispatcher");
        outbox.MarkWorker("Durable outbox worker");

        return outbox;
    }
}
