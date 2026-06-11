# Bondstone.Capabilities.DomainEvents

Optional module-local domain event capability contracts for Bondstone.

This package owns the domain event contracts. It is not a transport event bus,
does not provide module pipeline behavior by itself, and does not persist
domain events without a provider bridge such as
`Bondstone.Capabilities.DomainEvents.EntityFrameworkCore`.

Provider bridges own source discovery and persistence semantics for their
runtime.
Local handler dispatch is intentionally deferred in the current runtime:
registering `IDomainEventHandler<TDomainEvent>` services does not cause
Bondstone to invoke them automatically.

See [../../docs/architecture/messaging.md](../../docs/architecture/messaging.md).
