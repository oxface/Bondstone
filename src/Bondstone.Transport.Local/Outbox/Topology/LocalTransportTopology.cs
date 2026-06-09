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
        string? queueName = ResolveQueueName(
            _queueNamesByTargetModule,
            _moduleQueueNameConvention,
            targetModule);

        if (queueName is null
            || !_queues.TryGetValue(queueName, out LocalQueueRegistration? queue)
            || !queue.AcceptedModules.Contains(targetModule))
        {
            binding = null;
            return false;
        }

        binding = new LocalCommandQueueBinding(queue.QueueName, targetModule);
        return true;
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
}
