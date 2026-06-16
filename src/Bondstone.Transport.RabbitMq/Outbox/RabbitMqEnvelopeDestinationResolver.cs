using Bondstone.Messaging;
using Bondstone.Persistence;

namespace Bondstone.Transport.RabbitMq.Outbox;

internal sealed class RabbitMqEnvelopeDestinationResolver(
    Func<DurableMessageEnvelope, RabbitMqPublishDestination?>? commandDestinationResolver,
    Func<DurableMessageEnvelope, RabbitMqPublishDestination?>? eventDestinationResolver)
{
    public bool HasOutboundResolver =>
        commandDestinationResolver is not null
        || eventDestinationResolver is not null;

    public bool CanResolve(
        DurableOutboxRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);

        return TryResolve(record.Envelope) is not null;
    }

    public RabbitMqPublishDestination Resolve(
        DurableOutboxRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);

        DurableMessageEnvelope envelope = record.Envelope;
        RabbitMqPublishDestination? destination = TryResolve(envelope);
        if (destination is not null)
        {
            return destination;
        }

        string messageDescription = envelope.MessageKind == MessageKind.Command
            ? $"command '{envelope.MessageTypeName}' for target module '{envelope.TargetModule}'"
            : $"event '{envelope.MessageTypeName}'";

        throw new InvalidOperationException(
            $"No RabbitMQ publish destination resolved for {messageDescription}.");
    }

    private RabbitMqPublishDestination? TryResolve(
        DurableMessageEnvelope envelope)
    {
        if (envelope.MessageKind == MessageKind.Command)
        {
            return commandDestinationResolver?.Invoke(envelope);
        }

        if (envelope.MessageKind == MessageKind.Event)
        {
            return eventDestinationResolver?.Invoke(envelope);
        }

        return null;
    }
}
