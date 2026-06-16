using Bondstone.Modules;
using Bondstone.Persistence;

namespace Bondstone.Messaging;

internal sealed class DurableEnvelopeReceiver(
    IModuleCommandReceivePipeline commandReceivePipeline,
    IModuleEventReceivePipeline eventReceivePipeline)
    : IDurableEnvelopeReceiver
{
    private readonly IModuleCommandReceivePipeline _commandReceivePipeline =
        commandReceivePipeline ?? throw new ArgumentNullException(nameof(commandReceivePipeline));
    private readonly IModuleEventReceivePipeline _eventReceivePipeline =
        eventReceivePipeline ?? throw new ArgumentNullException(nameof(eventReceivePipeline));

    public async ValueTask<DurableInboxHandleResult> ReceiveCommandAsync(
        DurableMessageEnvelope envelope,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(envelope);

        if (envelope.MessageKind != MessageKind.Command)
        {
            throw new NotSupportedException(
                $"Command receive supports command envelopes only. Envelope '{envelope.MessageId}' is '{envelope.MessageKind}'.");
        }

        return await _commandReceivePipeline.HandleOnceAsync(envelope, ct);
    }

    public async ValueTask<DurableInboxHandleResult> ReceiveEventAsync(
        DurableMessageEnvelope envelope,
        string subscriberModule,
        string subscriberIdentity,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(envelope);

        if (envelope.MessageKind != MessageKind.Event)
        {
            throw new NotSupportedException(
                $"Event receive supports event envelopes only. Envelope '{envelope.MessageId}' is '{envelope.MessageKind}'.");
        }

        return await _eventReceivePipeline.HandleOnceAsync(
            envelope,
            subscriberModule,
            subscriberIdentity,
            ct);
    }
}
