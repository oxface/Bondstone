namespace Bondstone.Transport.Local.Outbox;

public sealed class BondstoneLocalQueueBuilder(
    BondstoneLocalTransportBuilder transportBuilder,
    string queueName)
{
    public BondstoneLocalQueueBuilder AcceptModule(
        string moduleName)
    {
        transportBuilder.AddAcceptedModule(queueName, moduleName);
        return this;
    }

    public BondstoneLocalQueueBuilder SubscribeEvent(
        string messageTypeName,
        string subscriberModule,
        string subscriberIdentity)
    {
        transportBuilder.AddEventSubscription(
            queueName,
            messageTypeName,
            subscriberModule,
            subscriberIdentity);
        return this;
    }
}
