using Bondstone.Utility;

namespace Bondstone.Messaging;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false)]
public sealed class DurableCommandIdentityAttribute(string name) : Attribute
{
    public string Name { get; } = name;
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false)]
public sealed class IntegrationEventIdentityAttribute(string name) : Attribute
{
    public string Name { get; } = name;
}

internal static class MessageIdentityMetadata
{
    public static bool HasIdentity(Type messageType)
    {
        ArgumentNullException.ThrowIfNull(messageType);

        return (typeof(IDurableCommand).IsAssignableFrom(messageType) && HasDurableCommandIdentity(messageType))
            || (typeof(IIntegrationEvent).IsAssignableFrom(messageType) && HasIntegrationEventIdentity(messageType));
    }

    public static MessageTypeRegistration GetRequiredRegistration(Type messageType)
    {
        ArgumentNullException.ThrowIfNull(messageType);

        bool isDurableCommand = typeof(IDurableCommand).IsAssignableFrom(messageType);
        bool isIntegrationEvent = typeof(IIntegrationEvent).IsAssignableFrom(messageType);
        DurableCommandIdentityAttribute? commandIdentity = GetIdentity<DurableCommandIdentityAttribute>(messageType);
        IntegrationEventIdentityAttribute? eventIdentity = GetIdentity<IntegrationEventIdentityAttribute>(messageType);

        if (isDurableCommand && isIntegrationEvent)
        {
            throw new InvalidOperationException(
                $"Message type '{messageType.FullName}' must not implement both {nameof(IDurableCommand)} and {nameof(IIntegrationEvent)}.");
        }

        if (isDurableCommand)
        {
            if (eventIdentity is not null)
            {
                throw new InvalidOperationException(
                    $"Durable command '{messageType.FullName}' must use {nameof(DurableCommandIdentityAttribute)}, not {nameof(IntegrationEventIdentityAttribute)}.");
            }

            return new MessageTypeRegistration(
                messageType,
                (commandIdentity?.Name).NormalizeRequired(
                    nameof(DurableCommandIdentityAttribute.Name),
                    "Message identity"),
                MessageKind.Command);
        }

        if (isIntegrationEvent)
        {
            if (commandIdentity is not null)
            {
                throw new InvalidOperationException(
                    $"Integration event '{messageType.FullName}' must use {nameof(IntegrationEventIdentityAttribute)}, not {nameof(DurableCommandIdentityAttribute)}.");
            }

            return new MessageTypeRegistration(
                messageType,
                (eventIdentity?.Name).NormalizeRequired(
                    nameof(IntegrationEventIdentityAttribute.Name),
                    "Message identity"),
                MessageKind.Event);
        }

        throw new InvalidOperationException(
            $"Message type '{messageType.FullName}' must implement {nameof(IDurableCommand)} or {nameof(IIntegrationEvent)}.");
    }

    private static bool HasDurableCommandIdentity(Type messageType)
    {
        return GetIdentity<DurableCommandIdentityAttribute>(messageType) is not null;
    }

    private static bool HasIntegrationEventIdentity(Type messageType)
    {
        return GetIdentity<IntegrationEventIdentityAttribute>(messageType) is not null;
    }

    private static TAttribute? GetIdentity<TAttribute>(Type messageType)
        where TAttribute : Attribute
    {
        return messageType
            .GetCustomAttributes(typeof(TAttribute), inherit: false)
            .OfType<TAttribute>()
            .SingleOrDefault();
    }

}
