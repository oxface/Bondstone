# Bondstone.Persistence.EntityFrameworkCore.Postgres

PostgreSQL provider integration for Bondstone Entity Framework Core
persistence.

This package owns PostgreSQL-specific EF Core service registration,
duplicate/unique-violation classification, and provider-owned SQL for durable
outbox, direct receive inbox, durable incoming inbox, and module persistence
operations.

## Quick Path

Use this package with `Bondstone.Persistence.EntityFrameworkCore` for the
supported EF/PostgreSQL production durable persistence path. Normal modules call
`module.UsePostgreSqlPersistence<TDbContext>(...)` from their
`IBondstoneModule` registration as shown in
[the setup guide](https://github.com/oxface/Bondstone/blob/main/docs/setup.md).

Install this package in EF Core modules that store Bondstone durable messaging
state in PostgreSQL. It provides PostgreSQL provider helpers, duplicate
classification, and provider-owned SQL for durable claiming and mutation.
Applications generate, review, and apply their own module-owned EF migrations;
Bondstone does not ship package-owned migrations or automatic schema rollout.
Projects that only need provider-neutral EF mappings can reference
`Bondstone.Persistence.EntityFrameworkCore` instead.

See:

- [Setup guide](https://github.com/oxface/Bondstone/blob/main/docs/setup.md)
- [Package discovery](https://github.com/oxface/Bondstone/blob/main/docs/package-discovery.md)
- [Operations guide](https://github.com/oxface/Bondstone/blob/main/docs/operations.md)
- [Observability guide](https://github.com/oxface/Bondstone/blob/main/docs/observability.md)
- [Packaging, release, and migration policy](https://github.com/oxface/Bondstone/blob/main/docs/packaging.md)
- [Bondstone architecture](https://github.com/oxface/Bondstone/blob/main/docs/architecture.md)
- [EF/PostgreSQL persistence tests](https://github.com/oxface/Bondstone/tree/main/tests/Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests)
