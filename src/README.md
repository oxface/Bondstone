# Source Packages

This folder contains Bondstone package projects.

Package IDs, dependency direction, target framework, and release policy are
documented in [../docs/packaging.md](../docs/packaging.md).

Start with [../docs/setup.md](../docs/setup.md) for normal host composition.
Use the package READMEs below to choose the package set for a module,
persistence provider, transport adapter, or optional capability.

## Packages

- [Bondstone](Bondstone) contains core abstractions, including module-local
  domain event contracts.
- [Bondstone.Hosting](Bondstone.Hosting) contains hosted worker composition.
- [Bondstone.Persistence](Bondstone.Persistence) contains provider-neutral
  durable persistence contracts and records.
- [Bondstone.Persistence.EntityFrameworkCore](Bondstone.Persistence.EntityFrameworkCore) contains EF
  Core persistence mappings, boundaries, and optional EF-backed domain event
  persistence.
- [Bondstone.Persistence.EntityFrameworkCore.Postgres](Bondstone.Persistence.EntityFrameworkCore.Postgres)
  contains PostgreSQL-specific EF Core integration.
- [Bondstone.Transport.Local](Bondstone.Transport.Local) contains explicit
  local queue routing for samples, tests, and local development.
- [Bondstone.Transport.RabbitMq](Bondstone.Transport.RabbitMq) contains the
  RabbitMQ direct transport adapter.

See [../tests](../tests) for package and integration-boundary tests.
