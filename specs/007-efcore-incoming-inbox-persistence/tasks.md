---
description: "Migrated task list for existing EF Core incoming inbox persistence feature"
---

# Tasks: EF Core Incoming Inbox Persistence

**Input**: Migrated design documents from `specs/007-efcore-incoming-inbox-persistence/`

**Prerequisites**: Existing generic EF Core implementation in `src/Bondstone.Persistence.EntityFrameworkCore`; existing focused tests in `tests/Bondstone.Persistence.EntityFrameworkCore.Tests`

**Tests**: Existing xUnit tests use `Category=Unit` and `Category=Application`.

**Organization**: Tasks are grouped by migrated user story. All implementation tasks are marked complete because the feature existed before SpecKit adoption.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Could have been implemented in parallel at the time because it touched different files or concerns.
- **[Story]**: Migrated user story identifier.
- All paths refer to existing source-controlled files.

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Establish generic EF Core package support for durable incoming inbox mapping and stores.

- [x] T001 Create EF Core persistence package project `src/Bondstone.Persistence.EntityFrameworkCore/Bondstone.Persistence.EntityFrameworkCore.csproj`
- [x] T002 Add package documentation in `src/Bondstone.Persistence.EntityFrameworkCore/README.md`
- [x] T003 Add scoped package guidance in `src/Bondstone.Persistence.EntityFrameworkCore/AGENTS.md`
- [x] T004 Add EF Core persistence test project `tests/Bondstone.Persistence.EntityFrameworkCore.Tests/Bondstone.Persistence.EntityFrameworkCore.Tests.csproj`
- [x] T005 Add EF Core persistence test guidance in `tests/Bondstone.Persistence.EntityFrameworkCore.Tests/README.md` and `tests/Bondstone.Persistence.EntityFrameworkCore.Tests/AGENTS.md`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Define shared EF Core setup hooks used by all generic durable persistence mappings.

- [x] T006 [P] Implement model builder extension container in `src/Bondstone.Persistence.EntityFrameworkCore/Persistence/BondstoneModelBuilderExtensions.cs`
- [x] T007 [P] Implement EF Core service registration extension in `src/Bondstone.Persistence.EntityFrameworkCore/Persistence/BondstoneEntityFrameworkCoreServiceCollectionExtensions.cs`
- [x] T008 [P] Implement EF Core module persistence setup in `src/Bondstone.Persistence.EntityFrameworkCore/Persistence/BondstoneEntityFrameworkCoreModuleBuilderExtensions.cs`
- [x] T009 [P] Add shared EF Core InMemory test context in `tests/Bondstone.Persistence.EntityFrameworkCore.Tests/EntityFrameworkCoreTestDbContext.cs`
- [x] T010 [P] Add missing incoming inbox mapping test context in `tests/Bondstone.Persistence.EntityFrameworkCore.Tests/MissingIncomingInboxMappingDbContext.cs`

**Checkpoint**: EF Core package has shared setup surfaces for adding durable persistence mappings, services, and module-specific boundaries.

---

## Phase 3: User Story 1 - Map Durable Incoming Inbox Rows Into EF Core (Priority: P1)

**Goal**: EF Core can map durable incoming inbox rows without losing provider-neutral record data.

**Independent Test**: `dotnet test tests/Bondstone.Persistence.EntityFrameworkCore.Tests/Bondstone.Persistence.EntityFrameworkCore.Tests.csproj --configuration Release --filter "Category=Unit|Category=Application"`

### Tests for User Story 1

- [x] T011 [US1] Add entity conversion coverage in `tests/Bondstone.Persistence.EntityFrameworkCore.Tests/IncomingInbox/IncomingInboxMessageEntityTests.cs`
- [x] T012 [US1] Add command record round-trip coverage in `tests/Bondstone.Persistence.EntityFrameworkCore.Tests/IncomingInbox/IncomingInboxMessageEntityTests.cs`
- [x] T013 [US1] Add event subscriber record round-trip coverage in `tests/Bondstone.Persistence.EntityFrameworkCore.Tests/IncomingInbox/IncomingInboxMessageEntityTests.cs`
- [x] T014 [US1] Add incoming inbox mapping coverage through `ApplyBondstonePersistence(...)` in `tests/Bondstone.Persistence.EntityFrameworkCore.Tests/Persistence/BondstoneModelBuilderExtensionsTests.cs`
- [x] T015 [US1] Add granular `ApplyBondstoneIncomingInbox(...)` mapping coverage in `tests/Bondstone.Persistence.EntityFrameworkCore.Tests/Persistence/BondstoneModelBuilderExtensionsTests.cs`
- [x] T016 [US1] Add primary key, length-limit, and index coverage in `tests/Bondstone.Persistence.EntityFrameworkCore.Tests/Persistence/BondstoneModelBuilderExtensionsTests.cs`

### Implementation for User Story 1

- [x] T017 [US1] Implement `IncomingInboxMessageEntity` in `src/Bondstone.Persistence.EntityFrameworkCore/IncomingInbox/IncomingInboxMessageEntity.cs`
- [x] T018 [US1] Implement `IncomingInboxMessageEntity.FromRecord(...)`
- [x] T019 [US1] Implement `IncomingInboxMessageEntity.ToRecord()`
- [x] T020 [US1] Implement `IncomingInboxMessageEntityConfiguration` in `src/Bondstone.Persistence.EntityFrameworkCore/IncomingInbox/IncomingInboxMessageEntityConfiguration.cs`
- [x] T021 [US1] Map table `incoming_inbox_messages` and primary key `{ ReceiverModule, MessageId, HandlerIdentity }`
- [x] T022 [US1] Configure message, trace, source transport, state, retry, failure, and claim columns
- [x] T023 [US1] Configure string enum conversions, max lengths, required fields, and incoming inbox query indexes
- [x] T024 [US1] Add `ApplyBondstoneIncomingInbox(...)` model builder extension
- [x] T025 [US1] Include incoming inbox mapping in `ApplyBondstonePersistence(...)`

**Checkpoint**: EF Core can represent and map durable incoming inbox records.

---

## Phase 4: User Story 2 - Ingest Incoming Inbox Records Idempotently With EF Core (Priority: P2)

**Goal**: EF Core ingestion store stages pending durable incoming inbox rows and detects duplicates by receive identity.

**Independent Test**: EF Core persistence tests after build.

### Tests for User Story 2

- [x] T026 [US2] Add new-row ingestion coverage in `tests/Bondstone.Persistence.EntityFrameworkCore.Tests/IncomingInbox/EntityFrameworkCoreDurableIncomingInboxIngestionStoreTests.cs`
- [x] T027 [US2] Add persisted duplicate coverage in `tests/Bondstone.Persistence.EntityFrameworkCore.Tests/IncomingInbox/EntityFrameworkCoreDurableIncomingInboxIngestionStoreTests.cs`
- [x] T028 [US2] Add tracked-before-save duplicate coverage in `tests/Bondstone.Persistence.EntityFrameworkCore.Tests/IncomingInbox/EntityFrameworkCoreDurableIncomingInboxIngestionStoreTests.cs`
- [x] T029 [US2] Add non-pending new-row rejection coverage in `tests/Bondstone.Persistence.EntityFrameworkCore.Tests/IncomingInbox/EntityFrameworkCoreDurableIncomingInboxIngestionStoreTests.cs`
- [x] T030 [US2] Add missing mapping setup error coverage in `tests/Bondstone.Persistence.EntityFrameworkCore.Tests/IncomingInbox/EntityFrameworkCoreDurableIncomingInboxIngestionStoreTests.cs`

### Implementation for User Story 2

- [x] T031 [US2] Implement `EntityFrameworkCoreDurableIncomingInboxIngestionStore<TDbContext>`
- [x] T032 [US2] Validate incoming inbox mapping before ingestion
- [x] T033 [US2] Find existing rows by receiver module, message id, and handler identity
- [x] T034 [US2] Return `AlreadyIngested` with the existing record for duplicate receive identity
- [x] T035 [US2] Reject new records whose state is not `Pending`
- [x] T036 [US2] Add new pending records to `DbContext.Set<IncomingInboxMessageEntity>()`
- [x] T037 [US2] Implement `EntityFrameworkCoreDurableIncomingInboxIngestionPersistenceScope`
- [x] T038 [US2] Delegate ingestion-scope execution and save behavior to `IEntityFrameworkCorePersistenceScope`

**Checkpoint**: Generic EF Core can stage durable incoming inbox ingestion rows idempotently.

---

## Phase 5: User Story 3 - Inspect Incoming Inbox Rows With EF Core (Priority: P3)

**Goal**: EF Core inspection store exposes read-only incoming inbox queries for operations and maintenance.

**Independent Test**: EF Core persistence tests after build.

### Tests for User Story 3

- [x] T039 [US3] Add filtered `FindAsync(...)` coverage in `tests/Bondstone.Persistence.EntityFrameworkCore.Tests/IncomingInbox/EntityFrameworkCoreDurableIncomingInboxInspectionStoreTests.cs`
- [x] T040 [US3] Add stale processing query coverage in `tests/Bondstone.Persistence.EntityFrameworkCore.Tests/IncomingInbox/EntityFrameworkCoreDurableIncomingInboxInspectionStoreTests.cs`
- [x] T041 [US3] Add terminal failure query coverage in `tests/Bondstone.Persistence.EntityFrameworkCore.Tests/IncomingInbox/EntityFrameworkCoreDurableIncomingInboxInspectionStoreTests.cs`
- [x] T042 [US3] Add non-UTC cutoff validation coverage in `tests/Bondstone.Persistence.EntityFrameworkCore.Tests/IncomingInbox/EntityFrameworkCoreDurableIncomingInboxInspectionStoreTests.cs`
- [x] T043 [US3] Add missing mapping setup error coverage in `tests/Bondstone.Persistence.EntityFrameworkCore.Tests/IncomingInbox/EntityFrameworkCoreDurableIncomingInboxInspectionStoreTests.cs`

### Implementation for User Story 3

- [x] T044 [US3] Implement `EntityFrameworkCoreDurableIncomingInboxInspectionStore<TDbContext>`
- [x] T045 [US3] Validate incoming inbox mapping before inspection
- [x] T046 [US3] Implement no-tracking base query with receiver module and source transport filters
- [x] T047 [US3] Implement `FindAsync(...)` with status, ingested cutoff, max count, and deterministic ordering
- [x] T048 [US3] Implement `FindStaleProcessingAsync(...)` with processing status, claim expiration cutoff, max count, and deterministic ordering
- [x] T049 [US3] Implement `FindTerminalFailedAsync(...)` with terminal status, failed cutoff, max count, and deterministic ordering
- [x] T050 [US3] Normalize optional receiver module and source transport filters
- [x] T051 [US3] Validate positive `maxCount`
- [x] T052 [US3] Validate non-default UTC cutoff timestamps
- [x] T053 [US3] Convert inspection query results back to `DurableIncomingInboxRecord`

**Checkpoint**: Generic EF Core can read durable incoming inbox rows for operations and inspection.

---

## Phase 6: User Story 4 - Register EF Core Incoming Inbox Services And Module Boundaries (Priority: P4)

**Goal**: EF Core setup registers incoming inbox stores and module-specific ingestion boundaries.

**Independent Test**: EF Core persistence setup and transaction behavior tests after build.

### Tests for User Story 4

- [x] T054 [US4] Add service registration coverage in `tests/Bondstone.Persistence.EntityFrameworkCore.Tests/Persistence/BondstoneEntityFrameworkCoreServiceCollectionExtensionsTests.cs`
- [x] T055 [US4] Add `TryAdd` preservation coverage in `tests/Bondstone.Persistence.EntityFrameworkCore.Tests/Persistence/BondstoneEntityFrameworkCoreServiceCollectionExtensionsTests.cs`
- [x] T056 [US4] Add module-specific incoming inbox ingestion boundary coverage in `tests/Bondstone.Persistence.EntityFrameworkCore.Tests/Persistence/EntityFrameworkCoreModuleTransactionBehaviorTests.cs`

### Implementation for User Story 4

- [x] T057 [US4] Register `IDurableIncomingInboxIngestionStore` in `AddBondstoneEntityFrameworkCorePersistence<TDbContext>()`
- [x] T058 [US4] Register `IDurableIncomingInboxIngestionPersistenceScope` in `AddBondstoneEntityFrameworkCorePersistence<TDbContext>()`
- [x] T059 [US4] Register `IDurableIncomingInboxInspectionStore` in `AddBondstoneEntityFrameworkCorePersistence<TDbContext>()`
- [x] T060 [US4] Use `TryAdd` semantics for EF Core durable persistence service registrations
- [x] T061 [US4] Register module-specific incoming inbox ingestion boundary from `UseEntityFrameworkCoreModulePersistence<TDbContext>()`
- [x] T062 [US4] Build module-specific ingestion boundary with the receiver module's `TDbContext` and EF persistence scope

**Checkpoint**: EF-backed modules get incoming inbox services and receiver-module ingestion boundaries through normal setup.

---

## Phase N: Polish & Cross-Cutting Concerns

**Purpose**: Documentation, packaging, and compatibility support for the migrated feature.

- [x] T063 [P] Document EF Core package purpose in `src/Bondstone.Persistence.EntityFrameworkCore/README.md`
- [x] T064 [P] Document EF Core mapping package in `docs/package-discovery.md`
- [x] T065 [P] Document application-owned EF migrations in `docs/packaging.md`
- [x] T066 [P] Document durable incoming inbox inspection in `docs/operations.md`
- [x] T067 [P] Document EF InMemory test limitations in `docs/testing.md`
- [x] T068 [P] Preserve public API classification expectations through `docs/public-api.md`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies.
- **Foundational (Phase 2)**: Depends on EF Core package and test project setup.
- **User Story 1 (Phase 3)**: Depends on provider-neutral incoming inbox contracts.
- **User Story 2 (Phase 4)**: Depends on incoming inbox entity and mapping.
- **User Story 3 (Phase 5)**: Depends on incoming inbox entity and mapping.
- **User Story 4 (Phase 6)**: Depends on EF Core stores, persistence scope, and module persistence registration infrastructure.
- **Polish**: Depends on stable package behavior and public setup surface.

### User Story Dependencies

- **User Story 1 (P1)**: Can be implemented once provider-neutral incoming inbox contracts exist.
- **User Story 2 (P2)**: Depends on mapped incoming inbox entity.
- **User Story 3 (P3)**: Depends on mapped incoming inbox entity.
- **User Story 4 (P4)**: Depends on ingestion store, inspection store, and EF Core module setup.

## Gaps Identified

- EF InMemory tests do not prove relational uniqueness, SQL generation, locking, or concurrency behavior.
- PostgreSQL-specific incoming inbox claim, lease renewal, processed, retry, terminal failure, stale mutation, and SQL behavior is outside this migration.
- Application EF migrations are not shipped by Bondstone and remain application-owned.
- Generic EF Core incoming inbox does not implement cleanup, purge, retention, replay, or operator repair flows.
- Concrete public EF Core entity/store types remain compatibility-sensitive public API; broad visibility cleanup would need separate review.

## Verification Commands

The migrated feature is covered by existing tests. Use these commands after building:

```bash
pnpm backend:build
dotnet test tests/Bondstone.Persistence.EntityFrameworkCore.Tests/Bondstone.Persistence.EntityFrameworkCore.Tests.csproj --configuration Release --no-build --filter "Category=Unit|Category=Application"
```
