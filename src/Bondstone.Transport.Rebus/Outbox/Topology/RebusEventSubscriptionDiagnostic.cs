using Bondstone.Messaging;
using Bondstone.Utility;

namespace Bondstone.Transport.Rebus.Outbox;

public sealed class RebusEventSubscriptionDiagnostic
{
    public RebusEventSubscriptionDiagnostic(
        string messageTypeName,
        RebusEventTopicDiagnostic topic,
        IReadOnlyCollection<RebusEventSubscriberDiagnostic> subscribers,
        string? failureReason = null)
    {
        ArgumentNullException.ThrowIfNull(topic);
        ArgumentNullException.ThrowIfNull(subscribers);

        MessageTypeName = messageTypeName.NormalizeRequired(
            nameof(messageTypeName),
            "Message type name");
        Topic = topic;
        Subscribers = subscribers.ToArray();
        FailureReason = failureReason;
    }

    public DurableMessageTopologyDiagnosticKind Kind =>
        DurableMessageTopologyDiagnosticKind.EventSubscription;

    public string MessageTypeName { get; }

    public RebusEventTopicDiagnostic Topic { get; }

    public IReadOnlyCollection<RebusEventSubscriberDiagnostic> Subscribers { get; }

    public string? FailureReason { get; }

    public bool HasSubscriptions => Subscribers.Count > 0;
}
