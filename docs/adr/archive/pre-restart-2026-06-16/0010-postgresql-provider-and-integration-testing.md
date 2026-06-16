# 0010 PostgreSQL Provider And Integration Testing

Status: Archived
Application: Not Applicable
Date: 2026-06-04

## Context

Bondstone now has core persistence contracts and a provider-neutral EF Core
package with durable outbox, inbox, and operation-state entities, model
mappings, service registration, and staging stores.

The next extraction step is PostgreSQL provider behavior. PostgreSQL support is
a durable decision because it affects package boundaries, provider-specific
dependencies, schema behavior, exception mapping, locking and claiming
semantics, integration-test infrastructure, and future migration guidance.

EF Core InMemory tests cannot verify PostgreSQL uniqueness, transaction,
locking, SQL, or concurrency behavior. These semantics need a real PostgreSQL
database before Bondstone exposes higher-level provider contracts or claims
reliable inbox/outbox behavior.

## Decision

`Bondstone.EntityFrameworkCore.Postgres` owns PostgreSQL-specific integration
for Bondstone EF Core persistence.

The generic `Bondstone.EntityFrameworkCore` package remains provider-neutral.
It must not gain PostgreSQL dependencies, Npgsql exception handling, raw SQL,
locking, claiming, or migration conventions.

PostgreSQL behavior must be verified with `Integration` tests backed by
Testcontainers or an equivalent explicit PostgreSQL fixture. These tests should
cover real database behavior before provider abstractions are widened,
including:

- model creation against PostgreSQL;
- transaction commit and rollback behavior for staged Bondstone entities;
- inbox uniqueness and duplicate-conflict behavior;
- outbox claiming and lease behavior before retry/dead-letter behavior.

The first PostgreSQL slice should prove the current EF Core store
implementations against PostgreSQL without introducing a public unit-of-work,
dispatcher, migration helper, or claiming API.

## Amendment 2026-06-05

ADR 0015 adds a provider-neutral inbox handler executor in `Bondstone`.
`Bondstone.EntityFrameworkCore.Postgres` may compose that executor through its
service registration after registering PostgreSQL inbox registration and the
provider-neutral EF inbox store. PostgreSQL still does not own transport
acknowledgement, receive retry policy, stale receive recovery, handler
discovery, or a public unit-of-work abstraction.

ADR 0016 adds an EF-specific persistence scope in
`Bondstone.EntityFrameworkCore`. The PostgreSQL package composes it through the
generic EF registration and verifies its transaction behavior against
PostgreSQL.

## Consequences

PostgreSQL-specific concerns stay out of the core and provider-neutral EF Core
packages.

Integration tests become the source of truth for PostgreSQL persistence
semantics. Fast `Unit` and `Application` tests continue to cover pure mapping
and change-tracker staging boundaries only.

Higher-level conflict handling, savepoint usage, operation-state concurrency,
lease renewal, retry-delay calculation, max-attempt policy, dead-letter
routing, and migration helper design remain deferred until real PostgreSQL
tests expose the shape those APIs need.

## Application Notes

- Current contract: PostgreSQL-specific persistence behavior belongs in
  `Bondstone.EntityFrameworkCore.Postgres`; provider-neutral EF Core behavior
  remains in `Bondstone.EntityFrameworkCore`.
- Stable docs: Current persistence rules are described in
  [docs/architecture/persistence.md](../architecture/persistence.md), testing
  rules in [docs/testing.md](../testing.md), PostgreSQL-specific EF behavior
  in [docs/architecture/persistence-postgresql.md](../architecture/persistence-postgresql.md),
  and current implementation state in [docs/archive/mvp-plan.md](../archive/mvp-plan.md).
- Agent guidance: Root [AGENTS.md](../../AGENTS.md) requires ADR review before
  broad provider support, migration policy, or durable behavior changes.
- Application evidence: The PostgreSQL package shell exists. First
  Testcontainers-backed integration tests cover provider-neutral EF Core schema
  creation against PostgreSQL, outbox transaction commit/rollback behavior, and
  inbox duplicate unique-constraint behavior, inbox processed timestamps,
  operation-state updates, outbox claim lease columns, savepoint rollback after
  duplicate inbox inserts, `FOR UPDATE SKIP LOCKED` outbox selection behavior,
  scheduled pending outbox claim behavior, schema-aware claiming registration,
  public inbox registration outcomes, neutral inbox handler executor
  composition with the EF persistence scope, EF persistence-scope transaction
  behavior, and provider registration. PostgreSQL service registration and
  unique-violation exception classification helpers exist. Constraint names
  live with the provider-neutral EF mappings; constants are exposed only where
  another package needs reuse.
- Pending or deferred: None for this PostgreSQL provider/integration-test
  decision. Migration helpers, stale recovery, and advanced operation-state
  policy remain separate future decisions.

## Verification

Read back [docs/architecture/persistence.md](../architecture/persistence.md),
[docs/architecture/persistence-postgresql.md](../architecture/persistence-postgresql.md),
[docs/testing.md](../testing.md), and [docs/archive/mvp-plan.md](../archive/mvp-plan.md). Ran targeted PostgreSQL
integration tests for provider schema, primary-key constraints, transaction,
duplicate-conflict, inbox processed-state, operation-state, and registration
behavior. PostgreSQL tests also verify outbox claim lease columns, savepoint
rollback after inbox duplicates, `FOR UPDATE SKIP LOCKED` row selection, and
the public PostgreSQL outbox claimer behavior including scheduled rows and
schema-aware registration.

For the 2026-06-05 amendment, read back
[docs/architecture/persistence.md](../architecture/persistence.md),
[docs/testing.md](../testing.md), and
[docs/adr/0015-inbox-handle-once-orchestration.md](0015-inbox-handle-once-orchestration.md).
The applied PostgreSQL service-composition slice was verified by the ADR 0015
command set.
