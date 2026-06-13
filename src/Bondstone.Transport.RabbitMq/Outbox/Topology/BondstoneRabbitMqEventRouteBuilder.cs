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

    public BondstoneRabbitMqTransportBuilder ToQueue(
        string queueName)
    {
        transportBuilder.SetEventQueue(messageTypeName, queueName);
        return transportBuilder;
    }
}
