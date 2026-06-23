# Implementation Plan: PostgreSQL Outbox Persistence

**Branch**: `N/A - migrated existing functionality` | **Date**: 2026-06-23 | **Spec**: [spec.md](spec.md)

**Input**: Migrated feature specification from `specs/010-postgresql-outbox-persistence/spec.md`

## Summary

PostgreSQL outbox persistence is an existing provider-specific capability in
`Bondstone.Persistence.EntityFrameworkCore.Postgres`. It supplies real
relational mutation semantics for durable outbox dispatch: atomic row claiming
with PostgreSQL locking, lease renewal, dispatched/retry/terminal outcome
recording, module-scoped dispatchers, and setup registration. Generic EF Core
mapping and provider-neutral outbox dispatcher behavior are upstream
dependencies and are not duplicated in this migration.

## Technical Context

**Language/Version**: C# on `net10.0`

**Primary Dependencies**: `Bondstone`, `Bondstone.Persistence`, `Bondstone.Persistence.EntityFrameworkCore`, `Microsoft.EntityFrameworkCore`, `Npgsql`, `Npgsql.EntityFrameworkCore.PostgreSQL`

**Storage**: PostgreSQL via EF Core/Npgsql. SQL is provider-owned for outbox claim, lease renewal, and dispatch outcome mutation.

**Testing**: xUnit unit and integration tests in `tests/Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests` using PostgreSQL test infrastructure for provider behavior.

**Target Platform**: Packable PostgreSQL provider package consumed by EF-backed Bondstone modules.

**Project Type**: Library package in a .NET monorepo.

**Performance Goals**: No explicit throughput target is documented for the migrated feature.

**Constraints**:

- Preserve PostgreSQL-specific mutation behavior and avoid moving generic EF
  mapping or provider-neutral dispatcher logic into this feature.
- Preserve application-owned EF migrations and schema rollout.
- Preserve message-id guard plus processing status, claim owner, and active
  lease checks for outcome mutation.
- Preserve worker safety through PostgreSQL locking and stale-claim checks.
- Preserve public setup API compatibility.

**Scale/Scope**:

- Source and setup files: 7 files, 791 lines under `src/Bondstone.Persistence.EntityFrameworkCore.Postgres`.
- Focused unit/integration/setup tests: 9 files, 1,644 lines under `tests/Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests`.
- Total migrated scope: 16 files and 2,435 lines excluding ignored build output.

## Constitution Check

_GATE: Passed for migrated behavior based on current implementation evidence._

- **Library Boundary First**: Pass. The feature provides provider package
  primitives and setup helpers, while applications own schema rollout,
  retention, cleanup, repair, and hosting policy.
- **Durable Identities And Message Semantics**: Pass. Mutations are guarded by
  explicit durable message id and claim owner.
- **Package Boundaries And Public API Compatibility**: Pass with caution.
  Provider mutation classes are internal, while setup APIs remain public and
  compatibility-sensitive.
- **Persistence And Transport Ownership**: Pass. PostgreSQL owns durable
  persistence mutation semantics; transport envelope dispatch and broker
  behavior remain outside this feature.
- **Evidence-Based Verification**: Pass. Tests cover validation, claim,
  skip-locked, lease, outcome, dispatcher, retry, terminal, module scoping,
  and setup behavior.

## Project Structure

### Documentation (this feature)

```text
specs/010-postgresql-outbox-persistence/
├── spec.md
├── plan.md
└── tasks.md
```

### Source Code

```text
src/Bondstone.Persistence.EntityFrameworkCore.Postgres/
├── Outbox/
│   ├── PostgreSqlDurableOutboxClaimer.cs
│   ├── PostgreSqlDurableOutboxDispatchRecorder.cs
│   ├── PostgreSqlDurableOutboxLeaseRenewer.cs
│   ├── PostgreSqlModuleDurableOutboxDispatcher.cs
│   └── PostgreSqlOutboxTableIdentifier.cs
└── Persistence/
    ├── BondstonePostgreSqlBuilderExtensions.cs
    └── BondstonePostgreSqlServiceCollectionExtensions.cs
```

### Tests

```text
tests/Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests/
├── Outbox/
│   ├── PostgreSqlDurableOutboxClaimerTests.cs
│   ├── PostgreSqlDurableOutboxDispatchRecorderTests.cs
│   └── PostgreSqlDurableOutboxLeaseRenewerTests.cs
└── Persistence/
    ├── BondstonePostgreSqlServiceCollectionExtensionsTests.cs
    ├── PostgreSqlOutboxClaimTests.cs
    ├── PostgreSqlOutboxDispatchTests.cs
    ├── PostgreSqlOutboxDispatcherTests.cs
    ├── PostgreSqlOutboxLeaseTests.cs
    └── PostgreSqlPersistenceRegistrationTests.cs
```

**Structure Decision**: Keep this migration scoped to PostgreSQL-specific
outbox mutation and dispatcher registration. Generic EF Core mapping and
provider-neutral runtime behavior remain documented in prior migrations.

## Reconstructed Implementation Approach

### Phase 1: PostgreSQL Outbox Table Identity

The feature uses `PostgreSqlOutboxTableIdentifier` with the generic EF Core
`OutboxMessageEntityConfiguration.TableName` and optional schema. SQL uses
quoted identifiers for mapped columns.

### Phase 2: Atomic Claiming

`PostgreSqlDurableOutboxClaimer<TDbContext>` validates arguments, computes
current time and claim expiration from `TimeProvider`, and uses a PostgreSQL
CTE with `FOR UPDATE SKIP LOCKED` to select pending rows whose next attempt is
due and stale processing rows. It updates selected rows to `Processing`,
increments attempt count, records claim owner and expiration, and returns the
updated rows as provider-neutral records.

### Phase 3: Lease Renewal

`PostgreSqlDurableOutboxLeaseRenewer<TDbContext>` extends `ClaimedUntilUtc`
only when the row matches the message id, status is `Processing`, claim owner
matches, and the current lease is still active. It returns whether exactly one
row was updated.

### Phase 4: Dispatch Outcome Recording

`PostgreSqlDurableOutboxDispatchRecorder<TDbContext>` records dispatched,
retry scheduled, and terminal failed outcomes with guarded SQL updates. Every
outcome requires matching message id, processing status, matching claim owner,
and an active lease at the outcome timestamp. Successful outcomes clear claim
fields and outcome fields that no longer apply.

### Phase 5: Module Dispatcher And Setup

`PostgreSqlModuleDurableOutboxDispatcher<TDbContext>` composes the PostgreSQL
claimer, lease renewer, and dispatch recorder with the provider-neutral
`DurableOutboxDispatcher`, scoped to a source module. PostgreSQL setup
registers root outbox mutation services, module outbox dispatchers, and the
module dispatcher aggregator through normal `UsePostgreSqlPersistence` paths.

## Verification Strategy

- **Focused checks**:
  - `dotnet test tests/Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests/Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests.csproj --configuration Release --filter "Category=Unit|Category=Integration"`
- **Default gate**: `pnpm check`
- **Provider gate**: `pnpm backend:test:integration`
- **Package gate**: `pnpm backend:pack` when package metadata or public API baselines are affected.

## Complexity Tracking

> Fill only if Constitution Check has violations that must be justified.

No constitution violations were identified for the migrated feature.

## Gaps And Follow-Up Candidates

- Cleanup, purge, replay, retention, and operator repair flows are not
  implemented by this PostgreSQL outbox feature.
- Long-running dispatch lease heartbeat is not implemented here; this feature
  exposes lease renewal, but no worker heartbeat loop.
- Broker-native dead-letter movement and retry policy remain application or
  transport-owned.
- Applications still own EF migrations and operational rollout for the mapped
  outbox table.
- Provider tests cover behavior through PostgreSQL integration tests plus
  validation unit tests, but do not isolate every generated SQL branch as unit
  tests.
