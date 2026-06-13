namespace Bondstone.Transport.RabbitMq.Outbox;

internal sealed record RabbitMqEventDestinationConvention(
    RabbitMqPublishDestinationKind Kind,
    Func<string, string> DestinationNameFactory);
