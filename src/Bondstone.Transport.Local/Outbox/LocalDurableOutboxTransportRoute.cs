using Bondstone.Messaging;
using Bondstone.Modules;
using Bondstone.Persistence;

namespace Bondstone.Transport.Local.Outbox;

internal sealed class LocalDurableOutboxTransportRoute(
    LocalTransportTopology topology,
    IModuleCommandReceivePipeline commandReceivePipeline,
    IModuleEventReceivePipeline eventReceivePipeline)
    : IDurableOutboxTransportRoute
{
    private readonly LocalTransportTopology _topology =
        topology ?? throw new ArgumentNullException(nameof(topology));
    private readonly IModuleCommandReceivePipeline _commandReceivePipeline =
        commandReceivePipeline ?? throw new ArgumentNullException(nameof(commandReceivePipeline));
    private readonly IModuleEventReceivePipeline _eventReceivePipeline =
        eventReceivePipeline ?? throw new ArgumentNullException(nameof(eventReceivePipeline));

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

    public async ValueTask SendAsync(
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
            throw new InvalidOperationException(
                $"Local transport has no queue binding for command '{envelope.MessageTypeName}' targeting module '{envelope.TargetModule}'.");
        }

        await _commandReceivePipeline.HandleOnceAsync(envelope, ct);
    }

    private async ValueTask SendEventAsync(
        DurableMessageEnvelope envelope,
        CancellationToken ct)
    {
        IReadOnlyCollection<LocalEventSubscription> subscriptions =
            _topology.GetEventSubscriptions(envelope);
        if (subscriptions.Count == 0)
        {
            throw new InvalidOperationException(
                $"Local transport has no subscriber queue binding for event '{envelope.MessageTypeName}'.");
        }

        foreach (LocalEventSubscription subscription in subscriptions)
        {
            await _eventReceivePipeline.HandleOnceAsync(
                envelope,
                subscription.SubscriberModule,
                subscription.SubscriberIdentity,
                ct);
        }
    }
}
