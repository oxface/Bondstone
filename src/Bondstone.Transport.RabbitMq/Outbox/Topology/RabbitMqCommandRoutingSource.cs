namespace Bondstone.Transport.RabbitMq.Outbox;

internal enum RabbitMqCommandRoutingSource
{
    Missing = 0,
    ExplicitRoutingKey = 1,
    RoutingKeyConvention = 2,
}
