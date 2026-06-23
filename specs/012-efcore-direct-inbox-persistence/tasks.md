---
description: "Migrated task list for existing EF Core direct inbox persistence feature"
---

# Tasks: EF Core Direct Inbox Persistence

**Input**: Migrated design documents from `specs/012-efcore-direct-inbox-persistence/`

**Prerequisites**: Existing generic EF Core implementation in `src/Bondstone.Persistence.EntityFrameworkCore`; existing package tests in `tests/Bondstone.Persistence.EntityFrameworkCore.Tests`

**Tests**: Existing xUnit tests use `Category=Unit` and `Category=Application`.

**Organization**: Tasks are grouped by migrated user story. All implementation tasks are marked complete because the feature existed before SpecKit adoption.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Could have been implemented in parallel at the time because it touched different files or concerns.
- **[Story]**: Migrated user story identifier.
- All paths refer to existing source-controlled files.

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Establish the generic EF Core package and package-local tests for direct inbox mapping/store behavior.

- [x] T001 Create generic EF Core persistence package project `src/Bondstone.Persistence.EntityFrameworkCore/Bondstone.Persistence.EntityFrameworkCore.csproj`
- [x] T002 Create package-local test project `tests/Bondstone.Persistence.EntityFrameworkCore.Tests/Bondstone.Persistence.EntityFrameworkCore.Tests.csproj`
- [x] T003 Add EF Core test DbContext with Bondstone persistence mappings in `tests/Bondstone.Persistence.EntityFrameworkCore.Tests/EntityFrameworkCoreTestDbContext.cs`
- [x] T004 Document EF Core persistence package scope in `src/Bondstone.Persistence.EntityFrameworkCore/README.md`
- [x] T005 Keep EF Core InMemory test expectations documented in `docs/testing.md`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Define the direct inbox EF Core row shape and setup entry points.

- [x] T006 [P] Implement direct inbox EF Core entity in `src/Bondstone.Persistence.EntityFrameworkCore/Inbox/InboxMessageEntity.cs`
- [x] T007 [P] Implement direct inbox EF Core configuration in `src/Bondstone.Persistence.EntityFrameworkCore/Inbox/InboxMessageEntityConfiguration.cs`
- [x] T008 [P] Add `ApplyBondstoneInbox(...)` model-builder extension in `src/Bondstone.Persistence.EntityFrameworkCore/Persistence/BondstoneModelBuilderExtensions.cs`
- [x] T009 [P] Include direct inbox mapping in `ApplyBondstonePersistence(...)`
- [x] T010 [P] Register direct inbox services in `AddBondstoneEntityFrameworkCorePersistence<TDbContext>()`

**Checkpoint**: A DbContext can opt into the direct inbox table mapping and service registration path.

---

## Phase 3: User Story 1 - Map Direct Inbox Records To EF Core Rows (Priority: P1)

**Goal**: Provider-neutral direct inbox records map to and from EF Core entities with a stable table shape.

**Independent Test**: `dotnet test tests/Bondstone.Persistence.EntityFrameworkCore.Tests/Bondstone.Persistence.EntityFrameworkCore.Tests.csproj --configuration Release --filter "Category=Unit|Category=Application"`

### Tests for User Story 1

- [x] T011 [US1] Add entity field mapping coverage in `tests/Bondstone.Persistence.EntityFrameworkCore.Tests/Inbox/InboxMessageEntityTests.cs`
- [x] T012 [US1] Add entity-to-record round-trip coverage in `tests/Bondstone.Persistence.EntityFrameworkCore.Tests/Inbox/InboxMessageEntityTests.cs`
- [x] T013 [US1] Add direct inbox mapping metadata coverage in `tests/Bondstone.Persistence.EntityFrameworkCore.Tests/Persistence/BondstoneModelBuilderExtensionsTests.cs`
- [x] T014 [US1] Add isolated `ApplyBondstoneInbox(...)` coverage in `tests/Bondstone.Persistence.EntityFrameworkCore.Tests/Persistence/BondstoneModelBuilderExtensionsTests.cs`

### Implementation for User Story 1

- [x] T015 [US1] Implement `InboxMessageEntity.FromRecord(...)`
- [x] T016 [US1] Implement `InboxMessageEntity.ToRecord()`
- [x] T017 [US1] Configure `inbox_messages` table name
- [x] T018 [US1] Configure composite key over module name, message id, and handler identity
- [x] T019 [US1] Configure direct inbox columns and max lengths
- [x] T020 [US1] Configure received timestamp index

**Checkpoint**: Direct inbox records have a stable generic EF Core table mapping.

---

## Phase 4: User Story 2 - Stage And Read Direct Inbox Records Through EF Core (Priority: P2)

**Goal**: Generic EF Core direct inbox store supports get, add, and mark-processed behavior inside the caller's DbContext transaction.

**Independent Test**: package application tests after build.

### Tests for User Story 2

- [x] T021 [US2] Add missing-record read coverage in `tests/Bondstone.Persistence.EntityFrameworkCore.Tests/Inbox/EntityFrameworkCoreDurableInboxStoreTests.cs`
- [x] T022 [US2] Add staged-add coverage in `tests/Bondstone.Persistence.EntityFrameworkCore.Tests/Inbox/EntityFrameworkCoreDurableInboxStoreTests.cs`
- [x] T023 [US2] Add persisted-add and get coverage in `tests/Bondstone.Persistence.EntityFrameworkCore.Tests/Inbox/EntityFrameworkCoreDurableInboxStoreTests.cs`
- [x] T024 [US2] Add mark-processed coverage in `tests/Bondstone.Persistence.EntityFrameworkCore.Tests/Inbox/EntityFrameworkCoreDurableInboxStoreTests.cs`
- [x] T025 [US2] Add missing-record mark-processed failure coverage in `tests/Bondstone.Persistence.EntityFrameworkCore.Tests/Inbox/EntityFrameworkCoreDurableInboxStoreTests.cs`

### Implementation for User Story 2

- [x] T026 [US2] Implement `EntityFrameworkCoreDurableInboxStore<TDbContext>`
- [x] T027 [US2] Implement composite-key lookup with `FindAsync(...)`
- [x] T028 [US2] Stage new inbox rows through the current DbContext
- [x] T029 [US2] Stage processed timestamp updates without saving changes
- [x] T030 [US2] Throw clear failure when marking a missing row processed

**Checkpoint**: Direct inbox records can be staged and updated through generic EF Core.

---

## Phase 5: User Story 3 - Inspect Unprocessed Direct Inbox Rows Through EF Core (Priority: P3)

**Goal**: Generic EF Core inspection store returns deterministic received-but-unprocessed direct inbox rows.

**Independent Test**: package application tests after build.

### Tests for User Story 3

- [x] T031 [US3] Add filtered unprocessed-row coverage in `tests/Bondstone.Persistence.EntityFrameworkCore.Tests/Inbox/EntityFrameworkCoreDurableInboxInspectionStoreTests.cs`
- [x] T032 [US3] Add max-count and ordering coverage in `tests/Bondstone.Persistence.EntityFrameworkCore.Tests/Inbox/EntityFrameworkCoreDurableInboxInspectionStoreTests.cs`
- [x] T033 [US3] Add module and received cutoff coverage in `tests/Bondstone.Persistence.EntityFrameworkCore.Tests/Inbox/EntityFrameworkCoreDurableInboxInspectionStoreTests.cs`
- [x] T034 [US3] Add non-UTC cutoff validation coverage in `tests/Bondstone.Persistence.EntityFrameworkCore.Tests/Inbox/EntityFrameworkCoreDurableInboxInspectionStoreTests.cs`

### Implementation for User Story 3

- [x] T035 [US3] Implement `EntityFrameworkCoreDurableInboxInspectionStore<TDbContext>`
- [x] T036 [US3] Validate positive max count and UTC cutoff
- [x] T037 [US3] Normalize optional module filter
- [x] T038 [US3] Query unprocessed rows with `AsNoTracking()`
- [x] T039 [US3] Apply module, cutoff, ordering, and max-count filters
- [x] T040 [US3] Map inspection entities back to `DurableInboxRecord`

**Checkpoint**: Operators can inspect generic EF Core direct inbox rows without mutating them.

---

## Phase 6: User Story 4 - Register EF Core Direct Inbox Services And Mapping (Priority: P4)

**Goal**: Generic EF Core setup exposes direct inbox mapping and services through the normal package setup path.

**Independent Test**: package unit tests after build.

### Tests for User Story 4

- [x] T041 [US4] Add service-registration coverage in `tests/Bondstone.Persistence.EntityFrameworkCore.Tests/Persistence/BondstoneEntityFrameworkCoreServiceCollectionExtensionsTests.cs`
- [x] T042 [US4] Add existing-store preservation coverage in `tests/Bondstone.Persistence.EntityFrameworkCore.Tests/Persistence/BondstoneEntityFrameworkCoreServiceCollectionExtensionsTests.cs`
- [x] T043 [US4] Add full model direct inbox mapping coverage in `tests/Bondstone.Persistence.EntityFrameworkCore.Tests/Persistence/BondstoneModelBuilderExtensionsTests.cs`

### Implementation for User Story 4

- [x] T044 [US4] Register `IDurableInboxStore` with `EntityFrameworkCoreDurableInboxStore<TDbContext>`
- [x] T045 [US4] Register `IDurableInboxInspectionStore` with a scoped factory
- [x] T046 [US4] Preserve existing direct inbox store registrations through `TryAddScoped`
- [x] T047 [US4] Include direct inbox mapping in full Bondstone persistence setup

**Checkpoint**: Consumers get direct inbox EF Core services through generic EF Core setup.

---

## Phase N: Polish & Cross-Cutting Concerns

**Purpose**: Documentation, diagnostics, and compatibility support for the migrated feature.

- [x] T048 [P] Document EF Core package ownership in `docs/architecture.md`
- [x] T049 [P] Document EF Core package ID and dependency direction in `docs/packaging.md`
- [x] T050 [P] Document EF Core InMemory testing limits in `docs/testing.md`
- [x] T051 [P] Keep public API compatibility covered by `tests/Bondstone.PublicApi.Tests`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies.
- **Foundational (Phase 2)**: Depends on generic EF Core package and provider-neutral direct inbox contracts.
- **User Stories (Phase 3+)**: Depend on `DurableInboxRecord`, `DurableInboxMessageKey`, and EF Core model setup.
- **Polish**: Depends on stable public API and table shape.

### User Story Dependencies

- **User Story 1 (P1)**: Can be implemented after EF Core package setup.
- **User Story 2 (P2)**: Depends on direct inbox entity mapping.
- **User Story 3 (P3)**: Depends on direct inbox entity mapping.
- **User Story 4 (P4)**: Depends on mapping/store/inspection implementations.

## Gaps Identified

- PostgreSQL direct inbox registrar behavior, unique constraint races, SQL exception classification, and provider-backed schema checks are outside this migration.
- EF Core InMemory tests do not prove real database transaction isolation, uniqueness, indexing, or SQL generation.
- Consumer EF migration generation and rollout are application-owned.
- Direct inbox cleanup, purge, replay, retention, and operator repair flows are not implemented by this generic EF Core feature.
- Transport-facing durable incoming inbox behavior is separate and already migrated under `specs/006-durable-incoming-inbox-persistence`.

## Verification Commands

The migrated feature is covered by existing tests. Use these commands after building:

```bash
pnpm backend:build
dotnet test tests/Bondstone.Persistence.EntityFrameworkCore.Tests/Bondstone.Persistence.EntityFrameworkCore.Tests.csproj --configuration Release --no-build --filter "Category=Unit|Category=Application"
```
