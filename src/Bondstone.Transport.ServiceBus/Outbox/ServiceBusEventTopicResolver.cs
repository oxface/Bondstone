using Bondstone.Messaging;
using Bondstone.Persistence;

namespace Bondstone.Transport.ServiceBus.Outbox;

internal sealed class ServiceBusEventTopicResolver(
    ServiceBusEventTopicTopology topology)
    : IServiceBusOutboxEventTopicResolver
{
    public string ResolveTopicName(
        DurableOutboxRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);

        if (record.Envelope.MessageKind != MessageKind.Event)
        {
            throw new NotSupportedException(
                "Service Bus event topic resolution supports event envelopes only.");
        }

        ServiceBusEventTopicDiagnostic diagnostic =
            topology.DescribeTopic(record.Envelope.MessageTypeName);

        return diagnostic.TopicName
            ?? throw new InvalidOperationException(diagnostic.FailureReason);
    }
}
