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

Inbox registration returns registered, already-received, or already-processed
results using PostgreSQL `INSERT ... ON CONFLICT DO NOTHING` semantics.
Already-received but unprocessed rows stay operationally loud through the
module receive pipelines; this package does not add inbox leases, stale-row
recovery, failed receive states, or provider-neutral row mutation helpers.

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

The builder-level `UsePostgresPersistence(moduleName, connectionString, ...)`
overload delegates to the same module-level setup for the named module.

Handlers that need application SQL can take `IPostgresModuleSession` and
execute commands through the current connection and transaction.

The module transaction owns the commit boundary for handler SQL, inbox markers,
operation state, and outgoing outbox rows. The low-level inbox executor only
stages receive-side work inside that transaction.

The provider contributes passive durable module runtime registrations for the
module writer, inbox executor, and operation-state store into
`DurableModulePersistenceRegistrationRegistry`. Those registrations carry the
module name and create PostgreSQL executable services only for the selected
module inside the current DI scope, so resolving another module's runtime
metadata does not open or resolve this provider's
`IPostgresModuleSession`.

## Domain Events

`Bondstone.Persistence.Postgres` does not currently own module-local domain
event staging. Non-EF PostgreSQL modules may use `IPostgresModuleSession` to
persist application-owned domain event records inside their module
transaction, but Bondstone does not discover pending `IDomainEventSource`
instances, define a PostgreSQL domain-event staging table, clear pending
domain events, or map domain events to outbox, inbox, messaging, or transport
records.

`UsePostgresPersistence(...)` records the module persistence provider name but
does not record or require a CLR context type. Provider-specific details such
as schema, data source, session, transaction, and SQL behavior belong to this
provider's module services.

Outbox terminal status semantics are defined in
[persistence-core.md](persistence-core.md); this provider only records the
provider-specific PostgreSQL outcome update.

Module outbox dispatch remains provider-owned per module. When the app-facing
dispatcher aggregates module dispatchers, this provider still owns each
module's PostgreSQL claim, lease renewal, and outcome-recording SQL; the
aggregate dispatcher only supplies the sequential call order and remaining
batch budget.

Follow-up PostgreSQL non-EF persistence ideas that are outside the current
contract are tracked in [../backlog/00-plans.md](../backlog/00-plans.md).
