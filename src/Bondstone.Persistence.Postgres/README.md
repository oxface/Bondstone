# Bondstone.Persistence.Postgres

PostgreSQL persistence proof for Bondstone durable messaging without EF Core.

This package is PostgreSQL-specific and Dapper-backed internally. It owns
PostgreSQL durable outbox, inbox, operation-state, and module transaction
behavior without depending on EF Core.

## Quick Path

Use this package for PostgreSQL modules that intentionally do not use EF Core.
Normal modules call `module.UsePostgresPersistence(...)` from their
`IBondstoneModule` registration as shown in
[../../docs/setup.md](../../docs/setup.md).

See:

- [../../docs/architecture/persistence-postgres.md](../../docs/architecture/persistence-postgres.md)
- [../../docs/architecture/persistence-core.md](../../docs/architecture/persistence-core.md)
- [../../tests/Bondstone.Persistence.Postgres.Tests](../../tests/Bondstone.Persistence.Postgres.Tests)
