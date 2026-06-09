using Bondstone.Persistence;
using Bondstone.Utility;

namespace Bondstone.Transport.Rebus.Outbox;

public interface IRebusOutboxEventTopicResolver
{
    string ResolveTopic(DurableOutboxRecord record);
}

public sealed class RebusEventTopicResolver : IRebusOutboxEventTopicResolver
{
    private readonly RebusEventTopicTopology _topology;

    public RebusEventTopicResolver(
        IReadOnlyDictionary<string, string> topicNamesByMessageTypeName,
        Func<string, string>? topicNameConvention = null)
        : this(RebusEventTopicTopology.FromConfiguredTopics(
            topicNamesByMessageTypeName,
            topicNameConvention))
    {
    }

    internal RebusEventTopicResolver(
        RebusEventTopicTopology topology)
    {
        _topology = topology ?? throw new ArgumentNullException(nameof(topology));
    }

    public string ResolveTopic(DurableOutboxRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);

        string messageTypeName = record.Envelope.MessageTypeName.NormalizeRequired(
            nameof(record.Envelope.MessageTypeName),
            "Message type name");

        RebusEventTopicDiagnostic diagnostic =
            _topology.DescribeTopic(messageTypeName);

        return diagnostic.TopicName
            ?? throw new InvalidOperationException(diagnostic.FailureReason);
    }
}
