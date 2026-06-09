using Bondstone.Messaging;
using Bondstone.Utility;

namespace Bondstone.Transport.Rebus.Outbox;

public sealed class RebusEventTopicDiagnostic
{
    public RebusEventTopicDiagnostic(
        string messageTypeName,
        RebusEventTopicSource source,
        string? topicName,
        string? failureReason = null)
    {
        MessageTypeName = messageTypeName.NormalizeRequired(
            nameof(messageTypeName),
            "Message type name");
        Source = source;
        TopicName = topicName?.NormalizeRequired(
            nameof(topicName),
            "Rebus event topic name");
        FailureReason = failureReason;
    }

    public DurableMessageTopologyDiagnosticKind Kind =>
        DurableMessageTopologyDiagnosticKind.EventTopic;

    public string MessageTypeName { get; }

    public RebusEventTopicSource Source { get; }

    public string? TopicName { get; }

    public string? FailureReason { get; }

    public bool HasTopic => TopicName is not null;
}
