# Bondstone.Capabilities.DomainEvents

Optional module-local domain event capability contracts for Bondstone.

This package owns the domain event contracts and opt-in local dispatch
behavior. It is not a transport event bus and does not persist domain events
without a provider bridge such as
`Bondstone.Capabilities.DomainEvents.EntityFrameworkCore`.

Modules that call `UseDomainEventDispatch()` dispatch pending domain events to
registered `IDomainEventHandler<TDomainEvent>` services only when an active
provider or application behavior exposes `IDomainEventSourceFeature` for the
current module execution. Dispatch does not clear pending events.

See [../../docs/architecture/messaging.md](../../docs/architecture/messaging.md).
