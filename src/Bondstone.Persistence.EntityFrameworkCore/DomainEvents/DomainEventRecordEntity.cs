using Bondstone.DomainEvents;
using Bondstone.Messaging;

namespace Bondstone.Persistence.EntityFrameworkCore.DomainEvents;

public sealed class DomainEventRecordEntity
{
    private DomainEventRecordEntity(
        Guid domainEventId,
        string moduleName,
        string domainEventName,
        string payloadTypeName,
        string payload,
        string? payloadMetadata,
        DateTimeOffset occurredAtUtc,
        DateTimeOffset capturedAtUtc,
        string? traceParent,
        string? traceState,
        string? traceBaggage,
        Guid? causationId)
    {
        DomainEventId = domainEventId;
        ModuleName = moduleName;
        DomainEventName = domainEventName;
        PayloadTypeName = payloadTypeName;
        Payload = payload;
        PayloadMetadata = payloadMetadata;
        OccurredAtUtc = occurredAtUtc;
        CapturedAtUtc = capturedAtUtc;
        TraceParent = traceParent;
        TraceState = traceState;
        TraceBaggage = traceBaggage;
        CausationId = causationId;
    }

    private DomainEventRecordEntity()
    {
    }

    public Guid DomainEventId { get; private set; }

    public string ModuleName { get; private set; } = string.Empty;

    public string DomainEventName { get; private set; } = string.Empty;

    public string PayloadTypeName { get; private set; } = string.Empty;

    public string Payload { get; private set; } = string.Empty;

    public string? PayloadMetadata { get; private set; }

    public DateTimeOffset OccurredAtUtc { get; private set; }

    public DateTimeOffset CapturedAtUtc { get; private set; }

    public string? TraceParent { get; private set; }

    public string? TraceState { get; private set; }

    public string? TraceBaggage { get; private set; }

    public Guid? CausationId { get; private set; }

    internal static DomainEventRecordEntity FromDomainEvent(
        string moduleName,
        IDomainEvent domainEvent,
        string domainEventName,
        string payload,
        DateTimeOffset capturedAtUtc,
        MessageTraceContext? traceContext = null,
        Guid? causationId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(moduleName);
        ArgumentNullException.ThrowIfNull(domainEvent);
        ArgumentException.ThrowIfNullOrWhiteSpace(domainEventName);
        ArgumentException.ThrowIfNullOrWhiteSpace(payload);

        Type domainEventType = domainEvent.GetType();
        string payloadTypeName = domainEventType.AssemblyQualifiedName
            ?? domainEventType.FullName
            ?? domainEventType.Name;

        return new DomainEventRecordEntity(
            Guid.NewGuid(),
            moduleName,
            domainEventName,
            payloadTypeName,
            payload,
            payloadMetadata: null,
            capturedAtUtc,
            capturedAtUtc,
            traceContext?.TraceParent,
            traceContext?.TraceState,
            traceContext?.Baggage,
            causationId);
    }
}
