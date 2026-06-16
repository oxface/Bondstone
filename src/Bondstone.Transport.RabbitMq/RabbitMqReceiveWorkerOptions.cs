using Bondstone.Messaging;

namespace Bondstone.Transport.RabbitMq;

public sealed class RabbitMqReceiveWorkerOptions
{
    public string? QueueName { get; set; }

    public DurableEnvelopeReceiveBinding? Binding { get; private set; }

    public bool RequeueOnFailure { get; set; }

    public string? ConsumerTag { get; set; }

    public RabbitMqReceiveWorkerOptions ReceiveCommand()
    {
        Binding = null;
        return this;
    }

    public RabbitMqReceiveWorkerOptions ReceiveEvent(
        string subscriberModule,
        string subscriberIdentity)
    {
        Binding = new DurableEnvelopeReceiveBinding(
            subscriberModule,
            subscriberIdentity);
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
            ConsumerTag);
    }
}
