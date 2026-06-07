using Bondstone.Utility;

namespace Bondstone.Modules;

internal sealed class ModuleEventSubscriberRegistry : IModuleEventSubscriberRegistry
{
    private readonly Dictionary<SubscriberKey, ModuleEventSubscriberRegistration> _subscribers = [];

    public IReadOnlyCollection<ModuleEventSubscriberRegistration> Subscribers
    {
        get
        {
            lock (_subscribers)
            {
                return _subscribers.Values.ToArray();
            }
        }
    }

    public IReadOnlyCollection<ModuleEventSubscriberRegistration> GetByMessageTypeName(
        string messageTypeName)
    {
        string normalizedMessageTypeName = messageTypeName.NormalizeRequired(
            nameof(messageTypeName),
            "Message type name");

        lock (_subscribers)
        {
            return _subscribers.Values
                .Where(subscriber => string.Equals(
                    subscriber.MessageTypeName,
                    normalizedMessageTypeName,
                    StringComparison.Ordinal))
                .ToArray();
        }
    }

    internal ModuleEventSubscriberRegistration Register(
        ModuleEventSubscriberRegistration subscriber)
    {
        ArgumentNullException.ThrowIfNull(subscriber);

        var key = new SubscriberKey(
            subscriber.ModuleName,
            subscriber.MessageTypeName,
            subscriber.SubscriberIdentity);

        lock (_subscribers)
        {
            if (_subscribers.TryGetValue(key, out ModuleEventSubscriberRegistration? existingSubscriber))
            {
                if (existingSubscriber.EventType != subscriber.EventType
                    || existingSubscriber.HandlerType != subscriber.HandlerType)
                {
                    throw new InvalidOperationException(
                        $"Module '{subscriber.ModuleName}' already has an event subscriber '{subscriber.SubscriberIdentity}' for message type '{subscriber.MessageTypeName}'.");
                }

                return existingSubscriber;
            }

            _subscribers.Add(key, subscriber);
            return subscriber;
        }
    }

    private sealed record SubscriberKey(
        string ModuleName,
        string MessageTypeName,
        string SubscriberIdentity);
}
