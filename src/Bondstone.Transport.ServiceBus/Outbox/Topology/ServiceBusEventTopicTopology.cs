using Bondstone.Utility;

namespace Bondstone.Transport.ServiceBus.Outbox;

internal sealed class ServiceBusEventTopicTopology
{
    private readonly IReadOnlyDictionary<string, string> _topicNamesByMessageTypeName;
    private readonly Func<string, string>? _topicNameConvention;

    public ServiceBusEventTopicTopology(
        IReadOnlyDictionary<string, string> topicNamesByMessageTypeName,
        Func<string, string>? topicNameConvention = null)
    {
        ArgumentNullException.ThrowIfNull(topicNamesByMessageTypeName);

        _topicNamesByMessageTypeName = topicNamesByMessageTypeName
            .Select(static entry => new KeyValuePair<string, string>(
                entry.Key.NormalizeRequired("messageTypeName", "Message type name"),
                entry.Value.NormalizeRequired("topicName", "Service Bus topic name")))
            .ToDictionary(
                static entry => entry.Key,
                static entry => entry.Value,
                StringComparer.Ordinal);
        _topicNameConvention = topicNameConvention;
    }

    public ServiceBusEventTopicDiagnostic DescribeTopic(
        string messageTypeName)
    {
        string normalizedMessageTypeName = messageTypeName.NormalizeRequired(
            nameof(messageTypeName),
            "Message type name");

        if (_topicNamesByMessageTypeName.TryGetValue(
            normalizedMessageTypeName,
            out string? topicName))
        {
            return new ServiceBusEventTopicDiagnostic(
                normalizedMessageTypeName,
                ServiceBusEventTopicSource.ExplicitTopic,
                topicName);
        }

        if (_topicNameConvention is not null)
        {
            return new ServiceBusEventTopicDiagnostic(
                normalizedMessageTypeName,
                ServiceBusEventTopicSource.TopicConvention,
                _topicNameConvention(normalizedMessageTypeName).NormalizeRequired(
                    nameof(_topicNameConvention),
                    "Service Bus topic name"));
        }

        return new ServiceBusEventTopicDiagnostic(
            normalizedMessageTypeName,
            ServiceBusEventTopicSource.Missing,
            topicName: null,
            failureReason:
                $"No Service Bus topic is configured for message type '{normalizedMessageTypeName}'.");
    }
}
