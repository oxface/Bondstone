using Bondstone.Configuration;

namespace Bondstone.Hosting.IncomingInbox;

public static class BondstoneIncomingInboxHostingBuilderExtensions
{
    public static BondstoneBuilder UseDurableIncomingInboxWorker(
        this BondstoneBuilder builder,
        Action<DurableIncomingInboxWorkerOptions>? configureOptions = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.AddBondstoneDurableIncomingInboxWorker(configureOptions);

        return builder;
    }
}
