namespace Bondstone.Transport.ServiceBus.Outbox;

public enum ServiceBusEventDestinationSource
{
    Missing = 0,
    ExplicitQueue = 1,
    ExplicitTopic = 2,
    QueueConvention = 3,
    TopicConvention = 4,
}
