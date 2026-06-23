---
description: "Migrated task list for existing durable incoming inbox persistence feature"
---

# Tasks: Durable Incoming Inbox Persistence

**Input**: Migrated design documents from `specs/006-durable-incoming-inbox-persistence/`

**Prerequisites**: Existing provider-neutral implementation in `src/Bondstone` and `src/Bondstone.Persistence`; existing focused tests in `tests/Bondstone.Tests/Persistence`

**Tests**: Existing xUnit tests use `Category=Unit`.

**Organization**: Tasks are grouped by migrated user story. All implementation tasks are marked complete because the feature existed before SpecKit adoption.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Could have been implemented in parallel at the time because it touched different files or concerns.
- **[Story]**: Migrated user story identifier.
- All paths refer to existing source-controlled files.

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Establish the provider-neutral persistence and runtime packages for durable incoming inbox behavior.

- [x] T001 Create provider-neutral persistence package project `src/Bondstone.Persistence/Bondstone.Persistence.csproj`
- [x] T002 Create core runtime package project `src/Bondstone/Bondstone.csproj`
- [x] T003 Add package documentation in `src/Bondstone.Persistence/README.md` and scoped guidance in `src/Bondstone.Persistence/AGENTS.md`
- [x] T004 Add focused unit tests under `tests/Bondstone.Tests/Persistence`
- [x] T005 Document durable incoming inbox role in `docs/architecture.md`, `docs/package-discovery.md`, and `docs/packaging.md`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Define the public contracts and shared diagnostics used by durable incoming inbox ingestion and processing.

- [x] T006 [P] Define incoming inbox claim, lease, outcome recording, processing, ingestion, and inspection contracts in `src/Bondstone.Persistence/Persistence/Contracts`
- [x] T007 [P] Define incoming inbox module registration types in `src/Bondstone.Persistence/Persistence/Registration`
- [x] T008 [P] Define incoming inbox failure policy contract in `src/Bondstone/Persistence/IncomingInbox/IDurableIncomingInboxFailurePolicy.cs`
- [x] T009 [P] Implement incoming inbox processing diagnostics in `src/Bondstone/Persistence/IncomingInbox/IncomingInboxProcessingDiagnostics.cs`

**Checkpoint**: Provider-neutral incoming inbox contract surface exists for storage providers, hosting, transport ingestion, and runtime processing.

---

## Phase 3: User Story 1 - Represent Durable Incoming Inbox Receive Identities And State (Priority: P1)

**Goal**: Provider-neutral keys, records, and state represent durable receive rows safely.

**Independent Test**: `dotnet test tests/Bondstone.Tests/Bondstone.Tests.csproj --configuration Release --filter "Category=Unit"`

### Tests for User Story 1

- [x] T010 [US1] Add incoming inbox key validation coverage in `tests/Bondstone.Tests/Persistence/DurableIncomingInboxKeyTests.cs`
- [x] T011 [US1] Add incoming inbox record validation coverage in `tests/Bondstone.Tests/Persistence/DurableIncomingInboxRecordTests.cs`
- [x] T012 [US1] Add incoming inbox state validation coverage in `tests/Bondstone.Tests/Persistence/DurableIncomingInboxStateTests.cs`
- [x] T013 [US1] Add incoming inbox ingestion result coverage in `tests/Bondstone.Tests/Persistence/DurableIncomingInboxIngestionResultTests.cs`
- [x] T014 [US1] Add incoming inbox failure decision coverage in `tests/Bondstone.Tests/Persistence/DurableIncomingInboxFailureDecisionTests.cs`

### Implementation for User Story 1

- [x] T015 [US1] Implement `DurableIncomingInboxKey` with message id, receiver module, and handler identity
- [x] T016 [US1] Implement `DurableIncomingInboxRecord` with key, envelope, ingested timestamp, state, and source transport name
- [x] T017 [US1] Implement `DurableIncomingInboxState` and `DurableIncomingInboxStatus`
- [x] T018 [US1] Implement `DurableIncomingInboxIngestionResult` and `DurableIncomingInboxIngestionStatus`
- [x] T019 [US1] Implement `DurableIncomingInboxProcessingResult`
- [x] T020 [US1] Implement `DurableIncomingInboxFailureDecision` and `DurableIncomingInboxFailureDecisionKind`
- [x] T021 [US1] Validate key/envelope consistency, UTC timestamps, claim pairs, and status-specific state shape

**Checkpoint**: Durable incoming inbox rows and state can be exchanged safely across provider-neutral contracts.

---

## Phase 4: User Story 2 - Ingest Native Deliveries Into A Durable Incoming Inbox Boundary (Priority: P2)

**Goal**: Transport adapters can durably and idempotently ingest native deliveries before native settlement.

**Independent Test**: focused unit tests plus transport/provider integration tests after build.

### Tests for User Story 2

- [x] T022 [US2] Add ingestion result validation coverage in `tests/Bondstone.Tests/Persistence/DurableIncomingInboxIngestionResultTests.cs`
- [x] T023 [US2] Add module ingestion boundary registration and resolver coverage in `tests/Bondstone.Tests/Persistence/DurableModulePersistenceRegistrationTests.cs`
- [x] T024 [US2] Add transport receive worker ingestion ordering coverage in transport package tests
- [x] T025 [US2] Add PostgreSQL ingestion boundary integration coverage in `tests/Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests/Persistence/PostgreSqlIncomingInboxProcessingTests.cs`

### Implementation for User Story 2

- [x] T026 [US2] Implement `IDurableIncomingInboxIngestionStore`
- [x] T027 [US2] Implement `IDurableIncomingInboxIngestionPersistenceScope`
- [x] T028 [US2] Implement `IDurableIncomingInboxIngestionBoundaryResolver`
- [x] T029 [US2] Implement `DurableIncomingInboxIngestionBoundary`
- [x] T030 [US2] Execute ingestion through provider persistence scope and save changes before returning
- [x] T031 [US2] Implement module incoming inbox ingestion boundary registration
- [x] T032 [US2] Implement runtime ingestion boundary resolver with module-specific and fallback resolution paths

**Checkpoint**: Native transport deliveries can become durable incoming inbox records before native acknowledgement.

---

## Phase 5: User Story 3 - Process Claimed Incoming Inbox Rows Through Module Receive Pipelines (Priority: P3)

**Goal**: Provider-neutral dispatcher claims rows, invokes receive pipelines, records outcomes, and reports counts.

**Independent Test**: `dotnet test tests/Bondstone.Tests/Bondstone.Tests.csproj --configuration Release --filter "Category=Unit"`

### Tests for User Story 3

- [x] T033 [US3] Add successful command/event processing coverage in `tests/Bondstone.Tests/Persistence/DurableIncomingInboxDispatcherTests.cs`
- [x] T034 [US3] Add retry scheduling coverage after handler failure in `tests/Bondstone.Tests/Persistence/DurableIncomingInboxDispatcherTests.cs`
- [x] T035 [US3] Add terminal failure coverage after max attempts in `tests/Bondstone.Tests/Persistence/DurableIncomingInboxDispatcherTests.cs`
- [x] T036 [US3] Add stale processed outcome coverage in `tests/Bondstone.Tests/Persistence/DurableIncomingInboxDispatcherTests.cs`
- [x] T037 [US3] Add continue-after-row-failure coverage in `tests/Bondstone.Tests/Persistence/DurableIncomingInboxDispatcherTests.cs`

### Implementation for User Story 3

- [x] T038 [US3] Implement `IDurableIncomingInboxClaimer`
- [x] T039 [US3] Implement `IDurableIncomingInboxOutcomeRecorder`
- [x] T040 [US3] Implement `IDurableIncomingInboxDispatcher`
- [x] T041 [US3] Implement `DurableIncomingInboxDispatcher`
- [x] T042 [US3] Validate claim owner, lease duration, and max count
- [x] T043 [US3] Claim incoming inbox records through `IDurableIncomingInboxClaimer`
- [x] T044 [US3] Invoke command receive pipeline for command envelopes
- [x] T045 [US3] Invoke event receive pipeline with receiver module and handler identity for event envelopes
- [x] T046 [US3] Mark successful records processed
- [x] T047 [US3] Schedule retry or terminal failure outcomes after receive failures
- [x] T048 [US3] Count stale rows when outcome recording fails
- [x] T049 [US3] Continue processing remaining rows after one row fails

**Checkpoint**: Durable incoming inbox processing moves claimed rows through module receive pipelines and records durable outcomes.

---

## Phase 6: User Story 4 - Apply Incoming Inbox Failure Policy, Diagnostics, And Module Aggregation (Priority: P4)

**Goal**: Failure policy, diagnostics, and module aggregation support consistent incoming inbox processing across hosts and modules.

**Independent Test**: unit tests plus provider integration tests after build.

### Tests for User Story 4

- [x] T050 [US4] Add failure decision validation coverage in `tests/Bondstone.Tests/Persistence/DurableIncomingInboxFailureDecisionTests.cs`
- [x] T051 [US4] Add module dispatcher aggregation coverage through registration and provider tests
- [x] T052 [US4] Add downstream processing diagnostics coverage through provider integration where available

### Implementation for User Story 4

- [x] T053 [US4] Implement `DurableIncomingInboxProcessingOptions`
- [x] T054 [US4] Implement `DurableIncomingInboxFailurePolicy`
- [x] T055 [US4] Choose retry decisions below max attempts and terminal failures at max attempts
- [x] T056 [US4] Implement `IncomingInboxProcessingDiagnostics`
- [x] T057 [US4] Emit processing activities and outcome metrics
- [x] T058 [US4] Implement `DurableModuleIncomingInboxDispatcherRegistration`
- [x] T059 [US4] Implement `DurableModuleIncomingInboxDispatcherAggregator`
- [x] T060 [US4] Process registered module dispatchers in order with remaining max count
- [x] T061 [US4] Aggregate incoming inbox processing result counts
- [x] T062 [US4] Fail when no module incoming inbox dispatchers are registered

**Checkpoint**: Incoming inbox processing is retryable, observable, and aggregatable across module boundaries.

---

## Phase N: Polish & Cross-Cutting Concerns

**Purpose**: Documentation, diagnostics, and compatibility support for the migrated feature.

- [x] T063 [P] Document durable incoming inbox ledger behavior in `docs/architecture.md`
- [x] T064 [P] Document incoming inbox contracts in `docs/package-discovery.md`
- [x] T065 [P] Document terminal failure and stale processing inspection in `docs/operations.md`
- [x] T066 [P] Document incoming inbox processing diagnostics in `docs/observability.md`
- [x] T067 [P] Document package dependency direction in `docs/packaging.md`
- [x] T068 [P] Document public API classification in `docs/public-api.md`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies.
- **Foundational (Phase 2)**: Depends on provider-neutral package and runtime package setup.
- **User Stories (Phase 3+)**: Depend on durable message envelopes, module receive pipelines, and incoming inbox contracts.
- **Polish**: Depends on stable public API and behavior.

### User Story Dependencies

- **User Story 1 (P1)**: Can be implemented after provider-neutral package setup.
- **User Story 2 (P2)**: Depends on incoming inbox records and ingestion contracts.
- **User Story 3 (P3)**: Depends on incoming inbox records, processing contracts, module receive pipelines, and failure policy contract.
- **User Story 4 (P4)**: Depends on processing result types, failure decisions, and module persistence registration contracts.

## Gaps Identified

- This migration intentionally excludes EF Core and PostgreSQL incoming inbox storage behavior; those should be migrated as provider-specific features.
- Hosted incoming inbox worker setup is already migrated under `specs/004-hosted-workers` and is not duplicated here.
- Transport receive worker ingestion behavior is already migrated under transport specs and is not duplicated here.
- Incoming inbox processing diagnostics exist in source but have less focused unit coverage than durable outbox diagnostics.
- Incoming inbox failure policy behavior is covered mainly through dispatcher tests; focused policy option tests are thinner than durable outbox policy coverage.
- The dispatcher source notes that long-running handler lease renewal requires a separate heartbeat loop tied to handler lifetime; this is not implemented by the current dispatcher.

## Verification Commands

The migrated feature is covered by existing tests. Use these commands after building:

```bash
pnpm backend:build
dotnet test tests/Bondstone.Tests/Bondstone.Tests.csproj --configuration Release --no-build --filter "Category=Unit"
```
