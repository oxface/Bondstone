# PostgreSQL Dapper Persistence

`Bondstone.Persistence.Dapper.Postgres` owns the first non-EF persistence proof
for Bondstone durable module messaging.

## Current Scope

The package is PostgreSQL-specific and Dapper-assisted. It implements durable
module messaging persistence without `DbContext`:

- outbox writing for durable commands and integration events;
- inbox registration and handle-once execution;
- operation-state read/write for the current command-loop states;
- module-owned command and integration event subscriber transactions;
- module outbox dispatch over PostgreSQL-backed durable records;
- a scoped `IPostgresDapperModuleSession` exposing the current
  `NpgsqlConnection` and transaction for module handler SQL.

Dapper is an implementation helper, not a generic product abstraction. The
public API is provider-specific and keeps PostgreSQL session/transaction
ownership inside this package.

The current proof supports one `NpgsqlDataSource` per service provider. Multiple
PostgreSQL/Dapper modules can use separate schemas in the same database. Modules
that require different connection strings should run in separate service
providers until a later ADR accepts multi-data-source selection.

## Schema

The provider reuses the current durable table names and column shape:

- `outbox_messages`
- `inbox_messages`
- `operation_states`

Schema creation remains application-owned for production. The package exposes
`PostgresDapperSchema.EnsureDurableMessagingTablesAsync(...)` as an explicit
proof/test/sample helper; normal provider registration does not silently
create or migrate schemas.

## Module Usage

Modules opt in through:

```csharp
module.UseDurableMessaging();
module.UsePostgresDapperPersistence(connectionString, schema: "billing");
```

Handlers that need application SQL can take `IPostgresDapperModuleSession` and
execute commands through the current connection and transaction.

## Deferred Work

Production migration helpers, stale outbox claim recovery, stale inbox receive
recovery, cleanup workers, advanced operation-state transitions,
multi-data-source selection, and a generic Dapper/provider abstraction remain
later decisions.
