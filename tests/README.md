# Tests

This folder contains Bondstone tests grouped by package or integration
boundary.

Testing policy, categories, and commands are documented in
[../docs/testing.md](../docs/testing.md).

## Test Projects

- [Bondstone.Tests](Bondstone.Tests) covers core package behavior.
- [Bondstone.Hosting.Tests](Bondstone.Hosting.Tests) covers hosted worker
  composition.
- [Bondstone.EntityFrameworkCore.Tests](Bondstone.EntityFrameworkCore.Tests)
  covers EF Core mappings and boundaries.
- [Bondstone.EntityFrameworkCore.Postgres.Tests](Bondstone.EntityFrameworkCore.Postgres.Tests)
  covers PostgreSQL EF integration.
- [Bondstone.Persistence.Postgres.Tests](Bondstone.Persistence.Postgres.Tests)
  covers non-EF PostgreSQL persistence.
- [Bondstone.Transport.Local.Tests](Bondstone.Transport.Local.Tests) covers
  local transport behavior.
- [Bondstone.Transport.RabbitMq.Tests](Bondstone.Transport.RabbitMq.Tests)
  covers RabbitMQ transport behavior.
- [Bondstone.Transport.ServiceBus.Tests](Bondstone.Transport.ServiceBus.Tests)
  covers Service Bus transport behavior.
- [Bondstone.Composition.Tests](Bondstone.Composition.Tests) covers cross-package
  composition.
- [Bondstone.Samples.Tests](Bondstone.Samples.Tests) covers sample smoke tests.
