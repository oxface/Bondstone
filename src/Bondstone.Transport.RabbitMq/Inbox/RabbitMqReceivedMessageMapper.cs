using System.Text;
using Bondstone.Transport.RabbitMq.Outbox;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Bondstone.Transport.RabbitMq.Inbox;

public static class RabbitMqReceivedMessageMapper
{
    public static RabbitMqTransportMessage FromBasicDeliver(
        BasicDeliverEventArgs delivery)
    {
        ArgumentNullException.ThrowIfNull(delivery);

        return FromBodyAndProperties(delivery.Body, delivery.BasicProperties);
    }

    public static RabbitMqTransportMessage FromBodyAndProperties(
        ReadOnlyMemory<byte> body,
        IReadOnlyBasicProperties properties)
    {
        ArgumentNullException.ThrowIfNull(properties);

        string messageId = properties.MessageId
            ?? GetHeaderValue(properties, BondstoneRabbitMqHeaders.MessageId)
            ?? throw new InvalidOperationException(
                "RabbitMQ received message is missing the Bondstone message id.");
        string messageTypeName = properties.Type
            ?? GetHeaderValue(properties, BondstoneRabbitMqHeaders.MessageTypeName)
            ?? throw new InvalidOperationException(
                "RabbitMQ received message is missing the Bondstone message type name.");
        string correlationId = properties.CorrelationId ?? messageId;

        return new RabbitMqTransportMessage(
            Encoding.UTF8.GetString(body.Span),
            messageId,
            messageTypeName,
            correlationId,
            ReadHeaders(properties));
    }

    private static IReadOnlyDictionary<string, object> ReadHeaders(
        IReadOnlyBasicProperties properties)
    {
        if (properties.Headers is null)
        {
            return new Dictionary<string, object>(StringComparer.Ordinal);
        }

        return properties.Headers.ToDictionary(
            static entry => entry.Key,
            static entry => ConvertHeaderValue(entry.Value),
            StringComparer.Ordinal);
    }

    private static string? GetHeaderValue(
        IReadOnlyBasicProperties properties,
        string key)
    {
        if (properties.Headers is null
            || !properties.Headers.TryGetValue(key, out object? value))
        {
            return null;
        }

        object converted = ConvertHeaderValue(value);
        return converted as string ?? converted.ToString();
    }

    private static object ConvertHeaderValue(
        object? value)
    {
        return value switch
        {
            null => string.Empty,
            byte[] bytes => Encoding.UTF8.GetString(bytes),
            ReadOnlyMemory<byte> memory => Encoding.UTF8.GetString(memory.Span),
            Memory<byte> memory => Encoding.UTF8.GetString(memory.Span),
            _ => value,
        };
    }
}
