using System.Diagnostics;
using Bondstone.Messaging;
using Bondstone.Persistence;

namespace Bondstone.Modules;

internal sealed class ModuleEventReceivePipeline(
    IMessageTypeRegistry messageTypeRegistry,
    IModuleEventSubscriberExecutor moduleEventSubscriberExecutor,
    TimeProvider? timeProvider = null,
    IDurablePayloadSerializer? payloadSerializer = null)
    : IModuleEventReceivePipeline
{
    private const string ActivityName = "bondstone.module_event.receive";
    private readonly IMessageTypeRegistry _messageTypeRegistry =
        messageTypeRegistry ?? throw new ArgumentNullException(nameof(messageTypeRegistry));
    private readonly IModuleEventSubscriberExecutor _moduleEventSubscriberExecutor =
        moduleEventSubscriberExecutor ?? throw new ArgumentNullException(nameof(moduleEventSubscriberExecutor));
    private readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;
    private readonly IDurablePayloadSerializer _payloadSerializer =
        payloadSerializer ?? new SystemTextJsonDurablePayloadSerializer();

    public async ValueTask<DurableInboxHandleResult> HandleOnceAsync(
        DurableMessageEnvelope envelope,
        string subscriberModule,
        string subscriberIdentity,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(envelope);

        MessageTypeRegistration registration = ResolveEventRegistration(envelope);
        object integrationEvent = _payloadSerializer.Deserialize(
            envelope.Payload,
            registration.ClrType);
        var record = new DurableInboxRecord(
            DurableInboxMessageKey.ForEventSubscriber(
                envelope.MessageId,
                subscriberModule,
                subscriberIdentity),
            _timeProvider.GetUtcNow());

        using Activity? activity = ModuleReceiveTelemetry.StartReceiveActivity(
            ActivityName,
            envelope,
            subscriberIdentity);

        ModuleEventSubscriberExecutionResult executionResult;
        try
        {
            executionResult = await _moduleEventSubscriberExecutor.ExecuteAsync(
                subscriberModule,
                registration.MessageTypeName,
                subscriberIdentity,
                integrationEvent,
                new ModuleEventSubscriberReceiveContext(
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
                "Module event subscriber receive did not produce an inbox handle result.");

        if (result.Status == DurableInboxHandleStatus.AlreadyReceived)
        {
            throw new DurableInboxAlreadyReceivedException(result);
        }

        return result;
    }

    private MessageTypeRegistration ResolveEventRegistration(
        DurableMessageEnvelope envelope)
    {
        if (envelope.MessageKind != MessageKind.Event)
        {
            throw new NotSupportedException(
                "Module event receive supports event envelopes only.");
        }

        MessageTypeRegistration registration = _messageTypeRegistry.ResolveRegistration(
            envelope.MessageTypeName);

        if (registration.Kind != MessageKind.Event)
        {
            throw new InvalidOperationException(
                $"Message type '{registration.MessageTypeName}' is registered as '{registration.Kind}', not '{MessageKind.Event}'.");
        }

        return registration;
    }
}
