# 0009 EF Core Persistence Entities And Migrations

Status: Amended
Application: Applied
Date: 2026-06-04

## Context

Bondstone now has provider-neutral core contracts for durable message
envelopes, outbox records, inbox records, operation state, and outbox dispatch
state. The next extraction step is the `Bondstone.EntityFrameworkCore` package,
which needs entity shapes and model mappings before PostgreSQL-specific
claiming, locking, and migrations can be implemented.

Entity shape and migration policy are durable decisions because they influence
public package behavior, persisted schemas, provider adapters, integration
tests, and consumer upgrade paths.

The historical source repository mixed EF entities, retry policy, dispatch
claiming, PostgreSQL behavior, and module unit-of-work concerns. Bondstone
should extract those concerns gradually and avoid turning the generic EF Core
package into a provider-specific implementation.

## Decision

`Bondstone.EntityFrameworkCore` owns provider-neutral EF Core entity classes,
model configuration, and service-registration helpers for durable outbox,
inbox, and operation-state storage.

The generic EF Core package must not own provider-specific SQL, locks,
claiming, savepoint conflict detection, or PostgreSQL annotations. Provider
packages can extend the generic mappings when provider behavior is needed.

Service-registration helpers can wire the provider-neutral EF Core
implementations to core contracts for a consumer-owned DbContext type, but they
must not configure a database provider, migrations, hosted dispatchers, locks,
or retry policy.

EF Core entity types should be named as entities rather than core records, so
they do not blur the boundary between stable core contracts and persistence
implementation details.

Model configuration should use stable, provider-neutral table and column
shapes that preserve the core contract fields:

- outbox message envelope fields;
- outbox dispatch status and retry scheduling fields;
- inbox message deduplication key and processing timestamps;
- durable operation state fields.

Provider-neutral EF mappings own stable table, column, and primary-key names
for the Bondstone tables. Provider packages can use those names when building
provider-specific SQL or interpreting provider exceptions, but the persisted
shape is not a PostgreSQL-only concept. Expose name constants only when another
package needs to reuse them.

Migrations are consumer-owned for now. Bondstone provides model mappings and
documentation; application modules or samples generate and apply migrations in
their own DbContext projects. A later ADR may introduce migration helpers,
embedded migrations, or provider-specific migration conventions after samples
and integration tests prove the need.

EF Core persistence components stage entities in the current DbContext. They do
not call `SaveChangesAsync`; caller-owned DbContext transactions or module
unit-of-work code commit source state, outbox messages, and operation state
atomically.

## Amendment 2026-06-05

ADR 0015 adds a provider-neutral `IDurableInboxHandlerExecutor` in `Bondstone`.
The generic EF Core package still does not own handler execution, transaction
helper APIs, transport acknowledgement, retry policy, or a public unit-of-work
abstraction. EF Core stores continue to stage data, while the handle-once
executor requires a caller-supplied commit delegate.

ADR 0016 adds the first EF-specific persistence scope. The EF Core package now
owns `IEntityFrameworkCorePersistenceScope` as a transaction and explicit
`SaveChangesAsync` companion for lower-level durable primitives. Higher-level
module identity scopes and public unit-of-work abstractions remain deferred.

## Consequences

The EF Core package can implement persistence without importing PostgreSQL
claiming or transport concerns.

Consumers retain control over schema names, DbContext ownership, and migration
history.

The first EF Core slice can be tested through metadata and value-mapping unit
tests without requiring a database provider.

Fast EF Core InMemory tests can verify change-tracker staging boundaries, but
real persistence semantics require Testcontainers-backed integration tests.

Provider-specific packages still need ADR review before adding locking,
claiming, SQL, or migration conventions.

Retry policy, dead-letter ownership, lease semantics, public unit-of-work
abstractions, higher-level transaction helper APIs, and transport-level inbox
orchestration remain deferred.

## Application Notes

- Current contract: `Bondstone.EntityFrameworkCore` owns provider-neutral EF
  Core entity types, model mappings, and service-registration helpers for
  outbox, inbox, and operation-state persistence. Consumers own migrations for
  now.
- Stable docs: Current persistence rules are described in
  [docs/architecture/persistence.md](../architecture/persistence.md),
  [docs/architecture/persistence-ef-core.md](../architecture/persistence-ef-core.md),
  and [docs/archive/mvp-plan.md](../archive/mvp-plan.md).
- Agent guidance: Root [AGENTS.md](../../AGENTS.md) requires ADR review before
  broad changes to provider support, migration policy, package boundaries, or
  durable behavior.
- Application evidence: Core persistence contracts exist. Initial EF Core
  entities, canonical table/column/constraint names, model mappings,
  service-registration helper, outbox writer, inbox store, operation state
  store, mapping tests, metadata tests, registration tests, fast store behavior
  tests, and PostgreSQL integration tests for real provider schema,
  transaction, unique-constraint, and registration-helper behavior, plus inbox
  processed-state, operation-state, outbox claim lease columns, savepoint
  rollback, `FOR UPDATE SKIP LOCKED` behavior, and EF persistence-scope
  transaction behavior, exist.
- Pending or deferred: None for this EF Core mapping and migration-ownership
  decision. Provider lifecycle, stale recovery, migration helpers, and
  advanced operation policies remain separate future decisions.

## Verification

Read back [docs/architecture/persistence.md](../architecture/persistence.md),
[docs/architecture/persistence-ef-core.md](../architecture/persistence-ef-core.md),
[docs/packaging.md](../packaging.md), and [AGENTS.md](../../AGENTS.md). Ran
`pnpm check`; formatting, restore, build, fast tests, and pack pass for the
current EF Core entity/mapping slice.

For the 2026-06-05 amendment, read back
[docs/architecture/persistence.md](../architecture/persistence.md) and
[docs/adr/0015-inbox-handle-once-orchestration.md](0015-inbox-handle-once-orchestration.md).
The applied handle-once slice was verified by the ADR 0015 command set.
