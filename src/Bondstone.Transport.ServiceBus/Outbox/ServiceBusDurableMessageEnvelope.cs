using Bondstone.Messaging;

namespace Bondstone.Transport.ServiceBus.Outbox;

public sealed record ServiceBusDurableMessageEnvelope(
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
    public static ServiceBusDurableMessageEnvelope From(DurableMessageEnvelope envelope)
    {
        ArgumentNullException.ThrowIfNull(envelope);

        return new ServiceBusDurableMessageEnvelope(
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
