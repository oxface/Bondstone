using Bondstone.Messaging;
using Bondstone.Persistence;

namespace Bondstone.Transport.RabbitMq.Outbox;

internal sealed class RabbitMqEventRouteResolver(
    RabbitMqEventRoutingTopology topology)
    : IRabbitMqOutboxEventRouteResolver
{
    public RabbitMqPublishDestination ResolveDestination(
        DurableOutboxRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);

        if (record.Envelope.MessageKind != MessageKind.Event)
        {
            throw new NotSupportedException(
                "RabbitMQ event route resolution supports event envelopes only.");
        }

        RabbitMqEventRoutingDiagnostic diagnostic =
            topology.DescribeRoute(record.Envelope.MessageTypeName);

        return diagnostic.HasRoute
            ? new RabbitMqPublishDestination(diagnostic.ExchangeName!, diagnostic.RoutingKey!)
            : throw new InvalidOperationException(diagnostic.FailureReason);
    }
}
