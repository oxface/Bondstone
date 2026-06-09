using Bondstone.Utility;

namespace Bondstone.Transport.Rebus.Inbox;

public interface IRebusEventSubscriptionRegistry
{
    IReadOnlyCollection<RebusEventSubscriptionBinding> Subscriptions { get; }

    IReadOnlyCollection<RebusEventSubscriptionBinding> GetSubscriptions(
        string endpointName,
        string messageTypeName);
}

public sealed class RebusEventSubscriptionRegistry : IRebusEventSubscriptionRegistry
{
    private readonly IReadOnlyDictionary<SubscriptionKey, RebusEventSubscriptionBinding> _subscriptionsByKey;

    public RebusEventSubscriptionRegistry(
        IEnumerable<RebusEventSubscriptionBinding> subscriptions)
    {
        ArgumentNullException.ThrowIfNull(subscriptions);

        var subscriptionsByKey = new Dictionary<SubscriptionKey, RebusEventSubscriptionBinding>();
        foreach (RebusEventSubscriptionBinding subscription in subscriptions)
        {
            var key = new SubscriptionKey(
                subscription.EndpointName,
                subscription.MessageTypeName,
                subscription.SubscriberModule,
                subscription.SubscriberIdentity);

            if (subscriptionsByKey.TryGetValue(
                key,
                out RebusEventSubscriptionBinding? existingSubscription))
            {
                throw new InvalidOperationException(
                    $"Rebus event subscription for endpoint '{existingSubscription.EndpointName}', message type '{existingSubscription.MessageTypeName}', subscriber module '{existingSubscription.SubscriberModule}', and subscriber identity '{existingSubscription.SubscriberIdentity}' is already configured.");
            }

            subscriptionsByKey.Add(key, subscription);
        }

        _subscriptionsByKey = subscriptionsByKey;
    }

    public IReadOnlyCollection<RebusEventSubscriptionBinding> Subscriptions =>
        _subscriptionsByKey.Values.ToArray();

    public IReadOnlyCollection<RebusEventSubscriptionBinding> GetSubscriptions(
        string endpointName,
        string messageTypeName)
    {
        string normalizedEndpointName = endpointName.NormalizeRequired(
            nameof(endpointName),
            "Rebus receive endpoint name");
        string normalizedMessageTypeName = messageTypeName.NormalizeRequired(
            nameof(messageTypeName),
            "Message type name");

        RebusEventSubscriptionBinding[] subscriptions = _subscriptionsByKey
            .Values
            .Where(subscription => StringComparer.Ordinal.Equals(
                    subscription.EndpointName,
                    normalizedEndpointName)
                && StringComparer.Ordinal.Equals(
                    subscription.MessageTypeName,
                    normalizedMessageTypeName))
            .ToArray();

        if (subscriptions.Length > 0)
        {
            return subscriptions;
        }

        throw new InvalidOperationException(
            $"Rebus receive endpoint '{normalizedEndpointName}' has no event subscription binding for message type '{normalizedMessageTypeName}'.");
    }

    private sealed record SubscriptionKey(
        string EndpointName,
        string MessageTypeName,
        string SubscriberModule,
        string SubscriberIdentity);
}
