namespace Bondstone.Capabilities.DomainEvents;

public interface IDomainEventSource
{
    IReadOnlyCollection<IDomainEvent> PendingDomainEvents { get; }

    void ClearPendingDomainEvents();
}
