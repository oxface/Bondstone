using Bondstone.Utility;

namespace Bondstone.Transport.ServiceBus.Inbox;

internal sealed class ServiceBusReceiveSourceRegistration
{
    private readonly HashSet<string> _acceptedModules = new(StringComparer.Ordinal);
    private readonly List<ServiceBusEventSubscriptionBinding> _eventSubscriptions = [];

    public ServiceBusReceiveSourceRegistration(
        ServiceBusReceiveSource source)
    {
        Source = source ?? throw new ArgumentNullException(nameof(source));
    }

    public ServiceBusReceiveSource Source { get; }

    public IReadOnlyCollection<string> AcceptedModules => _acceptedModules;

    public IReadOnlyCollection<ServiceBusEventSubscriptionBinding> EventSubscriptions =>
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
        var subscription = new ServiceBusEventSubscriptionBinding(
            Source,
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
