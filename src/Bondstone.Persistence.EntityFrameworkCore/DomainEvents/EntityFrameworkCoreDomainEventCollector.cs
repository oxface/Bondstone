using System.Text.Json;
using Bondstone.DomainEvents;
using Bondstone.Messaging;
using Microsoft.EntityFrameworkCore;

namespace Bondstone.Persistence.EntityFrameworkCore.DomainEvents;

internal sealed class EntityFrameworkCoreDomainEventCollector(
    DbContext dbContext,
    string moduleName,
    DurablePayloadJsonOptions? payloadJsonOptions = null,
    TimeProvider? timeProvider = null)
{
    private readonly DbContext _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    private readonly string _moduleName = string.IsNullOrWhiteSpace(moduleName)
        ? throw new ArgumentException("Module name is required.", nameof(moduleName))
        : moduleName.Trim();
    private readonly JsonSerializerOptions _jsonSerializerOptions =
        payloadJsonOptions?.JsonSerializerOptions ?? new JsonSerializerOptions(JsonSerializerDefaults.Web);
    private readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;

    public IReadOnlyList<IDomainEventSource> CollectAndStage()
    {
        IReadOnlyList<IDomainEventSource> sources = GetPendingDomainEventSources(_dbContext);
        if (sources.Count == 0)
        {
            return [];
        }

        DateTimeOffset capturedAtUtc = _timeProvider.GetUtcNow();
        MessageTraceContext? traceContext = MessageTraceContext.CaptureCurrent();
        List<DomainEventRecordEntity> records = [];

        foreach (IDomainEventSource source in sources)
        {
            foreach (IDomainEvent domainEvent in source.PendingDomainEvents)
            {
                string domainEventName = GetDomainEventName(domainEvent);
                string payload = JsonSerializer.Serialize(
                    domainEvent,
                    domainEvent.GetType(),
                    _jsonSerializerOptions);

                records.Add(DomainEventRecordEntity.FromDomainEvent(
                    _moduleName,
                    domainEvent,
                    domainEventName,
                    payload,
                    capturedAtUtc,
                    traceContext));
            }
        }

        _dbContext.Set<DomainEventRecordEntity>().AddRange(records);

        return sources;
    }

    internal static IReadOnlyList<IDomainEventSource> GetPendingDomainEventSources(DbContext dbContext)
    {
        ArgumentNullException.ThrowIfNull(dbContext);

        List<IDomainEventSource> sources = [];
        foreach (IDomainEventSource source in dbContext.ChangeTracker
            .Entries()
            .Select(static entry => entry.Entity)
            .OfType<IDomainEventSource>())
        {
            if (source.PendingDomainEvents.Count > 0
                && !sources.Contains(source, ReferenceEqualityComparer.Instance))
            {
                sources.Add(source);
            }
        }

        return sources;
    }

    private static string GetDomainEventName(IDomainEvent domainEvent)
    {
        Type domainEventType = domainEvent.GetType();
        DomainEventIdentityAttribute? identity = domainEventType
            .GetCustomAttributes(typeof(DomainEventIdentityAttribute), inherit: false)
            .OfType<DomainEventIdentityAttribute>()
            .SingleOrDefault();

        if (string.IsNullOrWhiteSpace(identity?.Name))
        {
            throw new InvalidOperationException(
                $"Domain event type '{domainEventType.FullName}' is missing a non-empty {nameof(DomainEventIdentityAttribute)}.");
        }

        return identity.Name.Trim();
    }
}
