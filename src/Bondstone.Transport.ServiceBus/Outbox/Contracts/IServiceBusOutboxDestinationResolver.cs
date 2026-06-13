using Bondstone.Persistence;

namespace Bondstone.Transport.ServiceBus.Outbox;

public interface IServiceBusOutboxDestinationResolver
{
    string ResolveQueueName(
        DurableOutboxRecord record);
}
