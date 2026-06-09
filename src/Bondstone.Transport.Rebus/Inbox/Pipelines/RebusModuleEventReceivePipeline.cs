using System.Diagnostics;
using Bondstone.Messaging;
using Bondstone.Modules;
using Bondstone.Persistence;
using Bondstone.Transport.Rebus.Outbox;

namespace Bondstone.Transport.Rebus.Inbox;

public interface IRebusModuleEventReceivePipeline
{
    ValueTask<DurableInboxHandleResult> HandleOnceAsync(
        RebusDurableMessageEnvelope envelope,
        string subscriberModule,
        string subscriberIdentity,
        CancellationToken ct = default);
}

public sealed class RebusModuleEventReceivePipeline(
    IMessageTypeRegistry messageTypeRegistry,
    IModuleEventSubscriberExecutor moduleEventSubscriberExecutor,
    TimeProvider? timeProvider = null,
    IDurablePayloadSerializer? payloadSerializer = null)
    : IRebusModuleEventReceivePipeline
{
    private const string ActivityName = "bondstone.rebus.module_event.receive";
    private readonly IMessageTypeRegistry _messageTypeRegistry =
        messageTypeRegistry ?? throw new ArgumentNullException(nameof(messageTypeRegistry));
    private readonly IModuleEventSubscriberExecutor _moduleEventSubscriberExecutor =
        moduleEventSubscriberExecutor ?? throw new ArgumentNullException(nameof(moduleEventSubscriberExecutor));
    private readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;
    private readonly IDurablePayloadSerializer _payloadSerializer =
        payloadSerializer ?? new SystemTextJsonDurablePayloadSerializer();

    public async ValueTask<DurableInboxHandleResult> HandleOnceAsync(
        RebusDurableMessageEnvelope envelope,
        string subscriberModule,
        string subscriberIdentity,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(envelope);

        MessageTypeRegistration registration = ResolveEventRegistration(envelope);
        object integrationEvent = DeserializeEvent(envelope, registration.ClrType);
        var record = new DurableInboxRecord(
            DurableInboxMessageKey.ForEventSubscriber(
                envelope.MessageId,
                subscriberModule,
                subscriberIdentity),
            _timeProvider.GetUtcNow());

        using Activity? activity = StartReceiveActivity(envelope, subscriberIdentity);
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
            throw new RebusDurableInboxAlreadyReceivedException(result);
        }

        return result;
    }

    private MessageTypeRegistration ResolveEventRegistration(
        RebusDurableMessageEnvelope envelope)
    {
        if (!Enum.TryParse(envelope.MessageKind, out MessageKind messageKind)
            || !Enum.IsDefined(messageKind))
        {
            throw new NotSupportedException(
                $"Rebus durable inbox message kind '{envelope.MessageKind}' is not supported.");
        }

        if (messageKind != MessageKind.Event)
        {
            throw new NotSupportedException(
                "Rebus module event receive supports event envelopes only.");
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

    private object DeserializeEvent(
        RebusDurableMessageEnvelope envelope,
        Type eventType)
    {
        return _payloadSerializer.Deserialize(
            envelope.Payload,
            eventType);
    }

    private static Activity? StartReceiveActivity(
        RebusDurableMessageEnvelope envelope,
        string subscriberIdentity)
    {
        return RebusReceiveTelemetry.StartReceiveActivity(
            ActivityName,
            envelope,
            subscriberIdentity);
    }
}
