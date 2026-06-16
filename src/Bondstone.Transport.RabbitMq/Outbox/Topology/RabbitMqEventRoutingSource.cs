namespace Bondstone.Transport.RabbitMq.Outbox;

internal enum RabbitMqEventRoutingSource
{
    Missing = 0,
    ExplicitRoutingKey = 1,
    RoutingKeyConvention = 2,
    ExplicitQueue = 3,
    QueueConvention = 4,
}
