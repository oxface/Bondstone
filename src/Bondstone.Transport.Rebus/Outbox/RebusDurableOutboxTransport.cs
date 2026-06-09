using Bondstone.Messaging;
using Bondstone.Persistence;
using Rebus.Bus;

namespace Bondstone.Transport.Rebus.Outbox;

public sealed class RebusDurableOutboxTransport : IDurableOutboxTransport
{
    private readonly RebusCommandOutboxTransport _commandTransport;
    private readonly RebusEventOutboxTransport _eventTransport;

    public RebusDurableOutboxTransport(
        IBus bus,
        IRebusOutboxDestinationResolver destinationResolver,
        IRebusOutboxEventTopicResolver eventTopicResolver)
    {
        ArgumentNullException.ThrowIfNull(bus);

        _commandTransport = new RebusCommandOutboxTransport(
            bus,
            destinationResolver ?? throw new ArgumentNullException(nameof(destinationResolver)));
        _eventTransport = new RebusEventOutboxTransport(
            bus,
            eventTopicResolver ?? throw new ArgumentNullException(nameof(eventTopicResolver)));
    }

    public async ValueTask SendAsync(
        DurableOutboxRecord record,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(record);
        ct.ThrowIfCancellationRequested();

        DurableMessageEnvelope envelope = record.Envelope;

        if (envelope.MessageKind == MessageKind.Command)
        {
            await _commandTransport.SendAsync(record);
            return;
        }

        if (envelope.MessageKind == MessageKind.Event)
        {
            await _eventTransport.PublishAsync(record);
            return;
        }

        throw new NotSupportedException(
            $"Rebus outbox transport does not support message kind '{envelope.MessageKind}'.");
    }
}
