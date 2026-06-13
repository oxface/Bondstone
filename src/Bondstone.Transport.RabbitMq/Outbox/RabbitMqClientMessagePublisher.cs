using System.Text;
using RabbitMQ.Client;

namespace Bondstone.Transport.RabbitMq.Outbox;

internal sealed class RabbitMqClientMessagePublisher(IConnection connection)
    : IRabbitMqMessagePublisher
{
    private readonly IConnection _connection =
        connection ?? throw new ArgumentNullException(nameof(connection));

    public async ValueTask PublishAsync(
        RabbitMqPublishDestination destination,
        RabbitMqTransportMessage message,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(destination);
        ArgumentNullException.ThrowIfNull(message);

        await using IChannel channel = await _connection.CreateChannelAsync(
            options: null,
            cancellationToken: ct);
        var properties = new BasicProperties
        {
            ContentType = "application/json",
            MessageId = message.MessageId,
            CorrelationId = message.CorrelationId,
            Type = message.MessageTypeName,
            Persistent = true,
            Headers = message.Headers.ToDictionary(
                static entry => entry.Key,
                static entry => (object?)entry.Value,
                StringComparer.Ordinal),
        };

        await channel.BasicPublishAsync(
            destination.ExchangeName,
            destination.RoutingKey,
            mandatory: false,
            basicProperties: properties,
            body: Encoding.UTF8.GetBytes(message.Body),
            cancellationToken: ct);
    }
}
