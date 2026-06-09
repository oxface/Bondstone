using Bondstone.Messaging;
using Bondstone.Utility;

namespace Bondstone.Transport.ServiceBus.Outbox;

public sealed class ServiceBusEventTopicDiagnostic
{
    public ServiceBusEventTopicDiagnostic(
        string messageTypeName,
        ServiceBusEventTopicSource source,
        string? topicName,
        string? failureReason = null)
    {
        MessageTypeName = messageTypeName.NormalizeRequired(
            nameof(messageTypeName),
            "Message type name");
        Source = source;
        TopicName = topicName?.NormalizeRequired(
            nameof(topicName),
            "Service Bus topic name");
        FailureReason = failureReason;
    }

    public DurableMessageTopologyDiagnosticKind Kind =>
        DurableMessageTopologyDiagnosticKind.EventTopic;

    public string MessageTypeName { get; }

    public ServiceBusEventTopicSource Source { get; }

    public string? TopicName { get; }

    public string? FailureReason { get; }

    public bool HasTopic => TopicName is not null;
}
