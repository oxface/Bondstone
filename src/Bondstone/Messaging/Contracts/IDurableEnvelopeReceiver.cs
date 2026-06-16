using Bondstone.Persistence;

namespace Bondstone.Messaging;

public interface IDurableEnvelopeReceiver
{
    ValueTask<DurableInboxHandleResult> ReceiveCommandAsync(
        DurableMessageEnvelope envelope,
        CancellationToken ct = default);

    ValueTask<DurableInboxHandleResult> ReceiveEventAsync(
        DurableMessageEnvelope envelope,
        string subscriberModule,
        string subscriberIdentity,
        CancellationToken ct = default);
}
