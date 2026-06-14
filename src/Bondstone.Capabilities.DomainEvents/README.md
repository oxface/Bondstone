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

Install this package in modules whose domain model exposes module-local domain
events through `IDomainEventSource`. Do not install it as a replacement for
integration events, transport publishing, or durable outbox setup.

See:

- [Messaging architecture](https://github.com/oxface/Bondstone/blob/main/docs/architecture/messaging.md)
- [Package discovery](https://github.com/oxface/Bondstone/blob/main/docs/package-discovery.md)
