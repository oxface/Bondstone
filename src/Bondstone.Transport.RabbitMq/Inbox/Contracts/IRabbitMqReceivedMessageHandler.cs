using RabbitMQ.Client.Events;

namespace Bondstone.Transport.RabbitMq.Inbox;

public interface IRabbitMqReceivedMessageHandler
{
    ValueTask HandleAsync(
        string queueName,
        BasicDeliverEventArgs delivery,
        Func<ulong, CancellationToken, ValueTask> acknowledgeAsync,
        CancellationToken ct = default);
}
