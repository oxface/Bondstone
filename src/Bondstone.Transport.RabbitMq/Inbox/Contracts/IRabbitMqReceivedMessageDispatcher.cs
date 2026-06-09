using Bondstone.Transport.RabbitMq.Outbox;

namespace Bondstone.Transport.RabbitMq.Inbox;

public interface IRabbitMqReceivedMessageDispatcher
{
    ValueTask DispatchAsync(
        string queueName,
        RabbitMqTransportMessage message,
        CancellationToken ct = default);
}
