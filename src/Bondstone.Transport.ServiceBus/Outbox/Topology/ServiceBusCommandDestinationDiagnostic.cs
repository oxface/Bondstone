using Bondstone.Messaging;
using Bondstone.Utility;

namespace Bondstone.Transport.ServiceBus.Outbox;

public sealed class ServiceBusCommandDestinationDiagnostic
{
    public ServiceBusCommandDestinationDiagnostic(
        string targetModule,
        ServiceBusCommandDestinationSource source,
        string? queueName = null,
        string? failureReason = null)
    {
        TargetModule = targetModule.NormalizeRequired(
            nameof(targetModule),
            "Target module");
        Source = source;
        QueueName = queueName?.NormalizeRequired(
            nameof(queueName),
            "Service Bus queue name");
        FailureReason = failureReason;
    }

    public DurableMessageTopologyDiagnosticKind Kind =>
        DurableMessageTopologyDiagnosticKind.CommandDestination;

    public string TargetModule { get; }

    public ServiceBusCommandDestinationSource Source { get; }

    public string? QueueName { get; }

    public string? FailureReason { get; }

    public bool HasDestination => QueueName is not null;
}
