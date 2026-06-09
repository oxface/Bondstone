namespace Bondstone.Transport.ServiceBus.Outbox;

public sealed class BondstoneServiceBusEventRouteBuilder(
    BondstoneServiceBusTransportBuilder transportBuilder,
    string messageTypeName)
{
    public BondstoneServiceBusTransportBuilder ToTopic(
        string topicName)
    {
        transportBuilder.SetEventTopicName(messageTypeName, topicName);
        return transportBuilder;
    }
}
