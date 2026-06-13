# Bondstone.Capabilities.DomainEvents.EntityFrameworkCore

Entity Framework Core bridge for optional Bondstone module-local domain event
persistence.

This package owns EF change-tracker collection, EF record mapping, and the
capability pipeline contribution that stages domain event records inside an
observed EF module transaction.

The EF bridge does not dispatch local `IDomainEventHandler<TDomainEvent>`
handlers or map domain events to integration events. It persists module-local
domain event records only.

## Quick Path

Use this bridge with `Bondstone.Capabilities.DomainEvents` for EF-backed
modules that persist module-local domain event records. Map the domain event
records in the module `DbContext` and keep integration-event publishing
explicit in module handlers.

See [../../docs/architecture/persistence-ef-core.md](../../docs/architecture/persistence-ef-core.md).
