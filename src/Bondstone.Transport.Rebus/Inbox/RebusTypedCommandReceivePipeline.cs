using System.Diagnostics;
using Bondstone.Messaging;
using Bondstone.Persistence;
using Bondstone.Transport.Rebus.Outbox;
using Bondstone.Utility;

namespace Bondstone.Transport.Rebus.Inbox;

public sealed class RebusTypedCommandReceivePipeline(
    IMessageTypeRegistry messageTypeRegistry,
    IRebusDurableInboxHandlerExecutor inboxHandlerExecutor,
    IDurablePayloadSerializer? payloadSerializer = null)
    : IRebusTypedCommandReceivePipeline
{
    private const string ActivityName = "bondstone.rebus.command.receive";
    private readonly IMessageTypeRegistry _messageTypeRegistry =
        messageTypeRegistry ?? throw new ArgumentNullException(nameof(messageTypeRegistry));
    private readonly IRebusDurableInboxHandlerExecutor _inboxHandlerExecutor =
        inboxHandlerExecutor ?? throw new ArgumentNullException(nameof(inboxHandlerExecutor));
    private readonly IDurablePayloadSerializer _payloadSerializer =
        payloadSerializer ?? new SystemTextJsonDurablePayloadSerializer();

    public async ValueTask<DurableInboxHandleResult> HandleOnceAsync<TCommand>(
        RebusDurableMessageEnvelope envelope,
        string handlerIdentity,
        Func<TCommand, CancellationToken, ValueTask> handler,
        Func<CancellationToken, ValueTask> commit,
        CancellationToken ct = default)
        where TCommand : IDurableCommand
    {
        ArgumentNullException.ThrowIfNull(envelope);
        ArgumentNullException.ThrowIfNull(handler);
        ArgumentNullException.ThrowIfNull(commit);

        string normalizedHandlerIdentity = handlerIdentity.NormalizeRequired(
            nameof(handlerIdentity),
            "Handler identity");
        MessageTypeRegistration registration = ResolveCommandRegistration<TCommand>(envelope);
        TCommand command = DeserializeCommand<TCommand>(envelope, registration.ClrType);

        using Activity? activity = StartReceiveActivity(envelope, normalizedHandlerIdentity);

        try
        {
            return await _inboxHandlerExecutor.HandleOnceAsync(
                envelope,
                normalizedHandlerIdentity,
                (_, handlerCt) => handler(command, handlerCt),
                commit,
                ct);
        }
        catch (Exception exception)
        {
            activity?.SetStatus(ActivityStatusCode.Error, exception.Message);
            throw;
        }
    }

    private MessageTypeRegistration ResolveCommandRegistration<TCommand>(
        RebusDurableMessageEnvelope envelope)
        where TCommand : IDurableCommand
    {
        MessageTypeRegistration registration = _messageTypeRegistry.ResolveRegistration(
            envelope.MessageTypeName);

        if (registration.Kind != MessageKind.Command)
        {
            throw new InvalidOperationException(
                $"Message type '{registration.MessageTypeName}' is registered as '{registration.Kind}', not '{MessageKind.Command}'.");
        }

        if (registration.ClrType != typeof(TCommand))
        {
            throw new InvalidOperationException(
                $"Message type '{registration.MessageTypeName}' resolves to '{registration.ClrType.FullName}', not '{typeof(TCommand).FullName}'.");
        }

        return registration;
    }

    private TCommand DeserializeCommand<TCommand>(
        RebusDurableMessageEnvelope envelope,
        Type registeredType)
        where TCommand : IDurableCommand
    {
        return (TCommand)_payloadSerializer.Deserialize(
            envelope.Payload,
            registeredType);
    }

    private static Activity? StartReceiveActivity(
        RebusDurableMessageEnvelope envelope,
        string handlerIdentity)
    {
        return RebusReceiveTelemetry.StartReceiveActivity(
            ActivityName,
            envelope,
            handlerIdentity);
    }
}
