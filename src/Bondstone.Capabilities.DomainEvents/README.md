# Bondstone.Capabilities.DomainEvents

Optional module-local domain event capability contracts for Bondstone.

This package owns the domain event contracts. It is not a transport event bus,
does not provide module pipeline behavior by itself, and does not persist
domain events without a provider bridge such as
`Bondstone.Capabilities.DomainEvents.EntityFrameworkCore`.

Provider bridges own source discovery and persistence semantics for their
runtime.
Registering `IDomainEventHandler<TDomainEvent>` services does not cause
Bondstone to invoke them automatically.

## Quick Path

Add this package only when module domain model types expose module-local
domain events. Pair it with a provider bridge, such as
`Bondstone.Capabilities.DomainEvents.EntityFrameworkCore`, when those events
should be persisted. It is not an integration event transport package; use the
durable publish APIs from `Bondstone` for integration events.

See [../../docs/architecture/messaging.md](../../docs/architecture/messaging.md).
