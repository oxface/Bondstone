# Bondstone.Capabilities.DomainEvents.EntityFrameworkCore

Entity Framework Core bridge for optional Bondstone module-local domain event
persistence.

This package owns EF change-tracker collection, EF record mapping, and the
system pipeline behavior that stages domain event records inside an observed
EF module transaction.

The EF bridge does not dispatch local `IDomainEventHandler<TDomainEvent>`
handlers or map domain events to integration events. It persists module-local
domain event records only.

See [../../docs/architecture/persistence-ef-core.md](../../docs/architecture/persistence-ef-core.md).
