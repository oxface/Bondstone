using Bondstone.Diagnostics;
using Bondstone.Messaging;
using Bondstone.Persistence;

namespace Bondstone.Transport.Local.Outbox;

internal sealed class LocalDurableEnvelopeDispatchRoute(
    LocalTransportTopology topology,
    IDurableEnvelopeReceiver envelopeReceiver)
    : IDurableEnvelopeDispatchRoute
{
    private readonly LocalTransportTopology _topology =
        topology ?? throw new ArgumentNullException(nameof(topology));
    private readonly IDurableEnvelopeReceiver _envelopeReceiver =
        envelopeReceiver ?? throw new ArgumentNullException(nameof(envelopeReceiver));

    public string TransportName => "Local";

    public bool CanSend(
        DurableOutboxRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);

        DurableMessageEnvelope envelope = record.Envelope;
        if (envelope.MessageKind == MessageKind.Command)
        {
            return _topology.TryGetCommandBinding(envelope, out _);
        }

        if (envelope.MessageKind == MessageKind.Event)
        {
            return _topology.GetEventSubscriptions(envelope).Count > 0;
        }

        return false;
    }

    public async ValueTask DispatchAsync(
        DurableOutboxRecord record,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(record);

        DurableMessageEnvelope envelope = record.Envelope;
        switch (envelope.MessageKind)
        {
            case MessageKind.Command:
                await SendCommandAsync(envelope, ct);
                return;
            case MessageKind.Event:
                await SendEventAsync(envelope, ct);
                return;
            default:
                throw new NotSupportedException(
                    $"Local transport does not support message kind '{envelope.MessageKind}'.");
        }
    }

    private async ValueTask SendCommandAsync(
        DurableMessageEnvelope envelope,
        CancellationToken ct)
    {
        if (!_topology.TryGetCommandBinding(envelope, out _))
        {
            throw new BondstoneSetupException(
                BondstoneSetupCodes.MissingReceiveBinding,
                $"Local transport has no queue binding for command '{envelope.MessageTypeName}' targeting module '{envelope.TargetModule}'.");
        }

        await _envelopeReceiver.ReceiveCommandAsync(envelope, ct);
    }

    private async ValueTask SendEventAsync(
        DurableMessageEnvelope envelope,
        CancellationToken ct)
    {
        IReadOnlyCollection<LocalEventSubscription> subscriptions =
            _topology.GetEventSubscriptions(envelope);
        if (subscriptions.Count == 0)
        {
            throw new BondstoneSetupException(
                BondstoneSetupCodes.MissingReceiveBinding,
                $"Local transport has no subscriber queue binding for event '{envelope.MessageTypeName}'.");
        }

        foreach (LocalEventSubscription subscription in subscriptions)
        {
            await _envelopeReceiver.ReceiveEventAsync(
                envelope,
                subscription.SubscriberModule,
                subscription.SubscriberIdentity,
                ct);
        }
    }
}
