using Bondstone.Messaging;
using Bondstone.Persistence;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace Bondstone.Transport.RabbitMq;

internal sealed class RabbitMqEnvelopeDispatcher(
    IChannel channel,
    IDurableMessageEnvelopeSerializer serializer,
    IOptions<RabbitMqEnvelopeDispatcherOptions> options)
    : IDurableEnvelopeDispatcher
{
    private readonly IChannel _channel =
        channel ?? throw new ArgumentNullException(nameof(channel));
    private readonly IDurableMessageEnvelopeSerializer _serializer =
        serializer ?? throw new ArgumentNullException(nameof(serializer));
    private readonly RabbitMqEnvelopeDispatcherOptions _options =
        options?.Value ?? throw new ArgumentNullException(nameof(options));

    public async ValueTask DispatchAsync(
        DurableOutboxRecord record,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(record);

        RabbitMqEnvelopeDestination destination =
            _options.GetDestination(record.Envelope);
        byte[] body = _serializer.SerializeToUtf8Bytes(record.Envelope);

        await _channel.BasicPublishAsync(
            destination.Exchange,
            destination.RoutingKey,
            _options.Mandatory,
            body,
            ct);
    }
}
