using Bondstone.Transport.RabbitMq.Inbox;
using Bondstone.Utility;

namespace Bondstone.Transport.RabbitMq.Outbox;

public sealed class BondstoneRabbitMqTransportBuilder
{
    private readonly Dictionary<string, string> _routingKeysByTargetModule =
        new(StringComparer.Ordinal);
    private readonly Dictionary<string, RabbitMqEventRouteRegistration> _eventRoutesByMessageTypeName =
        new(StringComparer.Ordinal);
    private readonly Dictionary<string, RabbitMqReceiveQueueRegistration> _receiveQueues =
        new(StringComparer.Ordinal);
    private string? _commandExchangeName;
    private string? _eventExchangeName;
    private Func<string, string>? _commandRoutingKeyConvention;
    private RabbitMqEventDestinationConvention? _eventDestinationConvention;

    internal RabbitMqCommandRoutingTopology CommandRoutingTopology =>
        new(
            _commandExchangeName,
            _routingKeysByTargetModule,
            _commandRoutingKeyConvention);

    internal RabbitMqEventRoutingTopology EventRoutingTopology =>
        new(
            _eventExchangeName,
            _eventRoutesByMessageTypeName,
            _eventDestinationConvention);

    internal RabbitMqReceiveTopology ReceiveTopology =>
        new(_receiveQueues);

    public BondstoneRabbitMqTransportBuilder UseCommandExchange(
        string exchangeName)
    {
        _commandExchangeName = exchangeName.NormalizeRequired(
            nameof(exchangeName),
            "RabbitMQ command exchange name");

        return this;
    }

    public BondstoneRabbitMqTransportBuilder UseEventExchange(
        string exchangeName)
    {
        _eventExchangeName = exchangeName.NormalizeRequired(
            nameof(exchangeName),
            "RabbitMQ event exchange name");

        return this;
    }

    public BondstoneRabbitMqModuleRouteBuilder RouteModule(
        string targetModule)
    {
        string normalizedTargetModule = targetModule.NormalizeRequired(
            nameof(targetModule),
            "Target module");

        return new BondstoneRabbitMqModuleRouteBuilder(
            this,
            normalizedTargetModule);
    }

    public BondstoneRabbitMqTransportBuilder UseModuleRoutingKeyConvention()
    {
        return UseModuleRoutingKeyConvention(static moduleName => $"{moduleName}.commands");
    }

    public BondstoneRabbitMqTransportBuilder UseModuleRoutingKeyConvention(
        Func<string, string> routingKeyFactory)
    {
        ArgumentNullException.ThrowIfNull(routingKeyFactory);

        _commandRoutingKeyConvention = moduleName =>
            routingKeyFactory(moduleName).NormalizeRequired(
                nameof(routingKeyFactory),
                "RabbitMQ routing key");

        return this;
    }

    public BondstoneRabbitMqEventRouteBuilder RouteEvent(
        string messageTypeName)
    {
        string normalizedMessageTypeName = messageTypeName.NormalizeRequired(
            nameof(messageTypeName),
            "Message type name");

        return new BondstoneRabbitMqEventRouteBuilder(
            this,
            normalizedMessageTypeName);
    }

    public BondstoneRabbitMqReceiveQueueBuilder ReceiveQueue(
        string queueName)
    {
        string normalizedQueueName = queueName.NormalizeRequired(
            nameof(queueName),
            "RabbitMQ queue name");

        EnsureReceiveQueue(normalizedQueueName);

        return new BondstoneRabbitMqReceiveQueueBuilder(
            this,
            normalizedQueueName);
    }

    public BondstoneRabbitMqTransportBuilder UseEventRoutingKeyConvention()
    {
        return UseEventRoutingKeyConvention(static messageTypeName => messageTypeName);
    }

    public BondstoneRabbitMqTransportBuilder UseEventRoutingKeyConvention(
        Func<string, string> routingKeyFactory)
    {
        ArgumentNullException.ThrowIfNull(routingKeyFactory);

        _eventDestinationConvention = new RabbitMqEventDestinationConvention(
            RabbitMqPublishDestinationKind.ExchangeRoute,
            messageTypeName => routingKeyFactory(messageTypeName).NormalizeRequired(
                nameof(routingKeyFactory),
                "RabbitMQ routing key"));

        return this;
    }

    public BondstoneRabbitMqTransportBuilder UseEventQueueConvention()
    {
        return UseEventQueueConvention(static messageTypeName => messageTypeName);
    }

    public BondstoneRabbitMqTransportBuilder UseEventQueueConvention(
        Func<string, string> queueNameFactory)
    {
        ArgumentNullException.ThrowIfNull(queueNameFactory);

        _eventDestinationConvention = new RabbitMqEventDestinationConvention(
            RabbitMqPublishDestinationKind.Queue,
            messageTypeName => queueNameFactory(messageTypeName).NormalizeRequired(
                nameof(queueNameFactory),
                "RabbitMQ queue name"));

        return this;
    }

    internal void SetModuleRoutingKey(
        string targetModule,
        string routingKey)
    {
        string normalizedRoutingKey = routingKey.NormalizeRequired(
            nameof(routingKey),
            "RabbitMQ routing key");

        if (_routingKeysByTargetModule.TryGetValue(
            targetModule,
            out string? existingRoutingKey))
        {
            if (existingRoutingKey == normalizedRoutingKey)
            {
                return;
            }

            throw new InvalidOperationException(
                $"Target module '{targetModule}' already routes to RabbitMQ routing key '{existingRoutingKey}'.");
        }

        _routingKeysByTargetModule.Add(targetModule, normalizedRoutingKey);
    }

    internal void SetEventRoutingKey(
        string messageTypeName,
        string routingKey)
    {
        string normalizedRoutingKey = routingKey.NormalizeRequired(
            nameof(routingKey),
            "RabbitMQ routing key");

        SetEventRoute(
            messageTypeName,
            new RabbitMqEventRouteRegistration(
                RabbitMqPublishDestinationKind.ExchangeRoute,
                normalizedRoutingKey));
    }

    internal void SetEventQueue(
        string messageTypeName,
        string queueName)
    {
        SetEventRoute(
            messageTypeName,
            new RabbitMqEventRouteRegistration(
                RabbitMqPublishDestinationKind.Queue,
                queueName));
    }

    internal void AddReceiveQueueAcceptedModule(
        string queueName,
        string moduleName)
    {
        RabbitMqReceiveQueueRegistration queue = EnsureReceiveQueue(queueName);
        queue.AddAcceptedModule(moduleName);
    }

    internal void AddReceiveQueueEventSubscription(
        string queueName,
        string messageTypeName,
        string subscriberModule,
        string subscriberIdentity)
    {
        RabbitMqReceiveQueueRegistration queue = EnsureReceiveQueue(queueName);
        queue.AddEventSubscription(
            messageTypeName,
            subscriberModule,
            subscriberIdentity);
    }

    private void SetEventRoute(
        string messageTypeName,
        RabbitMqEventRouteRegistration route)
    {
        if (_eventRoutesByMessageTypeName.TryGetValue(
            messageTypeName,
            out RabbitMqEventRouteRegistration? existingRoute))
        {
            if (existingRoute.Kind == route.Kind
                && existingRoute.DestinationName == route.DestinationName)
            {
                return;
            }

            throw new InvalidOperationException(
                $"Message type '{messageTypeName}' already publishes to RabbitMQ {existingRoute.Kind.ToString().ToLowerInvariant()} '{existingRoute.DestinationName}'.");
        }

        _eventRoutesByMessageTypeName.Add(messageTypeName, route);
    }

    private RabbitMqReceiveQueueRegistration EnsureReceiveQueue(
        string queueName)
    {
        string normalizedQueueName = queueName.NormalizeRequired(
            nameof(queueName),
            "RabbitMQ queue name");

        if (_receiveQueues.TryGetValue(
            normalizedQueueName,
            out RabbitMqReceiveQueueRegistration? queue))
        {
            return queue;
        }

        var createdQueue = new RabbitMqReceiveQueueRegistration(normalizedQueueName);
        _receiveQueues.Add(normalizedQueueName, createdQueue);

        return createdQueue;
    }
}
