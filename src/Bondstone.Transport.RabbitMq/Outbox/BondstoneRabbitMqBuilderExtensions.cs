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
        RabbitMqEnvelopeDestinationResolver destinationResolver =
            rabbitMq.DestinationResolver;

        builder.Services.AddBondstoneRabbitMqEnvelopeDispatcher(
            destinationResolver,
            rabbitMq.ReceiveTopology,
            rabbitMq.ReceiveWorkerRegistration);
        if (destinationResolver.HasOutboundResolver)
        {
            builder.Outbox.MarkTransport("RabbitMq");
        }

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
        RabbitMqEnvelopeDestinationResolver destinationResolver =
            rabbitMq.DestinationResolver;

        outbox.Services.AddBondstoneRabbitMqEnvelopeDispatcher(
            destinationResolver,
            rabbitMq.ReceiveTopology,
            rabbitMq.ReceiveWorkerRegistration);
        if (destinationResolver.HasOutboundResolver)
        {
            outbox.MarkTransport("RabbitMq");
        }

        return outbox;
    }
}
