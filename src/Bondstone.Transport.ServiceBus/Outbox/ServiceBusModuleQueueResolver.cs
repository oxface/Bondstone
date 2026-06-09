using Bondstone.Messaging;
using Bondstone.Persistence;

namespace Bondstone.Transport.ServiceBus.Outbox;

internal sealed class ServiceBusModuleQueueResolver(
    ServiceBusCommandDestinationTopology topology)
    : IServiceBusOutboxDestinationResolver
{
    public string ResolveQueueName(
        DurableOutboxRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);

        if (record.Envelope.MessageKind != MessageKind.Command)
        {
            throw new NotSupportedException(
                "Service Bus command queue resolution supports command envelopes only.");
        }

        ServiceBusCommandDestinationDiagnostic diagnostic =
            topology.DescribeDestination(record.Envelope.TargetModule!);

        return diagnostic.QueueName
            ?? throw new InvalidOperationException(diagnostic.FailureReason);
    }
}
