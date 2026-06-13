using Bondstone.Messaging;
using Bondstone.Utility;

namespace Bondstone.Modules;

internal static class MessageTypeRegistryResolution
{
    public static MessageTypeRegistration ResolveRegistration(
        this IMessageTypeRegistry registry,
        string messageTypeName)
    {
        ArgumentNullException.ThrowIfNull(registry);

        string normalizedMessageTypeName = messageTypeName.NormalizeRequired(
            nameof(messageTypeName),
            "Message type name");

        MessageTypeRegistration? registration = registry.Registrations.SingleOrDefault(
            item => StringComparer.Ordinal.Equals(
                item.MessageTypeName,
                normalizedMessageTypeName));

        return registration
            ?? throw new KeyNotFoundException(
                $"No message type registration exists for '{normalizedMessageTypeName}'.");
    }
}
