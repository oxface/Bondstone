using System.Text.Json;
using Bondstone.Messaging;

namespace Bondstone.Transport.ServiceBus.Outbox;

internal static class ServiceBusDurableEnvelopeMapper
{
    public static ServiceBusTransportMessage CreateMessage(
        DurableMessageEnvelope envelope)
    {
        ArgumentNullException.ThrowIfNull(envelope);

        string messageId = envelope.MessageId.ToString("D");
        return new ServiceBusTransportMessage(
            JsonSerializer.Serialize(ServiceBusDurableMessageEnvelope.From(envelope)),
            messageId,
            envelope.MessageTypeName,
            CreateCorrelationId(envelope, messageId),
            envelope.PartitionKey,
            CreateApplicationProperties(envelope, messageId));
    }

    private static IReadOnlyDictionary<string, object> CreateApplicationProperties(
        DurableMessageEnvelope envelope,
        string messageId)
    {
        var properties = new Dictionary<string, object>(StringComparer.Ordinal)
        {
            [BondstoneServiceBusHeaders.MessageId] = messageId,
            [BondstoneServiceBusHeaders.MessageKind] = envelope.MessageKind.ToString(),
            [BondstoneServiceBusHeaders.MessageTypeName] = envelope.MessageTypeName,
            [BondstoneServiceBusHeaders.SourceModule] = envelope.SourceModule,
        };

        AddOptional(properties, BondstoneServiceBusHeaders.TargetModule, envelope.TargetModule);
        AddOptional(
            properties,
            BondstoneServiceBusHeaders.DurableOperationId,
            envelope.DurableOperationId?.ToString("D"));
        AddOptional(
            properties,
            BondstoneServiceBusHeaders.CausationId,
            envelope.CausationId?.ToString("D"));
        AddOptional(properties, BondstoneServiceBusHeaders.PartitionKey, envelope.PartitionKey);

        MessageTraceContext? traceContext = envelope.TraceContext;
        if (traceContext is not null)
        {
            properties[BondstoneServiceBusHeaders.TraceParent] = traceContext.TraceParent;
            AddOptional(properties, BondstoneServiceBusHeaders.TraceState, traceContext.TraceState);
            AddOptional(properties, BondstoneServiceBusHeaders.Baggage, traceContext.Baggage);
        }

        return properties;
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
        Dictionary<string, object> properties,
        string key,
        string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            properties[key] = value;
        }
    }
}
