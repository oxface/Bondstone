using Bondstone.Messaging;
using Bondstone.Utility;

namespace Bondstone.Transport.Local.Outbox;

internal sealed class LocalTransportTopology
{
    private readonly IReadOnlyDictionary<string, string> _queueNamesByTargetModule;
    private readonly IReadOnlyDictionary<string, string> _queueNamesByMessageTypeName;
    private readonly IReadOnlyDictionary<string, LocalQueueRegistration> _queues;
    private readonly Func<string, string>? _moduleQueueNameConvention;
    private readonly Func<string, string>? _eventQueueNameConvention;

    public LocalTransportTopology(
        IReadOnlyDictionary<string, string> queueNamesByTargetModule,
        IReadOnlyDictionary<string, string> queueNamesByMessageTypeName,
        IReadOnlyDictionary<string, LocalQueueRegistration> queues,
        Func<string, string>? moduleQueueNameConvention,
        Func<string, string>? eventQueueNameConvention)
    {
        ArgumentNullException.ThrowIfNull(queueNamesByTargetModule);
        ArgumentNullException.ThrowIfNull(queueNamesByMessageTypeName);
        ArgumentNullException.ThrowIfNull(queues);

        _queueNamesByTargetModule = queueNamesByTargetModule
            .ToDictionary(
                static entry => entry.Key.NormalizeRequired(
                    "targetModule",
                    "Target module"),
                static entry => entry.Value.NormalizeRequired(
                    "queueName",
                    "Local queue name"),
                StringComparer.Ordinal);
        _queueNamesByMessageTypeName = queueNamesByMessageTypeName
            .ToDictionary(
                static entry => entry.Key.NormalizeRequired(
                    "messageTypeName",
                    "Message type name"),
                static entry => entry.Value.NormalizeRequired(
                    "queueName",
                    "Local queue name"),
                StringComparer.Ordinal);
        _queues = queues
            .ToDictionary(
                static entry => entry.Key.NormalizeRequired(
                    "queueName",
                    "Local queue name"),
                static entry => entry.Value,
                StringComparer.Ordinal);
        _moduleQueueNameConvention = moduleQueueNameConvention;
        _eventQueueNameConvention = eventQueueNameConvention;
    }

    public bool TryGetCommandBinding(
        DurableMessageEnvelope envelope,
        out LocalCommandQueueBinding? binding)
    {
        ArgumentNullException.ThrowIfNull(envelope);

        if (envelope.MessageKind != MessageKind.Command)
        {
            binding = null;
            return false;
        }

        string targetModule = envelope.TargetModule.NormalizeRequired(
            nameof(envelope),
            "Target module");

        if (!TryResolveCommandQueueName(targetModule, out string? queueName))
        {
            binding = null;
            return false;
        }

        binding = new LocalCommandQueueBinding(queueName, targetModule);
        return true;
    }

    public bool HasCommandRoute(
        string targetModule)
    {
        string normalizedTargetModule = targetModule.NormalizeRequired(
            nameof(targetModule),
            "Target module");

        return TryResolveCommandQueueName(normalizedTargetModule, out _);
    }

    public IReadOnlyCollection<LocalEventSubscription> GetEventSubscriptions(
        DurableMessageEnvelope envelope)
    {
        ArgumentNullException.ThrowIfNull(envelope);

        if (envelope.MessageKind != MessageKind.Event)
        {
            return [];
        }

        string messageTypeName = envelope.MessageTypeName.NormalizeRequired(
            nameof(envelope),
            "Message type name");
        string? queueName = ResolveQueueName(
            _queueNamesByMessageTypeName,
            _eventQueueNameConvention,
            messageTypeName);

        if (queueName is null
            || !_queues.TryGetValue(queueName, out LocalQueueRegistration? queue))
        {
            return [];
        }

        return queue.EventSubscriptions
            .Where(subscription => subscription.MessageTypeName == messageTypeName)
            .ToArray();
    }

    public bool HasEventRoute(
        string messageTypeName)
    {
        string normalizedMessageTypeName = messageTypeName.NormalizeRequired(
            nameof(messageTypeName),
            "Message type name");
        string? queueName = ResolveQueueName(
            _queueNamesByMessageTypeName,
            _eventQueueNameConvention,
            normalizedMessageTypeName);

        return queueName is not null
            && _queues.TryGetValue(queueName, out LocalQueueRegistration? queue)
            && queue.EventSubscriptions.Any(subscription =>
                subscription.MessageTypeName == normalizedMessageTypeName);
    }

    private static string? ResolveQueueName(
        IReadOnlyDictionary<string, string> explicitQueues,
        Func<string, string>? queueNameConvention,
        string key)
    {
        if (explicitQueues.TryGetValue(key, out string? queueName))
        {
            return queueName;
        }

        return queueNameConvention?.Invoke(key).NormalizeRequired(
            nameof(queueNameConvention),
            "Local queue name");
    }

    private bool TryResolveCommandQueueName(
        string targetModule,
        out string queueName)
    {
        if (_queueNamesByTargetModule.TryGetValue(targetModule, out string? explicitQueueName))
        {
            if (_queues.TryGetValue(explicitQueueName, out LocalQueueRegistration? queue)
                && queue.AcceptedModules.Contains(targetModule))
            {
                queueName = queue.QueueName;
                return true;
            }

            queueName = string.Empty;
            return false;
        }

        string? conventionQueueName = ResolveConventionQueueName(
            _moduleQueueNameConvention,
            targetModule);
        if (conventionQueueName is not null)
        {
            queueName = conventionQueueName;
            return true;
        }

        queueName = string.Empty;
        return false;
    }

    private static string? ResolveConventionQueueName(
        Func<string, string>? queueNameConvention,
        string key)
    {
        return queueNameConvention?.Invoke(key).NormalizeRequired(
            nameof(queueNameConvention),
            "Local queue name");
    }
}
