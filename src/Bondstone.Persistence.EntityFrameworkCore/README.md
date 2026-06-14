# Bondstone.Persistence.EntityFrameworkCore

Entity Framework Core abstractions for Bondstone durable persistence.

This package owns EF Core entity mappings, stores, persistence scope, and
module transaction behavior for EF-backed modules.

## Quick Path

Use this package for EF-backed modules, together with a provider package such
as `Bondstone.Persistence.EntityFrameworkCore.Postgres`. Map durable tables
with `ApplyBondstonePersistence()` or the granular mapping helpers, then use
the provider module helper shown in
[the setup guide](https://github.com/oxface/Bondstone/blob/main/docs/setup.md).

Install this package in projects that define EF Core `DbContext` mappings for
Bondstone durable outbox, inbox, and operation state, or that compose
provider-neutral EF Core module persistence.

See:

- [EF Core persistence architecture](https://github.com/oxface/Bondstone/blob/main/docs/architecture/persistence-ef-core.md)
- [Persistence contracts](https://github.com/oxface/Bondstone/blob/main/docs/architecture/persistence-core.md)
- [EF Core persistence tests](https://github.com/oxface/Bondstone/tree/main/tests/Bondstone.Persistence.EntityFrameworkCore.Tests)
