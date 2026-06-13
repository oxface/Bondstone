using Bondstone.Transport.ServiceBus.Inbox;

namespace Bondstone.Transport.ServiceBus.Outbox;

public sealed class BondstoneServiceBusReceiveSourceBuilder(
    BondstoneServiceBusTransportBuilder transportBuilder,
    ServiceBusReceiveSource source)
{
    public BondstoneServiceBusReceiveSourceBuilder AcceptModule(
        string moduleName)
    {
        transportBuilder.AddReceiveSourceAcceptedModule(source, moduleName);
        return this;
    }

    public BondstoneServiceBusReceiveSourceBuilder SubscribeEvent(
        string messageTypeName,
        string subscriberModule,
        string subscriberIdentity)
    {
        transportBuilder.AddReceiveSourceEventSubscription(
            source,
            messageTypeName,
            subscriberModule,
            subscriberIdentity);
        return this;
    }
}
