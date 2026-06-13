namespace Bondstone.Transport.ServiceBus.Inbox;

public sealed class ServiceBusReceiveSourceDiagnostic
{
    public ServiceBusReceiveSourceDiagnostic(
        ServiceBusReceiveSource source,
        IReadOnlyCollection<string> acceptedModules,
        IReadOnlyCollection<ServiceBusReceiveSourceEventSubscriptionDiagnostic> eventSubscriptions,
        string? failureReason = null)
    {
        Source = source ?? throw new ArgumentNullException(nameof(source));
        AcceptedModules = acceptedModules.ToArray();
        EventSubscriptions = eventSubscriptions.ToArray();
        FailureReason = failureReason;
    }

    public ServiceBusReceiveSource Source { get; }

    public IReadOnlyCollection<string> AcceptedModules { get; }

    public IReadOnlyCollection<ServiceBusReceiveSourceEventSubscriptionDiagnostic> EventSubscriptions { get; }

    public string? FailureReason { get; }

    public bool HasBinding => AcceptedModules.Count > 0 || EventSubscriptions.Count > 0;
}
