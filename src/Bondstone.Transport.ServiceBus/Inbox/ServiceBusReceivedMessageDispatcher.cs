using Bondstone.Messaging;
using Bondstone.Modules;
using Bondstone.Transport.ServiceBus.Outbox;

namespace Bondstone.Transport.ServiceBus.Inbox;

internal sealed class ServiceBusReceivedMessageDispatcher(
    ServiceBusReceiveTopology receiveTopology,
    IModuleCommandReceivePipeline commandReceivePipeline,
    IModuleEventReceivePipeline eventReceivePipeline)
    : IServiceBusReceivedMessageDispatcher
{
    private readonly ServiceBusReceiveTopology _receiveTopology =
        receiveTopology ?? throw new ArgumentNullException(nameof(receiveTopology));
    private readonly IModuleCommandReceivePipeline _commandReceivePipeline =
        commandReceivePipeline ?? throw new ArgumentNullException(nameof(commandReceivePipeline));
    private readonly IModuleEventReceivePipeline _eventReceivePipeline =
        eventReceivePipeline ?? throw new ArgumentNullException(nameof(eventReceivePipeline));

    public async ValueTask DispatchAsync(
        ServiceBusReceiveSource source,
        ServiceBusTransportMessage message,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(message);

        DurableMessageEnvelope envelope =
            ServiceBusDurableEnvelopeMapper.ReadEnvelope(message.Body);

        switch (envelope.MessageKind)
        {
            case MessageKind.Command:
                await DispatchCommandAsync(source, envelope, ct);
                return;
            case MessageKind.Event:
                await DispatchEventAsync(source, envelope, ct);
                return;
            default:
                throw new NotSupportedException(
                    $"Service Bus receive does not support message kind '{envelope.MessageKind}'.");
        }
    }

    private async ValueTask DispatchCommandAsync(
        ServiceBusReceiveSource source,
        DurableMessageEnvelope envelope,
        CancellationToken ct)
    {
        if (!_receiveTopology.AcceptsCommand(source, envelope))
        {
            throw new InvalidOperationException(
                $"Service Bus receive source '{source.DisplayName}' is not bound to command target module '{envelope.TargetModule}'.");
        }

        await _commandReceivePipeline.HandleOnceAsync(envelope, ct);
    }

    private async ValueTask DispatchEventAsync(
        ServiceBusReceiveSource source,
        DurableMessageEnvelope envelope,
        CancellationToken ct)
    {
        IReadOnlyCollection<ServiceBusEventSubscriptionBinding> subscriptions =
            _receiveTopology.GetEventSubscriptions(source, envelope);
        if (subscriptions.Count == 0)
        {
            throw new InvalidOperationException(
                $"Service Bus receive source '{source.DisplayName}' has no subscriber binding for event '{envelope.MessageTypeName}'.");
        }

        foreach (ServiceBusEventSubscriptionBinding subscription in subscriptions)
        {
            await _eventReceivePipeline.HandleOnceAsync(
                envelope,
                subscription.SubscriberModule,
                subscription.SubscriberIdentity,
                ct);
        }
    }
}
