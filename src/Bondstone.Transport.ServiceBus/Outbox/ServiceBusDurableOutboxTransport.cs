using Bondstone.Messaging;
using Bondstone.Persistence;

namespace Bondstone.Transport.ServiceBus.Outbox;

public sealed class ServiceBusDurableOutboxTransport(
    IServiceBusMessageSender messageSender,
    IServiceBusOutboxDestinationResolver destinationResolver,
    IServiceBusOutboxEventDestinationResolver eventDestinationResolver)
    : IDurableOutboxTransport
{
    private readonly IServiceBusMessageSender _messageSender =
        messageSender ?? throw new ArgumentNullException(nameof(messageSender));
    private readonly IServiceBusOutboxDestinationResolver _destinationResolver =
        destinationResolver ?? throw new ArgumentNullException(nameof(destinationResolver));
    private readonly IServiceBusOutboxEventDestinationResolver _eventDestinationResolver =
        eventDestinationResolver ?? throw new ArgumentNullException(nameof(eventDestinationResolver));

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
            ServiceBusEventDestination destination =
                _eventDestinationResolver.ResolveDestination(record);
            await _messageSender.SendAsync(destination.EntityName, message, ct);
            return;
        }

        throw new NotSupportedException(
            $"Durable message kind '{envelope.MessageKind}' is not supported.");
    }
}
