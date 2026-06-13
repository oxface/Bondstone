namespace Bondstone.Transport.ServiceBus.Outbox;

public enum ServiceBusCommandDestinationSource
{
    Missing = 0,
    ExplicitQueue = 1,
    QueueConvention = 2,
}
