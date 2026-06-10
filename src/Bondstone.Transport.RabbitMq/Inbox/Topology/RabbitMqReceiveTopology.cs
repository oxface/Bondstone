using System.Diagnostics.CodeAnalysis;
using Bondstone.Messaging;
using Bondstone.Utility;

namespace Bondstone.Transport.RabbitMq.Inbox;

internal sealed class RabbitMqReceiveTopology
{
    private readonly IReadOnlyDictionary<string, RabbitMqReceiveQueueRegistration> _queues;

    public RabbitMqReceiveTopology(
        IReadOnlyDictionary<string, RabbitMqReceiveQueueRegistration> queues)
    {
        ArgumentNullException.ThrowIfNull(queues);

        _queues = queues.ToDictionary(
            static entry => entry.Key.NormalizeRequired(
                "queueName",
                "RabbitMQ queue name"),
            static entry => entry.Value,
            StringComparer.Ordinal);
    }

    public IReadOnlyCollection<string> QueueNames => _queues.Keys.ToArray();

    public RabbitMqReceiveQueueDiagnostic DescribeQueue(
        string queueName)
    {
        string normalizedQueueName = queueName.NormalizeRequired(
            nameof(queueName),
            "RabbitMQ queue name");

        if (!_queues.TryGetValue(
            normalizedQueueName,
            out RabbitMqReceiveQueueRegistration? queue))
        {
            return new RabbitMqReceiveQueueDiagnostic(
                normalizedQueueName,
                [],
                [],
                $"RabbitMQ queue '{normalizedQueueName}' is not bound to any Bondstone receive handlers.");
        }

        return new RabbitMqReceiveQueueDiagnostic(
            queue.QueueName,
            queue.AcceptedModules,
            queue.EventSubscriptions
                .Select(static subscription =>
                    new RabbitMqReceiveQueueEventSubscriptionDiagnostic(
                        subscription.MessageTypeName,
                        subscription.SubscriberModule,
                        subscription.SubscriberIdentity))
                .ToArray());
    }

    public bool AcceptsCommand(
        string queueName,
        DurableMessageEnvelope envelope)
    {
        ArgumentNullException.ThrowIfNull(envelope);

        if (envelope.MessageKind != MessageKind.Command)
        {
            return false;
        }

        string? targetModule = envelope.TargetModule.NormalizeOptional();
        if (targetModule is null
            || !TryGetQueue(queueName, out RabbitMqReceiveQueueRegistration? queue))
        {
            return false;
        }

        return queue.AcceptedModules.Contains(targetModule);
    }

    public IReadOnlyCollection<RabbitMqEventSubscriptionBinding> GetEventSubscriptions(
        string queueName,
        DurableMessageEnvelope envelope)
    {
        ArgumentNullException.ThrowIfNull(envelope);

        if (envelope.MessageKind != MessageKind.Event)
        {
            return [];
        }

        if (!TryGetQueue(queueName, out RabbitMqReceiveQueueRegistration? queue))
        {
            return [];
        }

        return queue.EventSubscriptions
            .Where(subscription => subscription.MessageTypeName == envelope.MessageTypeName)
            .ToArray();
    }

    private bool TryGetQueue(
        string queueName,
        [NotNullWhen(true)]
        out RabbitMqReceiveQueueRegistration? queue)
    {
        string normalizedQueueName = queueName.NormalizeRequired(
            nameof(queueName),
            "RabbitMQ queue name");

        return _queues.TryGetValue(normalizedQueueName, out queue);
    }
}
