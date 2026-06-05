using Bondstone.Messaging;

namespace Bondstone.Transport.Rebus.Outbox;

public sealed record RebusDurableMessageEnvelope(
    Guid MessageId,
    string MessageKind,
    string MessageTypeName,
    string SourceModule,
    string? TargetModule,
    string Payload,
    string? Metadata,
    DateTimeOffset CreatedAtUtc,
    Guid? DurableOperationId,
    string? TraceParent,
    string? TraceState,
    string? TraceBaggage,
    Guid? CausationId,
    string? PartitionKey)
{
    public static RebusDurableMessageEnvelope From(DurableMessageEnvelope envelope)
    {
        ArgumentNullException.ThrowIfNull(envelope);

        return new RebusDurableMessageEnvelope(
            envelope.MessageId,
            envelope.MessageKind.ToString(),
            envelope.MessageTypeName,
            envelope.SourceModule,
            envelope.TargetModule,
            envelope.Payload,
            envelope.Metadata,
            envelope.CreatedAtUtc,
            envelope.DurableOperationId,
            envelope.TraceContext?.TraceParent,
            envelope.TraceContext?.TraceState,
            envelope.TraceContext?.Baggage,
            envelope.CausationId,
            envelope.PartitionKey);
    }
}
