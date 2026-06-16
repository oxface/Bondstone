using Bondstone.Persistence;

namespace Bondstone.Transport.RabbitMq.Outbox;

internal sealed class RabbitMqDurableEnvelopeDispatcher(
    IRabbitMqMessagePublisher publisher,
    RabbitMqEnvelopeDestinationResolver destinationResolver)
    : IDurableEnvelopeDispatcher
{
    private readonly IRabbitMqMessagePublisher _publisher =
        publisher ?? throw new ArgumentNullException(nameof(publisher));
    private readonly RabbitMqEnvelopeDestinationResolver _destinationResolver =
        destinationResolver ?? throw new ArgumentNullException(nameof(destinationResolver));

    public async ValueTask DispatchAsync(
        DurableOutboxRecord record,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(record);

        RabbitMqTransportMessage message =
            RabbitMqDurableEnvelopeMapper.CreateMessage(record.Envelope);
        RabbitMqPublishDestination destination =
            _destinationResolver.Resolve(record);
        await _publisher.PublishAsync(destination, message, ct);
    }
}
