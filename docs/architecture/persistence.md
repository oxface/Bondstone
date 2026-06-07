# Persistence Architecture

Persistence docs are split by ownership boundary:

- [persistence-core.md](persistence-core.md) describes provider-neutral
  contracts in `Bondstone`.
- [persistence-ef-core.md](persistence-ef-core.md) describes provider-neutral
  EF Core mappings, stores, and the EF persistence scope.
- [persistence-postgresql.md](persistence-postgresql.md) describes
  PostgreSQL-specific registration, SQL behavior, and integration coverage.

Current implementation and verification state is summarized in
[../mvp-plan.md](../mvp-plan.md). Historical extraction details remain in
[../archive/extraction-plan.md](../archive/extraction-plan.md).

## Cross-Cutting Rules

Core persistence contracts stay independent from EF Core, PostgreSQL, Rebus,
SQL locking, schema migration, and background dispatch mechanics.

EF Core components stage data and expose transaction/save boundaries, but they
do not own transport acknowledgement, retry policy, domain events, or a
generic mediator. Module command execution owns handler registration and now
has EF transaction pipeline groundwork for modules that opt into EF
persistence. Broader inbox/outbox receive orchestration remains future work.

Optional EF Core mapping helpers are proposed in
[ADR 0027](../adr/0027-optional-ef-core-persistence-mapping.md). Until that
decision is accepted and applied, `ApplyBondstonePersistence` remains the
current generic EF Core mapping entrypoint.

Domain event persistence is proposed as an optional module boundary capability
in [ADR 0028](../adr/0028-domain-event-persistence-capability.md). It is not
part of the current implemented persistence contract.

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
- operation-state transition policy or optimistic concurrency;
- provider-specific schemas, migration commands, or payload storage such as
  PostgreSQL `jsonb`.

Those decisions require ADR review before broad implementation.
