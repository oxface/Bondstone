using Bondstone.Configuration;

namespace Bondstone.Transport.Local.Outbox;

public static class BondstoneLocalBuilderExtensions
{
    public static BondstoneBuilder UseLocalTransport(
        this BondstoneBuilder builder,
        Action<BondstoneLocalTransportBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configure);

        var local = new BondstoneLocalTransportBuilder();
        configure(local);

        builder.Services.AddBondstoneLocalOutboxTransport(local.Topology);
        builder.AddTransportTopologyDiagnosticSource(
            new LocalTransportTopologyDiagnosticSource(local.Topology));
        builder.Outbox.MarkTransport("Local");

        return builder;
    }

    public static BondstoneOutboxBuilder UseLocalTransport(
        this BondstoneOutboxBuilder outbox,
        Action<BondstoneLocalTransportBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(outbox);
        ArgumentNullException.ThrowIfNull(configure);

        var local = new BondstoneLocalTransportBuilder();
        configure(local);

        outbox.Services.AddBondstoneLocalOutboxTransport(local.Topology);
        outbox.MarkTransport("Local");

        return outbox;
    }
}
