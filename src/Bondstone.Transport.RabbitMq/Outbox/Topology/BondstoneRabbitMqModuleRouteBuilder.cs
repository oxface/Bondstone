namespace Bondstone.Transport.RabbitMq.Outbox;

public sealed class BondstoneRabbitMqModuleRouteBuilder(
    BondstoneRabbitMqTransportBuilder transportBuilder,
    string targetModule)
{
    public BondstoneRabbitMqTransportBuilder ToRoutingKey(
        string routingKey)
    {
        transportBuilder.SetModuleRoutingKey(targetModule, routingKey);
        return transportBuilder;
    }
}
