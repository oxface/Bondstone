using Bondstone.Persistence;

namespace Bondstone.Transport.ServiceBus.Outbox;

public interface IServiceBusOutboxEventDestinationResolver
{
    ServiceBusEventDestination ResolveDestination(
        DurableOutboxRecord record);
}
