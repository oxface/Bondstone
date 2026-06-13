using Bondstone.Utility;

namespace Bondstone.Transport.Local.Outbox;

internal sealed class LocalQueueRegistration
{
    private readonly HashSet<string> _acceptedModules = new(StringComparer.Ordinal);
    private readonly List<LocalEventSubscription> _eventSubscriptions = [];

    public LocalQueueRegistration(
        string queueName)
    {
        QueueName = queueName.NormalizeRequired(
            nameof(queueName),
            "Local queue name");
    }

    public string QueueName { get; }

    public IReadOnlyCollection<string> AcceptedModules => _acceptedModules;

    public IReadOnlyCollection<LocalEventSubscription> EventSubscriptions => _eventSubscriptions;

    public void AddAcceptedModule(
        string moduleName)
    {
        string normalizedModuleName = moduleName.NormalizeRequired(
            nameof(moduleName),
            "Module name");

        _acceptedModules.Add(normalizedModuleName);
    }

    public void AddEventSubscription(
        string messageTypeName,
        string subscriberModule,
        string subscriberIdentity)
    {
        var subscription = new LocalEventSubscription(
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
