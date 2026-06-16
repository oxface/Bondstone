namespace Bondstone.DomainEvents;

public interface IDomainEventSource
{
    IReadOnlyCollection<IDomainEvent> PendingDomainEvents { get; }

    void ClearPendingDomainEvents();
}
