using Bondstone.Messaging;
using Bondstone.Persistence;

namespace Bondstone.Transport.RabbitMq.Outbox;

internal sealed class RabbitMqCommandRouteResolver(
    RabbitMqCommandRoutingTopology topology)
    : IRabbitMqOutboxCommandRouteResolver
{
    public RabbitMqPublishDestination ResolveDestination(
        DurableOutboxRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);

        if (record.Envelope.MessageKind != MessageKind.Command)
        {
            throw new NotSupportedException(
                "RabbitMQ command route resolution supports command envelopes only.");
        }

        RabbitMqCommandRoutingDiagnostic diagnostic =
            topology.DescribeRoute(record.Envelope.TargetModule!);

        return diagnostic.HasRoute
            ? new RabbitMqPublishDestination(diagnostic.ExchangeName!, diagnostic.RoutingKey!)
            : throw new InvalidOperationException(diagnostic.FailureReason);
    }
}
