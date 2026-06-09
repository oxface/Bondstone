using Bondstone.Utility;

namespace Bondstone.Transport.RabbitMq.Inbox;

internal sealed class RabbitMqReceiveQueueRegistration
{
    private readonly HashSet<string> _acceptedModules = new(StringComparer.Ordinal);
    private readonly List<RabbitMqEventSubscriptionBinding> _eventSubscriptions = [];

    public RabbitMqReceiveQueueRegistration(
        string queueName)
    {
        QueueName = queueName.NormalizeRequired(
            nameof(queueName),
            "RabbitMQ queue name");
    }

    public string QueueName { get; }

    public IReadOnlyCollection<string> AcceptedModules => _acceptedModules;

    public IReadOnlyCollection<RabbitMqEventSubscriptionBinding> EventSubscriptions =>
        _eventSubscriptions;

    public void AddAcceptedModule(
        string moduleName)
    {
        _acceptedModules.Add(moduleName.NormalizeRequired(
            nameof(moduleName),
            "Module name"));
    }

    public void AddEventSubscription(
        string messageTypeName,
        string subscriberModule,
        string subscriberIdentity)
    {
        var subscription = new RabbitMqEventSubscriptionBinding(
            QueueName,
            messageTypeName.NormalizeRequired(
                nameof(messageTypeName),
                "Message type name"),
            subscriberModule.NormalizeRequired(
                nameof(subscriberModule),
                "Subscriber module"),
            subscriberIdentity.NormalizeRequired(
                nameof(subscriberIdentity),
                "Subscriber identity"));

        if (_eventSubscriptions.Any(existing =>
                existing.MessageTypeName == subscription.MessageTypeName
                && existing.SubscriberModule == subscription.SubscriberModule
                && existing.SubscriberIdentity == subscription.SubscriberIdentity))
        {
            return;
        }

        _eventSubscriptions.Add(subscription);
    }
}
