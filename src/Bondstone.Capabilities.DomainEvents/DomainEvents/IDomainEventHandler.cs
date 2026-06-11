namespace Bondstone.Capabilities.DomainEvents;

public interface IDomainEventHandler<in TDomainEvent>
    where TDomainEvent : IDomainEvent
{
    ValueTask HandleAsync(
        TDomainEvent domainEvent,
        CancellationToken ct = default);
}
