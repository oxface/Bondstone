using System.Diagnostics;
using Bondstone.Messaging;
using Bondstone.Persistence;
using Bondstone.Utility;

namespace Bondstone.Modules;

internal sealed class ModuleCommandReceivePipeline(
    IMessageTypeRegistry messageTypeRegistry,
    IModuleCommandRouteRegistry routeRegistry,
    IModuleCommandExecutor moduleCommandExecutor,
    TimeProvider? timeProvider = null,
    IDurablePayloadSerializer? payloadSerializer = null)
    : IModuleCommandReceivePipeline
{
    private const string ActivityName = "bondstone.module_command.receive";
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
        DurableMessageEnvelope envelope,
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
        object command = _payloadSerializer.Deserialize(
            envelope.Payload,
            registration.ClrType);
        var record = new DurableInboxRecord(
            new DurableInboxMessageKey(
                envelope.MessageId,
                targetModule,
                handlerIdentity),
            _timeProvider.GetUtcNow());

        using Activity? activity = ModuleReceiveTelemetry.StartReceiveActivity(
            ActivityName,
            envelope,
            handlerIdentity);

        ModuleCommandExecutionResult executionResult;
        try
        {
            executionResult = await _moduleCommandExecutor.ExecuteAsync(
                targetModule,
                command,
                new ModuleCommandReceiveContext(
                    record,
                    envelope.DurableOperationId),
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
            throw new DurableInboxAlreadyReceivedException(result);
        }

        return result;
    }

    private MessageTypeRegistration ResolveCommandRegistration(
        DurableMessageEnvelope envelope)
    {
        if (envelope.MessageKind != MessageKind.Command)
        {
            throw new NotSupportedException(
                "Module command receive supports command envelopes only.");
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
}
