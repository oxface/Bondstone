using System.Diagnostics;
using System.Text.Json;
using Bondstone.Messaging;
using Bondstone.Modules;
using Bondstone.Persistence;
using Bondstone.Transport.Rebus.Outbox;
using Bondstone.Utility;

namespace Bondstone.Transport.Rebus.Inbox;

public sealed class RebusModuleCommandReceivePipeline(
    IMessageTypeRegistry messageTypeRegistry,
    IModuleCommandRouteRegistry routeRegistry,
    IModuleCommandExecutor moduleCommandExecutor,
    TimeProvider? timeProvider = null,
    JsonSerializerOptions? jsonSerializerOptions = null)
    : IRebusModuleCommandReceivePipeline
{
    private const string ActivityName = "bondstone.rebus.module_command.receive";
    private readonly IMessageTypeRegistry _messageTypeRegistry =
        messageTypeRegistry ?? throw new ArgumentNullException(nameof(messageTypeRegistry));
    private readonly IModuleCommandRouteRegistry _routeRegistry =
        routeRegistry ?? throw new ArgumentNullException(nameof(routeRegistry));
    private readonly IModuleCommandExecutor _moduleCommandExecutor =
        moduleCommandExecutor ?? throw new ArgumentNullException(nameof(moduleCommandExecutor));
    private readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;
    private readonly JsonSerializerOptions _jsonSerializerOptions =
        jsonSerializerOptions ?? new JsonSerializerOptions(JsonSerializerDefaults.Web);

    public async ValueTask<DurableInboxHandleResult> HandleOnceAsync(
        RebusDurableMessageEnvelope envelope,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(envelope);

        string targetModule = envelope.TargetModule.NormalizeRequired(
            nameof(envelope.TargetModule),
            "Target module");
        MessageTypeRegistration registration = ResolveCommandRegistration(envelope);
        ModuleCommandRoute route = _routeRegistry.GetByMessageTypeName(
            targetModule,
            registration.MessageTypeName);
        string handlerIdentity = route.HandlerIdentity.NormalizeRequired(
            nameof(route.HandlerIdentity),
            "Handler identity");
        object command = DeserializeCommand(envelope, registration.ClrType);
        var record = new DurableInboxRecord(
            new DurableInboxMessageKey(
                envelope.MessageId,
                targetModule,
                handlerIdentity),
            _timeProvider.GetUtcNow());

        using Activity? activity = StartReceiveActivity(envelope, handlerIdentity);
        ModuleCommandExecutionResult executionResult;

        try
        {
            executionResult = await _moduleCommandExecutor.ExecuteAsync(
                targetModule,
                command,
                record,
                ct);
        }
        catch (Exception exception)
        {
            activity?.SetStatus(ActivityStatusCode.Error, exception.Message);
            throw;
        }

        DurableInboxHandleResult result =
            executionResult.ReceiveInboxResult
            ?? throw new InvalidOperationException(
                "Module command receive did not produce an inbox handle result.");

        if (result.Status == DurableInboxHandleStatus.AlreadyReceived)
        {
            throw new RebusDurableInboxAlreadyReceivedException(result);
        }

        return result;
    }

    private MessageTypeRegistration ResolveCommandRegistration(
        RebusDurableMessageEnvelope envelope)
    {
        if (!Enum.TryParse(envelope.MessageKind, out MessageKind messageKind)
            || !Enum.IsDefined(messageKind))
        {
            throw new NotSupportedException(
                $"Rebus durable inbox message kind '{envelope.MessageKind}' is not supported.");
        }

        if (messageKind != MessageKind.Command)
        {
            throw new NotSupportedException(
                "Rebus module command receive supports command envelopes only.");
        }

        Type registeredType = _messageTypeRegistry.ResolveClrType(envelope.MessageTypeName);
        MessageTypeRegistration registration = _messageTypeRegistry.Registrations.Single(
            item => item.ClrType == registeredType);

        if (registration.Kind != MessageKind.Command)
        {
            throw new InvalidOperationException(
                $"Message type '{registration.MessageTypeName}' is registered as '{registration.Kind}', not '{MessageKind.Command}'.");
        }

        return registration;
    }

    private object DeserializeCommand(
        RebusDurableMessageEnvelope envelope,
        Type commandType)
    {
        object? command = JsonSerializer.Deserialize(
            envelope.Payload,
            commandType,
            _jsonSerializerOptions);

        if (command is null)
        {
            throw new JsonException(
                $"Message payload for '{envelope.MessageTypeName}' deserialized to null.");
        }

        return command;
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
