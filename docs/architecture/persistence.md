# Persistence Architecture

Persistence docs are split by ownership boundary:

- [persistence-core.md](persistence-core.md) describes provider-neutral
  contracts in `Bondstone`.
- [persistence-ef-core.md](persistence-ef-core.md) describes provider-neutral
  EF Core mappings, stores, and the EF persistence scope.
- [persistence-postgresql.md](persistence-postgresql.md) describes
  PostgreSQL-specific registration, SQL behavior, and integration coverage.

Current implementation and verification state is summarized in
[../status.md](../status.md). Tactical extraction details remain in
[../extraction-plan.md](../extraction-plan.md).

## Cross-Cutting Rules

Core persistence contracts stay independent from EF Core, PostgreSQL, Rebus,
SQL locking, schema migration, and background dispatch mechanics.

EF Core components stage data and expose transaction/save boundaries, but they
do not own transport acknowledgement, retry policy, handler discovery, domain
events, or a generic mediator.

Provider packages own provider-specific SQL, locking, conflict detection,
schema targeting, and integration tests. Provider-owned SQL should reuse table,
column, and constraint names from provider-neutral mappings when those mappings
define the persisted shape.

Future non-EF providers such as Dapper or direct ADO.NET packages should
implement the core `Bondstone` persistence contracts directly and own their
connection or transaction boundary in their own package. They should not depend
on EF entity mappings, `DbContext`, or `IEntityFrameworkCorePersistenceScope`.

## Deferred Persistence Decisions

The current contracts intentionally do not decide:

- a public unit-of-work abstraction;
- Bondstone-owned migration helpers or provider-specific migration
  conventions;
- hosted outbox worker loops, transport adapter implementations, stale-claim
  recovery, dead-letter routing, configuration binding, or hosted worker
  registration;
- inbox handler discovery, stale receive recovery, receive retry policy, and
  transport acknowledgement coordination;
- module identity scopes, domain-event capture, and higher-level transaction
  helpers above the EF persistence scope;
- operation-state transition policy or optimistic concurrency;
- provider-specific schemas, migration commands, or payload storage such as
  PostgreSQL `jsonb`.

Those decisions require ADR review before broad implementation.
