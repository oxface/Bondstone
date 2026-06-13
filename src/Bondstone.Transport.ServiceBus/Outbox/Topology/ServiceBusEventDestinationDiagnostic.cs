using Bondstone.Messaging;
using Bondstone.Utility;

namespace Bondstone.Transport.ServiceBus.Outbox;

public sealed class ServiceBusEventDestinationDiagnostic
{
    public ServiceBusEventDestinationDiagnostic(
        string messageTypeName,
        ServiceBusEventDestinationSource source,
        ServiceBusEventDestination? destination,
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

    public ServiceBusEventDestinationSource Source { get; }

    public ServiceBusEventDestination? Destination { get; }

    public string? EntityName => Destination?.EntityName;

    public ServiceBusEventDestinationKind? DestinationKind => Destination?.Kind;

    public string? FailureReason { get; }

    public bool HasDestination => Destination is not null;
}
