using Bondstone.Persistence;

namespace Bondstone.Transport.RabbitMq.Outbox;

public interface IRabbitMqOutboxEventRouteResolver
{
    RabbitMqPublishDestination ResolveDestination(
        DurableOutboxRecord record);
}
