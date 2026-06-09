using Bondstone.Messaging;
using Bondstone.Utility;

namespace Bondstone.Transport.RabbitMq.Outbox;

public sealed class RabbitMqEventRoutingDiagnostic
{
    public RabbitMqEventRoutingDiagnostic(
        string messageTypeName,
        RabbitMqEventRoutingSource source,
        string? exchangeName,
        string? routingKey,
        string? failureReason = null)
    {
        MessageTypeName = messageTypeName.NormalizeRequired(
            nameof(messageTypeName),
            "Message type name");
        Source = source;
        ExchangeName = exchangeName?.NormalizeRequired(
            nameof(exchangeName),
            "RabbitMQ exchange name");
        RoutingKey = routingKey?.NormalizeRequired(
            nameof(routingKey),
            "RabbitMQ routing key");
        FailureReason = failureReason;
    }

    public DurableMessageTopologyDiagnosticKind Kind =>
        DurableMessageTopologyDiagnosticKind.EventTopic;

    public string MessageTypeName { get; }

    public RabbitMqEventRoutingSource Source { get; }

    public string? ExchangeName { get; }

    public string? RoutingKey { get; }

    public string? FailureReason { get; }

    public bool HasRoute => ExchangeName is not null && RoutingKey is not null;
}
