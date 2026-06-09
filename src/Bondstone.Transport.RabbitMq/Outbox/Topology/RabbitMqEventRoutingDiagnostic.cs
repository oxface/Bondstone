using Bondstone.Messaging;
using Bondstone.Utility;

namespace Bondstone.Transport.RabbitMq.Outbox;

public sealed class RabbitMqEventRoutingDiagnostic
{
    public RabbitMqEventRoutingDiagnostic(
        string messageTypeName,
        RabbitMqEventRoutingSource source,
        RabbitMqPublishDestination? destination,
        string? failureReason = null)
    {
        MessageTypeName = messageTypeName.NormalizeRequired(
            nameof(messageTypeName),
            "Message type name");
        Source = source;
        Destination = destination;
        FailureReason = failureReason;
    }

    public DurableMessageTopologyDiagnosticKind Kind =>
        DurableMessageTopologyDiagnosticKind.EventDestination;

    public string MessageTypeName { get; }

    public RabbitMqEventRoutingSource Source { get; }

    public RabbitMqPublishDestination? Destination { get; }

    public RabbitMqPublishDestinationKind? DestinationKind => Destination?.Kind;

    public string? ExchangeName => Destination?.ExchangeName;

    public string? RoutingKey => Destination?.RoutingKey;

    public string? FailureReason { get; }

    public bool HasRoute => Destination is not null;
}
