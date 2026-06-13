using Azure.Messaging.ServiceBus;

namespace Bondstone.Transport.ServiceBus.Outbox;

internal sealed class AzureServiceBusMessageSender(ServiceBusClient client)
    : IServiceBusMessageSender
{
    private readonly ServiceBusClient _client =
        client ?? throw new ArgumentNullException(nameof(client));

    public async ValueTask SendAsync(
        string entityName,
        ServiceBusTransportMessage message,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(entityName);
        ArgumentNullException.ThrowIfNull(message);

        ServiceBusSender sender = _client.CreateSender(entityName);
        await sender.SendMessageAsync(CreateMessage(message), ct);
    }

    private static ServiceBusMessage CreateMessage(
        ServiceBusTransportMessage message)
    {
        var serviceBusMessage = new ServiceBusMessage(
            BinaryData.FromString(message.Body))
        {
            MessageId = message.MessageId,
            Subject = message.Subject,
            CorrelationId = message.CorrelationId,
            PartitionKey = message.PartitionKey,
            ContentType = "application/json",
        };

        foreach ((string key, object value) in message.ApplicationProperties)
        {
            serviceBusMessage.ApplicationProperties[key] = value;
        }

        return serviceBusMessage;
    }
}
