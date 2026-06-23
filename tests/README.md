# Tests

This folder contains Bondstone tests grouped by package or integration
boundary.

Testing policy, categories, and commands are documented in
[../docs/testing.md](../docs/testing.md).

## Test Projects

- [Bondstone.Tests](Bondstone.Tests) covers core package behavior.
- [Bondstone.Hosting.Tests](Bondstone.Hosting.Tests) covers hosted worker
  composition.
- [Bondstone.Persistence.EntityFrameworkCore.Tests](Bondstone.Persistence.EntityFrameworkCore.Tests)
  covers EF Core mappings, boundaries, and EF-backed domain event persistence.
- [Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests](Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests)
  covers PostgreSQL EF integration, including EF-backed domain event
  transaction behavior.
- [Bondstone.PublicApi.Tests](Bondstone.PublicApi.Tests) covers checked-in
  public API baselines for packable packages.
- [Bondstone.Transport.Local.Tests](Bondstone.Transport.Local.Tests) covers
  local transport behavior.
- [Bondstone.Transport.RabbitMq.Tests](Bondstone.Transport.RabbitMq.Tests)
  covers RabbitMQ adapter behavior.
- [Bondstone.Transport.ServiceBus.Tests](Bondstone.Transport.ServiceBus.Tests)
  covers Azure Service Bus adapter behavior.
- [Bondstone.Composition.Tests](Bondstone.Composition.Tests) covers cross-package
  composition.
- [Bondstone.Samples.Tests](Bondstone.Samples.Tests) covers sample smoke tests.
- [Bondstone.Package.Tests](Bondstone.Package.Tests) covers produced package
  artifacts.
