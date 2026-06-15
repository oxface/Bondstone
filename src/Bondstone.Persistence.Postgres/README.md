# Bondstone.Persistence.Postgres

PostgreSQL persistence proof for Bondstone durable messaging without EF Core.

This package is PostgreSQL-specific and Dapper-backed internally. It owns
PostgreSQL durable outbox, inbox, operation-state, and module transaction
behavior without depending on EF Core.

## Quick Path

Use this package for PostgreSQL modules that intentionally do not use EF Core.
Normal modules call `module.UsePostgresPersistence(...)` from their
`IBondstoneModule` registration as shown in
[the setup guide](https://github.com/oxface/Bondstone/blob/main/docs/setup.md).

Install this package when a module wants Dapper-backed PostgreSQL durable
messaging persistence and `IPostgresModuleSession` access instead of EF Core
module persistence. EF-backed modules should use
`Bondstone.Persistence.EntityFrameworkCore.Postgres`.

See:

- [Non-EF PostgreSQL persistence architecture](https://github.com/oxface/Bondstone/blob/main/docs/architecture/persistence-postgres.md)
- [Persistence contracts](https://github.com/oxface/Bondstone/blob/main/docs/architecture/persistence-core.md)
- [Non-EF PostgreSQL persistence tests](https://github.com/oxface/Bondstone/tree/main/tests/Bondstone.Persistence.Postgres.Tests)
