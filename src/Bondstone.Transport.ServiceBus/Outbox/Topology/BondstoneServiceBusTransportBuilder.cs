using Bondstone.Utility;

namespace Bondstone.Transport.ServiceBus.Outbox;

public sealed class BondstoneServiceBusTransportBuilder
{
    private readonly Dictionary<string, string> _queueNamesByTargetModule =
        new(StringComparer.Ordinal);
    private readonly Dictionary<string, string> _topicNamesByMessageTypeName =
        new(StringComparer.Ordinal);
    private Func<string, string>? _queueNameConvention;
    private Func<string, string>? _topicNameConvention;

    internal ServiceBusCommandDestinationTopology CommandDestinationTopology =>
        new(_queueNamesByTargetModule, _queueNameConvention);

    internal ServiceBusEventTopicTopology EventTopicTopology =>
        new(_topicNamesByMessageTypeName, _topicNameConvention);

    public BondstoneServiceBusModuleRouteBuilder RouteModule(
        string targetModule)
    {
        string normalizedTargetModule = targetModule.NormalizeRequired(
            nameof(targetModule),
            "Target module");

        return new BondstoneServiceBusModuleRouteBuilder(
            this,
            normalizedTargetModule);
    }

    public BondstoneServiceBusTransportBuilder UseModuleQueueConvention()
    {
        return UseModuleQueueConvention(static moduleName => $"{moduleName}-commands");
    }

    public BondstoneServiceBusTransportBuilder UseModuleQueueConvention(
        Func<string, string> queueNameFactory)
    {
        ArgumentNullException.ThrowIfNull(queueNameFactory);

        _queueNameConvention = moduleName =>
            queueNameFactory(moduleName).NormalizeRequired(
                nameof(queueNameFactory),
                "Service Bus queue name");

        return this;
    }

    public BondstoneServiceBusEventRouteBuilder RouteEvent(
        string messageTypeName)
    {
        string normalizedMessageTypeName = messageTypeName.NormalizeRequired(
            nameof(messageTypeName),
            "Message type name");

        return new BondstoneServiceBusEventRouteBuilder(
            this,
            normalizedMessageTypeName);
    }

    public BondstoneServiceBusTransportBuilder UseEventTopicConvention()
    {
        return UseEventTopicConvention(static messageTypeName => messageTypeName);
    }

    public BondstoneServiceBusTransportBuilder UseEventTopicConvention(
        Func<string, string> topicNameFactory)
    {
        ArgumentNullException.ThrowIfNull(topicNameFactory);

        _topicNameConvention = messageTypeName =>
            topicNameFactory(messageTypeName).NormalizeRequired(
                nameof(topicNameFactory),
                "Service Bus topic name");

        return this;
    }

    internal void SetModuleQueueName(
        string targetModule,
        string queueName)
    {
        string normalizedQueueName = queueName.NormalizeRequired(
            nameof(queueName),
            "Service Bus queue name");

        if (_queueNamesByTargetModule.TryGetValue(
            targetModule,
            out string? existingQueueName))
        {
            if (existingQueueName == normalizedQueueName)
            {
                return;
            }

            throw new InvalidOperationException(
                $"Target module '{targetModule}' already routes to Service Bus queue '{existingQueueName}'.");
        }

        _queueNamesByTargetModule.Add(targetModule, normalizedQueueName);
    }

    internal void SetEventTopicName(
        string messageTypeName,
        string topicName)
    {
        string normalizedTopicName = topicName.NormalizeRequired(
            nameof(topicName),
            "Service Bus topic name");

        if (_topicNamesByMessageTypeName.TryGetValue(
            messageTypeName,
            out string? existingTopicName))
        {
            if (existingTopicName == normalizedTopicName)
            {
                return;
            }

            throw new InvalidOperationException(
                $"Message type '{messageTypeName}' already publishes to Service Bus topic '{existingTopicName}'.");
        }

        _topicNamesByMessageTypeName.Add(messageTypeName, normalizedTopicName);
    }
}
