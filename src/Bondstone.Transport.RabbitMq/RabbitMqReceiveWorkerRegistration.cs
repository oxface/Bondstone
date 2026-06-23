using Bondstone.Messaging;

namespace Bondstone.Transport.RabbitMq;

internal sealed record RabbitMqReceiveWorkerRegistration(
    string QueueName,
    DurableEnvelopeReceiveBinding? Binding,
    bool RequeueOnFailure,
    string? ConsumerTag,
    RabbitMqReceiveWorkerMode ReceiveMode,
    string SourceTransportName);

internal enum RabbitMqReceiveWorkerMode
{
    DirectReceive,
    DurableIncomingInboxIngestion,
}
