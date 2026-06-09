using Bondstone.Utility;

namespace Bondstone.Transport.RabbitMq.Inbox;

public sealed class RabbitMqReceiveQueueDiagnostic
{
    public RabbitMqReceiveQueueDiagnostic(
        string queueName,
        IReadOnlyCollection<string> acceptedModules,
        IReadOnlyCollection<RabbitMqReceiveQueueEventSubscriptionDiagnostic> eventSubscriptions,
        string? failureReason = null)
    {
        QueueName = queueName.NormalizeRequired(
            nameof(queueName),
            "RabbitMQ queue name");
        AcceptedModules = acceptedModules
            .Select(static moduleName => moduleName.NormalizeRequired(
                "moduleName",
                "Module name"))
            .ToArray();
        EventSubscriptions = eventSubscriptions.ToArray();
        FailureReason = failureReason;
    }

    public string QueueName { get; }

    public IReadOnlyCollection<string> AcceptedModules { get; }

    public IReadOnlyCollection<RabbitMqReceiveQueueEventSubscriptionDiagnostic> EventSubscriptions { get; }

    public string? FailureReason { get; }

    public bool HasBinding => AcceptedModules.Count > 0 || EventSubscriptions.Count > 0;
}
