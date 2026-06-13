namespace Bondstone.Transport.RabbitMq.Outbox;

public sealed class BondstoneRabbitMqReceiveQueueBuilder(
    BondstoneRabbitMqTransportBuilder transportBuilder,
    string queueName)
{
    public BondstoneRabbitMqReceiveQueueBuilder AcceptModule(
        string moduleName)
    {
        transportBuilder.AddReceiveQueueAcceptedModule(queueName, moduleName);
        return this;
    }

    public BondstoneRabbitMqReceiveQueueBuilder SubscribeEvent(
        string messageTypeName,
        string subscriberModule,
        string subscriberIdentity)
    {
        transportBuilder.AddReceiveQueueEventSubscription(
            queueName,
            messageTypeName,
            subscriberModule,
            subscriberIdentity);
        return this;
    }
}
