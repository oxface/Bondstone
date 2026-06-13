# Bondstone.Persistence.EntityFrameworkCore

Entity Framework Core abstractions for Bondstone durable persistence.

This package owns EF Core entity mappings, stores, persistence scope, and
module transaction behavior for EF-backed modules.

## Quick Path

Use this package for EF-backed modules, together with a provider package such
as `Bondstone.Persistence.EntityFrameworkCore.Postgres`. Map durable tables
with `ApplyBondstonePersistence()` or the granular mapping helpers, then use
the provider module helper shown in [../../docs/setup.md](../../docs/setup.md).

See:

- [../../docs/architecture/persistence-ef-core.md](../../docs/architecture/persistence-ef-core.md)
- [../../docs/architecture/persistence-core.md](../../docs/architecture/persistence-core.md)
- [../../tests/Bondstone.Persistence.EntityFrameworkCore.Tests](../../tests/Bondstone.Persistence.EntityFrameworkCore.Tests)
