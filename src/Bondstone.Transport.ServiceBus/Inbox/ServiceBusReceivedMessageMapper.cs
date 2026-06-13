using Azure.Messaging.ServiceBus;
using Bondstone.Transport.ServiceBus.Outbox;

namespace Bondstone.Transport.ServiceBus.Inbox;

public static class ServiceBusReceivedMessageMapper
{
    public static ServiceBusTransportMessage FromReceivedMessage(
        ServiceBusReceivedMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);

        string messageId = message.MessageId
            ?? GetApplicationProperty(message, BondstoneServiceBusHeaders.MessageId)
            ?? throw new InvalidOperationException(
                "Service Bus received message is missing the Bondstone message id.");
        string subject = message.Subject
            ?? GetApplicationProperty(message, BondstoneServiceBusHeaders.MessageTypeName)
            ?? throw new InvalidOperationException(
                "Service Bus received message is missing the Bondstone message type name.");
        string correlationId = message.CorrelationId ?? messageId;

        return new ServiceBusTransportMessage(
            message.Body.ToString(),
            messageId,
            subject,
            correlationId,
            message.PartitionKey,
            message.ApplicationProperties.ToDictionary(
                static entry => entry.Key,
                static entry => entry.Value,
                StringComparer.Ordinal));
    }

    private static string? GetApplicationProperty(
        ServiceBusReceivedMessage message,
        string key)
    {
        return message.ApplicationProperties.TryGetValue(key, out object? value)
            ? value as string ?? value?.ToString()
            : null;
    }
}
