using Bondstone.Messaging;
using Bondstone.Transport.Rebus.Outbox;

namespace Bondstone.Transport.Rebus.Inbox;

public interface IRebusDurableMessageEndpointDispatcher
{
    ValueTask DispatchAsync(
        string endpointName,
        RebusDurableMessageEnvelope envelope,
        CancellationToken ct = default);
}

internal sealed class RebusDurableMessageEndpointDispatcher(
    IRebusModuleCommandEndpointDispatcher commandDispatcher,
    IRebusModuleEventEndpointDispatcher eventDispatcher)
    : IRebusDurableMessageEndpointDispatcher
{
    private readonly IRebusModuleCommandEndpointDispatcher _commandDispatcher =
        commandDispatcher ?? throw new ArgumentNullException(nameof(commandDispatcher));
    private readonly IRebusModuleEventEndpointDispatcher _eventDispatcher =
        eventDispatcher ?? throw new ArgumentNullException(nameof(eventDispatcher));

    public async ValueTask DispatchAsync(
        string endpointName,
        RebusDurableMessageEnvelope envelope,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(envelope);

        if (!Enum.TryParse(envelope.MessageKind, out MessageKind messageKind)
            || !Enum.IsDefined(messageKind))
        {
            throw new NotSupportedException(
                $"Rebus durable message kind '{envelope.MessageKind}' is not supported.");
        }

        if (messageKind == MessageKind.Command)
        {
            await _commandDispatcher.DispatchAsync(
                endpointName,
                envelope,
                ct);
            return;
        }

        if (messageKind == MessageKind.Event)
        {
            await _eventDispatcher.DispatchAsync(
                endpointName,
                envelope,
                ct);
            return;
        }

        throw new NotSupportedException(
            $"Rebus durable message kind '{messageKind}' is not supported.");
    }
}
