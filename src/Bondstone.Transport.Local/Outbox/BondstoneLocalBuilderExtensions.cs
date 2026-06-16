using Bondstone.Configuration;

namespace Bondstone.Transport.Local.Outbox;

/// <summary>
/// Adds local in-process durable message transport routing to Bondstone setup.
/// </summary>
public static class BondstoneLocalBuilderExtensions
{
    /// <summary>
    /// Configures local in-process transport for a Bondstone host.
    /// </summary>
    /// <param name="builder">The Bondstone host builder.</param>
    /// <param name="configure">Configures local queues, module routes, and event routes.</param>
    /// <returns>The same Bondstone builder for chained setup.</returns>
    public static BondstoneBuilder UseLocalTransport(
        this BondstoneBuilder builder,
        Action<BondstoneLocalTransportBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configure);

        var local = new BondstoneLocalTransportBuilder();
        configure(local);

        builder.Services.AddBondstoneLocalEnvelopeDispatcher(local.Topology);
        builder.AddTransportTopologyDiagnosticSource(
            new LocalTransportTopologyDiagnosticSource(local.Topology));
        builder.Outbox.MarkTransport("Local");

        return builder;
    }

    /// <summary>
    /// Configures local in-process transport for the durable outbox only.
    /// </summary>
    /// <param name="outbox">The Bondstone durable outbox builder.</param>
    /// <param name="configure">Configures local queues, module routes, and event routes.</param>
    /// <returns>The same outbox builder for chained setup.</returns>
    public static BondstoneOutboxBuilder UseLocalTransport(
        this BondstoneOutboxBuilder outbox,
        Action<BondstoneLocalTransportBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(outbox);
        ArgumentNullException.ThrowIfNull(configure);

        var local = new BondstoneLocalTransportBuilder();
        configure(local);

        outbox.Services.AddBondstoneLocalEnvelopeDispatcher(local.Topology);
        outbox.MarkTransport("Local");

        return outbox;
    }
}
