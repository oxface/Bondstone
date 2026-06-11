# Source Packages

This folder contains Bondstone package projects.

Package IDs, dependency direction, target framework, and release policy are
documented in [../docs/packaging.md](../docs/packaging.md).

## Packages

- [Bondstone](Bondstone) contains core abstractions.
- [Bondstone.Capabilities.DomainEvents](Bondstone.Capabilities.DomainEvents)
  contains optional module-local domain event capability contracts.
- [Bondstone.Capabilities.DomainEvents.EntityFrameworkCore](Bondstone.Capabilities.DomainEvents.EntityFrameworkCore)
  contains the EF Core bridge for domain event persistence.
- [Bondstone.Hosting](Bondstone.Hosting) contains hosted worker composition.
- [Bondstone.Persistence](Bondstone.Persistence) contains provider-neutral
  durable persistence contracts and records.
- [Bondstone.Persistence.EntityFrameworkCore](Bondstone.Persistence.EntityFrameworkCore) contains EF
  Core persistence mappings and boundaries.
- [Bondstone.Persistence.EntityFrameworkCore.Postgres](Bondstone.Persistence.EntityFrameworkCore.Postgres)
  contains PostgreSQL-specific EF Core integration.
- [Bondstone.Persistence.Postgres](Bondstone.Persistence.Postgres) contains
  PostgreSQL non-EF durable module persistence.
- [Bondstone.Transport](Bondstone.Transport) contains provider-neutral
  transport topology diagnostic contracts.
- [Bondstone.Transport.Local](Bondstone.Transport.Local) contains explicit
  local queue routing for samples, tests, and local development.
- [Bondstone.Transport.RabbitMq](Bondstone.Transport.RabbitMq) contains the
  RabbitMQ direct transport adapter.
- [Bondstone.Transport.ServiceBus](Bondstone.Transport.ServiceBus) contains the
  Azure Service Bus direct transport adapter.

See [../tests](../tests) for package and integration-boundary tests.
