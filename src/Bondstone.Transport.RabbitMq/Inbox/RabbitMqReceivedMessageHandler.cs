using RabbitMQ.Client.Events;

namespace Bondstone.Transport.RabbitMq.Inbox;

internal sealed class RabbitMqReceivedMessageHandler(
    IRabbitMqReceivedMessageDispatcher dispatcher)
    : IRabbitMqReceivedMessageHandler
{
    private readonly IRabbitMqReceivedMessageDispatcher _dispatcher =
        dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));

    public async ValueTask HandleAsync(
        string queueName,
        BasicDeliverEventArgs delivery,
        Func<ulong, CancellationToken, ValueTask> acknowledgeAsync,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(queueName);
        ArgumentNullException.ThrowIfNull(delivery);
        ArgumentNullException.ThrowIfNull(acknowledgeAsync);

        await _dispatcher.DispatchAsync(
            queueName,
            RabbitMqReceivedMessageMapper.FromBasicDeliver(delivery),
            ct);
        await acknowledgeAsync(delivery.DeliveryTag, ct);
    }
}
