# Bondstone.Capabilities.DomainEvents.EntityFrameworkCore

Entity Framework Core bridge for optional Bondstone module-local domain event
persistence.

This package owns EF change-tracker collection, EF record mapping, and the
system pipeline behavior that stages domain event records inside an observed
EF module transaction.

The EF bridge exposes pending domain event sources through
`IDomainEventSourceFeature` while its persistence behavior is active. It does
not dispatch handlers by itself; modules that also call
`UseDomainEventDispatch()` get provider-neutral local dispatch before EF
collection and staging.

See [../../docs/architecture/persistence-ef-core.md](../../docs/architecture/persistence-ef-core.md).
