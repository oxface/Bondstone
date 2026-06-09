using Bondstone.Configuration;

namespace Bondstone.Transport.Rebus.Inbox;

public static class BondstoneRebusModuleCommandReceiveBuilderExtensions
{
    public static BondstoneBuilder UseRebusModuleCommandReceivePipeline(
        this BondstoneBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.AddBondstoneRebusModuleCommandReceivePipeline();

        return builder;
    }

    public static BondstoneBuilder UseRebusModuleEventReceiveTopology(
        this BondstoneBuilder builder,
        IReadOnlyCollection<RebusEventSubscriptionBinding> subscriptions)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(subscriptions);

        builder.Services.AddBondstoneRebusModuleEventReceiveTopology(subscriptions);

        return builder;
    }
}
