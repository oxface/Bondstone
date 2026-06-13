namespace Bondstone.Messaging;

/// <summary>
/// Accepts durable integration events for asynchronous outbox-backed publish.
/// </summary>
public interface IDurableEventPublisher
{
    ValueTask<DurableEventPublishResult> PublishAsync<TEvent>(
        TEvent integrationEvent,
        CancellationToken ct = default)
        where TEvent : IIntegrationEvent;

    ValueTask<DurableEventPublishResult> PublishAsync<TEvent>(
        TEvent integrationEvent,
        string? partitionKey,
        Guid? durableOperationId = null,
        MessageTraceContext? traceContext = null,
        Guid? causationId = null,
        CancellationToken ct = default)
        where TEvent : IIntegrationEvent;
}
