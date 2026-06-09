using Bondstone.Persistence;

namespace Bondstone.Transport.ServiceBus.Outbox;

public interface IServiceBusOutboxEventTopicResolver
{
    string ResolveTopicName(
        DurableOutboxRecord record);
}
