using Bondstone.Transport.RabbitMq.Inbox;
using Bondstone.Messaging;
using Bondstone.Utility;

namespace Bondstone.Transport.RabbitMq.Outbox;

public sealed class BondstoneRabbitMqTransportBuilder
{
    private readonly Dictionary<string, RabbitMqReceiveQueueRegistration> _receiveQueues =
        new(StringComparer.Ordinal);
    private RabbitMqReceiveWorkerRegistration? _receiveWorkerRegistration;
    private Func<DurableMessageEnvelope, RabbitMqPublishDestination?>? _commandDestinationResolver;
    private Func<DurableMessageEnvelope, RabbitMqPublishDestination?>? _eventDestinationResolver;

    internal RabbitMqEnvelopeDestinationResolver DestinationResolver =>
        new(
            _commandDestinationResolver,
            _eventDestinationResolver);

    internal RabbitMqReceiveTopology ReceiveTopology =>
        new(_receiveQueues);

    internal RabbitMqReceiveWorkerRegistration? ReceiveWorkerRegistration =>
        _receiveWorkerRegistration;

    public BondstoneRabbitMqTransportBuilder DispatchCommandsTo(
        Func<DurableMessageEnvelope, RabbitMqPublishDestination?> resolveDestination)
    {
        ArgumentNullException.ThrowIfNull(resolveDestination);

        _commandDestinationResolver = resolveDestination;
        return this;
    }

    public BondstoneRabbitMqTransportBuilder DispatchEventsTo(
        Func<DurableMessageEnvelope, RabbitMqPublishDestination?> resolveDestination)
    {
        ArgumentNullException.ThrowIfNull(resolveDestination);

        _eventDestinationResolver = resolveDestination;
        return this;
    }

    public BondstoneRabbitMqReceiveQueueBuilder ReceiveQueue(
        string queueName)
    {
        string normalizedQueueName = queueName.NormalizeRequired(
            nameof(queueName),
            "RabbitMQ queue name");

        EnsureReceiveQueue(normalizedQueueName);

        return new BondstoneRabbitMqReceiveQueueBuilder(
            this,
            normalizedQueueName);
    }

    public BondstoneRabbitMqTransportBuilder UseReceiveWorker(
        Action<RabbitMqReceiveWorkerOptions>? configureOptions = null)
    {
        _receiveWorkerRegistration = new RabbitMqReceiveWorkerRegistration(configureOptions);

        return this;
    }

    internal void AddReceiveQueueAcceptedModule(
        string queueName,
        string moduleName)
    {
        RabbitMqReceiveQueueRegistration queue = EnsureReceiveQueue(queueName);
        queue.AddAcceptedModule(moduleName);
    }

    internal void AddReceiveQueueEventSubscription(
        string queueName,
        string messageTypeName,
        string subscriberModule,
        string subscriberIdentity)
    {
        RabbitMqReceiveQueueRegistration queue = EnsureReceiveQueue(queueName);
        queue.AddEventSubscription(
            messageTypeName,
            subscriberModule,
            subscriberIdentity);
    }

    private RabbitMqReceiveQueueRegistration EnsureReceiveQueue(
        string queueName)
    {
        string normalizedQueueName = queueName.NormalizeRequired(
            nameof(queueName),
            "RabbitMQ queue name");

        if (_receiveQueues.TryGetValue(
            normalizedQueueName,
            out RabbitMqReceiveQueueRegistration? queue))
        {
            return queue;
        }

        var createdQueue = new RabbitMqReceiveQueueRegistration(normalizedQueueName);
        _receiveQueues.Add(normalizedQueueName, createdQueue);

        return createdQueue;
    }
}
