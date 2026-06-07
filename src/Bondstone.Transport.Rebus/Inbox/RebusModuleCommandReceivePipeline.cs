using System.Diagnostics;
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
    IDurablePayloadSerializer? payloadSerializer = null)
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
    private readonly IDurablePayloadSerializer _payloadSerializer =
        payloadSerializer ?? new SystemTextJsonDurablePayloadSerializer();

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

        MessageTypeRegistration registration = _messageTypeRegistry.ResolveRegistration(
            envelope.MessageTypeName);

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
        return _payloadSerializer.Deserialize(
            envelope.Payload,
            commandType);
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
