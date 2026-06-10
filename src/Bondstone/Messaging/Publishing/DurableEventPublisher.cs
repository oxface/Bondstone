using Bondstone.Modules;
using Bondstone.Persistence;

namespace Bondstone.Messaging;

internal sealed class DurableEventPublisher(
    DurableModuleOutboxWriterResolver outboxWriterResolver,
    IMessageTypeRegistry messageTypeRegistry,
    IModulePublishedEventRegistry publishedEventRegistry,
    IModuleExecutionContextAccessor executionContextAccessor,
    IDurablePayloadSerializer? payloadSerializer = null,
    TimeProvider? timeProvider = null)
    : IDurableEventPublisher
{
    private readonly DurableModuleOutboxWriterResolver _outboxWriterResolver =
        outboxWriterResolver ?? throw new ArgumentNullException(nameof(outboxWriterResolver));
    private readonly IMessageTypeRegistry _messageTypeRegistry =
        messageTypeRegistry ?? throw new ArgumentNullException(nameof(messageTypeRegistry));
    private readonly IModulePublishedEventRegistry _publishedEventRegistry =
        publishedEventRegistry ?? throw new ArgumentNullException(nameof(publishedEventRegistry));
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
        if (!_publishedEventRegistry.PublishedEvents.Any(publishedEvent =>
                publishedEvent.ModuleName == executionContext.ModuleName
                && publishedEvent.MessageTypeName == messageTypeName))
        {
            throw new InvalidOperationException(
                $"Module '{executionContext.ModuleName}' has not registered published event '{messageTypeName}'. Call RegisterPublishedEvent for the publishing module before using {nameof(IDurableEventPublisher)}.");
        }

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

        IDurableOutboxWriter outboxWriter = _outboxWriterResolver.Resolve(
            executionContext.ModuleName);

        await outboxWriter.WriteAsync(envelope, ct);

        return new DurableEventPublishResult(
            messageId,
            durableOperationId,
            DurableEventPublishStatus.Accepted);
    }
}
