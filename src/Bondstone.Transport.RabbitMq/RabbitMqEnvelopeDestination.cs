namespace Bondstone.Transport.RabbitMq;

public sealed record RabbitMqEnvelopeDestination(
    string Exchange,
    string RoutingKey);
