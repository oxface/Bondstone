# PostgreSQL Persistence

`Bondstone.EntityFrameworkCore.Postgres` owns PostgreSQL-specific persistence
behavior for Bondstone EF Core mappings.

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
retry scheduling, and dead-letter outcomes only when the row is still
processing, still owned by the supplied claimant, and still inside the active
claim lease.

`PostgreSqlDurableInboxRegistrar<TDbContext>` returns explicit registered,
already-received, or already-processed results without using duplicate
exceptions as the public flow.

`AddBondstonePostgreSqlPersistence<TDbContext>` configures Npgsql for a
consumer-owned DbContext and composes the provider-neutral EF registrations,
including the EF persistence scope and neutral inbox handler executor.

For module-owned durable persistence,
`module.UsePostgreSqlPersistence<TDbContext>(connectionString, schema: ...)`
is the preferred setup shape. It records EF module persistence metadata and
binds PostgreSQL durable components for that module's EF context. Root-level
`UsePostgreSqlPersistence<TDbContext>(moduleName, connectionString, schema:
...)` remains available for compatibility and advanced composition. Those
module bindings provide source-module outbox writing, target-module inbox
handling, target-module operation-state persistence, and per-module outbox
dispatch. The app-facing dispatcher can aggregate dispatch results across
configured local module outboxes while each underlying claim, lease, and
dispatch-record update remains scoped to one module's PostgreSQL tables.

Application code should prefer this module-aware setup helper over directly
registering the provider-facing `IDurableModule*` persistence services.

## Integration Tests

PostgreSQL Testcontainers tests verify real database behavior, including:

- schema creation and stable primary-key names;
- transaction commit/rollback;
- savepoint rollback after duplicate inbox inserts;
- inbox processed timestamps and registration outcomes;
- operation-state updates;
- outbox claim lease columns;
- `FOR UPDATE SKIP LOCKED` selection;
- outbox claiming for due rows, scheduled rows, locked-row skipping, expired
  lease reclaim, and active lease exclusion;
- outbox lease renewal for active claims, wrong owners, expired leases, and
  non-processing rows;
- outbox dispatch success, retry, dead-letter, stale claimant, and expired
  lease outcomes;
- outbox dispatcher composition using real PostgreSQL claim, lease renewal,
  and dispatch outcome recording with fake transport success and failure;
- schema-aware provider registration and composition with the EF persistence
  scope.

Follow-up PostgreSQL ideas that are outside the current contract are tracked
in [../backlog/09-future-work.md](../backlog/09-future-work.md).
