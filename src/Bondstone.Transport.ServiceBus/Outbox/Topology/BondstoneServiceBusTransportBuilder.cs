using Bondstone.Transport.ServiceBus.Inbox;
using Bondstone.Utility;

namespace Bondstone.Transport.ServiceBus.Outbox;

public sealed class BondstoneServiceBusTransportBuilder
{
    private readonly Dictionary<string, string> _queueNamesByTargetModule =
        new(StringComparer.Ordinal);
    private readonly Dictionary<string, ServiceBusEventDestination> _eventDestinationsByMessageTypeName =
        new(StringComparer.Ordinal);
    private readonly Dictionary<string, ServiceBusReceiveSourceRegistration> _receiveSources =
        new(StringComparer.Ordinal);
    private Func<string, string>? _queueNameConvention;
    private ServiceBusEventDestinationConvention? _eventDestinationConvention;

    internal ServiceBusCommandDestinationTopology CommandDestinationTopology =>
        new(_queueNamesByTargetModule, _queueNameConvention);

    internal ServiceBusEventDestinationTopology EventDestinationTopology =>
        new(_eventDestinationsByMessageTypeName, _eventDestinationConvention);

    internal ServiceBusReceiveTopology ReceiveTopology =>
        new(_receiveSources);

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

    public BondstoneServiceBusReceiveSourceBuilder ReceiveQueue(
        string queueName)
    {
        ServiceBusReceiveSource source = ServiceBusReceiveSource.ForQueue(queueName);
        EnsureReceiveSource(source);

        return new BondstoneServiceBusReceiveSourceBuilder(this, source);
    }

    public BondstoneServiceBusReceiveSourceBuilder ReceiveSubscription(
        string topicName,
        string subscriptionName)
    {
        ServiceBusReceiveSource source = ServiceBusReceiveSource.ForSubscription(
            topicName,
            subscriptionName);
        EnsureReceiveSource(source);

        return new BondstoneServiceBusReceiveSourceBuilder(this, source);
    }

    public BondstoneServiceBusTransportBuilder UseEventTopicConvention()
    {
        return UseEventTopicConvention(static messageTypeName => messageTypeName);
    }

    public BondstoneServiceBusTransportBuilder UseEventTopicConvention(
        Func<string, string> topicNameFactory)
    {
        ArgumentNullException.ThrowIfNull(topicNameFactory);

        _eventDestinationConvention = new ServiceBusEventDestinationConvention(
            ServiceBusEventDestinationKind.Topic,
            messageTypeName => topicNameFactory(messageTypeName).NormalizeRequired(
                nameof(topicNameFactory),
                "Service Bus topic name"));

        return this;
    }

    public BondstoneServiceBusTransportBuilder UseEventQueueConvention()
    {
        return UseEventQueueConvention(static messageTypeName => messageTypeName);
    }

    public BondstoneServiceBusTransportBuilder UseEventQueueConvention(
        Func<string, string> queueNameFactory)
    {
        ArgumentNullException.ThrowIfNull(queueNameFactory);

        _eventDestinationConvention = new ServiceBusEventDestinationConvention(
            ServiceBusEventDestinationKind.Queue,
            messageTypeName => queueNameFactory(messageTypeName).NormalizeRequired(
                nameof(queueNameFactory),
                "Service Bus queue name"));

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

    internal void SetEventDestination(
        string messageTypeName,
        ServiceBusEventDestinationKind kind,
        string entityName)
    {
        var destination = new ServiceBusEventDestination(kind, entityName);

        if (_eventDestinationsByMessageTypeName.TryGetValue(
            messageTypeName,
            out ServiceBusEventDestination? existingDestination))
        {
            if (existingDestination.Kind == destination.Kind
                && existingDestination.EntityName == destination.EntityName)
            {
                return;
            }

            throw new InvalidOperationException(
                $"Message type '{messageTypeName}' already publishes to Service Bus {existingDestination.Kind.ToString().ToLowerInvariant()} '{existingDestination.EntityName}'.");
        }

        _eventDestinationsByMessageTypeName.Add(messageTypeName, destination);
    }

    internal void AddReceiveSourceAcceptedModule(
        ServiceBusReceiveSource source,
        string moduleName)
    {
        ServiceBusReceiveSourceRegistration registration = EnsureReceiveSource(source);
        registration.AddAcceptedModule(moduleName);
    }

    internal void AddReceiveSourceEventSubscription(
        ServiceBusReceiveSource source,
        string messageTypeName,
        string subscriberModule,
        string subscriberIdentity)
    {
        ServiceBusReceiveSourceRegistration registration = EnsureReceiveSource(source);
        registration.AddEventSubscription(
            messageTypeName,
            subscriberModule,
            subscriberIdentity);
    }

    private ServiceBusReceiveSourceRegistration EnsureReceiveSource(
        ServiceBusReceiveSource source)
    {
        ArgumentNullException.ThrowIfNull(source);

        if (_receiveSources.TryGetValue(
            source.Key,
            out ServiceBusReceiveSourceRegistration? registration))
        {
            return registration;
        }

        var createdRegistration = new ServiceBusReceiveSourceRegistration(source);
        _receiveSources.Add(source.Key, createdRegistration);

        return createdRegistration;
    }
}
