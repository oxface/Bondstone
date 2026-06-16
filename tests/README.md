# Tests

This folder contains Bondstone tests grouped by package or integration
boundary.

Testing policy, categories, and commands are documented in
[../docs/testing.md](../docs/testing.md).

## Test Projects

- [Bondstone.Tests](Bondstone.Tests) covers core package behavior.
- [Bondstone.Capabilities.DomainEvents.Tests](Bondstone.Capabilities.DomainEvents.Tests)
  covers domain event capability contracts.
- [Bondstone.Capabilities.DomainEvents.EntityFrameworkCore.Tests](Bondstone.Capabilities.DomainEvents.EntityFrameworkCore.Tests)
  covers the EF Core domain event capability bridge.
- [Bondstone.Capabilities.DomainEvents.EntityFrameworkCore.Postgres.Tests](Bondstone.Capabilities.DomainEvents.EntityFrameworkCore.Postgres.Tests)
  covers PostgreSQL-backed EF domain event transaction behavior.
- [Bondstone.Hosting.Tests](Bondstone.Hosting.Tests) covers hosted worker
  composition.
- [Bondstone.Persistence.EntityFrameworkCore.Tests](Bondstone.Persistence.EntityFrameworkCore.Tests)
  covers EF Core mappings and boundaries.
- [Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests](Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests)
  covers PostgreSQL EF integration.
- [Bondstone.PublicApi.Tests](Bondstone.PublicApi.Tests) covers checked-in
  public API baselines for packable packages.
- [Bondstone.Transport.Local.Tests](Bondstone.Transport.Local.Tests) covers
  local transport behavior.
- [Bondstone.Transport.RabbitMq.Tests](Bondstone.Transport.RabbitMq.Tests)
  covers RabbitMQ transport behavior.
- [Bondstone.Composition.Tests](Bondstone.Composition.Tests) covers cross-package
  composition.
- [Bondstone.Samples.Tests](Bondstone.Samples.Tests) covers sample smoke tests.
