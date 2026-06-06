using System.Diagnostics;
using System.Text.Json;
using Bondstone.Messaging;
using Bondstone.Persistence;
using Bondstone.Transport.Rebus.Outbox;
using Bondstone.Utility;

namespace Bondstone.Transport.Rebus.Inbox;

public sealed class RebusTypedCommandReceivePipeline(
    IMessageTypeRegistry messageTypeRegistry,
    IRebusDurableInboxHandlerExecutor inboxHandlerExecutor,
    JsonSerializerOptions? jsonSerializerOptions = null)
    : IRebusTypedCommandReceivePipeline
{
    private const string ActivityName = "bondstone.rebus.command.receive";
    private readonly IMessageTypeRegistry _messageTypeRegistry =
        messageTypeRegistry ?? throw new ArgumentNullException(nameof(messageTypeRegistry));
    private readonly IRebusDurableInboxHandlerExecutor _inboxHandlerExecutor =
        inboxHandlerExecutor ?? throw new ArgumentNullException(nameof(inboxHandlerExecutor));
    private readonly JsonSerializerOptions _jsonSerializerOptions =
        jsonSerializerOptions ?? new JsonSerializerOptions(JsonSerializerDefaults.Web);

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
        Type registeredType = _messageTypeRegistry.ResolveClrType(envelope.MessageTypeName);
        MessageTypeRegistration registration = _messageTypeRegistry.Registrations.Single(
            item => item.ClrType == registeredType);

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
        object? command = JsonSerializer.Deserialize(
            envelope.Payload,
            registeredType,
            _jsonSerializerOptions);

        if (command is null)
        {
            throw new JsonException(
                $"Message payload for '{envelope.MessageTypeName}' deserialized to null.");
        }

        return (TCommand)command;
    }

    private static Activity? StartReceiveActivity(
        RebusDurableMessageEnvelope envelope,
        string handlerIdentity)
    {
        ActivityContext parentContext = default;
        bool hasParentContext = false;

        if (!string.IsNullOrWhiteSpace(envelope.TraceParent))
        {
            if (!ActivityContext.TryParse(envelope.TraceParent, envelope.TraceState, out parentContext))
            {
                throw new ArgumentException(
                    "Trace parent must be a valid W3C traceparent value.",
                    nameof(envelope));
            }

            hasParentContext = true;
        }

        Activity? activity = hasParentContext
            ? BondstoneRebusTelemetry.ActivitySource.StartActivity(
                ActivityName,
                ActivityKind.Consumer,
                parentContext)
            : BondstoneRebusTelemetry.ActivitySource.StartActivity(
                ActivityName,
                ActivityKind.Consumer);

        if (activity is null)
        {
            return null;
        }

        activity.SetTag("bondstone.transport", "rebus");
        activity.SetTag("bondstone.message_id", envelope.MessageId.ToString("D"));
        activity.SetTag("bondstone.message_type", envelope.MessageTypeName);
        activity.SetTag("bondstone.source_module", envelope.SourceModule);
        activity.SetTag("bondstone.target_module", envelope.TargetModule);
        activity.SetTag("bondstone.handler_identity", handlerIdentity);

        return activity;
    }
}
