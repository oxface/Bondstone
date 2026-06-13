using Bondstone.Utility;

namespace Bondstone.Transport.ServiceBus.Inbox;

public sealed record ServiceBusReceiveSource
{
    private ServiceBusReceiveSource(
        ServiceBusReceiveSourceKind kind,
        string entityName,
        string? subscriptionName)
    {
        if (!Enum.IsDefined(kind))
        {
            throw new ArgumentOutOfRangeException(nameof(kind), kind, "Service Bus receive source kind is not supported.");
        }

        Kind = kind;
        EntityName = entityName.NormalizeRequired(
            nameof(entityName),
            "Service Bus entity name");
        SubscriptionName = subscriptionName.NormalizeOptional();

        if (Kind == ServiceBusReceiveSourceKind.Subscription
            && SubscriptionName is null)
        {
            throw new ArgumentException(
                "Service Bus subscription receive sources require a subscription name.",
                nameof(subscriptionName));
        }

        if (Kind == ServiceBusReceiveSourceKind.Queue
            && SubscriptionName is not null)
        {
            throw new ArgumentException(
                "Service Bus queue receive sources must not specify a subscription name.",
                nameof(subscriptionName));
        }
    }

    public ServiceBusReceiveSourceKind Kind { get; }

    public string EntityName { get; }

    public string? SubscriptionName { get; }

    public string DisplayName => Kind == ServiceBusReceiveSourceKind.Queue
        ? $"queue '{EntityName}'"
        : $"topic '{EntityName}' subscription '{SubscriptionName}'";

    internal string Key => Kind == ServiceBusReceiveSourceKind.Queue
        ? $"queue:{EntityName}"
        : $"subscription:{EntityName}:{SubscriptionName}";

    public static ServiceBusReceiveSource ForQueue(
        string queueName)
    {
        return new ServiceBusReceiveSource(
            ServiceBusReceiveSourceKind.Queue,
            queueName,
            subscriptionName: null);
    }

    public static ServiceBusReceiveSource ForSubscription(
        string topicName,
        string subscriptionName)
    {
        return new ServiceBusReceiveSource(
            ServiceBusReceiveSourceKind.Subscription,
            topicName,
            subscriptionName);
    }
}
