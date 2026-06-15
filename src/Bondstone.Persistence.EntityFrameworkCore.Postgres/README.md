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
[the setup guide](https://github.com/oxface/Bondstone/blob/main/docs/setup.md).

Install this package in EF Core modules that store Bondstone durable messaging
state in PostgreSQL. It provides the PostgreSQL provider behavior; projects
that only need provider-neutral EF mappings can reference
`Bondstone.Persistence.EntityFrameworkCore` instead.

See:

- [EF/PostgreSQL persistence architecture](https://github.com/oxface/Bondstone/blob/main/docs/architecture/persistence-postgresql.md)
- [EF Core persistence architecture](https://github.com/oxface/Bondstone/blob/main/docs/architecture/persistence-ef-core.md)
- [EF/PostgreSQL persistence tests](https://github.com/oxface/Bondstone/tree/main/tests/Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests)
