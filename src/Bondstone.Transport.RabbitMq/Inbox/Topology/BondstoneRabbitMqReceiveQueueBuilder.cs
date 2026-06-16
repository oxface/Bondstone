namespace Bondstone.Transport.RabbitMq.Outbox;

public sealed class BondstoneRabbitMqReceiveQueueBuilder
{
    private readonly BondstoneRabbitMqTransportBuilder _transportBuilder;
    private readonly string _queueName;

    internal BondstoneRabbitMqReceiveQueueBuilder(
        BondstoneRabbitMqTransportBuilder transportBuilder,
        string queueName)
    {
        _transportBuilder = transportBuilder;
        _queueName = queueName;
    }

    public BondstoneRabbitMqReceiveQueueBuilder AcceptModule(
        string moduleName)
    {
        _transportBuilder.AddReceiveQueueAcceptedModule(_queueName, moduleName);
        return this;
    }

    public BondstoneRabbitMqReceiveQueueBuilder SubscribeEvent(
        string messageTypeName,
        string subscriberModule,
        string subscriberIdentity)
    {
        _transportBuilder.AddReceiveQueueEventSubscription(
            _queueName,
            messageTypeName,
            subscriberModule,
            subscriberIdentity);
        return this;
    }
}
