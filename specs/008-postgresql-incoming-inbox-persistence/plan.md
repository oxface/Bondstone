# Implementation Plan: PostgreSQL Incoming Inbox Persistence

**Branch**: `N/A - migrated existing functionality` | **Date**: 2026-06-23 | **Spec**: [spec.md](spec.md)

**Input**: Migrated feature specification from `specs/008-postgresql-incoming-inbox-persistence/spec.md`

## Summary

PostgreSQL incoming inbox persistence is an existing provider-specific
capability in `Bondstone.Persistence.EntityFrameworkCore.Postgres`. It supplies
real relational mutation semantics for durable incoming inbox processing:
atomic row claiming with PostgreSQL locking, lease renewal, processed/retry
/terminal outcome recording, module-scoped dispatchers, and setup registration.
Generic EF Core mapping and provider-neutral dispatcher behavior are upstream
dependencies and are not duplicated in this migration.

## Technical Context

**Language/Version**: C# on `net10.0`

**Primary Dependencies**: `Bondstone`, `Bondstone.Persistence`, `Bondstone.Persistence.EntityFrameworkCore`, `Microsoft.EntityFrameworkCore`, `Npgsql`, `Npgsql.EntityFrameworkCore.PostgreSQL`

**Storage**: PostgreSQL via EF Core/Npgsql. SQL is provider-owned for incoming inbox claim, lease renewal, and outcome mutation.

**Testing**: xUnit integration tests in `tests/Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests` using PostgreSQL test infrastructure.

**Target Platform**: Packable PostgreSQL provider package consumed by EF-backed Bondstone modules.

**Project Type**: Library package in a .NET monorepo.

**Performance Goals**: No explicit throughput target is documented for the migrated feature.

**Constraints**:

- Preserve PostgreSQL-specific mutation behavior and avoid moving generic EF
  mapping or provider-neutral dispatcher logic into this feature.
- Preserve application-owned EF migrations and schema rollout.
- Preserve receive identity guard: receiver module, message id, handler
  identity, processing status, claim owner, and active lease.
- Preserve worker safety through PostgreSQL locking and stale-claim checks.
- Preserve public setup API compatibility.

**Scale/Scope**:

- Source and setup files: 7 files, 912 lines under `src/Bondstone.Persistence.EntityFrameworkCore.Postgres`.
- Focused integration/setup tests: 5 files, 1,785 lines under `tests/Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests`.
- Total migrated scope: 12 files and 2,697 lines excluding ignored build output.

## Constitution Check

_GATE: Passed for migrated behavior based on current implementation evidence._

- **Library Boundary First**: Pass. The feature provides provider package
  primitives and setup helpers, while applications own schema rollout,
  retention, cleanup, repair, and hosting policy.
- **Durable Identities And Message Semantics**: Pass. Mutations are guarded by
  explicit durable receive identity and claim owner.
- **Package Boundaries And Public API Compatibility**: Pass with caution.
  Provider mutation classes are internal, while setup APIs remain public and
  compatibility-sensitive.
- **Persistence And Transport Ownership**: Pass. PostgreSQL owns durable
  persistence mutation semantics; transport settlement and broker retry policy
  remain outside this feature.
- **Evidence-Based Verification**: Pass. Real PostgreSQL integration tests
  cover claim, lease, outcome, dispatcher, duplicate, stale, terminal, and
  setup behavior.

## Project Structure

### Documentation (this feature)

```text
specs/008-postgresql-incoming-inbox-persistence/
├── spec.md
├── plan.md
└── tasks.md
```

### Source Code

```text
src/Bondstone.Persistence.EntityFrameworkCore.Postgres/
├── IncomingInbox/
│   ├── PostgreSqlDurableIncomingInboxClaimer.cs
│   ├── PostgreSqlDurableIncomingInboxLeaseRenewer.cs
│   ├── PostgreSqlDurableIncomingInboxOutcomeRecorder.cs
│   ├── PostgreSqlIncomingInboxTableIdentifier.cs
│   └── PostgreSqlModuleDurableIncomingInboxDispatcher.cs
└── Persistence/
    ├── BondstonePostgreSqlBuilderExtensions.cs
    └── BondstonePostgreSqlServiceCollectionExtensions.cs
```

### Tests

```text
tests/Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests/
├── PostgreSqlIncomingInboxTestDbContext.cs
└── Persistence/
    ├── BondstonePostgreSqlServiceCollectionExtensionsTests.cs
    ├── PostgreSqlIncomingInboxMutationTests.cs
    ├── PostgreSqlIncomingInboxProcessingTests.cs
    └── PostgreSqlPersistenceRegistrationTests.cs
```

**Structure Decision**: Keep this migration scoped to PostgreSQL-specific
incoming inbox mutation and dispatcher registration. Generic EF Core mapping
and provider-neutral runtime behavior remain documented in prior migrations.

## Reconstructed Implementation Approach

### Phase 1: PostgreSQL Incoming Inbox Table Identity

The feature uses `PostgreSqlIncomingInboxTableIdentifier` with the generic EF
Core `IncomingInboxMessageEntityConfiguration.TableName` and optional schema.
SQL uses quoted identifiers for mapped columns.

### Phase 2: Atomic Claiming

`PostgreSqlDurableIncomingInboxClaimer<TDbContext>` validates mapping and
arguments, computes current time and claim expiration from `TimeProvider`, and
uses a PostgreSQL CTE with `FOR UPDATE SKIP LOCKED` to select pending, due
retry, and stale processing candidates. It updates selected rows to
`Processing`, increments attempt count, clears previous outcome fields, records
claim owner and expiration, and returns the updated rows as provider-neutral
records.

### Phase 3: Lease Renewal

`PostgreSqlDurableIncomingInboxLeaseRenewer<TDbContext>` extends
`ClaimedUntilUtc` only when the row matches the durable incoming inbox key,
status is `Processing`, claim owner matches, and the current lease is still
active. It returns whether exactly one row was updated.

### Phase 4: Outcome Recording

`PostgreSqlDurableIncomingInboxOutcomeRecorder<TDbContext>` records processed,
retry scheduled, and terminal failed outcomes with guarded SQL updates. Every
outcome requires matching receive identity, processing status, matching claim
owner, and an active lease at the outcome timestamp. Processed and terminal
outcomes clear retry data where appropriate; all successful outcomes clear
claim fields.

### Phase 5: Module Dispatcher And Setup

`PostgreSqlModuleDurableIncomingInboxDispatcher<TDbContext>` composes the
PostgreSQL claimer and outcome recorder with the provider-neutral
`DurableIncomingInboxDispatcher`, scoped to a receiver module. PostgreSQL setup
registers root incoming inbox mutation services, module incoming inbox
dispatchers, and the module dispatcher aggregator through normal
`UsePostgreSqlPersistence` paths.

## Verification Strategy

- **Focused checks**:
  - `dotnet test tests/Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests/Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests.csproj --configuration Release --filter "Category=Integration"`
- **Default gate**: `pnpm check`
- **Provider gate**: `pnpm backend:test:integration`
- **Package gate**: `pnpm backend:pack` when package metadata or public API baselines are affected.

## Complexity Tracking

> Fill only if Constitution Check has violations that must be justified.

No constitution violations were identified for the migrated feature.

## Gaps And Follow-Up Candidates

- Cleanup, purge, replay, retention, and operator repair flows are not
  implemented by this PostgreSQL incoming inbox feature.
- Long-running handler lease heartbeat is not implemented here; this feature
  exposes lease renewal, but no dispatcher heartbeat loop.
- Broker-native dead-letter movement and retry policy remain application or
  transport-owned.
- Tests cover provider behavior through PostgreSQL integration tests, but do
  not isolate every SQL validation branch as unit tests.
- Applications still own EF migrations and operational rollout for the mapped
  incoming inbox table.
