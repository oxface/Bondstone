namespace Bondstone.Transport.ServiceBus.Outbox;

public sealed class BondstoneServiceBusEventRouteBuilder(
    BondstoneServiceBusTransportBuilder transportBuilder,
    string messageTypeName)
{
    public BondstoneServiceBusTransportBuilder ToTopic(
        string topicName)
    {
        transportBuilder.SetEventDestination(
            messageTypeName,
            ServiceBusEventDestinationKind.Topic,
            topicName);
        return transportBuilder;
    }

    public BondstoneServiceBusTransportBuilder ToQueue(
        string queueName)
    {
        transportBuilder.SetEventDestination(
            messageTypeName,
            ServiceBusEventDestinationKind.Queue,
            queueName);
        return transportBuilder;
    }
}
