# 0009 EF Core Persistence Entities And Migrations

Status: Accepted
Application: Partially Applied
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

Provider-neutral EF mappings own stable primary-key names for the Bondstone
tables. Provider packages can use those names when interpreting provider
exceptions, but the constraints are not PostgreSQL-only concepts. Expose
constraint-name constants only when another package needs to reuse them.

Migrations are consumer-owned for now. Bondstone provides model mappings and
documentation; application modules or samples generate and apply migrations in
their own DbContext projects. A later ADR may introduce migration helpers,
embedded migrations, or provider-specific migration conventions after samples
and integration tests prove the need.

EF Core persistence components stage entities in the current DbContext. They do
not call `SaveChangesAsync`; caller-owned DbContext transactions or module
unit-of-work code commit source state, outbox messages, and operation state
atomically.

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
abstractions, and inbox handle-once orchestration remain deferred.

## Application Notes

- Current contract: `Bondstone.EntityFrameworkCore` owns provider-neutral EF
  Core entity types, model mappings, and service-registration helpers for
  outbox, inbox, and operation-state persistence. Consumers own migrations for
  now.
- Stable docs: Current persistence rules are described in
  [docs/architecture/persistence.md](../architecture/persistence.md), with
  extraction state in [docs/extraction.md](../extraction.md) and
  [docs/extraction-plan.md](../extraction-plan.md).
- Agent guidance: Root [AGENTS.md](../../AGENTS.md) requires ADR review before
  broad changes to provider support, migration policy, package boundaries, or
  durable behavior.
- Application evidence: Core persistence contracts exist. Initial EF Core
  entities, model mappings, service-registration helper, outbox writer, inbox
  store, operation state store, mapping tests, metadata tests, registration
  tests, fast store behavior tests, and PostgreSQL integration tests for real
  provider schema, transaction, unique-constraint, and registration-helper
  behavior, plus inbox processed-state, operation-state, outbox claim lease
  columns, savepoint rollback, and `FOR UPDATE SKIP LOCKED` behavior, exist.
- Pending or deferred: Inbox duplicate-result orchestration,
  retry-delay calculation, max-attempt policy, dead-letter routing, migration
  helpers, additional provider-specific lifecycle implementations, additional
  integration tests, and samples remain future work.

## Verification

Read back [docs/architecture/persistence.md](../architecture/persistence.md),
[docs/extraction.md](../extraction.md), [docs/extraction-plan.md](../extraction-plan.md),
[docs/packaging.md](../packaging.md), and [AGENTS.md](../../AGENTS.md). Ran
`pnpm check`; formatting, restore, build, fast tests, and pack pass for the
current EF Core entity/mapping slice.
