using System.Text.Json;
using Bondstone.Messaging;

namespace Bondstone.Transport.RabbitMq.Outbox;

internal static class RabbitMqDurableEnvelopeMapper
{
    public static RabbitMqTransportMessage CreateMessage(
        DurableMessageEnvelope envelope)
    {
        ArgumentNullException.ThrowIfNull(envelope);

        string messageId = envelope.MessageId.ToString("D");
        return new RabbitMqTransportMessage(
            JsonSerializer.Serialize(RabbitMqDurableMessageEnvelope.From(envelope)),
            messageId,
            envelope.MessageTypeName,
            CreateCorrelationId(envelope, messageId),
            CreateHeaders(envelope, messageId));
    }

    public static DurableMessageEnvelope ReadEnvelope(
        string body)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(body);

        RabbitMqDurableMessageEnvelope envelope =
            JsonSerializer.Deserialize<RabbitMqDurableMessageEnvelope>(body)
            ?? throw new InvalidOperationException(
                "RabbitMQ message body did not contain a Bondstone durable envelope.");
        MessageKind messageKind = Enum.Parse<MessageKind>(
            envelope.MessageKind,
            ignoreCase: false);

        return new DurableMessageEnvelope(
            envelope.MessageId,
            messageKind,
            envelope.MessageTypeName,
            envelope.SourceModule,
            envelope.TargetModule,
            envelope.Payload,
            envelope.CreatedAtUtc,
            envelope.DurableOperationId,
            CreateTraceContext(envelope),
            envelope.CausationId,
            envelope.PartitionKey,
            envelope.Metadata);
    }

    private static IReadOnlyDictionary<string, object> CreateHeaders(
        DurableMessageEnvelope envelope,
        string messageId)
    {
        var headers = new Dictionary<string, object>(StringComparer.Ordinal)
        {
            [BondstoneRabbitMqHeaders.MessageId] = messageId,
            [BondstoneRabbitMqHeaders.MessageKind] = envelope.MessageKind.ToString(),
            [BondstoneRabbitMqHeaders.MessageTypeName] = envelope.MessageTypeName,
            [BondstoneRabbitMqHeaders.SourceModule] = envelope.SourceModule,
        };

        AddOptional(headers, BondstoneRabbitMqHeaders.TargetModule, envelope.TargetModule);
        AddOptional(
            headers,
            BondstoneRabbitMqHeaders.DurableOperationId,
            envelope.DurableOperationId?.ToString("D"));
        AddOptional(
            headers,
            BondstoneRabbitMqHeaders.CausationId,
            envelope.CausationId?.ToString("D"));
        AddOptional(headers, BondstoneRabbitMqHeaders.PartitionKey, envelope.PartitionKey);

        MessageTraceContext? traceContext = envelope.TraceContext;
        if (traceContext is not null)
        {
            headers[BondstoneRabbitMqHeaders.TraceParent] = traceContext.TraceParent;
            AddOptional(headers, BondstoneRabbitMqHeaders.TraceState, traceContext.TraceState);
            AddOptional(headers, BondstoneRabbitMqHeaders.Baggage, traceContext.Baggage);
        }

        return headers;
    }

    private static string CreateCorrelationId(
        DurableMessageEnvelope envelope,
        string messageId)
    {
        if (envelope.TraceContext?.TryGetW3CTraceId(out string? traceId) == true)
        {
            return traceId!;
        }

        return envelope.DurableOperationId?.ToString("D") ?? messageId;
    }

    private static void AddOptional(
        Dictionary<string, object> headers,
        string key,
        string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            headers[key] = value;
        }
    }

    private static MessageTraceContext? CreateTraceContext(
        RabbitMqDurableMessageEnvelope envelope)
    {
        return string.IsNullOrWhiteSpace(envelope.TraceParent)
            ? null
            : new MessageTraceContext(
                envelope.TraceParent,
                envelope.TraceState,
                envelope.TraceBaggage);
    }
}
