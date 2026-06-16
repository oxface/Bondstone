using Bondstone.Persistence;

namespace Bondstone.Transport.RabbitMq.Outbox;

internal sealed class RabbitMqDurableEnvelopeDispatchRoute(
    IRabbitMqMessagePublisher publisher,
    RabbitMqEnvelopeDestinationResolver destinationResolver)
    : IDurableEnvelopeDispatchRoute
{
    private readonly RabbitMqDurableEnvelopeDispatcher _dispatcher =
        new(
            publisher ?? throw new ArgumentNullException(nameof(publisher)),
            destinationResolver ?? throw new ArgumentNullException(nameof(destinationResolver)));
    private readonly RabbitMqEnvelopeDestinationResolver _destinationResolver =
        destinationResolver ?? throw new ArgumentNullException(nameof(destinationResolver));

    public string TransportName => "RabbitMq";

    public bool CanSend(
        DurableOutboxRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);

        return _destinationResolver.CanResolve(record);
    }

    public ValueTask DispatchAsync(
        DurableOutboxRecord record,
        CancellationToken ct = default)
    {
        return _dispatcher.DispatchAsync(record, ct);
    }
}
