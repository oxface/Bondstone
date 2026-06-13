namespace Bondstone.Transport.ServiceBus.Outbox;

public sealed class BondstoneServiceBusModuleRouteBuilder(
    BondstoneServiceBusTransportBuilder transportBuilder,
    string targetModule)
{
    public BondstoneServiceBusTransportBuilder ToQueue(
        string queueName)
    {
        transportBuilder.SetModuleQueueName(targetModule, queueName);
        return transportBuilder;
    }
}
