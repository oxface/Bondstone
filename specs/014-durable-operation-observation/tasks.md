---
description: "Migrated task list for existing durable operation observation feature"
---

# Tasks: Durable Operation Observation

**Input**: Migrated design documents from `specs/014-durable-operation-observation/`

**Prerequisites**: Existing provider-neutral implementation in `src/Bondstone` and `src/Bondstone.Persistence`; existing focused tests in `tests/Bondstone.Tests`

**Tests**: Existing xUnit tests use `Category=Unit`.

**Organization**: Tasks are grouped by migrated user story. All implementation tasks are marked complete because the feature existed before SpecKit adoption.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Could have been implemented in parallel at the time because it touched different files or concerns.
- **[Story]**: Migrated user story identifier.
- All paths refer to existing source-controlled files.

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Establish provider-neutral operation observation records, contracts, runtime services, and focused unit tests.

- [x] T001 Create provider-neutral operation state records in `src/Bondstone.Persistence/Messaging/Operations`
- [x] T002 Create operation observation contracts in `src/Bondstone.Persistence/Messaging/Contracts`, `src/Bondstone.Persistence/Persistence/Contracts`, and `src/Bondstone/Messaging/Contracts`
- [x] T003 Register operation observation services in `src/Bondstone/Configuration/BondstoneServiceCollectionExtensions.cs`
- [x] T004 Add focused operation observation unit tests under `tests/Bondstone.Tests/Messaging`
- [x] T005 Document operation observation ownership in `docs/architecture.md`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Define operation state, result, and module store primitives shared by readers, finalizers, and receive completion.

- [x] T006 [P] Implement `DurableOperationState`
- [x] T007 [P] Implement `DurableOperationStatus`
- [x] T008 [P] Implement `DurableOperationHandle`
- [x] T009 [P] Implement `DurableOperationDiagnosticContext`
- [x] T010 [P] Implement `IDurableOperationStateStore`
- [x] T011 [P] Implement `IDurableOperationExpirationStore`
- [x] T012 [P] Implement `DurableModuleOperationStateStoreRegistration`
- [x] T013 [P] Implement result/wait/finalization/expiration result types in `src/Bondstone/Messaging/Sending`
- [x] T014 [P] Implement `DurableOperationResultPayloadSerializer`

**Checkpoint**: Provider-neutral operation observation data and contracts exist.

---

## Phase 3: User Story 1 - Represent Durable Operation State And Handles (Priority: P1)

**Goal**: Operation state and handles represent accepted durable work safely.

**Independent Test**: `dotnet test tests/Bondstone.Tests/Bondstone.Tests.csproj --configuration Release --filter "Category=Unit"`

### Tests for User Story 1

- [x] T015 [US1] Add operation state validation coverage in `tests/Bondstone.Tests/Messaging/DurableOperationStateTests.cs`
- [x] T016 [US1] Add diagnostic context coverage in `tests/Bondstone.Tests/Messaging/DurableOperationStateTests.cs`
- [x] T017 [US1] Add operation handle coverage in `tests/Bondstone.Tests/Messaging/DurableOperationHandleTests.cs`
- [x] T018 [US1] Add module operation-state registration coverage in `tests/Bondstone.Tests/Persistence/DurableModulePersistenceRegistrationTests.cs`

### Implementation for User Story 1

- [x] T019 [US1] Validate operation ids, statuses, and UTC timestamps
- [x] T020 [US1] Normalize operation handle source and target module names
- [x] T021 [US1] Normalize optional result payloads and failure reasons
- [x] T022 [US1] Require at least one diagnostic context value

**Checkpoint**: Operation state and handles can be exchanged safely across provider-neutral contracts.

---

## Phase 4: User Story 2 - Read Operation State Across Module Stores (Priority: P2)

**Goal**: Operation reads use module-owned operation-state stores and stable ranking rules.

**Independent Test**: focused operation reader unit tests after build.

### Tests for User Story 2

- [x] T023 [US2] Add aggregate terminal-state precedence coverage in `tests/Bondstone.Tests/Messaging/DurableOperationReaderTests.cs`
- [x] T024 [US2] Add newest-state tie-break coverage in `tests/Bondstone.Tests/Messaging/DurableOperationReaderTests.cs`
- [x] T025 [US2] Add hinted module read coverage in `tests/Bondstone.Tests/Messaging/DurableOperationReaderTests.cs`
- [x] T026 [US2] Add operation-handle target-module read coverage in `tests/Bondstone.Tests/Messaging/DurableOperationReaderTests.cs`
- [x] T027 [US2] Add missing module/store diagnostics coverage in `tests/Bondstone.Tests/Messaging/DurableOperationReaderTests.cs`
- [x] T028 [US2] Add root reader/store bypass coverage in `tests/Bondstone.Tests/Messaging/DurableOperationReaderTests.cs`

### Implementation for User Story 2

- [x] T029 [US2] Implement `DurableModuleOperationReader`
- [x] T030 [US2] Implement aggregate module-store reads
- [x] T031 [US2] Implement operation state ranking and timestamp tie-break
- [x] T032 [US2] Implement hinted module reads
- [x] T033 [US2] Implement operation-handle reads through target module
- [x] T034 [US2] Implement `DurableModuleOperationStateStoreResolver`
- [x] T035 [US2] Register module operation reader during Bondstone setup

**Checkpoint**: Operation reads resolve through module operation-state boundaries.

---

## Phase 5: User Story 3 - Read Typed Operation Results And Wait Without Mutating State (Priority: P3)

**Goal**: Typed result readers expose operation outcomes and wait semantics without mutating durable state.

**Independent Test**: focused operation result reader unit tests after build.

### Tests for User Story 3

- [x] T036 [US3] Add unknown result coverage in `tests/Bondstone.Tests/Messaging/DurableOperationResultReaderTests.cs`
- [x] T037 [US3] Add completed-with-result deserialization coverage in `tests/Bondstone.Tests/Messaging/DurableOperationResultReaderTests.cs`
- [x] T038 [US3] Add pending/running/completed-without-result/failed/cancelled coverage in `tests/Bondstone.Tests/Messaging/DurableOperationResultReaderTests.cs`
- [x] T039 [US3] Add module hint and operation handle read coverage in `tests/Bondstone.Tests/Messaging/DurableOperationResultReaderTests.cs`
- [x] T040 [US3] Add deserialization failure coverage in `tests/Bondstone.Tests/Messaging/DurableOperationResultReaderTests.cs`
- [x] T041 [US3] Add wait success and terminal failure/cancellation coverage in `tests/Bondstone.Tests/Messaging/DurableOperationResultReaderTests.cs`
- [x] T042 [US3] Add wait timeout coverage proving no state writes in `tests/Bondstone.Tests/Messaging/DurableOperationResultReaderTests.cs`
- [x] T043 [US3] Add cancellation coverage in `tests/Bondstone.Tests/Messaging/DurableOperationResultReaderTests.cs`

### Implementation for User Story 3

- [x] T044 [US3] Implement `IDurableOperationResultReader`
- [x] T045 [US3] Implement `DurableOperationResultReader`
- [x] T046 [US3] Implement typed result state mapping
- [x] T047 [US3] Implement durable operation result payload serialization/deserialization
- [x] T048 [US3] Implement deserialization failure diagnostics
- [x] T049 [US3] Implement wait and try-wait polling with `TimeProvider`
- [x] T050 [US3] Preserve timeout as caller patience without state mutation

**Checkpoint**: Callers can observe typed operation outcomes and wait for terminal state safely.

---

## Phase 6: User Story 4 - Explicitly Finalize Or Expire Operations (Priority: P4)

**Goal**: Application policy can write explicit failed/cancelled terminal operation states.

**Independent Test**: focused finalizer and expiration processor unit tests after build.

### Tests for User Story 4

- [x] T051 [US4] Add failed finalization coverage in `tests/Bondstone.Tests/Messaging/DurableOperationFinalizerTests.cs`
- [x] T052 [US4] Add cancellation finalization and diagnostic preservation coverage in `tests/Bondstone.Tests/Messaging/DurableOperationFinalizerTests.cs`
- [x] T053 [US4] Add already-terminal preservation coverage in `tests/Bondstone.Tests/Messaging/DurableOperationFinalizerTests.cs`
- [x] T054 [US4] Add missing store and blank reason validation coverage in `tests/Bondstone.Tests/Messaging/DurableOperationFinalizerTests.cs`
- [x] T055 [US4] Add finalization activity and metric coverage in `tests/Bondstone.Tests/Messaging/DurableOperationFinalizerTests.cs`
- [x] T056 [US4] Add expiry candidate finalization coverage in `tests/Bondstone.Tests/Messaging/DurableOperationExpirationProcessorTests.cs`
- [x] T057 [US4] Add expiry max-count and unsupported store coverage in `tests/Bondstone.Tests/Messaging/DurableOperationExpirationProcessorTests.cs`
- [x] T058 [US4] Add expiry terminal-status validation and metric coverage in `tests/Bondstone.Tests/Messaging/DurableOperationExpirationProcessorTests.cs`

### Implementation for User Story 4

- [x] T059 [US4] Implement `IDurableOperationFinalizer`
- [x] T060 [US4] Implement `DurableOperationFinalizer`
- [x] T061 [US4] Resolve finalization store by module
- [x] T062 [US4] Preserve existing terminal states
- [x] T063 [US4] Emit operation finalization telemetry
- [x] T064 [US4] Implement `IDurableOperationExpirationProcessor`
- [x] T065 [US4] Implement `DurableOperationExpirationProcessor`
- [x] T066 [US4] Require expiration-capable operation-state stores
- [x] T067 [US4] Emit operation expiration telemetry

**Checkpoint**: Application policy can explicitly finalize or expire durable operations.

---

## Phase 7: User Story 5 - Complete Operation State From Durable Receives (Priority: P5)

**Goal**: Durable command receive completion writes completed operation state and optional result payloads.

**Independent Test**: module receive pipeline unit tests after build.

### Tests for User Story 5

- [x] T068 [US5] Add durable command receive completed-result coverage in `tests/Bondstone.Tests/Modules/ModuleReceivePipelineTests.cs`
- [x] T069 [US5] Add serialized result payload coverage in `tests/Bondstone.Tests/Modules/ModuleReceivePipelineTests.cs`
- [x] T070 [US5] Add operation diagnostic context coverage in `tests/Bondstone.Tests/Modules/ModuleReceivePipelineTests.cs`

### Implementation for User Story 5

- [x] T071 [US5] Capture operation id in `ModuleCommandReceiveContext`
- [x] T072 [US5] Capture serialized operation result payload in `ModuleCommandExecutionContext`
- [x] T073 [US5] Serialize result command handler payloads in `ModuleCommandRoute`
- [x] T074 [US5] Resolve target module operation-state store in `ModuleCommandRuntime`
- [x] T075 [US5] Save completed operation state after handled direct inbox receive
- [x] T076 [US5] Skip completion writes for duplicate direct inbox receive outcomes

**Checkpoint**: Successful durable receives can be observed through operation result readers.

---

## Phase N: Polish & Cross-Cutting Concerns

**Purpose**: Documentation, diagnostics, and compatibility support for the migrated feature.

- [x] T077 [P] Document operation observation role in `docs/architecture.md`
- [x] T078 [P] Document operation observation test category expectations in `docs/testing.md`
- [x] T079 [P] Keep public API compatibility covered by `tests/Bondstone.PublicApi.Tests`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies.
- **Foundational (Phase 2)**: Depends on provider-neutral package and core runtime package setup.
- **User Stories (Phase 3+)**: Depend on operation state, module runtime registration, and durable receive identity.
- **Polish**: Depends on stable public API and behavior.

### User Story Dependencies

- **User Story 1 (P1)**: Can be implemented after provider-neutral package setup.
- **User Story 2 (P2)**: Depends on operation-state store contracts and module registration.
- **User Story 3 (P3)**: Depends on operation reader and payload serializer.
- **User Story 4 (P4)**: Depends on module operation-state store resolution.
- **User Story 5 (P5)**: Depends on direct inbox receive handling and operation-state store resolution.

## Gaps Identified

- EF Core operation-state entity mapping, store, and expiration candidate query behavior are outside this migration.
- PostgreSQL operation-state persistence, transaction, and schema behavior are outside this migration.
- Bondstone does not automatically mark operations failed or cancelled on caller wait timeout.
- Operation observation does not model workflow/saga/process-manager progress, broker retry, dead-letter state, or durable continuations.
- Cleanup, purge, replay, retention, and operator repair flows are not implemented by this provider-neutral feature.

## Verification Commands

The migrated feature is covered by existing tests. Use these commands after building:

```bash
pnpm backend:build
dotnet test tests/Bondstone.Tests/Bondstone.Tests.csproj --configuration Release --no-build --filter "Category=Unit"
```
