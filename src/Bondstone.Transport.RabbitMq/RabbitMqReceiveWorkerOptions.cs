using Bondstone.Messaging;

namespace Bondstone.Transport.RabbitMq;

public sealed class RabbitMqReceiveWorkerOptions
{
    private RabbitMqReceiveWorkerMode _receiveMode =
        RabbitMqReceiveWorkerMode.DirectReceive;

    public string? QueueName { get; set; }

    public DurableEnvelopeReceiveBinding? Binding { get; private set; }

    public bool RequeueOnFailure { get; set; }

    public string? ConsumerTag { get; set; }

    public string? SourceTransportName { get; set; }

    public RabbitMqReceiveWorkerOptions ReceiveCommand()
    {
        Binding = null;
        _receiveMode = RabbitMqReceiveWorkerMode.DirectReceive;
        return this;
    }

    public RabbitMqReceiveWorkerOptions ReceiveEvent(
        string subscriberModule,
        string subscriberIdentity)
    {
        Binding = new DurableEnvelopeReceiveBinding(
            subscriberModule,
            subscriberIdentity);
        _receiveMode = RabbitMqReceiveWorkerMode.DirectReceive;
        return this;
    }

    public RabbitMqReceiveWorkerOptions IngestCommandToDurableIncomingInbox()
    {
        Binding = null;
        _receiveMode = RabbitMqReceiveWorkerMode.DurableIncomingInboxIngestion;
        return this;
    }

    public RabbitMqReceiveWorkerOptions IngestEventToDurableIncomingInbox(
        string subscriberModule,
        string subscriberIdentity)
    {
        Binding = new DurableEnvelopeReceiveBinding(
            subscriberModule,
            subscriberIdentity);
        _receiveMode = RabbitMqReceiveWorkerMode.DurableIncomingInboxIngestion;
        return this;
    }

    internal RabbitMqReceiveWorkerRegistration ToRegistration()
    {
        if (string.IsNullOrWhiteSpace(QueueName))
        {
            throw new InvalidOperationException(
                "RabbitMQ receive worker requires QueueName.");
        }

        return new RabbitMqReceiveWorkerRegistration(
            QueueName,
            Binding,
            RequeueOnFailure,
            ConsumerTag,
            _receiveMode,
            string.IsNullOrWhiteSpace(SourceTransportName)
                ? $"rabbitmq:{QueueName}"
                : SourceTransportName.Trim());
    }
}
