namespace Bondstone.Transport.RabbitMq.Outbox;

public enum RabbitMqCommandRoutingSource
{
    Missing = 0,
    ExplicitRoutingKey = 1,
    RoutingKeyConvention = 2,
}
