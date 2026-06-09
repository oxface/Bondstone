namespace Bondstone.Transport.RabbitMq.Outbox;

public sealed class RabbitMqTransportMessage
{
    public RabbitMqTransportMessage(
        string body,
        string messageId,
        string messageTypeName,
        string correlationId,
        IReadOnlyDictionary<string, object> headers)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(body);
        ArgumentException.ThrowIfNullOrWhiteSpace(messageId);
        ArgumentException.ThrowIfNullOrWhiteSpace(messageTypeName);
        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);
        ArgumentNullException.ThrowIfNull(headers);

        Body = body;
        MessageId = messageId;
        MessageTypeName = messageTypeName;
        CorrelationId = correlationId;
        Headers = new Dictionary<string, object>(
            headers,
            StringComparer.Ordinal);
    }

    public string Body { get; }

    public string MessageId { get; }

    public string MessageTypeName { get; }

    public string CorrelationId { get; }

    public IReadOnlyDictionary<string, object> Headers { get; }
}
