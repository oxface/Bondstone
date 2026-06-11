# Bondstone.Persistence.EntityFrameworkCore.Postgres

PostgreSQL provider integration for Bondstone Entity Framework Core
persistence.

This package owns PostgreSQL-specific EF Core service registration,
duplicate/unique-violation classification, and provider-owned SQL for durable
outbox, inbox, and module persistence operations.

See:

- [../../docs/architecture/persistence-postgresql.md](../../docs/architecture/persistence-postgresql.md)
- [../../docs/architecture/persistence-ef-core.md](../../docs/architecture/persistence-ef-core.md)
- [../../tests/Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests](../../tests/Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests)
