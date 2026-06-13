using Bondstone.Messaging;
using Bondstone.Modules;
using Bondstone.Transport.RabbitMq.Outbox;

namespace Bondstone.Transport.RabbitMq.Inbox;

internal sealed class RabbitMqReceivedMessageDispatcher(
    RabbitMqReceiveTopology receiveTopology,
    IModuleCommandReceivePipeline commandReceivePipeline,
    IModuleEventReceivePipeline eventReceivePipeline)
    : IRabbitMqReceivedMessageDispatcher
{
    private readonly RabbitMqReceiveTopology _receiveTopology =
        receiveTopology ?? throw new ArgumentNullException(nameof(receiveTopology));
    private readonly IModuleCommandReceivePipeline _commandReceivePipeline =
        commandReceivePipeline ?? throw new ArgumentNullException(nameof(commandReceivePipeline));
    private readonly IModuleEventReceivePipeline _eventReceivePipeline =
        eventReceivePipeline ?? throw new ArgumentNullException(nameof(eventReceivePipeline));

    public async ValueTask DispatchAsync(
        string queueName,
        RabbitMqTransportMessage message,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        DurableMessageEnvelope envelope =
            RabbitMqDurableEnvelopeMapper.ReadEnvelope(message.Body);

        switch (envelope.MessageKind)
        {
            case MessageKind.Command:
                await DispatchCommandAsync(queueName, envelope, ct);
                return;
            case MessageKind.Event:
                await DispatchEventAsync(queueName, envelope, ct);
                return;
            default:
                throw new NotSupportedException(
                    $"RabbitMQ receive does not support message kind '{envelope.MessageKind}'.");
        }
    }

    private async ValueTask DispatchCommandAsync(
        string queueName,
        DurableMessageEnvelope envelope,
        CancellationToken ct)
    {
        if (!_receiveTopology.AcceptsCommand(queueName, envelope))
        {
            throw new InvalidOperationException(
                $"RabbitMQ queue '{queueName}' is not bound to command target module '{envelope.TargetModule}'.");
        }

        await _commandReceivePipeline.HandleOnceAsync(envelope, ct);
    }

    private async ValueTask DispatchEventAsync(
        string queueName,
        DurableMessageEnvelope envelope,
        CancellationToken ct)
    {
        IReadOnlyCollection<RabbitMqEventSubscriptionBinding> subscriptions =
            _receiveTopology.GetEventSubscriptions(queueName, envelope);
        if (subscriptions.Count == 0)
        {
            throw new InvalidOperationException(
                $"RabbitMQ queue '{queueName}' has no subscriber binding for event '{envelope.MessageTypeName}'.");
        }

        foreach (RabbitMqEventSubscriptionBinding subscription in subscriptions)
        {
            await _eventReceivePipeline.HandleOnceAsync(
                envelope,
                subscription.SubscriberModule,
                subscription.SubscriberIdentity,
                ct);
        }
    }
}
