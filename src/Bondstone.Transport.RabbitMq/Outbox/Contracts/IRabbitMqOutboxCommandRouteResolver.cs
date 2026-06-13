using Bondstone.Persistence;

namespace Bondstone.Transport.RabbitMq.Outbox;

public interface IRabbitMqOutboxCommandRouteResolver
{
    RabbitMqPublishDestination ResolveDestination(
        DurableOutboxRecord record);
}
