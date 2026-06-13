using Bondstone.Messaging;
using Bondstone.Persistence;

namespace Bondstone.Transport.ServiceBus.Outbox;

internal sealed class ServiceBusEventDestinationResolver(
    ServiceBusEventDestinationTopology topology)
    : IServiceBusOutboxEventDestinationResolver
{
    public ServiceBusEventDestination ResolveDestination(
        DurableOutboxRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);

        if (record.Envelope.MessageKind != MessageKind.Event)
        {
            throw new NotSupportedException(
                "Service Bus event destination resolution supports event envelopes only.");
        }

        ServiceBusEventDestinationDiagnostic diagnostic =
            topology.DescribeDestination(record.Envelope.MessageTypeName);

        return diagnostic.Destination
            ?? throw new InvalidOperationException(diagnostic.FailureReason);
    }
}
