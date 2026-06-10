# Source Packages

This folder contains Bondstone package projects.

Package IDs, dependency direction, target framework, and release policy are
documented in [../docs/packaging.md](../docs/packaging.md).

## Packages

- [Bondstone](Bondstone) contains core abstractions.
- [Bondstone.Hosting](Bondstone.Hosting) contains hosted worker composition.
- [Bondstone.EntityFrameworkCore](Bondstone.EntityFrameworkCore) contains EF
  Core persistence mappings and boundaries.
- [Bondstone.EntityFrameworkCore.Postgres](Bondstone.EntityFrameworkCore.Postgres)
  contains PostgreSQL-specific EF Core integration.
- [Bondstone.Persistence.Postgres](Bondstone.Persistence.Postgres) contains
  PostgreSQL non-EF durable module persistence.
- [Bondstone.Transport.Local](Bondstone.Transport.Local) contains explicit
  local queue routing for samples, tests, and local development.
- [Bondstone.Transport.RabbitMq](Bondstone.Transport.RabbitMq) contains the
  RabbitMQ direct transport adapter.
- [Bondstone.Transport.ServiceBus](Bondstone.Transport.ServiceBus) contains the
  Azure Service Bus direct transport adapter.

See [../tests](../tests) for package and integration-boundary tests.
