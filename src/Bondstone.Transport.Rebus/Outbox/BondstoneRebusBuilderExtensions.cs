using Bondstone.Configuration;

namespace Bondstone.Transport.Rebus.Outbox;

public static class BondstoneRebusBuilderExtensions
{
    public static BondstoneBuilder UseRebusTransport(
        this BondstoneBuilder builder,
        Action<BondstoneRebusTransportBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configure);

        var rebus = new BondstoneRebusTransportBuilder();
        configure(rebus);

        builder.Services.AddBondstoneRebusOutboxTransport(
            rebus.DestinationAddressesByTargetModule);
        builder.Outbox.MarkTransport("Rebus");

        return builder;
    }

    public static BondstoneOutboxBuilder UseRebusTransport(
        this BondstoneOutboxBuilder outbox,
        Action<BondstoneRebusTransportBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(outbox);
        ArgumentNullException.ThrowIfNull(configure);

        var rebus = new BondstoneRebusTransportBuilder();
        configure(rebus);

        outbox.Services.AddBondstoneRebusOutboxTransport(
            rebus.DestinationAddressesByTargetModule);
        outbox.MarkTransport("Rebus");

        return outbox;
    }

    public static BondstoneOutboxBuilder UseRebusTransport(
        this BondstoneOutboxBuilder outbox,
        IReadOnlyDictionary<string, string> destinationAddressesByTargetModule)
    {
        ArgumentNullException.ThrowIfNull(outbox);

        outbox.Services.AddBondstoneRebusOutboxTransport(destinationAddressesByTargetModule);
        outbox.MarkTransport("Rebus");

        return outbox;
    }
}
