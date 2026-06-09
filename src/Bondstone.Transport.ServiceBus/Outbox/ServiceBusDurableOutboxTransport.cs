using Bondstone.Messaging;
using Bondstone.Persistence;

namespace Bondstone.Transport.ServiceBus.Outbox;

public sealed class ServiceBusDurableOutboxTransport(
    IServiceBusMessageSender messageSender,
    IServiceBusOutboxDestinationResolver destinationResolver,
    IServiceBusOutboxEventTopicResolver eventTopicResolver)
    : IDurableOutboxTransport
{
    private readonly IServiceBusMessageSender _messageSender =
        messageSender ?? throw new ArgumentNullException(nameof(messageSender));
    private readonly IServiceBusOutboxDestinationResolver _destinationResolver =
        destinationResolver ?? throw new ArgumentNullException(nameof(destinationResolver));
    private readonly IServiceBusOutboxEventTopicResolver _eventTopicResolver =
        eventTopicResolver ?? throw new ArgumentNullException(nameof(eventTopicResolver));

    public async ValueTask SendAsync(
        DurableOutboxRecord record,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(record);

        DurableMessageEnvelope envelope = record.Envelope;
        ServiceBusTransportMessage message =
            ServiceBusDurableEnvelopeMapper.CreateMessage(envelope);

        if (envelope.MessageKind == MessageKind.Command)
        {
            string queueName = _destinationResolver.ResolveQueueName(record);
            await _messageSender.SendAsync(queueName, message, ct);
            return;
        }

        if (envelope.MessageKind == MessageKind.Event)
        {
            string topicName = _eventTopicResolver.ResolveTopicName(record);
            await _messageSender.SendAsync(topicName, message, ct);
            return;
        }

        throw new NotSupportedException(
            $"Durable message kind '{envelope.MessageKind}' is not supported.");
    }
}
