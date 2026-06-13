namespace Bondstone.Transport.RabbitMq.Outbox;

public interface IRabbitMqMessagePublisher
{
    ValueTask PublishAsync(
        RabbitMqPublishDestination destination,
        RabbitMqTransportMessage message,
        CancellationToken ct = default);
}
