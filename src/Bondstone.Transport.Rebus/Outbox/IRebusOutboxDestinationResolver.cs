using Bondstone.Persistence;

namespace Bondstone.Transport.Rebus.Outbox;

public interface IRebusOutboxDestinationResolver
{
    string ResolveDestinationAddress(DurableOutboxRecord record);
}
