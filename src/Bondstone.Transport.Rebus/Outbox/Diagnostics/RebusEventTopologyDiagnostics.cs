using Bondstone.Transport.Rebus.Inbox;
using Bondstone.Utility;

namespace Bondstone.Transport.Rebus.Outbox;

internal sealed class RebusEventTopologyDiagnostics(
    RebusEventTopicTopology topology,
    IReadOnlyCollection<RebusEventSubscriptionBinding> subscriptionBindings)
    : IRebusEventTopologyDiagnostics
{
    public RebusEventTopicDiagnostic DescribeEventTopic(
        string messageTypeName)
    {
        return topology.DescribeTopic(messageTypeName);
    }

    public RebusEventSubscriptionDiagnostic DescribeEventSubscriptions(
        string messageTypeName)
    {
        string normalizedMessageTypeName = messageTypeName.NormalizeRequired(
            nameof(messageTypeName),
            "Message type name");
        RebusEventTopicDiagnostic topic = topology.DescribeTopic(normalizedMessageTypeName);
        RebusEventSubscriberDiagnostic[] subscribers = subscriptionBindings
            .Where(subscription => StringComparer.Ordinal.Equals(
                subscription.MessageTypeName,
                normalizedMessageTypeName))
            .Select(static subscription => new RebusEventSubscriberDiagnostic(
                subscription.EndpointName,
                subscription.SubscriberModule,
                subscription.SubscriberIdentity))
            .ToArray();

        string? failureReason = subscribers.Length == 0
            ? $"No Rebus event subscriptions are configured for message type '{normalizedMessageTypeName}'."
            : null;

        return new RebusEventSubscriptionDiagnostic(
            normalizedMessageTypeName,
            topic,
            subscribers,
            failureReason);
    }
}
