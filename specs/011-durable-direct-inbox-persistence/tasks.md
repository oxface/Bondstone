---
description: "Migrated task list for existing durable direct inbox persistence feature"
---

# Tasks: Durable Direct Inbox Persistence

**Input**: Migrated design documents from `specs/011-durable-direct-inbox-persistence/`

**Prerequisites**: Existing provider-neutral implementation in `src/Bondstone` and `src/Bondstone.Persistence`; existing focused direct inbox and module persistence registration tests in `tests/Bondstone.Tests/Persistence`

**Tests**: Existing xUnit tests use `Category=Unit`.

**Organization**: Tasks are grouped by migrated user story. All implementation tasks are marked complete because the feature existed before SpecKit adoption.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Could have been implemented in parallel at the time because it touched different files or concerns.
- **[Story]**: Migrated user story identifier.
- All paths refer to existing source-controlled files.

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Establish provider-neutral persistence and runtime packages for direct inbox idempotency.

- [x] T001 Create provider-neutral persistence package project `src/Bondstone.Persistence/Bondstone.Persistence.csproj`
- [x] T002 Create core runtime package project `src/Bondstone/Bondstone.csproj`
- [x] T003 Add package documentation in `src/Bondstone.Persistence/README.md`
- [x] T004 Add focused unit tests under `tests/Bondstone.Tests/Persistence`
- [x] T005 Document direct inbox and durable incoming inbox distinction in `docs/architecture.md`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Define public contracts and result types for direct inbox registration, storage, execution, and inspection.

- [x] T006 [P] Define direct inbox store, registrar, handler executor, inspection store, and inspector contracts in `src/Bondstone.Persistence/Persistence/Contracts`
- [x] T007 [P] Define direct inbox key, record, result, and status types in `src/Bondstone.Persistence/Persistence/Inbox`
- [x] T008 [P] Define direct inbox already-received exception in `src/Bondstone.Persistence/Persistence/Contracts/DurableInboxAlreadyReceivedException.cs`
- [x] T009 [P] Add module runtime registration types for direct inbox executors and inspection stores in `src/Bondstone.Persistence/Persistence/Registration`

**Checkpoint**: Provider-neutral direct inbox contract surface exists for runtime, stores, and operators.

---

## Phase 3: User Story 1 - Represent Direct Inbox Receive Identity And State (Priority: P1)

**Goal**: Provider-neutral keys and records represent direct receive idempotency safely.

**Independent Test**: `dotnet test tests/Bondstone.Tests/Bondstone.Tests.csproj --configuration Release --filter "Category=Unit"`

### Tests for User Story 1

- [x] T010 [US1] Add direct inbox key validation coverage in `tests/Bondstone.Tests/Persistence/DurableInboxMessageKeyTests.cs`
- [x] T011 [US1] Add direct inbox record validation coverage in `tests/Bondstone.Tests/Persistence/DurableInboxRecordTests.cs`
- [x] T012 [US1] Add registration result coverage in `tests/Bondstone.Tests/Persistence/DurableInboxRegistrationResultTests.cs`
- [x] T013 [US1] Add handle result coverage in `tests/Bondstone.Tests/Persistence/DurableInboxHandleResultTests.cs`

### Implementation for User Story 1

- [x] T014 [US1] Implement `DurableInboxMessageKey`
- [x] T015 [US1] Implement `DurableInboxRecord`
- [x] T016 [US1] Implement `DurableInboxRegistrationResult` and `DurableInboxRegistrationStatus`
- [x] T017 [US1] Implement `DurableInboxHandleResult` and `DurableInboxHandleStatus`
- [x] T018 [US1] Validate message id, module name, handler identity, UTC timestamps, and supported statuses
- [x] T019 [US1] Implement `DurableInboxRecord.MarkProcessed(...)`

**Checkpoint**: Direct inbox rows and result values can be exchanged safely across provider-neutral contracts.

---

## Phase 4: User Story 2 - Execute A Handler Once Through Direct Inbox Registration (Priority: P2)

**Goal**: Direct inbox handler executor guards handler execution with provider registration state.

**Independent Test**: focused direct inbox unit tests after build.

### Tests for User Story 2

- [x] T020 [US2] Add handled-path coverage in `tests/Bondstone.Tests/Persistence/DurableInboxHandlerExecutorTests.cs`
- [x] T021 [US2] Add `AlreadyReceived` skip coverage in `tests/Bondstone.Tests/Persistence/DurableInboxHandlerExecutorTests.cs`
- [x] T022 [US2] Add `AlreadyProcessed` skip coverage in `tests/Bondstone.Tests/Persistence/DurableInboxHandlerExecutorTests.cs`
- [x] T023 [US2] Add handler exception propagation coverage in `tests/Bondstone.Tests/Persistence/DurableInboxHandlerExecutorTests.cs`
- [x] T024 [US2] Add already-received exception coverage in `tests/Bondstone.Tests/Persistence/DurableInboxHandleResultTests.cs`

### Implementation for User Story 2

- [x] T025 [US2] Implement `IDurableInboxRegistrar`
- [x] T026 [US2] Implement `IDurableInboxStore`
- [x] T027 [US2] Implement `IDurableInboxHandlerExecutor`
- [x] T028 [US2] Implement `DurableInboxHandlerExecutor`
- [x] T029 [US2] Register direct inbox record before invoking handler
- [x] T030 [US2] Skip handler execution for duplicate registration results
- [x] T031 [US2] Mark records processed after successful handler execution
- [x] T032 [US2] Propagate handler exceptions without marking processed

**Checkpoint**: Direct inbox handling executes handlers at most once per registered receive identity.

---

## Phase 5: User Story 3 - Inspect Unprocessed Direct Inbox Rows By Module (Priority: P3)

**Goal**: Runtime inspection routes unprocessed direct inbox reads to module-specific provider stores.

**Independent Test**: focused direct inbox unit tests after build.

### Tests for User Story 3

- [x] T033 [US3] Add successful module inspection coverage in `tests/Bondstone.Tests/Persistence/DurableInboxInspectorTests.cs`
- [x] T034 [US3] Add module/max-count validation coverage in `tests/Bondstone.Tests/Persistence/DurableInboxInspectorTests.cs`
- [x] T035 [US3] Add missing module diagnostics coverage in `tests/Bondstone.Tests/Persistence/DurableInboxInspectorTests.cs`
- [x] T036 [US3] Add missing inspection store diagnostics coverage in `tests/Bondstone.Tests/Persistence/DurableInboxInspectorTests.cs`

### Implementation for User Story 3

- [x] T037 [US3] Implement `IDurableInboxInspectionStore`
- [x] T038 [US3] Implement `IDurableInboxInspector`
- [x] T039 [US3] Implement `DurableInboxInspector`
- [x] T040 [US3] Validate module name and max count before inspection
- [x] T041 [US3] Route inspection through module runtime registry
- [x] T042 [US3] Emit setup diagnostics for missing module or inspection store

**Checkpoint**: Operators can inspect received-but-unprocessed direct inbox rows through module boundaries.

---

## Phase 6: User Story 4 - Resolve Module-Specific Direct Inbox Handler Executors (Priority: P4)

**Goal**: Runtime receive pipelines resolve the correct module-specific direct inbox handler executor.

**Independent Test**: unit tests plus provider setup tests after build.

### Tests for User Story 4

- [x] T043 [US4] Add fallback executor coverage in `tests/Bondstone.Tests/Persistence/DurableModulePersistenceRegistrationTests.cs`
- [x] T044 [US4] Add module-specific executor coverage in `tests/Bondstone.Tests/Persistence/DurableModulePersistenceRegistrationTests.cs`
- [x] T045 [US4] Add missing module/executor diagnostics coverage in runtime and provider tests

### Implementation for User Story 4

- [x] T046 [US4] Implement module direct inbox handler executor registrations
- [x] T047 [US4] Implement `DurableModuleInboxHandlerExecutorResolver`
- [x] T048 [US4] Resolve fallback executor only when no durable module persistence registrations exist
- [x] T049 [US4] Resolve module-specific executor when registered
- [x] T050 [US4] Emit setup diagnostics for missing module or handler executor

**Checkpoint**: Direct inbox handling uses the correct module persistence boundary.

---

## Phase N: Polish & Cross-Cutting Concerns

**Purpose**: Documentation, diagnostics, and compatibility support for the migrated feature.

- [x] T051 [P] Document direct inbox role in `docs/architecture.md`
- [x] T052 [P] Document direct inbox contracts in `docs/package-discovery.md`
- [x] T053 [P] Document direct inbox inspection in `docs/operations.md`
- [x] T054 [P] Document package dependency direction in `docs/packaging.md`
- [x] T055 [P] Document test category expectations in `docs/testing.md`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies.
- **Foundational (Phase 2)**: Depends on provider-neutral package and runtime package setup.
- **User Stories (Phase 3+)**: Depend on durable message identities and module runtime registration.
- **Polish**: Depends on stable public API and behavior.

### User Story Dependencies

- **User Story 1 (P1)**: Can be implemented after provider-neutral package setup.
- **User Story 2 (P2)**: Depends on direct inbox records, registration contracts, and store contracts.
- **User Story 3 (P3)**: Depends on inspection contracts and module runtime registration.
- **User Story 4 (P4)**: Depends on handler executor contracts and module runtime registration.

## Gaps Identified

- EF Core and PostgreSQL direct inbox storage behavior are outside this migration and should be migrated separately.
- Direct inbox cleanup, purge, replay, retention, and operator repair flows are not implemented by this provider-neutral feature.
- Direct inbox inspection exposes received-but-unprocessed records but does not mutate stale rows.
- Transport-facing durable incoming inbox behavior is separate and already migrated under `specs/006-durable-incoming-inbox-persistence`.

## Verification Commands

The migrated feature is covered by existing tests. Use these commands after building:

```bash
pnpm backend:build
dotnet test tests/Bondstone.Tests/Bondstone.Tests.csproj --configuration Release --no-build --filter "Category=Unit"
```
