using Bondstone.Configuration;

namespace Bondstone.Transport.RabbitMq.Outbox;

public static class BondstoneRabbitMqBuilderExtensions
{
    public static BondstoneBuilder UseRabbitMqTransport(
        this BondstoneBuilder builder,
        Action<BondstoneRabbitMqTransportBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configure);

        var rabbitMq = new BondstoneRabbitMqTransportBuilder();
        configure(rabbitMq);

        builder.Services.AddBondstoneRabbitMqOutboxTransport(
            rabbitMq.CommandRoutingTopology,
            rabbitMq.EventRoutingTopology,
            rabbitMq.ReceiveTopology,
            rabbitMq.ReceiveWorkerRegistration);
        var validator = new RabbitMqTopologyConfigurationValidator(
            rabbitMq.CommandRoutingTopology,
            rabbitMq.EventRoutingTopology,
            rabbitMq.ReceiveTopology);
        builder.AddTransportTopologyDiagnosticSource(validator);
        builder.AddConfigurationValidator(validator);
        builder.Outbox.MarkTransport("RabbitMq");

        return builder;
    }

    public static BondstoneOutboxBuilder UseRabbitMqTransport(
        this BondstoneOutboxBuilder outbox,
        Action<BondstoneRabbitMqTransportBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(outbox);
        ArgumentNullException.ThrowIfNull(configure);

        var rabbitMq = new BondstoneRabbitMqTransportBuilder();
        configure(rabbitMq);

        outbox.Services.AddBondstoneRabbitMqOutboxTransport(
            rabbitMq.CommandRoutingTopology,
            rabbitMq.EventRoutingTopology,
            rabbitMq.ReceiveTopology,
            rabbitMq.ReceiveWorkerRegistration);
        outbox.MarkTransport("RabbitMq");

        return outbox;
    }
}
