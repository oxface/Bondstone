using Bondstone.Utility;

namespace Bondstone.Transport.RabbitMq.Outbox;

public sealed class RabbitMqPublishDestination
{
    public RabbitMqPublishDestination(
        string exchangeName,
        string routingKey)
    {
        ExchangeName = exchangeName.NormalizeRequired(
            nameof(exchangeName),
            "RabbitMQ exchange name");
        RoutingKey = routingKey.NormalizeRequired(
            nameof(routingKey),
            "RabbitMQ routing key");
    }

    public string ExchangeName { get; }

    public string RoutingKey { get; }
}
