---
description: "Migrated task list for existing durable outbox persistence feature"
---

# Tasks: Durable Outbox Persistence

**Input**: Migrated design documents from `specs/005-durable-outbox-persistence/`

**Prerequisites**: Existing provider-neutral implementation in `src/Bondstone.Persistence`; existing focused tests in `tests/Bondstone.Tests/Persistence`

**Tests**: Existing xUnit tests use `Category=Unit`.

**Organization**: Tasks are grouped by migrated user story. All implementation tasks are marked complete because the feature existed before SpecKit adoption.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Could have been implemented in parallel at the time because it touched different files or concerns.
- **[Story]**: Migrated user story identifier.
- All paths refer to existing source-controlled files.

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Establish the provider-neutral persistence package and focused tests for durable outbox behavior.

- [x] T001 Create provider-neutral package project `src/Bondstone.Persistence/Bondstone.Persistence.csproj`
- [x] T002 Add package documentation in `src/Bondstone.Persistence/README.md` and scoped agent guidance in `src/Bondstone.Persistence/AGENTS.md`
- [x] T003 Add focused unit tests under `tests/Bondstone.Tests/Persistence`
- [x] T004 Document provider-neutral persistence scope in `docs/architecture.md`, `docs/package-discovery.md`, and `docs/packaging.md`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Define the public contracts and shared diagnostics used by durable outbox dispatch.

- [x] T005 [P] Define envelope dispatch contracts in `src/Bondstone.Persistence/Persistence/Contracts/IDurableEnvelopeDispatcher.cs` and `src/Bondstone.Persistence/Persistence/Contracts/IDurableEnvelopeDispatchRoute.cs`
- [x] T006 [P] Define outbox writer contract in `src/Bondstone.Persistence/Persistence/Contracts/IDurableOutboxWriter.cs`
- [x] T007 [P] Define outbox claim, lease, dispatch recording, dispatcher, and failure policy contracts in `src/Bondstone.Persistence/Persistence/Contracts`
- [x] T008 [P] Define outbox inspection contracts in `src/Bondstone.Persistence/Persistence/Contracts/IDurableOutboxInspector.cs` and `src/Bondstone.Persistence/Persistence/Contracts/IDurableOutboxInspectionStore.cs`
- [x] T009 [P] Implement shared persistence diagnostics used by the outbox dispatcher in `src/Bondstone.Persistence/Persistence/BondstonePersistenceDiagnostics.cs`

**Checkpoint**: Provider-neutral outbox contract surface exists for storage providers, hosting, and transports.

---

## Phase 3: User Story 1 - Represent Durable Outbox Rows And Dispatch State (Priority: P1)

**Goal**: Provider-neutral records and state represent durable outbox rows safely.

**Independent Test**: `dotnet test tests/Bondstone.Tests/Bondstone.Tests.csproj --configuration Release --filter "Category=Unit"`

### Tests for User Story 1

- [x] T010 [US1] Add outbox record construction and validation coverage in `tests/Bondstone.Tests/Persistence/DurableOutboxRecordTests.cs`
- [x] T011 [US1] Add dispatch state validation coverage in `tests/Bondstone.Tests/Persistence/DurableOutboxDispatchStateTests.cs`
- [x] T012 [US1] Add dispatch result validation coverage in `tests/Bondstone.Tests/Persistence/DurableOutboxDispatchResultTests.cs`

### Implementation for User Story 1

- [x] T013 [US1] Implement `DurableOutboxRecord` with envelope, stored timestamp, and default pending state
- [x] T014 [US1] Implement `DurableOutboxDispatchState` with status, attempts, timestamps, failure reason, and claim lease fields
- [x] T015 [US1] Implement `DurableOutboxStatus`
- [x] T016 [US1] Implement `DurableOutboxDispatchResult` with non-negative counts and completed count
- [x] T017 [US1] Validate UTC timestamps and state timestamps relative to stored timestamp

**Checkpoint**: Durable outbox rows and state can be exchanged safely across provider-neutral contracts.

---

## Phase 4: User Story 2 - Dispatch Claimed Outbox Rows Through A Transport Dispatcher (Priority: P2)

**Goal**: Provider-neutral dispatcher claims rows, renews leases, sends envelopes, records outcomes, and reports counts.

**Independent Test**: `dotnet test tests/Bondstone.Tests/Bondstone.Tests.csproj --configuration Release --filter "Category=Unit"`

### Tests for User Story 2

- [x] T018 [US2] Add successful dispatch coverage in `tests/Bondstone.Tests/Persistence/DurableOutboxDispatcherTests.cs`
- [x] T019 [US2] Add dispatch activity coverage in `tests/Bondstone.Tests/Persistence/DurableOutboxDispatcherTests.cs`
- [x] T020 [US2] Add outbox metric coverage in `tests/Bondstone.Tests/Persistence/DurableOutboxDispatcherTests.cs`
- [x] T021 [US2] Add lease renewal stale-row coverage in `tests/Bondstone.Tests/Persistence/DurableOutboxDispatcherTests.cs`
- [x] T022 [US2] Add retry scheduling coverage after transport failure in `tests/Bondstone.Tests/Persistence/DurableOutboxDispatcherTests.cs`
- [x] T023 [US2] Add stale coverage when retry scheduling cannot be recorded in `tests/Bondstone.Tests/Persistence/DurableOutboxDispatcherTests.cs`
- [x] T024 [US2] Add terminal failure recording coverage after max attempts in `tests/Bondstone.Tests/Persistence/DurableOutboxDispatcherTests.cs`
- [x] T025 [US2] Add stale coverage when terminal failure cannot be recorded in `tests/Bondstone.Tests/Persistence/DurableOutboxDispatcherTests.cs`
- [x] T026 [US2] Add stale coverage when successful outcome cannot be recorded in `tests/Bondstone.Tests/Persistence/DurableOutboxDispatcherTests.cs`
- [x] T027 [US2] Add cancellation propagation coverage in `tests/Bondstone.Tests/Persistence/DurableOutboxDispatcherTests.cs`
- [x] T028 [US2] Add blank claim owner validation coverage in `tests/Bondstone.Tests/Persistence/DurableOutboxDispatcherTests.cs`

### Implementation for User Story 2

- [x] T029 [US2] Implement dispatch argument validation for claim owner, lease duration, and max count
- [x] T030 [US2] Claim outbox records with `IDurableOutboxClaimer`
- [x] T031 [US2] Record claimed metrics for claimed records
- [x] T032 [US2] Renew each row lease before envelope dispatch
- [x] T033 [US2] Skip and count stale rows when lease renewal fails
- [x] T034 [US2] Dispatch envelopes through `IDurableEnvelopeDispatcher`
- [x] T035 [US2] Mark successful sends through `IDurableOutboxDispatchRecorder.MarkDispatchedAsync(...)`
- [x] T036 [US2] Decide retry or terminal failure through `IDurableOutboxFailurePolicy`
- [x] T037 [US2] Schedule retry outcomes through `ScheduleRetryAsync(...)`
- [x] T038 [US2] Record terminal failure outcomes through `MarkTerminalFailedAsync(...)`
- [x] T039 [US2] Count stale rows when outcome recording fails
- [x] T040 [US2] Emit outbox dispatch activities and message activities without high-cardinality message ids
- [x] T041 [US2] Emit outbox claimed, dispatched, retry scheduled, terminal failed, and stale metrics

**Checkpoint**: Provider-neutral outbox dispatch moves claimed rows through transport dispatch and durable outcome recording.

---

## Phase 5: User Story 3 - Decide Outbox Retry Or Terminal Failure (Priority: P3)

**Goal**: Failed outbox dispatch attempts become retry or terminal failure decisions consistently.

**Independent Test**: `dotnet test tests/Bondstone.Tests/Bondstone.Tests.csproj --configuration Release --filter "Category=Unit"`

### Tests for User Story 3

- [x] T042 [US3] Add failure policy constructor validation coverage in `tests/Bondstone.Tests/Persistence/DurableOutboxFailurePolicyTests.cs`
- [x] T043 [US3] Add retry decision coverage in `tests/Bondstone.Tests/Persistence/DurableOutboxFailurePolicyTests.cs`
- [x] T044 [US3] Add final-delay fallback coverage in `tests/Bondstone.Tests/Persistence/DurableOutboxFailurePolicyTests.cs`
- [x] T045 [US3] Add immediate retry coverage when no retry delays are configured in `tests/Bondstone.Tests/Persistence/DurableOutboxFailurePolicyTests.cs`
- [x] T046 [US3] Add terminal failure decision coverage when max attempts are reached in `tests/Bondstone.Tests/Persistence/DurableOutboxFailurePolicyTests.cs`
- [x] T047 [US3] Add invalid record and timestamp validation coverage in `tests/Bondstone.Tests/Persistence/DurableOutboxFailurePolicyTests.cs`
- [x] T048 [US3] Add failure decision validation coverage in `tests/Bondstone.Tests/Persistence/DurableOutboxFailureDecisionTests.cs`

### Implementation for User Story 3

- [x] T049 [US3] Implement `DurableOutboxFailureDecision` and `DurableOutboxFailureDecisionKind`
- [x] T050 [US3] Implement configurable `DurableOutboxFailurePolicy`
- [x] T051 [US3] Validate max attempts and retry delays
- [x] T052 [US3] Validate processing record state, attempt count, failure reason, and UTC failure timestamp
- [x] T053 [US3] Return retry decisions for retryable attempts
- [x] T054 [US3] Return terminal failure decisions when max attempts are reached

**Checkpoint**: Outbox failures are transformed into durable retry or terminal failure decisions.

---

## Phase 6: User Story 4 - Route Durable Envelopes And Aggregate Module Dispatchers (Priority: P4)

**Goal**: Advanced composition can route envelopes and aggregate module dispatchers behind provider-neutral contracts.

**Independent Test**: `dotnet test tests/Bondstone.Tests/Bondstone.Tests.csproj --configuration Release --filter "Category=Unit"`

### Tests for User Story 4

- [x] T055 [US4] Add single-route dispatch coverage in `tests/Bondstone.Tests/Persistence/RoutedDurableEnvelopeDispatcherTests.cs`
- [x] T056 [US4] Add ambiguous route setup coverage in `tests/Bondstone.Tests/Persistence/RoutedDurableEnvelopeDispatcherTests.cs`
- [x] T057 [US4] Add missing route setup coverage in `tests/Bondstone.Tests/Persistence/RoutedDurableEnvelopeDispatcherTests.cs`
- [x] T058 [US4] Add module dispatch aggregation coverage in `tests/Bondstone.Tests/Persistence/DurableModuleOutboxDispatchAggregatorTests.cs`
- [x] T059 [US4] Add outbox inspector coverage in `tests/Bondstone.Tests/Persistence/DurableOutboxInspectorTests.cs`

### Implementation for User Story 4

- [x] T060 [US4] Implement `RoutedDurableEnvelopeDispatcher`
- [x] T061 [US4] Report missing routes with `BondstoneSetupCodes.MissingDispatcher`
- [x] T062 [US4] Report ambiguous routes with `BondstoneSetupCodes.AmbiguousDispatchRoute`
- [x] T063 [US4] Implement `DurableModuleOutboxDispatchAggregator`
- [x] T064 [US4] Dispatch module outbox dispatchers in registration order with remaining max count
- [x] T065 [US4] Aggregate module dispatch result counts
- [x] T066 [US4] Fail when no module dispatchers are registered

**Checkpoint**: Durable envelopes can be routed to exactly one adapter and module outbox dispatchers can be aggregated.

---

## Phase N: Polish & Cross-Cutting Concerns

**Purpose**: Documentation, diagnostics, and compatibility support for the migrated feature.

- [x] T067 [P] Document provider-neutral persistence scope in `src/Bondstone.Persistence/README.md`
- [x] T068 [P] Document source outbox role in `docs/architecture.md`
- [x] T069 [P] Document outbox contracts in `docs/package-discovery.md`
- [x] T070 [P] Document terminal outbox failure inspection in `docs/operations.md`
- [x] T071 [P] Document outbox activities and metrics in `docs/observability.md`
- [x] T072 [P] Document package dependency direction in `docs/packaging.md`
- [x] T073 [P] Document public API classification in `docs/public-api.md`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies.
- **Foundational (Phase 2)**: Depends on provider-neutral package and diagnostics setup.
- **User Stories (Phase 3+)**: Depend on outbox contracts and durable envelope contracts.
- **Polish**: Depends on stable public API and behavior.

### User Story Dependencies

- **User Story 1 (P1)**: Can be implemented after provider-neutral package setup.
- **User Story 2 (P2)**: Depends on outbox contracts, records, failure policy contract, and envelope dispatcher contract.
- **User Story 3 (P3)**: Depends on record/state types.
- **User Story 4 (P4)**: Depends on envelope dispatch route contracts and module persistence registration contracts.

## Gaps Identified

- This migration intentionally excludes EF Core and PostgreSQL outbox storage behavior; those should be migrated as provider-specific features.
- The public outbox surface is broad and compatibility-sensitive, including public implementation types that remain exposed for now.
- Focused diagnostics coverage exists for dispatcher activities and metrics, but docs, tests, and public API guidance should stay synchronized whenever diagnostic tag names or metric names change.
- `IDurableOutboxInspector` behavior is represented by focused tests, but provider-specific terminal-failure query ordering and filters belong to EF and PostgreSQL migrations.

## Verification Commands

The migrated feature is covered by existing tests. Use these commands after building:

```bash
pnpm backend:build
dotnet test tests/Bondstone.Tests/Bondstone.Tests.csproj --configuration Release --no-build --filter "Category=Unit"
```
