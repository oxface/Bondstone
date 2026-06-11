# Bondstone.Capabilities.DomainEvents.EntityFrameworkCore

Entity Framework Core bridge for optional Bondstone module-local domain event
persistence.

This package owns EF change-tracker collection, EF record mapping, and the
system pipeline behavior that stages domain event records inside an observed
EF module transaction.

See [../../docs/architecture/persistence-ef-core.md](../../docs/architecture/persistence-ef-core.md).
