namespace Bondstone.Transport.ServiceBus.Outbox;

public enum ServiceBusEventTopicSource
{
    Missing = 0,
    ExplicitTopic = 1,
    TopicConvention = 2,
}
