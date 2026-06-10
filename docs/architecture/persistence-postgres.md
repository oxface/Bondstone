# PostgreSQL Persistence

`Bondstone.Persistence.Postgres` owns non-EF PostgreSQL persistence for
Bondstone durable module messaging.

The package is PostgreSQL-specific and Dapper-backed internally. It implements
durable module messaging persistence without `DbContext`:

- outbox writing for durable commands and integration events;
- inbox registration and handle-once execution;
- operation-state read/write for the current command-loop states;
- module-owned command and integration event subscriber transactions;
- module outbox dispatch over PostgreSQL-backed durable records;
- a scoped `IPostgresModuleSession` exposing the current
  `NpgsqlConnection` and transaction for module handler SQL.

Dapper is an implementation helper, not a generic product abstraction. The
public API is provider-specific and keeps PostgreSQL session/transaction
ownership inside this package.

The package supports one `NpgsqlDataSource` per service provider. Multiple
PostgreSQL modules can use separate schemas in the same database. Modules that
require different connection strings should run in separate service providers.

## Schema

The provider reuses the current durable table names and column shape:

- `outbox_messages`
- `inbox_messages`
- `operation_states`

Schema creation remains application-owned for production. The package exposes
`PostgresSchema.EnsureDurableMessagingTablesAsync(...)` as an explicit
proof/test/sample helper; normal provider registration does not silently
create or migrate schemas.

## Module Usage

Modules opt in through:

```csharp
module.UseDurableMessaging();
module.UsePostgresPersistence(connectionString, schema: "billing");
```

Handlers that need application SQL can take `IPostgresModuleSession` and
execute commands through the current connection and transaction.

`UsePostgresPersistence(...)` records the module persistence provider name but
does not record or require a CLR context type. Provider-specific details such
as schema, data source, session, transaction, and SQL behavior belong to this
provider's module services.

Outbox terminal status semantics are defined in
[persistence-core.md](persistence-core.md); this provider only records the
provider-specific PostgreSQL outcome update.

Follow-up PostgreSQL non-EF persistence ideas that are outside the current
contract are tracked in [../backlog/09-future-work.md](../backlog/09-future-work.md).
