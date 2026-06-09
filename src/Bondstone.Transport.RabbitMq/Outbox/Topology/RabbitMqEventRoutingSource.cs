namespace Bondstone.Transport.RabbitMq.Outbox;

public enum RabbitMqEventRoutingSource
{
    Missing = 0,
    ExplicitRoutingKey = 1,
    RoutingKeyConvention = 2,
}
