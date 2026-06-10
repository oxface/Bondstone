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

Current implementation and verification state is summarized in
[../mvp-plan.md](../mvp-plan.md). Historical extraction details remain in
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

Optional EF Core mapping helpers are accepted in
[ADR 0027](../adr/0027-optional-ef-core-persistence-mapping.md).
`ApplyBondstonePersistence` remains the convenience entrypoint for the full
generic EF Core mapping shape, while `ApplyBondstoneOutbox`,
`ApplyBondstoneInbox`, and `ApplyBondstoneOperationState` let hosts map only
the durable persistence pieces a DbContext needs. The current EF module
runtime validates that modules using `UseDurableMessaging` with EF persistence
have outbox and inbox mappings in their DbContext model. Operation-state and
provider-specific schema validation remain deferred.

Module-owned durable EF persistence is accepted in
[ADR 0032](../adr/0032-module-owned-durable-ef-persistence.md). In the current
command loop, durable sends resolve outbox and pending operation-state writes
from the source module persistence context. Durable receives resolve inbox
handling, successful operation completion, handler state, and any outgoing
outbox writes from the target module persistence context. Existing
single-`DbContext` setups remain supported, but modular-monolith samples should
prefer one module-owned `DbContext` per module.

Domain event persistence is proposed as an optional module boundary capability
in [ADR 0028](../adr/0028-domain-event-persistence-capability.md). It is not
part of the current implemented persistence contract.

Provider packages own provider-specific SQL, locking, conflict detection,
schema targeting, and integration tests. Provider-owned SQL should reuse table,
column, and constraint names from provider-neutral mappings when those mappings
define the persisted shape.

`Bondstone.Persistence.Postgres` is accepted by
[ADR 0035](../adr/0035-postgresql-dapper-persistence-proof.md) as the first
non-EF persistence proof. It is PostgreSQL-specific and Dapper-backed, but
Dapper is an internal implementation helper rather than the public
abstraction. The package should implement the core `Bondstone` persistence
contracts directly and own its connection/session and transaction boundary
without depending on EF entity mappings, `DbContext`, or
`IEntityFrameworkCorePersistenceScope`.
The proof should include a mixed-persistence sample module before reliability
work hardens too deeply around EF Core.

## Deferred Persistence Decisions

The current contracts intentionally do not decide:

- a public unit-of-work abstraction beyond the current module command
  pipeline shape;
- Bondstone-owned migration helpers or provider-specific migration
  conventions;
- stale-claim recovery, dead-letter routing, cleanup/maintenance workers, or
  advanced worker configuration;
- inbox handler discovery, stale receive recovery, receive retry policy, and
  transport acknowledgement coordination;
- module identity scopes beyond the current source-module execution context
  and higher-level transaction helpers above the EF persistence scope;
- domain event collection and persistence;
- advanced operation-state transition policy or optimistic concurrency beyond
  current `Pending` and successful `Completed` command-loop updates;
- provider-specific schemas, migration commands, or payload storage such as
  PostgreSQL `jsonb`.

Those decisions require ADR review before broad implementation.
