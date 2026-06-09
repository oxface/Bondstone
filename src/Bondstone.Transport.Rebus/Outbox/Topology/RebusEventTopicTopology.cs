using Bondstone.Utility;

namespace Bondstone.Transport.Rebus.Outbox;

internal sealed class RebusEventTopicTopology
{
    private readonly IReadOnlyDictionary<string, string> _topicNamesByMessageTypeName;
    private readonly Func<string, string>? _topicNameConvention;

    public RebusEventTopicTopology(
        IReadOnlyDictionary<string, string> topicNamesByMessageTypeName,
        Func<string, string>? topicNameConvention = null)
    {
        ArgumentNullException.ThrowIfNull(topicNamesByMessageTypeName);

        _topicNamesByMessageTypeName =
            NormalizeTopicNames(topicNamesByMessageTypeName);
        _topicNameConvention = topicNameConvention;
    }

    public static RebusEventTopicTopology Empty { get; } =
        new(new Dictionary<string, string>());

    public static RebusEventTopicTopology FromConfiguredTopics(
        IReadOnlyDictionary<string, string> topicNamesByMessageTypeName,
        Func<string, string>? topicNameConvention = null)
    {
        return new RebusEventTopicTopology(
            topicNamesByMessageTypeName,
            topicNameConvention);
    }

    public RebusEventTopicDiagnostic DescribeTopic(
        string messageTypeName)
    {
        string normalizedMessageTypeName = messageTypeName.NormalizeRequired(
            nameof(messageTypeName),
            "Message type name");

        if (_topicNamesByMessageTypeName.TryGetValue(
            normalizedMessageTypeName,
            out string? explicitTopicName))
        {
            return new RebusEventTopicDiagnostic(
                normalizedMessageTypeName,
                RebusEventTopicSource.ExplicitRoute,
                explicitTopicName);
        }

        if (_topicNameConvention is not null)
        {
            return new RebusEventTopicDiagnostic(
                normalizedMessageTypeName,
                RebusEventTopicSource.Convention,
                _topicNameConvention(normalizedMessageTypeName));
        }

        return new RebusEventTopicDiagnostic(
            normalizedMessageTypeName,
            RebusEventTopicSource.Missing,
            topicName: null,
            failureReason: $"No Rebus event topic is configured for message type '{normalizedMessageTypeName}'.");
    }

    private static IReadOnlyDictionary<string, string> NormalizeTopicNames(
        IReadOnlyDictionary<string, string> topicNamesByMessageTypeName)
    {
        var normalized = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach ((string messageTypeName, string topicName) in topicNamesByMessageTypeName)
        {
            normalized.Add(
                messageTypeName.NormalizeRequired(
                    nameof(topicNamesByMessageTypeName),
                    "Message type name"),
                topicName.NormalizeRequired(
                    nameof(topicNamesByMessageTypeName),
                    "Rebus event topic name"));
        }

        return normalized;
    }
}
