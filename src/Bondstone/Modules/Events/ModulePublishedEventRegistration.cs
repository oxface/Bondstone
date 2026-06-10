using Bondstone.Messaging;
using Bondstone.Utility;

namespace Bondstone.Modules;

public sealed class ModulePublishedEventRegistration
{
    internal ModulePublishedEventRegistration(
        string moduleName,
        Type eventType,
        MessageTypeRegistration messageTypeRegistration)
    {
        ArgumentNullException.ThrowIfNull(eventType);
        ArgumentNullException.ThrowIfNull(messageTypeRegistration);

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
    }

    public string ModuleName { get; }

    public Type EventType { get; }

    public MessageTypeRegistration MessageTypeRegistration { get; }

    public string MessageTypeName => MessageTypeRegistration.MessageTypeName;

    internal static ModulePublishedEventRegistration Create<TEvent>(
        string moduleName,
        MessageTypeRegistration messageTypeRegistration)
        where TEvent : IIntegrationEvent
    {
        return new ModulePublishedEventRegistration(
            moduleName,
            typeof(TEvent),
            messageTypeRegistration);
    }
}
