# Persistence Architecture

Persistence docs are split by ownership boundary:

- [persistence-core.md](persistence-core.md) describes provider-neutral
  contracts in `Bondstone`.
- [persistence-ef-core.md](persistence-ef-core.md) describes provider-neutral
  EF Core mappings, stores, and the EF persistence scope.
- [persistence-postgresql.md](persistence-postgresql.md) describes
  PostgreSQL-specific registration, SQL behavior, and integration coverage.
- [persistence-postgres.md](persistence-postgres.md) describes
  the PostgreSQL-specific non-EF persistence proof.

Historical extraction details remain in
[../archive/extraction-plan.md](../archive/extraction-plan.md).

## Cross-Cutting Rules

Core persistence contracts stay independent from EF Core, PostgreSQL,
transport adapters, SQL locking, schema migration, and background dispatch
mechanics.

EF Core components stage data and expose transaction/save boundaries, but they
do not own transport acknowledgement, retry policy, domain events, or a
generic mediator. Module command execution and module event subscriber
execution own handler registration; modules that opt into EF persistence get
EF transaction pipeline behaviors for command handlers and event subscribers.
Transport-backed receive orchestration belongs in direct provider adapters
that call the provider-neutral module receive pipelines.

`ApplyBondstonePersistence` remains the convenience entrypoint for the full
generic EF Core mapping shape, while `ApplyBondstoneOutbox`,
`ApplyBondstoneInbox`, and `ApplyBondstoneOperationState` let hosts map only
the durable persistence pieces a DbContext needs. The current EF module
runtime validates that modules using `UseDurableMessaging` with EF persistence
have outbox and inbox mappings in their DbContext model.

In the command loop, durable sends resolve outbox and pending operation-state
writes from the source module persistence context. Durable receives resolve
inbox handling, successful operation completion, handler state, and any
outgoing outbox writes from the target module persistence context. Existing
single-`DbContext` setups remain supported, but modular-monolith samples should
prefer one module-owned `DbContext` per module.

Already-received but unprocessed inbox rows remain operationally loud. Current
Bondstone persistence has no inbox lease, stale-row sweeper, failed receive
state, or provider-neutral recovery hook that can prove handler re-execution
is safe. Applications that need to recover those rows own the inspection,
mutation, audit, and provider/broker coordination until a later ADR accepts a
durable Bondstone recovery model.

Module persistence metadata remains on current module registration. The
provider name marks that the module declares persistence and lets provider
transaction behaviors decide whether they own command or event subscriber
execution. EF Core module persistence also records the module `DbContext` type
so EF transaction behavior can resolve the context and validate required
durable messaging mappings. Non-EF providers keep provider-specific metadata
such as schema, session, connection, and SQL configuration in provider-owned
services.

Fallback non-module persistence services are intentionally supported advanced
composition for now. When no module-owned durable persistence implementations
are registered, core resolvers may use root-level outbox writer, inbox handler
executor, operation-state store, and operation reader services. This is useful
for low-level single-store composition and compatibility tests, but it is not
the preferred module-boundary setup.

Domain event persistence is not part of the current persistence contract.

Provider packages own provider-specific SQL, locking, conflict detection,
schema targeting, and integration tests. Provider-owned SQL should reuse table,
column, and constraint names from provider-neutral mappings when those mappings
define the persisted shape.

`Bondstone.Persistence.Postgres` is PostgreSQL-specific and Dapper-backed, but
Dapper is an internal implementation helper rather than the public
abstraction. The package implements the core `Bondstone` persistence contracts
directly and owns its connection/session and transaction boundary without
depending on EF entity mappings, `DbContext`, or
`IEntityFrameworkCorePersistenceScope`.

Persistence ideas outside the current contract are tracked in
[../backlog/09-future-work.md](../backlog/09-future-work.md).
