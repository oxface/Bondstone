namespace Bondstone.Capabilities.DomainEvents;

public interface IDomainEventSourceFeature
{
    IReadOnlyCollection<IDomainEventSource> GetPendingDomainEventSources();
}
