using Bondstone.Messaging;
using Bondstone.Persistence;

namespace Bondstone.Transport.RabbitMq.Outbox;

public sealed class RabbitMqDurableEnvelopeDispatcher(
    IRabbitMqMessagePublisher publisher,
    IRabbitMqOutboxCommandRouteResolver commandRouteResolver,
    IRabbitMqOutboxEventRouteResolver eventRouteResolver)
    : IDurableEnvelopeDispatcher
{
    private readonly IRabbitMqMessagePublisher _publisher =
        publisher ?? throw new ArgumentNullException(nameof(publisher));
    private readonly IRabbitMqOutboxCommandRouteResolver _commandRouteResolver =
        commandRouteResolver ?? throw new ArgumentNullException(nameof(commandRouteResolver));
    private readonly IRabbitMqOutboxEventRouteResolver _eventRouteResolver =
        eventRouteResolver ?? throw new ArgumentNullException(nameof(eventRouteResolver));

    public async ValueTask DispatchAsync(
        DurableOutboxRecord record,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(record);

        DurableMessageEnvelope envelope = record.Envelope;
        RabbitMqTransportMessage message =
            RabbitMqDurableEnvelopeMapper.CreateMessage(envelope);

        if (envelope.MessageKind == MessageKind.Command)
        {
            RabbitMqPublishDestination destination =
                _commandRouteResolver.ResolveDestination(record);
            await _publisher.PublishAsync(destination, message, ct);
            return;
        }

        if (envelope.MessageKind == MessageKind.Event)
        {
            RabbitMqPublishDestination destination =
                _eventRouteResolver.ResolveDestination(record);
            await _publisher.PublishAsync(destination, message, ct);
            return;
        }

        throw new NotSupportedException(
            $"Durable message kind '{envelope.MessageKind}' is not supported.");
    }
}
