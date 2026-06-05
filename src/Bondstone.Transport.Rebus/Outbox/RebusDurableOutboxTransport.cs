using Bondstone.Messaging;
using Bondstone.Persistence;
using Rebus.Bus.Advanced;
using Rebus.Messages;

namespace Bondstone.Transport.Rebus.Outbox;

public sealed class RebusDurableOutboxTransport(
    IRoutingApi routingApi,
    IRebusOutboxDestinationResolver destinationResolver)
    : IDurableOutboxTransport
{
    public async ValueTask SendAsync(
        DurableOutboxRecord record,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(record);
        ct.ThrowIfCancellationRequested();

        DurableMessageEnvelope envelope = record.Envelope;
        if (envelope.MessageKind != MessageKind.Command)
        {
            throw new NotSupportedException(
                "The first Rebus outbox transport supports command envelopes only.");
        }

        string destinationAddress = destinationResolver.ResolveDestinationAddress(record);
        RebusDurableMessageEnvelope rebusEnvelope = RebusDurableMessageEnvelope.From(envelope);
        Dictionary<string, string> headers = CreateHeaders(envelope);

        await routingApi.Send(destinationAddress, rebusEnvelope, headers);
    }

    private static Dictionary<string, string> CreateHeaders(DurableMessageEnvelope envelope)
    {
        string messageId = envelope.MessageId.ToString("D");
        var headers = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [Headers.MessageId] = messageId,
            [Headers.CorrelationId] = CreateCorrelationId(envelope, messageId),
            [BondstoneRebusHeaders.MessageId] = messageId,
            [BondstoneRebusHeaders.MessageKind] = envelope.MessageKind.ToString(),
            [BondstoneRebusHeaders.MessageTypeName] = envelope.MessageTypeName,
            [BondstoneRebusHeaders.SourceModule] = envelope.SourceModule,
        };

        AddOptional(headers, BondstoneRebusHeaders.TargetModule, envelope.TargetModule);
        AddOptional(headers, BondstoneRebusHeaders.DurableOperationId, envelope.DurableOperationId?.ToString("D"));
        AddOptional(headers, BondstoneRebusHeaders.CausationId, envelope.CausationId?.ToString("D"));
        AddOptional(headers, BondstoneRebusHeaders.PartitionKey, envelope.PartitionKey);

        if (envelope.CausationId is not null)
        {
            headers[Headers.InReplyTo] = envelope.CausationId.Value.ToString("D");
        }

        MessageTraceContext? traceContext = envelope.TraceContext;
        if (traceContext is not null)
        {
            headers[BondstoneRebusHeaders.TraceParent] = traceContext.TraceParent;
            AddOptional(headers, BondstoneRebusHeaders.TraceState, traceContext.TraceState);
            AddOptional(headers, BondstoneRebusHeaders.Baggage, traceContext.Baggage);
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
        Dictionary<string, string> headers,
        string key,
        string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            headers[key] = value;
        }
    }
}
