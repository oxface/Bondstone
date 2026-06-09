namespace Bondstone.Transport.RabbitMq.Outbox;

internal sealed record RabbitMqEventRouteRegistration(
    RabbitMqPublishDestinationKind Kind,
    string DestinationName);
