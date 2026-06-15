# PostgreSQL Persistence

`Bondstone.Persistence.EntityFrameworkCore.Postgres` owns PostgreSQL-specific
persistence behavior for Bondstone EF Core mappings.

## Provider Responsibilities

The PostgreSQL package owns:

- Npgsql service registration;
- PostgreSQL duplicate/unique-violation classification;
- PostgreSQL inbox registration using `INSERT ... ON CONFLICT DO NOTHING`;
- PostgreSQL outbox claiming using `FOR UPDATE SKIP LOCKED`;
- PostgreSQL outbox lease renewal using claim-owner and lease-aware `UPDATE`
  statements;
- PostgreSQL outbox dispatch outcome recording using claim-owner and
  lease-aware `UPDATE` statements.

Provider-owned SQL uses table, column, and constraint names from the
provider-neutral EF Core mappings with PostgreSQL identifier quoting. Consumers
that map Bondstone persistence to a non-default schema must pass the same
schema to PostgreSQL registration so provider-owned SQL targets the mapped
tables.

`PostgreSqlDurableOutboxClaimer<TDbContext>` claims due pending rows and
expired processing rows, writes claim lease state, respects scheduled pending
rows, and returns claimed records.

`PostgreSqlDurableOutboxLeaseRenewer<TDbContext>` extends the claim lease for
one active processing row when the claimant still owns an unexpired lease.

`PostgreSqlDurableOutboxDispatchRecorder<TDbContext>` records dispatch success,
retry scheduling, and terminal-failure outcomes only when the row is still
processing, still owned by the supplied claimant, and still inside the active
claim lease. The provider follows the core outbox terminal status contract in
[persistence-core.md](persistence-core.md).

`PostgreSqlDurableInboxRegistrar<TDbContext>` returns explicit registered,
already-received, or already-processed results without using duplicate
exceptions as the public flow. It does not implement inbox leases, stale
receive recovery, failed receive states, or row mutation helpers for
already-received unprocessed rows.

`AddBondstonePostgreSqlPersistence<TDbContext>` configures Npgsql for a
consumer-owned DbContext and composes the provider-neutral EF registrations,
including the EF persistence scope and neutral inbox handler executor.

Those root-level services may be used for advanced single-store fallback write
and receive composition when no module-owned durable persistence services are
registered, but they are not the preferred module-boundary path. Operation
reads use module-owned operation-state stores and do not fall back to root EF
operation readers or stores.

For module-owned durable persistence,
`module.UsePostgreSqlPersistence<TDbContext>(connectionString, schema: ...)`
is the preferred setup shape. It records EF module persistence metadata and
binds PostgreSQL durable components for that module's EF context. Root-level
`UsePostgreSqlPersistence<TDbContext>(moduleName, connectionString, schema:
...)` delegates to the same module-level setup for the named module. Those
module bindings provide source-module outbox writing, target-module inbox
handling, target-module operation-state persistence, transaction pipeline
participation, and per-module outbox dispatch. The app-facing dispatcher can
aggregate dispatch results across configured local module outboxes while each
underlying claim, lease, and dispatch-record update remains scoped to one
module's PostgreSQL tables. The aggregate worker topology does not change
PostgreSQL ownership: module dispatchers still perform provider-specific
claim, lease renewal, and outcome recording for their module, while the
aggregate dispatcher only chooses the sequential call order and shared batch
budget.

Command and receive execution use passive durable module runtime registrations
for the module writer, inbox executor, and operation-state store stored in
`DurableModulePersistenceRegistrationRegistry`. Those registrations carry the
module name and create EF-backed executable services only for the selected
module inside the current DI scope, so resolving another module's runtime
metadata does not construct this module's `DbContext`.

Application code should prefer this module-aware setup helper over directly
registering provider-facing durable module runtime registrations.

## Integration Tests

PostgreSQL Testcontainers tests verify real database behavior, including:

- schema creation and stable primary-key names;
- transaction commit/rollback;
- savepoint rollback after duplicate inbox inserts;
- inbox processed timestamps and registration outcomes;
- operation-state updates, including nullable result diagnostic context;
- outbox claim lease columns;
- `FOR UPDATE SKIP LOCKED` selection;
- outbox claiming for due rows, scheduled rows, locked-row skipping, expired
  lease reclaim, and active lease exclusion;
- outbox lease renewal for active claims, wrong owners, expired leases, and
  non-processing rows;
- outbox dispatch success, retry, terminal failure, stale claimant, and
  expired lease outcomes;
- outbox dispatcher composition using real PostgreSQL claim, lease renewal,
  and dispatch outcome recording with fake transport success and failure;
- schema-aware provider registration and composition with the EF persistence
  scope.
- single-root EF fallback composition where root PostgreSQL EF services handle
  outbox, inbox, and operation-state writes when modules declare EF persistence
  but no module-owned durable persistence services are registered.
