using Bondstone.Persistence;

namespace Bondstone.Messaging;

public interface IDurableEnvelopeReceiver
{
    ValueTask<DurableInboxHandleResult> ReceiveAsync(
        DurableMessageEnvelope envelope,
        DurableEnvelopeReceiveBinding? binding = null,
        CancellationToken ct = default);

    ValueTask<DurableInboxHandleResult> ReceiveAsync(
        ReadOnlyMemory<byte> utf8Json,
        DurableEnvelopeReceiveBinding? binding = null,
        CancellationToken ct = default);

    ValueTask<DurableInboxHandleResult> ReceiveAsync(
        string json,
        DurableEnvelopeReceiveBinding? binding = null,
        CancellationToken ct = default);

    ValueTask<DurableInboxHandleResult> ReceiveCommandAsync(
        DurableMessageEnvelope envelope,
        CancellationToken ct = default);

    ValueTask<DurableInboxHandleResult> ReceiveEventAsync(
        DurableMessageEnvelope envelope,
        string subscriberModule,
        string subscriberIdentity,
        CancellationToken ct = default);
}
