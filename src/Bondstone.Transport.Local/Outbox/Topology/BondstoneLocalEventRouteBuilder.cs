namespace Bondstone.Transport.Local.Outbox;

public sealed class BondstoneLocalEventRouteBuilder(
    BondstoneLocalTransportBuilder transportBuilder,
    string messageTypeName)
{
    public BondstoneLocalTransportBuilder ToQueue(
        string queueName)
    {
        transportBuilder.SetEventQueueName(messageTypeName, queueName);
        return transportBuilder;
    }
}
