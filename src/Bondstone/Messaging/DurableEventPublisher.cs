using Bondstone.Modules;
using Bondstone.Persistence;

namespace Bondstone.Messaging;

internal sealed class DurableEventPublisher(
    IDurableOutboxWriter outboxWriter,
    IMessageTypeRegistry messageTypeRegistry,
    IModuleExecutionContextAccessor executionContextAccessor,
    IDurablePayloadSerializer? payloadSerializer = null,
    TimeProvider? timeProvider = null)
    : IDurableEventPublisher
{
    private readonly IDurableOutboxWriter _outboxWriter =
        outboxWriter ?? throw new ArgumentNullException(nameof(outboxWriter));
    private readonly IMessageTypeRegistry _messageTypeRegistry =
        messageTypeRegistry ?? throw new ArgumentNullException(nameof(messageTypeRegistry));
    private readonly IModuleExecutionContextAccessor _executionContextAccessor =
        executionContextAccessor ?? throw new ArgumentNullException(nameof(executionContextAccessor));
    private readonly IDurablePayloadSerializer _payloadSerializer =
        payloadSerializer ?? new SystemTextJsonDurablePayloadSerializer();
    private readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;

    public async ValueTask<DurableEventPublishResult> PublishAsync<TEvent>(
        TEvent integrationEvent,
        CancellationToken ct = default)
        where TEvent : IIntegrationEvent
    {
        return await PublishAsync(
            integrationEvent,
            partitionKey: null,
            durableOperationId: null,
            traceContext: null,
            causationId: null,
            ct);
    }

    public async ValueTask<DurableEventPublishResult> PublishAsync<TEvent>(
        TEvent integrationEvent,
        string? partitionKey,
        Guid? durableOperationId = null,
        MessageTraceContext? traceContext = null,
        Guid? causationId = null,
        CancellationToken ct = default)
        where TEvent : IIntegrationEvent
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        ModuleExecutionContext executionContext =
            _executionContextAccessor.Current
            ?? throw new InvalidOperationException(
                "Durable event publishing requires a current module execution context.");

        Guid messageId = Guid.NewGuid();
        string messageTypeName = _messageTypeRegistry.GetMessageTypeName<TEvent>();
        string payload = _payloadSerializer.Serialize(integrationEvent);
        MessageTraceContext? capturedTraceContext =
            traceContext ?? MessageTraceContext.CaptureCurrent();

        var envelope = new DurableMessageEnvelope(
            messageId,
            MessageKind.Event,
            messageTypeName,
            executionContext.ModuleName,
            targetModule: null,
            payload,
            _timeProvider.GetUtcNow(),
            durableOperationId,
            capturedTraceContext,
            causationId,
            partitionKey);

        await _outboxWriter.WriteAsync(envelope, ct);

        return new DurableEventPublishResult(
            messageId,
            durableOperationId,
            DurableEventPublishStatus.Accepted);
    }
}
