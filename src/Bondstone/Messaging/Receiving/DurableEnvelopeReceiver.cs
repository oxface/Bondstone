using Bondstone.Diagnostics;
using Bondstone.Modules;
using Bondstone.Persistence;

namespace Bondstone.Messaging;

internal sealed class DurableEnvelopeReceiver(
    IModuleCommandReceivePipeline commandReceivePipeline,
    IModuleEventReceivePipeline eventReceivePipeline,
    IDurableMessageEnvelopeSerializer envelopeSerializer)
    : IDurableEnvelopeReceiver
{
    private readonly IModuleCommandReceivePipeline _commandReceivePipeline =
        commandReceivePipeline ?? throw new ArgumentNullException(nameof(commandReceivePipeline));
    private readonly IModuleEventReceivePipeline _eventReceivePipeline =
        eventReceivePipeline ?? throw new ArgumentNullException(nameof(eventReceivePipeline));
    private readonly IDurableMessageEnvelopeSerializer _envelopeSerializer =
        envelopeSerializer ?? throw new ArgumentNullException(nameof(envelopeSerializer));

    public async ValueTask<DurableInboxHandleResult> ReceiveAsync(
        DurableMessageEnvelope envelope,
        DurableEnvelopeReceiveBinding? binding = null,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(envelope);

        return envelope.MessageKind switch
        {
            MessageKind.Command => await ReceiveCommandAsync(envelope, ct),
            MessageKind.Event => binding is null
                ? throw new BondstoneSetupArgumentException(
                    BondstoneSetupCodes.MissingReceiveBinding,
                    "Event receive requires subscriber module and subscriber identity binding.",
                    nameof(binding))
                : await ReceiveEventAsync(envelope, binding, ct),
            _ => throw new NotSupportedException(
                $"Envelope receive does not support message kind '{envelope.MessageKind}'."),
        };
    }

    public ValueTask<DurableInboxHandleResult> ReceiveAsync(
        ReadOnlyMemory<byte> utf8Json,
        DurableEnvelopeReceiveBinding? binding = null,
        CancellationToken ct = default)
    {
        DurableMessageEnvelope envelope = _envelopeSerializer.Deserialize(utf8Json);
        return ReceiveAsync(envelope, binding, ct);
    }

    public ValueTask<DurableInboxHandleResult> ReceiveAsync(
        string json,
        DurableEnvelopeReceiveBinding? binding = null,
        CancellationToken ct = default)
    {
        DurableMessageEnvelope envelope = _envelopeSerializer.Deserialize(json);
        return ReceiveAsync(envelope, binding, ct);
    }

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

        DurableEnvelopeReceiveBinding binding = NormalizeReceiveBinding(
            subscriberModule,
            subscriberIdentity);

        return await _eventReceivePipeline.HandleOnceAsync(
            envelope,
            binding.SubscriberModule,
            binding.SubscriberIdentity,
            ct);
    }

    private ValueTask<DurableInboxHandleResult> ReceiveEventAsync(
        DurableMessageEnvelope envelope,
        DurableEnvelopeReceiveBinding binding,
        CancellationToken ct)
    {
        DurableEnvelopeReceiveBinding normalizedBinding = NormalizeReceiveBinding(
            binding.SubscriberModule,
            binding.SubscriberIdentity);

        return ReceiveEventAsync(
            envelope,
            normalizedBinding.SubscriberModule,
            normalizedBinding.SubscriberIdentity,
            ct);
    }

    private static DurableEnvelopeReceiveBinding NormalizeReceiveBinding(
        string? subscriberModule,
        string? subscriberIdentity)
    {
        if (string.IsNullOrWhiteSpace(subscriberModule)
            || string.IsNullOrWhiteSpace(subscriberIdentity))
        {
            throw new BondstoneSetupArgumentException(
                BondstoneSetupCodes.MissingReceiveBinding,
                "Event receive requires subscriber module and subscriber identity binding.",
                nameof(DurableEnvelopeReceiveBinding));
        }

        return new DurableEnvelopeReceiveBinding(
            subscriberModule.Trim(),
            subscriberIdentity.Trim());
    }
}
