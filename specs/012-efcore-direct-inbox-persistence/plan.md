# Implementation Plan: EF Core Direct Inbox Persistence

**Branch**: `N/A - migrated existing functionality` | **Date**: 2026-06-23 | **Spec**: [spec.md](spec.md)

**Input**: Migrated feature specification from `specs/012-efcore-direct-inbox-persistence/spec.md`

## Summary

EF Core direct inbox persistence is an existing generic provider package capability in `Bondstone.Persistence.EntityFrameworkCore`. It maps provider-neutral direct inbox records to the `inbox_messages` table, provides an EF Core store and inspection store, and wires the mapping and services through the generic EF Core persistence setup. PostgreSQL-specific registration, unique constraint race handling, and SQL exception classification are outside this migration.

## Technical Context

**Language/Version**: C# on `net10.0`

**Primary Dependencies**: `Bondstone.Persistence`, EF Core `10.0.8`, Microsoft dependency injection

**Storage**: Generic EF Core `DbContext` mapping and change tracking. Provider-specific SQL behavior is outside this migration.

**Testing**: xUnit `Unit` and `Application` tests in `tests/Bondstone.Persistence.EntityFrameworkCore.Tests`, using EF Core InMemory for package-local behavior.

**Target Platform**: Packable generic EF Core persistence package consumed by application DbContexts and PostgreSQL provider helpers.

**Project Type**: Library package in a .NET monorepo.

**Performance Goals**: No explicit performance target is documented for the migrated feature.

**Constraints**:

- Preserve the generic EF Core package boundary; no PostgreSQL-specific SQL, locking, or exception classification belongs here.
- Preserve the distinction between direct inbox idempotency markers and durable incoming inbox transport receive ledger rows.
- Store operations must use the caller's DbContext and must not save changes independently.
- Public API and table-shape changes are compatibility-sensitive because consumers generate migrations from this mapping.

**Scale/Scope**:

- Source mapping/store/setup: 6 files, 377 lines under `src/Bondstone.Persistence.EntityFrameworkCore`.
- Focused/shared tests: 6 files, 644 lines under `tests/Bondstone.Persistence.EntityFrameworkCore.Tests`.
- Total migrated scope: 12 files and 1,021 lines excluding ignored build output.

## Constitution Check

_GATE: Passed for migrated behavior based on current implementation evidence._

- **Library Boundary First**: Pass. The feature is a packable generic EF Core library capability.
- **Durable Identities And Message Semantics**: Pass. The mapping preserves message id, module name, and handler identity as direct receive identity.
- **Package Boundaries And Public API Compatibility**: Pass with caution. The public mapping/setup surface and table shape are compatibility-sensitive.
- **Persistence And Transport Ownership**: Pass. Generic EF Core persistence owns mapping and store behavior; PostgreSQL provider behavior and transports are excluded.
- **Evidence-Based Verification**: Pass. Mapping and change-tracker behavior are covered by package unit/application tests, with provider semantics left to PostgreSQL integration tests.

## Project Structure

### Documentation (this feature)

```text
specs/012-efcore-direct-inbox-persistence/
├── spec.md
├── plan.md
└── tasks.md
```

### Source Code

```text
src/Bondstone.Persistence.EntityFrameworkCore/
├── Inbox/
│   ├── InboxMessageEntity.cs
│   ├── InboxMessageEntityConfiguration.cs
│   ├── EntityFrameworkCoreDurableInboxStore.cs
│   └── EntityFrameworkCoreDurableInboxInspectionStore.cs
└── Persistence/
    ├── BondstoneModelBuilderExtensions.cs
    └── BondstoneEntityFrameworkCoreServiceCollectionExtensions.cs
```

### Tests

```text
tests/Bondstone.Persistence.EntityFrameworkCore.Tests/
├── EntityFrameworkCoreTestDbContext.cs
├── Inbox/
│   ├── InboxMessageEntityTests.cs
│   ├── EntityFrameworkCoreDurableInboxStoreTests.cs
│   └── EntityFrameworkCoreDurableInboxInspectionStoreTests.cs
└── Persistence/
    ├── BondstoneModelBuilderExtensionsTests.cs
    └── BondstoneEntityFrameworkCoreServiceCollectionExtensionsTests.cs
```

**Structure Decision**: Keep this migration scoped to generic EF Core direct inbox persistence. PostgreSQL direct inbox registration and database semantics remain a separate migration target.

## Reconstructed Implementation Approach

### Phase 1: Entity And Mapping

The feature defines `InboxMessageEntity` as the EF Core representation of `DurableInboxRecord`. `InboxMessageEntityConfiguration` maps it to `inbox_messages` with a composite primary key over module name, message id, and handler identity; required received timestamp; optional processed timestamp; bounded module/handler columns; and a received timestamp index.

### Phase 2: Store Implementation

`EntityFrameworkCoreDurableInboxStore<TDbContext>` implements `IDurableInboxStore` over the current DbContext. It reads rows by composite key, stages new entities through `DbSet.Add`, and stages processed timestamp updates by loading the existing entity and calling its record-backed `MarkProcessed(...)` behavior.

### Phase 3: Inspection Query

`EntityFrameworkCoreDurableInboxInspectionStore<TDbContext>` implements `IDurableInboxInspectionStore`. It validates max count and UTC cutoff, normalizes an optional module filter, queries unprocessed rows with `AsNoTracking`, orders deterministically, limits results, and maps entities back to provider-neutral records.

### Phase 4: Setup Extensions

`BondstoneModelBuilderExtensions.ApplyBondstoneInbox(...)` applies the direct inbox mapping, and `ApplyBondstonePersistence(...)` includes that mapping in the full durable persistence model. `BondstoneEntityFrameworkCoreServiceCollectionExtensions.AddBondstoneEntityFrameworkCorePersistence<TDbContext>()` registers direct inbox store and inspection services alongside the other generic EF Core persistence services, using `TryAdd` behavior for the store.

## Verification Strategy

- **Focused checks**:
  - `dotnet test tests/Bondstone.Persistence.EntityFrameworkCore.Tests/Bondstone.Persistence.EntityFrameworkCore.Tests.csproj --configuration Release --filter "Category=Unit|Category=Application"`
- **Default gate**: `pnpm check`
- **Provider gate**: `pnpm backend:test:integration` when validating PostgreSQL direct inbox uniqueness and registrar behavior.
- **Package gate**: `pnpm backend:pack` when package metadata or public API baselines are affected.

## Complexity Tracking

> Fill only if Constitution Check has violations that must be justified.

No constitution violations were identified for the migrated feature.

## Gaps And Follow-Up Candidates

- PostgreSQL direct inbox registrar behavior, unique constraint race handling, and SQL exception classification are outside this migration and should be migrated separately.
- EF Core InMemory tests do not prove real database uniqueness, transaction isolation, index behavior, or generated SQL.
- Consumer migration generation and rollout are application-owned and are not automated by this feature.
- Direct inbox cleanup, purge, replay, retention, and operator repair flows are not implemented by this generic EF Core feature.
- Transport-facing durable incoming inbox behavior is separate and already migrated under `specs/006-durable-incoming-inbox-persistence`.
