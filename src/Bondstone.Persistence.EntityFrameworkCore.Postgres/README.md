# Bondstone.Persistence.EntityFrameworkCore.Postgres

PostgreSQL provider integration for Bondstone Entity Framework Core
persistence.

This package owns PostgreSQL-specific EF Core service registration,
duplicate/unique-violation classification, and provider-owned SQL for durable
outbox, inbox, and module persistence operations.

## Quick Path

Use this package with `Bondstone.Persistence.EntityFrameworkCore` for
PostgreSQL EF-backed modules. Normal modules call
`module.UsePostgreSqlPersistence<TDbContext>(...)` from their
`IBondstoneModule` registration as shown in
[../../docs/setup.md](../../docs/setup.md).

See:

- [../../docs/architecture/persistence-postgresql.md](../../docs/architecture/persistence-postgresql.md)
- [../../docs/architecture/persistence-ef-core.md](../../docs/architecture/persistence-ef-core.md)
- [../../tests/Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests](../../tests/Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests)
