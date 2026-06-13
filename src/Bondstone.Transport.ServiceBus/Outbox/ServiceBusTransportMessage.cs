namespace Bondstone.Transport.ServiceBus.Outbox;

public sealed class ServiceBusTransportMessage
{
    public ServiceBusTransportMessage(
        string body,
        string messageId,
        string subject,
        string correlationId,
        string? partitionKey,
        IReadOnlyDictionary<string, object> applicationProperties)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(body);
        ArgumentException.ThrowIfNullOrWhiteSpace(messageId);
        ArgumentException.ThrowIfNullOrWhiteSpace(subject);
        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);
        ArgumentNullException.ThrowIfNull(applicationProperties);

        Body = body;
        MessageId = messageId;
        Subject = subject;
        CorrelationId = correlationId;
        PartitionKey = string.IsNullOrWhiteSpace(partitionKey) ? null : partitionKey;
        ApplicationProperties = new Dictionary<string, object>(
            applicationProperties,
            StringComparer.Ordinal);
    }

    public string Body { get; }

    public string MessageId { get; }

    public string Subject { get; }

    public string CorrelationId { get; }

    public string? PartitionKey { get; }

    public IReadOnlyDictionary<string, object> ApplicationProperties { get; }
}
