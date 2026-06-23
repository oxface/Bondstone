---
description: "Migrated task list for existing EF Core outbox persistence feature"
---

# Tasks: EF Core Outbox Persistence

**Input**: Migrated design documents from `specs/009-efcore-outbox-persistence/`

**Prerequisites**: Existing generic EF Core implementation in `src/Bondstone.Persistence.EntityFrameworkCore`; existing focused tests in `tests/Bondstone.Persistence.EntityFrameworkCore.Tests`

**Tests**: Existing xUnit tests use `Category=Unit` and `Category=Application`.

**Organization**: Tasks are grouped by migrated user story. All implementation tasks are marked complete because the feature existed before SpecKit adoption.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Could have been implemented in parallel at the time because it touched different files or concerns.
- **[Story]**: Migrated user story identifier.
- All paths refer to existing source-controlled files.

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Establish generic EF Core package support for durable outbox mapping, writing, and inspection.

- [x] T001 Create EF Core persistence package project `src/Bondstone.Persistence.EntityFrameworkCore/Bondstone.Persistence.EntityFrameworkCore.csproj`
- [x] T002 Add package documentation in `src/Bondstone.Persistence.EntityFrameworkCore/README.md`
- [x] T003 Add scoped package guidance in `src/Bondstone.Persistence.EntityFrameworkCore/AGENTS.md`
- [x] T004 Add EF Core persistence test project `tests/Bondstone.Persistence.EntityFrameworkCore.Tests/Bondstone.Persistence.EntityFrameworkCore.Tests.csproj`
- [x] T005 Add EF Core persistence test guidance in `tests/Bondstone.Persistence.EntityFrameworkCore.Tests/README.md` and `tests/Bondstone.Persistence.EntityFrameworkCore.Tests/AGENTS.md`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Define shared EF Core setup hooks and transaction behavior used by durable outbox persistence.

- [x] T006 [P] Implement model builder extension container in `src/Bondstone.Persistence.EntityFrameworkCore/Persistence/BondstoneModelBuilderExtensions.cs`
- [x] T007 [P] Implement EF Core service registration extension in `src/Bondstone.Persistence.EntityFrameworkCore/Persistence/BondstoneEntityFrameworkCoreServiceCollectionExtensions.cs`
- [x] T008 [P] Implement EF Core module persistence setup in `src/Bondstone.Persistence.EntityFrameworkCore/Persistence/BondstoneEntityFrameworkCoreModuleBuilderExtensions.cs`
- [x] T009 [P] Implement EF Core module transaction runner in `src/Bondstone.Persistence.EntityFrameworkCore/Persistence/EntityFrameworkCoreModuleTransactionRunner.cs`
- [x] T010 [P] Add shared EF Core InMemory test context in `tests/Bondstone.Persistence.EntityFrameworkCore.Tests/EntityFrameworkCoreTestDbContext.cs`

**Checkpoint**: EF Core package has shared setup and transaction surfaces for durable outbox behavior.

---

## Phase 3: User Story 1 - Map Durable Outbox Rows Into EF Core (Priority: P1)

**Goal**: EF Core can map durable outbox rows without losing provider-neutral record data.

**Independent Test**: `dotnet test tests/Bondstone.Persistence.EntityFrameworkCore.Tests/Bondstone.Persistence.EntityFrameworkCore.Tests.csproj --configuration Release --filter "Category=Unit|Category=Application"`

### Tests for User Story 1

- [x] T011 [US1] Add entity conversion coverage in `tests/Bondstone.Persistence.EntityFrameworkCore.Tests/Outbox/OutboxMessageEntityTests.cs`
- [x] T012 [US1] Add command record round-trip coverage in `tests/Bondstone.Persistence.EntityFrameworkCore.Tests/Outbox/OutboxMessageEntityTests.cs`
- [x] T013 [US1] Add event record without target module round-trip coverage in `tests/Bondstone.Persistence.EntityFrameworkCore.Tests/Outbox/OutboxMessageEntityTests.cs`
- [x] T014 [US1] Add outbox mapping coverage through `ApplyBondstonePersistence(...)` in `tests/Bondstone.Persistence.EntityFrameworkCore.Tests/Persistence/BondstoneModelBuilderExtensionsTests.cs`
- [x] T015 [US1] Add granular `ApplyBondstoneOutbox(...)` mapping coverage in `tests/Bondstone.Persistence.EntityFrameworkCore.Tests/Persistence/BondstoneModelBuilderExtensionsTests.cs`
- [x] T016 [US1] Add primary key, length-limit, and index coverage in `tests/Bondstone.Persistence.EntityFrameworkCore.Tests/Persistence/BondstoneModelBuilderExtensionsTests.cs`

### Implementation for User Story 1

- [x] T017 [US1] Implement `OutboxMessageEntity` in `src/Bondstone.Persistence.EntityFrameworkCore/Outbox/OutboxMessageEntity.cs`
- [x] T018 [US1] Implement `OutboxMessageEntity.FromRecord(...)`
- [x] T019 [US1] Implement `OutboxMessageEntity.ToRecord()`
- [x] T020 [US1] Implement `OutboxMessageEntityConfiguration` in `src/Bondstone.Persistence.EntityFrameworkCore/Outbox/OutboxMessageEntityConfiguration.cs`
- [x] T021 [US1] Map table `outbox_messages` and primary key `MessageId`
- [x] T022 [US1] Configure message, trace, stored, dispatch, retry, failure, and claim columns
- [x] T023 [US1] Configure string enum conversions, max lengths, required fields, and outbox query indexes
- [x] T024 [US1] Add `ApplyBondstoneOutbox(...)` model builder extension
- [x] T025 [US1] Include outbox mapping in `ApplyBondstonePersistence(...)`

**Checkpoint**: EF Core can represent and map durable outbox records.

---

## Phase 4: User Story 2 - Stage Outbox Messages With EF Core (Priority: P2)

**Goal**: EF Core outbox writer stages pending durable outbox rows in the current `DbContext`.

**Independent Test**: EF Core persistence tests after build.

### Tests for User Story 2

- [x] T026 [US2] Add change-tracker staging coverage in `tests/Bondstone.Persistence.EntityFrameworkCore.Tests/Outbox/EntityFrameworkCoreDurableOutboxWriterTests.cs`
- [x] T027 [US2] Add persisted row coverage in `tests/Bondstone.Persistence.EntityFrameworkCore.Tests/Outbox/EntityFrameworkCoreDurableOutboxWriterTests.cs`
- [x] T028 [US2] Add durable messaging transaction mapping coverage in `tests/Bondstone.Persistence.EntityFrameworkCore.Tests/Persistence/EntityFrameworkCoreModuleTransactionBehaviorTests.cs`

### Implementation for User Story 2

- [x] T029 [US2] Implement `EntityFrameworkCoreDurableOutboxWriter<TDbContext>`
- [x] T030 [US2] Validate envelope before writing
- [x] T031 [US2] Create `DurableOutboxRecord` with `TimeProvider.GetUtcNow()`
- [x] T032 [US2] Add `OutboxMessageEntity` to `DbContext.Set<OutboxMessageEntity>()`
- [x] T033 [US2] Implement `EntityFrameworkCoreModuleDurableOutboxWriter<TDbContext>`
- [x] T034 [US2] Normalize module name in module outbox writer
- [x] T035 [US2] Delegate module writer writes to the generic EF Core outbox writer

**Checkpoint**: Generic EF Core can stage durable outbox rows for later provider dispatch.

---

## Phase 5: User Story 3 - Inspect Terminal Outbox Failures With EF Core (Priority: P3)

**Goal**: EF Core inspection store exposes read-only terminal outbox failure queries for operations and maintenance.

**Independent Test**: EF Core persistence tests after build.

### Tests for User Story 3

- [x] T036 [US3] Add filtered terminal failure inspection coverage in `tests/Bondstone.Persistence.EntityFrameworkCore.Tests/Outbox/EntityFrameworkCoreDurableOutboxInspectionStoreTests.cs`
- [x] T037 [US3] Add non-UTC cutoff validation coverage in `tests/Bondstone.Persistence.EntityFrameworkCore.Tests/Outbox/EntityFrameworkCoreDurableOutboxInspectionStoreTests.cs`

### Implementation for User Story 3

- [x] T038 [US3] Implement `EntityFrameworkCoreDurableOutboxInspectionStore<TDbContext>`
- [x] T039 [US3] Implement no-tracking terminal failure query
- [x] T040 [US3] Filter terminal failures by source module and failed cutoff
- [x] T041 [US3] Order terminal failures by failed timestamp with stored timestamp fallback and message id
- [x] T042 [US3] Validate positive `maxCount`
- [x] T043 [US3] Validate UTC failed cutoff timestamp
- [x] T044 [US3] Convert inspection query results back to `DurableOutboxRecord`

**Checkpoint**: Generic EF Core can read terminal outbox failure rows for operations and inspection.

---

## Phase 6: User Story 4 - Register EF Core Outbox Services And Mapping Diagnostics (Priority: P4)

**Goal**: EF Core setup registers outbox services and fails clearly when durable messaging mappings are missing.

**Independent Test**: EF Core persistence setup and transaction behavior tests after build.

### Tests for User Story 4

- [x] T045 [US4] Add service registration coverage in `tests/Bondstone.Persistence.EntityFrameworkCore.Tests/Persistence/BondstoneEntityFrameworkCoreServiceCollectionExtensionsTests.cs`
- [x] T046 [US4] Add `TryAdd` preservation coverage in `tests/Bondstone.Persistence.EntityFrameworkCore.Tests/Persistence/BondstoneEntityFrameworkCoreServiceCollectionExtensionsTests.cs`
- [x] T047 [US4] Add missing outbox mapping diagnostics coverage in `tests/Bondstone.Persistence.EntityFrameworkCore.Tests/Persistence/EntityFrameworkCoreModuleTransactionBehaviorTests.cs`
- [x] T048 [US4] Add outbox-and-inbox mapping success coverage in `tests/Bondstone.Persistence.EntityFrameworkCore.Tests/Persistence/EntityFrameworkCoreModuleTransactionBehaviorTests.cs`

### Implementation for User Story 4

- [x] T049 [US4] Register `IDurableOutboxWriter` in `AddBondstoneEntityFrameworkCorePersistence<TDbContext>()`
- [x] T050 [US4] Register `IDurableOutboxInspectionStore` in `AddBondstoneEntityFrameworkCorePersistence<TDbContext>()`
- [x] T051 [US4] Use `TryAdd` semantics for EF Core durable persistence service registrations
- [x] T052 [US4] Register EF Core module persistence from `UseEntityFrameworkCoreModulePersistence<TDbContext>()`
- [x] T053 [US4] Validate durable messaging `DbContext` includes outbox mapping in `EntityFrameworkCoreModuleTransactionRunner`
- [x] T054 [US4] Throw `BondstoneSetupException` with `MissingEfMapping` when outbox mapping is missing

**Checkpoint**: EF-backed modules get outbox services through normal setup and fail clearly when required mapping is absent.

---

## Phase N: Polish & Cross-Cutting Concerns

**Purpose**: Documentation, packaging, and compatibility support for the migrated feature.

- [x] T055 [P] Document EF Core package purpose in `src/Bondstone.Persistence.EntityFrameworkCore/README.md`
- [x] T056 [P] Document EF Core mapping package in `docs/package-discovery.md`
- [x] T057 [P] Document application-owned EF migrations in `docs/packaging.md`
- [x] T058 [P] Document terminal outbox inspection in `docs/operations.md`
- [x] T059 [P] Document EF InMemory test limitations in `docs/testing.md`
- [x] T060 [P] Preserve public API classification expectations through `docs/public-api.md`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies.
- **Foundational (Phase 2)**: Depends on EF Core package and test project setup.
- **User Story 1 (Phase 3)**: Depends on provider-neutral durable outbox contracts.
- **User Story 2 (Phase 4)**: Depends on outbox entity and mapping.
- **User Story 3 (Phase 5)**: Depends on outbox entity and mapping.
- **User Story 4 (Phase 6)**: Depends on outbox writer, inspection store, and EF Core module transaction infrastructure.
- **Polish**: Depends on stable package behavior and public setup surface.

### User Story Dependencies

- **User Story 1 (P1)**: Can be implemented once provider-neutral durable outbox contracts exist.
- **User Story 2 (P2)**: Depends on mapped outbox entity.
- **User Story 3 (P3)**: Depends on mapped outbox entity.
- **User Story 4 (P4)**: Depends on EF Core setup and transaction infrastructure.

## Gaps Identified

- EF InMemory tests do not prove relational uniqueness, SQL generation, locking, or concurrency behavior.
- PostgreSQL-specific outbox claim, lease renewal, dispatched, retry, terminal failure, stale mutation, and SQL behavior is outside this migration.
- Application EF migrations are not shipped by Bondstone and remain application-owned.
- Generic EF Core outbox does not implement cleanup, purge, retention, replay, or operator repair flows.
- Concrete public EF Core entity/store types remain compatibility-sensitive public API; broad visibility cleanup would need separate review.

## Verification Commands

The migrated feature is covered by existing tests. Use these commands after building:

```bash
pnpm backend:build
dotnet test tests/Bondstone.Persistence.EntityFrameworkCore.Tests/Bondstone.Persistence.EntityFrameworkCore.Tests.csproj --configuration Release --no-build --filter "Category=Unit|Category=Application"
```
