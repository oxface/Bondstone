using Bondstone.Messaging;
using Bondstone.Utility;

namespace Bondstone.Modules;

public sealed class ModuleEventSubscriberRegistration
{
    internal ModuleEventSubscriberRegistration(
        string moduleName,
        Type eventType,
        MessageTypeRegistration messageTypeRegistration,
        string subscriberIdentity,
        Type handlerType)
    {
        ArgumentNullException.ThrowIfNull(eventType);
        ArgumentNullException.ThrowIfNull(messageTypeRegistration);
        ArgumentNullException.ThrowIfNull(handlerType);

        if (!typeof(IIntegrationEvent).IsAssignableFrom(eventType))
        {
            throw new ArgumentException(
                $"Event type '{eventType.FullName}' must implement {nameof(IIntegrationEvent)}.",
                nameof(eventType));
        }

        if (messageTypeRegistration.Kind != MessageKind.Event)
        {
            throw new ArgumentException(
                $"Message type '{messageTypeRegistration.MessageTypeName}' is registered as '{messageTypeRegistration.Kind}', not '{MessageKind.Event}'.",
                nameof(messageTypeRegistration));
        }

        if (messageTypeRegistration.ClrType != eventType)
        {
            throw new ArgumentException(
                $"Message type '{messageTypeRegistration.MessageTypeName}' is registered for '{messageTypeRegistration.ClrType.FullName}', not '{eventType.FullName}'.",
                nameof(messageTypeRegistration));
        }

        ModuleName = moduleName.NormalizeRequired(nameof(moduleName), "Module name");
        EventType = eventType;
        MessageTypeRegistration = messageTypeRegistration;
        SubscriberIdentity = subscriberIdentity.NormalizeRequired(
            nameof(subscriberIdentity),
            "Subscriber identity");
        HandlerType = handlerType;
    }

    public string ModuleName { get; }

    public Type EventType { get; }

    public MessageTypeRegistration MessageTypeRegistration { get; }

    public string MessageTypeName => MessageTypeRegistration.MessageTypeName;

    public string SubscriberIdentity { get; }

    public Type HandlerType { get; }

    internal static ModuleEventSubscriberRegistration Create<TEvent, THandler>(
        string moduleName,
        MessageTypeRegistration messageTypeRegistration,
        string subscriberIdentity)
        where TEvent : IIntegrationEvent
        where THandler : class, IIntegrationEventHandler<TEvent>
    {
        return new ModuleEventSubscriberRegistration(
            moduleName,
            typeof(TEvent),
            messageTypeRegistration,
            subscriberIdentity,
            typeof(THandler));
    }
}
