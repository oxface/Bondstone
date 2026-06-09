namespace Bondstone.Transport.RabbitMq.Outbox;

public sealed class BondstoneRabbitMqEventRouteBuilder(
    BondstoneRabbitMqTransportBuilder transportBuilder,
    string messageTypeName)
{
    public BondstoneRabbitMqTransportBuilder ToRoutingKey(
        string routingKey)
    {
        transportBuilder.SetEventRoutingKey(messageTypeName, routingKey);
        return transportBuilder;
    }
}
