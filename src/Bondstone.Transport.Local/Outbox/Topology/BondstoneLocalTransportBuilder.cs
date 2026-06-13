using Bondstone.Utility;

namespace Bondstone.Transport.Local.Outbox;

public sealed class BondstoneLocalTransportBuilder
{
    private readonly Dictionary<string, string> _queueNamesByTargetModule =
        new(StringComparer.Ordinal);
    private readonly Dictionary<string, string> _queueNamesByMessageTypeName =
        new(StringComparer.Ordinal);
    private readonly Dictionary<string, LocalQueueRegistration> _queues =
        new(StringComparer.Ordinal);
    private Func<string, string>? _moduleQueueNameConvention;
    private Func<string, string>? _eventQueueNameConvention;

    internal LocalTransportTopology Topology =>
        new(
            _queueNamesByTargetModule,
            _queueNamesByMessageTypeName,
            _queues,
            _moduleQueueNameConvention,
            _eventQueueNameConvention);

    public BondstoneLocalModuleRouteBuilder RouteModule(
        string targetModule)
    {
        string normalizedTargetModule = targetModule.NormalizeRequired(
            nameof(targetModule),
            "Target module");

        return new BondstoneLocalModuleRouteBuilder(
            this,
            normalizedTargetModule);
    }

    public BondstoneLocalTransportBuilder UseModuleQueueConvention()
    {
        return UseModuleQueueConvention(static moduleName => $"{moduleName}.commands");
    }

    public BondstoneLocalTransportBuilder UseModuleQueueConvention(
        Func<string, string> queueNameFactory)
    {
        ArgumentNullException.ThrowIfNull(queueNameFactory);

        _moduleQueueNameConvention = moduleName =>
            queueNameFactory(moduleName).NormalizeRequired(
                nameof(queueNameFactory),
                "Local queue name");

        return this;
    }

    public BondstoneLocalEventRouteBuilder RouteEvent(
        string messageTypeName)
    {
        string normalizedMessageTypeName = messageTypeName.NormalizeRequired(
            nameof(messageTypeName),
            "Message type name");

        return new BondstoneLocalEventRouteBuilder(
            this,
            normalizedMessageTypeName);
    }

    public BondstoneLocalTransportBuilder UseEventQueueConvention()
    {
        return UseEventQueueConvention(static messageTypeName => messageTypeName);
    }

    public BondstoneLocalTransportBuilder UseEventQueueConvention(
        Func<string, string> queueNameFactory)
    {
        ArgumentNullException.ThrowIfNull(queueNameFactory);

        _eventQueueNameConvention = messageTypeName =>
            queueNameFactory(messageTypeName).NormalizeRequired(
                nameof(queueNameFactory),
                "Local queue name");

        return this;
    }

    public BondstoneLocalQueueBuilder Queue(
        string queueName)
    {
        string normalizedQueueName = queueName.NormalizeRequired(
            nameof(queueName),
            "Local queue name");

        EnsureQueue(normalizedQueueName);

        return new BondstoneLocalQueueBuilder(
            this,
            normalizedQueueName);
    }

    internal void SetModuleQueueName(
        string targetModule,
        string queueName)
    {
        string normalizedQueueName = queueName.NormalizeRequired(
            nameof(queueName),
            "Local queue name");

        if (_queueNamesByTargetModule.TryGetValue(
            targetModule,
            out string? existingQueueName))
        {
            if (existingQueueName == normalizedQueueName)
            {
                return;
            }

            throw new InvalidOperationException(
                $"Target module '{targetModule}' already routes to local queue '{existingQueueName}'.");
        }

        _queueNamesByTargetModule.Add(targetModule, normalizedQueueName);
    }

    internal void SetEventQueueName(
        string messageTypeName,
        string queueName)
    {
        string normalizedQueueName = queueName.NormalizeRequired(
            nameof(queueName),
            "Local queue name");

        if (_queueNamesByMessageTypeName.TryGetValue(
            messageTypeName,
            out string? existingQueueName))
        {
            if (existingQueueName == normalizedQueueName)
            {
                return;
            }

            throw new InvalidOperationException(
                $"Message type '{messageTypeName}' already routes to local queue '{existingQueueName}'.");
        }

        _queueNamesByMessageTypeName.Add(messageTypeName, normalizedQueueName);
    }

    internal void AddAcceptedModule(
        string queueName,
        string moduleName)
    {
        LocalQueueRegistration queue = EnsureQueue(queueName);
        queue.AddAcceptedModule(moduleName);
    }

    internal void AddEventSubscription(
        string queueName,
        string messageTypeName,
        string subscriberModule,
        string subscriberIdentity)
    {
        LocalQueueRegistration queue = EnsureQueue(queueName);
        queue.AddEventSubscription(
            messageTypeName,
            subscriberModule,
            subscriberIdentity);
    }

    private LocalQueueRegistration EnsureQueue(
        string queueName)
    {
        string normalizedQueueName = queueName.NormalizeRequired(
            nameof(queueName),
            "Local queue name");

        if (_queues.TryGetValue(
            normalizedQueueName,
            out LocalQueueRegistration? queue))
        {
            return queue;
        }

        var createdQueue = new LocalQueueRegistration(normalizedQueueName);
        _queues.Add(normalizedQueueName, createdQueue);

        return createdQueue;
    }
}
