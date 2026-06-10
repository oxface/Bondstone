using Bondstone.Configuration;

namespace Bondstone.Transport.ServiceBus.Outbox;

public static class BondstoneServiceBusBuilderExtensions
{
    public static BondstoneBuilder UseServiceBusTransport(
        this BondstoneBuilder builder,
        Action<BondstoneServiceBusTransportBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configure);

        var serviceBus = new BondstoneServiceBusTransportBuilder();
        configure(serviceBus);

        builder.Services.AddBondstoneServiceBusOutboxTransport(
            serviceBus.CommandDestinationTopology,
            serviceBus.EventDestinationTopology,
            serviceBus.ReceiveTopology,
            serviceBus.ReceiveWorkerRegistration);
        builder.AddConfigurationValidator(
            new ServiceBusTopologyConfigurationValidator(
                serviceBus.CommandDestinationTopology,
                serviceBus.EventDestinationTopology,
                serviceBus.ReceiveTopology));
        builder.Outbox.MarkTransport("ServiceBus");

        return builder;
    }

    public static BondstoneOutboxBuilder UseServiceBusTransport(
        this BondstoneOutboxBuilder outbox,
        Action<BondstoneServiceBusTransportBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(outbox);
        ArgumentNullException.ThrowIfNull(configure);

        var serviceBus = new BondstoneServiceBusTransportBuilder();
        configure(serviceBus);

        outbox.Services.AddBondstoneServiceBusOutboxTransport(
            serviceBus.CommandDestinationTopology,
            serviceBus.EventDestinationTopology,
            serviceBus.ReceiveTopology,
            serviceBus.ReceiveWorkerRegistration);
        outbox.MarkTransport("ServiceBus");

        return outbox;
    }
}
