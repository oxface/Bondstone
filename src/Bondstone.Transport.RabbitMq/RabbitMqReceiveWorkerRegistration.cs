using Bondstone.Messaging;

namespace Bondstone.Transport.RabbitMq;

internal sealed record RabbitMqReceiveWorkerRegistration(
    string QueueName,
    DurableEnvelopeReceiveBinding? Binding,
    bool AutoAck,
    bool RequeueOnFailure,
    string? ConsumerTag);
