using Bondstone.Utility;

namespace Bondstone.Transport.RabbitMq.Outbox;

public sealed class RabbitMqPublishDestination
{
    public RabbitMqPublishDestination(
        string exchangeName,
        string routingKey)
        : this(
            RabbitMqPublishDestinationKind.ExchangeRoute,
            exchangeName,
            routingKey)
    {
    }

    private RabbitMqPublishDestination(
        RabbitMqPublishDestinationKind kind,
        string exchangeName,
        string routingKey)
    {
        if (!Enum.IsDefined(kind))
        {
            throw new ArgumentOutOfRangeException(nameof(kind), kind, "RabbitMQ publish destination kind is not supported.");
        }

        Kind = kind;
        ExchangeName = kind == RabbitMqPublishDestinationKind.Queue
            ? exchangeName ?? string.Empty
            : exchangeName.NormalizeRequired(
                nameof(exchangeName),
                "RabbitMQ exchange name");
        RoutingKey = routingKey.NormalizeRequired(
            nameof(routingKey),
            "RabbitMQ routing key");
    }

    public RabbitMqPublishDestinationKind Kind { get; }

    public string ExchangeName { get; }

    public string RoutingKey { get; }

    public static RabbitMqPublishDestination ForQueue(
        string queueName)
    {
        return new RabbitMqPublishDestination(
            RabbitMqPublishDestinationKind.Queue,
            string.Empty,
            queueName);
    }
}
