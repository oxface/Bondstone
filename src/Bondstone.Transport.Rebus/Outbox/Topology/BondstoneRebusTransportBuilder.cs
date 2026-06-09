using Bondstone.Transport.Rebus.Inbox;
using Bondstone.Utility;

namespace Bondstone.Transport.Rebus.Outbox;

public sealed class BondstoneRebusTransportBuilder
{
    private readonly Dictionary<string, string> _destinationAddressesByTargetModule =
        new(StringComparer.Ordinal);
    private readonly Dictionary<string, string> _topicNamesByMessageTypeName =
        new(StringComparer.Ordinal);
    private readonly Dictionary<string, HashSet<string>> _acceptedModuleNamesByEndpointName =
        new(StringComparer.Ordinal);
    private readonly Dictionary<string, string> _endpointNamesByAcceptedModuleName =
        new(StringComparer.Ordinal);
    private readonly List<RebusEventSubscriptionBinding> _eventSubscriptionBindings = [];
    private Func<string, string>? _moduleQueueNameConvention;
    private Func<string, string>? _eventTopicNameConvention;

    internal IReadOnlyCollection<RebusModuleReceiveEndpointBinding> ReceiveEndpointBindings =>
        _acceptedModuleNamesByEndpointName
            .Where(static entry => entry.Value.Count > 0)
            .Select(static entry => new RebusModuleReceiveEndpointBinding(
                entry.Key,
                entry.Value))
            .ToArray();

    internal IReadOnlyCollection<RebusEventSubscriptionBinding> EventSubscriptionBindings =>
        _eventSubscriptionBindings.ToArray();

    internal RebusCommandDestinationTopology CommandDestinationTopology =>
        new(
            _destinationAddressesByTargetModule,
            _endpointNamesByAcceptedModuleName,
            _moduleQueueNameConvention);

    internal RebusEventTopicTopology EventTopicTopology =>
        new(
            _topicNamesByMessageTypeName,
            _eventTopicNameConvention);

    public BondstoneRebusModuleRouteBuilder RouteModule(string targetModule)
    {
        string normalizedTargetModule = targetModule.NormalizeRequired(
            nameof(targetModule),
            "Target module");

        return new BondstoneRebusModuleRouteBuilder(this, normalizedTargetModule);
    }

    public BondstoneRebusTransportBuilder UseModuleQueueConvention()
    {
        return UseModuleQueueConvention(static moduleName => $"{moduleName}-commands");
    }

    public BondstoneRebusTransportBuilder UseModuleQueueConvention(
        Func<string, string> queueNameFactory)
    {
        ArgumentNullException.ThrowIfNull(queueNameFactory);

        _moduleQueueNameConvention = moduleName =>
            queueNameFactory(moduleName).NormalizeRequired(
                nameof(queueNameFactory),
                "Rebus module queue name");

        return this;
    }

    public BondstoneRebusTransportBuilder UseEventTopicConvention()
    {
        return UseEventTopicConvention(static messageTypeName => messageTypeName);
    }

    public BondstoneRebusTransportBuilder UseEventTopicConvention(
        Func<string, string> topicNameFactory)
    {
        ArgumentNullException.ThrowIfNull(topicNameFactory);

        _eventTopicNameConvention = messageTypeName =>
            topicNameFactory(messageTypeName).NormalizeRequired(
                nameof(topicNameFactory),
                "Rebus event topic name");

        return this;
    }

    public BondstoneRebusEventRouteBuilder RouteEvent(string messageTypeName)
    {
        string normalizedMessageTypeName = messageTypeName.NormalizeRequired(
            nameof(messageTypeName),
            "Message type name");

        return new BondstoneRebusEventRouteBuilder(this, normalizedMessageTypeName);
    }

    public BondstoneRebusTransportBuilder ReceiveModule(string moduleName)
    {
        string normalizedModuleName = moduleName.NormalizeRequired(
            nameof(moduleName),
            "Module name");

        if (_moduleQueueNameConvention is null)
        {
            throw new InvalidOperationException(
                $"Receiving module '{normalizedModuleName}' by convention requires {nameof(UseModuleQueueConvention)} to be configured first.");
        }

        string endpointName = _moduleQueueNameConvention(normalizedModuleName);
        AcceptModuleOnEndpoint(endpointName, normalizedModuleName);
        return this;
    }

    public BondstoneRebusReceiveEndpointBuilder ReceiveEndpoint(string endpointName)
    {
        string normalizedEndpointName = endpointName.NormalizeRequired(
            nameof(endpointName),
            "Rebus receive endpoint name");

        if (!_acceptedModuleNamesByEndpointName.ContainsKey(normalizedEndpointName))
        {
            _acceptedModuleNamesByEndpointName.Add(
                normalizedEndpointName,
                new HashSet<string>(StringComparer.Ordinal));
        }

        return new BondstoneRebusReceiveEndpointBuilder(this, normalizedEndpointName);
    }

    internal void SetModuleDestinationAddress(
        string targetModule,
        string destinationAddress)
    {
        string normalizedDestinationAddress = destinationAddress.NormalizeRequired(
            nameof(destinationAddress),
            "Rebus destination address");

        if (_destinationAddressesByTargetModule.TryGetValue(
            targetModule,
            out string? existingDestinationAddress))
        {
            if (existingDestinationAddress == normalizedDestinationAddress)
            {
                return;
            }

            throw new InvalidOperationException(
                $"Target module '{targetModule}' already routes to Rebus destination address '{existingDestinationAddress}'.");
        }

        _destinationAddressesByTargetModule.Add(
            targetModule,
            normalizedDestinationAddress);
    }

    internal void SetEventTopicName(
        string messageTypeName,
        string topicName)
    {
        string normalizedTopicName = topicName.NormalizeRequired(
            nameof(topicName),
            "Rebus event topic name");

        if (_topicNamesByMessageTypeName.TryGetValue(
            messageTypeName,
            out string? existingTopicName))
        {
            if (existingTopicName == normalizedTopicName)
            {
                return;
            }

            throw new InvalidOperationException(
                $"Message type '{messageTypeName}' already publishes to Rebus event topic '{existingTopicName}'.");
        }

        _topicNamesByMessageTypeName.Add(
            messageTypeName,
            normalizedTopicName);
    }

    internal void AcceptModuleOnEndpoint(
        string endpointName,
        string moduleName)
    {
        string normalizedModuleName = moduleName.NormalizeRequired(
            nameof(moduleName),
            "Module name");

        if (_endpointNamesByAcceptedModuleName.TryGetValue(
            normalizedModuleName,
            out string? existingEndpointName))
        {
            if (existingEndpointName == endpointName)
            {
                return;
            }

            throw new InvalidOperationException(
                $"Module '{normalizedModuleName}' is already accepted by Rebus receive endpoint '{existingEndpointName}'.");
        }

        if (!_acceptedModuleNamesByEndpointName.TryGetValue(
            endpointName,
            out HashSet<string>? moduleNames))
        {
            moduleNames = new HashSet<string>(StringComparer.Ordinal);
            _acceptedModuleNamesByEndpointName.Add(endpointName, moduleNames);
        }

        moduleNames.Add(normalizedModuleName);
        _endpointNamesByAcceptedModuleName.Add(normalizedModuleName, endpointName);
    }

    internal void SubscribeEventOnEndpoint(
        string endpointName,
        string messageTypeName,
        string subscriberModule,
        string subscriberIdentity)
    {
        var subscription = new RebusEventSubscriptionBinding(
            endpointName,
            messageTypeName,
            subscriberModule,
            subscriberIdentity);

        bool exists = _eventSubscriptionBindings.Any(existing =>
            StringComparer.Ordinal.Equals(existing.EndpointName, subscription.EndpointName)
            && StringComparer.Ordinal.Equals(existing.MessageTypeName, subscription.MessageTypeName)
            && StringComparer.Ordinal.Equals(existing.SubscriberModule, subscription.SubscriberModule)
            && StringComparer.Ordinal.Equals(existing.SubscriberIdentity, subscription.SubscriberIdentity));

        if (exists)
        {
            return;
        }

        _eventSubscriptionBindings.Add(subscription);
    }
}
