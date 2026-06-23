# Implementation Plan: EF Core Outbox Persistence

**Branch**: `N/A - migrated existing functionality` | **Date**: 2026-06-23 | **Spec**: [spec.md](spec.md)

**Input**: Migrated feature specification from `specs/009-efcore-outbox-persistence/spec.md`

## Summary

EF Core outbox persistence is an existing generic provider package capability
in `Bondstone.Persistence.EntityFrameworkCore`. It maps durable outbox rows
into EF Core, converts records to and from provider-neutral contracts, stages
pending outbox rows, exposes read-only terminal failure inspection, registers
writer/inspection services, and validates required durable messaging mappings.
PostgreSQL-specific claiming, lease renewal, dispatch outcome mutation,
locking, and SQL behavior is intentionally outside this migration.

## Technical Context

**Language/Version**: C# on `net10.0`

**Primary Dependencies**: `Bondstone`, `Bondstone.Persistence`, `Microsoft.EntityFrameworkCore`, `Microsoft.Extensions.DependencyInjection`

**Storage**: Generic EF Core mapping and stores. Tests use EF Core InMemory for package-local mapping and change-tracker behavior.

**Testing**: xUnit unit and application tests in `tests/Bondstone.Persistence.EntityFrameworkCore.Tests`.

**Target Platform**: Packable EF Core persistence package consumed by application `DbContext` projects and provider-specific packages.

**Project Type**: Library package in a .NET monorepo.

**Performance Goals**: No explicit performance target is documented for the migrated feature.

**Constraints**:

- Preserve generic EF Core boundary and avoid PostgreSQL-specific SQL or
  locking behavior in this feature.
- Preserve application-owned EF migrations and schema rollout.
- Preserve source-module outbox staging inside the EF Core transaction
  boundary.
- Preserve public API compatibility for EF Core setup and public outbox entity
  /store types.
- Preserve testing distinction: EF InMemory checks are not relational
  durability proof.

**Scale/Scope**:

- Source and setup files: 9 files, 807 lines under `src/Bondstone.Persistence.EntityFrameworkCore`.
- Focused and setup-related tests: 6 files, 1,342 lines under `tests/Bondstone.Persistence.EntityFrameworkCore.Tests`.
- Total migrated scope: 15 files and 2,149 lines excluding ignored build output.

## Constitution Check

_GATE: Passed for migrated behavior based on current implementation evidence._

- **Library Boundary First**: Pass. The feature provides library mappings,
  stores, setup helpers, and diagnostics; applications still own migrations
  and host behavior.
- **Durable Identities And Message Semantics**: Pass. EF rows preserve
  explicit durable message id and envelope metadata.
- **Package Boundaries And Public API Compatibility**: Pass with caution. The
  feature exposes public EF Core setup and outbox types guarded by public API
  review expectations.
- **Persistence And Transport Ownership**: Pass. Generic EF Core owns mapping,
  write staging, and inspection, while PostgreSQL and transports own their
  respective provider/native behavior.
- **Evidence-Based Verification**: Pass. Tests cover mapping, writing,
  inspection, service registration, and missing mapping diagnostics.

## Project Structure

### Documentation (this feature)

```text
specs/009-efcore-outbox-persistence/
├── spec.md
├── plan.md
└── tasks.md
```

### Source Code

```text
src/Bondstone.Persistence.EntityFrameworkCore/
├── Outbox/
│   ├── EntityFrameworkCoreDurableOutboxInspectionStore.cs
│   ├── EntityFrameworkCoreDurableOutboxWriter.cs
│   ├── EntityFrameworkCoreModuleDurableOutboxWriter.cs
│   ├── OutboxMessageEntity.cs
│   └── OutboxMessageEntityConfiguration.cs
└── Persistence/
    ├── BondstoneEntityFrameworkCoreModuleBuilderExtensions.cs
    ├── BondstoneEntityFrameworkCoreServiceCollectionExtensions.cs
    ├── BondstoneModelBuilderExtensions.cs
    └── EntityFrameworkCoreModuleTransactionRunner.cs
```

### Tests

```text
tests/Bondstone.Persistence.EntityFrameworkCore.Tests/
├── Outbox/
│   ├── EntityFrameworkCoreDurableOutboxInspectionStoreTests.cs
│   ├── EntityFrameworkCoreDurableOutboxWriterTests.cs
│   └── OutboxMessageEntityTests.cs
└── Persistence/
    ├── BondstoneEntityFrameworkCoreServiceCollectionExtensionsTests.cs
    ├── BondstoneModelBuilderExtensionsTests.cs
    └── EntityFrameworkCoreModuleTransactionBehaviorTests.cs
```

**Structure Decision**: Keep this migration scoped to generic EF Core outbox
persistence. PostgreSQL-specific dispatch mutation and relational concurrency
behavior remains a separate migration target.

## Reconstructed Implementation Approach

### Phase 1: EF Core Outbox Entity And Mapping

The feature defines `OutboxMessageEntity` as the EF representation of a
provider-neutral `DurableOutboxRecord`. The mapping stores durable envelope
fields, trace context, stored timestamp, status, retry, dispatched, failed,
failure reason, and claim fields. `OutboxMessageEntityConfiguration` maps the
table name, message-id key, required fields, max lengths, string enum
conversions, and dispatch/inspection indexes.

### Phase 2: Model Builder Setup

`ApplyBondstoneOutbox(...)` applies only outbox mapping.
`ApplyBondstonePersistence(...)` includes outbox mapping with direct inbox,
incoming inbox, and operation-state mappings. This lets applications choose
granular mapping or the whole Bondstone durable persistence table set.

### Phase 3: EF Core Outbox Writers

`EntityFrameworkCoreDurableOutboxWriter<TDbContext>` converts a durable
envelope into a pending outbox record with the configured `TimeProvider` and
adds the EF entity to the current `DbContext`. The module writer validates and
exposes module name while delegating write behavior to the generic writer.

### Phase 4: EF Core Terminal Failure Inspection

`EntityFrameworkCoreDurableOutboxInspectionStore<TDbContext>` exposes a
no-tracking read model for terminal dispatch failures. It validates positive
counts and UTC cutoffs, normalizes optional source module filters, orders by
failure timestamp with stored timestamp fallback, and returns provider-neutral
records.

### Phase 5: Service Setup And Mapping Diagnostics

EF Core service setup registers outbox writer and inspection store alongside
the broader durable persistence stores. The module transaction runner validates
durable messaging `DbContext` mappings before execution and reports missing
outbox/inbox mappings with a Bondstone setup error.

## Verification Strategy

- **Focused checks**:
  - `dotnet test tests/Bondstone.Persistence.EntityFrameworkCore.Tests/Bondstone.Persistence.EntityFrameworkCore.Tests.csproj --configuration Release --filter "Category=Unit|Category=Application"`
- **Default gate**: `pnpm check`
- **Provider gate**: `pnpm backend:test:integration` when validating PostgreSQL relational claim, lease, dispatch outcome, and concurrency behavior.
- **Package gate**: `pnpm backend:pack` when package metadata or public API baselines are affected.

## Complexity Tracking

> Fill only if Constitution Check has violations that must be justified.

No constitution violations were identified for the migrated feature.

## Gaps And Follow-Up Candidates

- EF InMemory tests do not prove relational uniqueness, SQL shape, locking, or
  concurrency behavior; PostgreSQL integration tests cover those concerns and
  should be migrated separately.
- Generic EF Core outbox does not implement claim, lease renewal, dispatched,
  retry, terminal failure, or stale mutation behavior.
- Application EF migrations are not shipped by Bondstone and remain
  application-owned.
- Generic EF Core outbox does not implement cleanup, purge, retention, replay,
  or operator repair flows.
- Concrete public EF Core entity/store types remain compatibility-sensitive
  public API; broad visibility cleanup would need separate review.
