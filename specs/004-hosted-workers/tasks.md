---
description: "Migrated task list for existing hosted workers feature"
---

# Tasks: Hosted Workers

**Input**: Migrated design documents from `specs/004-hosted-workers/`

**Prerequisites**: Existing implementation in `src/Bondstone.Hosting`; existing tests in `tests/Bondstone.Hosting.Tests`

**Tests**: Existing xUnit tests use `Category=Unit`.

**Organization**: Tasks are grouped by migrated user story. All implementation tasks are marked complete because the feature existed before SpecKit adoption.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Could have been implemented in parallel at the time because it touched different files or concerns.
- **[Story]**: Migrated user story identifier.
- All paths refer to existing source-controlled files.

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Establish the package and test project for hosted durable workers.

- [x] T001 Create packable package project `src/Bondstone.Hosting/Bondstone.Hosting.csproj`
- [x] T002 Add package references to `Bondstone`, `Bondstone.Persistence`, Microsoft hosting/logging/options abstractions, and DI abstractions
- [x] T003 Create test project `tests/Bondstone.Hosting.Tests/Bondstone.Hosting.Tests.csproj`
- [x] T004 Add test dependencies for xUnit, Microsoft test SDK, DI, and coverlet
- [x] T005 Add scoped package and test documentation in `src/Bondstone.Hosting/README.md`, `src/Bondstone.Hosting/AGENTS.md`, `tests/Bondstone.Hosting.Tests/README.md`, and `tests/Bondstone.Hosting.Tests/AGENTS.md`
- [x] T006 Expose internals to `Bondstone.Hosting.Tests` through `src/Bondstone.Hosting/Properties/AssemblyInfo.cs`
- [x] T007 [P] Implement shared string normalization helpers in `src/Bondstone.Hosting/Utility/StringExtensions.cs`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Build the shared hosted worker infrastructure required by outbox and incoming inbox workers.

- [x] T008 [P] Implement outbox worker options and validator in `src/Bondstone.Hosting/Outbox/DurableOutboxWorkerOptions.cs` and `src/Bondstone.Hosting/Outbox/DurableOutboxWorkerOptionsValidator.cs`
- [x] T009 [P] Implement incoming inbox worker options and validator in `src/Bondstone.Hosting/IncomingInbox/DurableIncomingInboxWorkerOptions.cs` and `src/Bondstone.Hosting/IncomingInbox/DurableIncomingInboxWorkerOptionsValidator.cs`
- [x] T010 [P] Implement outbox worker log event metadata in `src/Bondstone.Hosting/Outbox/DurableOutboxWorkerLogEvents.cs`
- [x] T011 [P] Implement incoming inbox worker log event metadata in `src/Bondstone.Hosting/IncomingInbox/DurableIncomingInboxWorkerLogEvents.cs`

**Checkpoint**: Worker options and log events exist for hosted durable processing loops.

---

## Phase 3: User Story 1 - Register Hosted Durable Outbox Worker (Priority: P1)

**Goal**: Hosts can register the default durable outbox dispatcher and hosted outbox worker.

**Independent Test**: `dotnet test tests/Bondstone.Hosting.Tests/Bondstone.Hosting.Tests.csproj --configuration Release --filter "Category=Unit"`

### Tests for User Story 1

- [x] T012 [US1] Add null service collection coverage in `tests/Bondstone.Hosting.Tests/Outbox/BondstoneHostingServiceCollectionExtensionsTests.cs`
- [x] T013 [US1] Add hosted worker and default dispatcher registration coverage in `tests/Bondstone.Hosting.Tests/Outbox/BondstoneHostingServiceCollectionExtensionsTests.cs`
- [x] T014 [US1] Add custom dispatcher preservation coverage in `tests/Bondstone.Hosting.Tests/Outbox/BondstoneHostingServiceCollectionExtensionsTests.cs`
- [x] T015 [US1] Add default dispatcher and failure policy registration coverage in `tests/Bondstone.Hosting.Tests/Outbox/BondstoneHostingServiceCollectionExtensionsTests.cs`
- [x] T016 [US1] Add `UseWorker(...)` capability marking coverage in `tests/Bondstone.Hosting.Tests/Outbox/BondstoneHostingServiceCollectionExtensionsTests.cs`

### Implementation for User Story 1

- [x] T017 [US1] Expose `UseDurableDispatcher()` from `BondstoneOutboxBuilder`
- [x] T018 [US1] Expose `UseWorker(...)` from `BondstoneOutboxBuilder`
- [x] T019 [US1] Expose `AddBondstoneDurableOutboxDispatcher()` from `IServiceCollection`
- [x] T020 [US1] Expose `AddBondstoneDurableOutboxWorker(...)` from `IServiceCollection`
- [x] T021 [US1] Register default `IDurableOutboxDispatcher` and `IDurableOutboxFailurePolicy` without replacing custom dispatcher registrations
- [x] T022 [US1] Register `DurableOutboxWorker` as an `IHostedService`
- [x] T023 [US1] Register `DurableOutboxWorkerOptionsValidator`

**Checkpoint**: Durable outbox worker setup APIs compose the default dispatcher and hosted worker.

---

## Phase 4: User Story 2 - Dispatch Durable Outbox Batches (Priority: P2)

**Goal**: Hosted outbox worker repeatedly claims and dispatches durable outbox batches.

**Independent Test**: `dotnet test tests/Bondstone.Hosting.Tests/Bondstone.Hosting.Tests.csproj --configuration Release --filter "Category=Unit"`

### Tests for User Story 2

- [x] T024 [US2] Add outbox worker option validation coverage in `tests/Bondstone.Hosting.Tests/Outbox/DurableOutboxWorkerOptionsTests.cs`
- [x] T025 [US2] Add options validator failure coverage in `tests/Bondstone.Hosting.Tests/Outbox/DurableOutboxWorkerOptionsTests.cs`
- [x] T026 [US2] Add worker option forwarding coverage in `tests/Bondstone.Hosting.Tests/Outbox/DurableOutboxWorkerTests.cs`
- [x] T027 [US2] Add immediate next-batch dispatch coverage when rows are claimed in `tests/Bondstone.Hosting.Tests/Outbox/DurableOutboxWorkerTests.cs`
- [x] T028 [US2] Add dispatcher failure logging and retry coverage in `tests/Bondstone.Hosting.Tests/Outbox/DurableOutboxWorkerTests.cs`
- [x] T029 [US2] Add invalid constructor option coverage in `tests/Bondstone.Hosting.Tests/Outbox/DurableOutboxWorkerTests.cs`
- [x] T030 [US2] Add missing dispatcher startup failure coverage in `tests/Bondstone.Hosting.Tests/Outbox/DurableOutboxWorkerTests.cs`

### Implementation for User Story 2

- [x] T031 [US2] Implement outbox worker option validation for worker id, lease duration, batch size, polling interval, and failure delay
- [x] T032 [US2] Validate outbox worker options during worker construction
- [x] T033 [US2] Verify `IDurableOutboxDispatcher` registration during outbox worker startup
- [x] T034 [US2] Resolve `IDurableOutboxDispatcher` from a new async scope for each batch
- [x] T035 [US2] Call `DispatchAsync(...)` with configured worker id, lease duration, and batch size
- [x] T036 [US2] Immediately dispatch another batch when claimed rows are returned
- [x] T037 [US2] Wait for polling interval when no rows are claimed
- [x] T038 [US2] Log dispatch batch failures with event id `1001`
- [x] T039 [US2] Wait for failure delay after unexpected dispatcher failure and continue
- [x] T040 [US2] Stop the loop when host cancellation is requested

**Checkpoint**: Hosted outbox worker loops over durable outbox dispatch batches using configured options.

---

## Phase 5: User Story 3 - Register Hosted Durable Incoming Inbox Worker (Priority: P3)

**Goal**: Hosts can register the durable incoming inbox processing worker and processing retry policy options.

**Independent Test**: `dotnet test tests/Bondstone.Hosting.Tests/Bondstone.Hosting.Tests.csproj --configuration Release --filter "Category=Unit"`

### Tests for User Story 3

- [x] T041 [US3] Add null service collection coverage in `tests/Bondstone.Hosting.Tests/IncomingInbox/BondstoneIncomingInboxHostingServiceCollectionExtensionsTests.cs`
- [x] T042 [US3] Add hosted worker, options validator, and processing options registration coverage in `tests/Bondstone.Hosting.Tests/IncomingInbox/BondstoneIncomingInboxHostingServiceCollectionExtensionsTests.cs`
- [x] T043 [US3] Add duplicate hosted worker prevention coverage in `tests/Bondstone.Hosting.Tests/IncomingInbox/BondstoneIncomingInboxHostingServiceCollectionExtensionsTests.cs`
- [x] T044 [US3] Add default processing options coverage in `tests/Bondstone.Hosting.Tests/IncomingInbox/BondstoneIncomingInboxHostingServiceCollectionExtensionsTests.cs`
- [x] T045 [US3] Add `UseDurableIncomingInboxWorker(...)` retry policy propagation coverage in `tests/Bondstone.Hosting.Tests/IncomingInbox/BondstoneIncomingInboxHostingServiceCollectionExtensionsTests.cs`

### Implementation for User Story 3

- [x] T046 [US3] Expose `UseDurableIncomingInboxWorker(...)` from `BondstoneBuilder`
- [x] T047 [US3] Expose `AddBondstoneDurableIncomingInboxWorker(...)` from `IServiceCollection`
- [x] T048 [US3] Register `DurableIncomingInboxWorker` as an `IHostedService`
- [x] T049 [US3] Register `DurableIncomingInboxWorkerOptionsValidator`
- [x] T050 [US3] Register `DurableIncomingInboxProcessingOptions` derived from worker options
- [x] T051 [US3] Avoid duplicate hosted incoming inbox worker registration

**Checkpoint**: Durable incoming inbox worker setup APIs compose hosted processing and retry policy options.

---

## Phase 6: User Story 4 - Process Durable Incoming Inbox Batches (Priority: P4)

**Goal**: Hosted incoming inbox worker repeatedly claims and processes durable incoming inbox batches.

**Independent Test**: `dotnet test tests/Bondstone.Hosting.Tests/Bondstone.Hosting.Tests.csproj --configuration Release --filter "Category=Unit"`

### Tests for User Story 4

- [x] T052 [US4] Add incoming inbox worker default option coverage in `tests/Bondstone.Hosting.Tests/IncomingInbox/DurableIncomingInboxWorkerOptionsTests.cs`
- [x] T053 [US4] Add incoming inbox worker option validation coverage in `tests/Bondstone.Hosting.Tests/IncomingInbox/DurableIncomingInboxWorkerOptionsTests.cs`
- [x] T054 [US4] Add processing options creation coverage in `tests/Bondstone.Hosting.Tests/IncomingInbox/DurableIncomingInboxWorkerOptionsTests.cs`
- [x] T055 [US4] Add options validator failure coverage in `tests/Bondstone.Hosting.Tests/IncomingInbox/DurableIncomingInboxWorkerOptionsTests.cs`
- [x] T056 [US4] Add worker option forwarding coverage in `tests/Bondstone.Hosting.Tests/IncomingInbox/DurableIncomingInboxWorkerTests.cs`
- [x] T057 [US4] Add immediate next-batch processing coverage when rows are claimed in `tests/Bondstone.Hosting.Tests/IncomingInbox/DurableIncomingInboxWorkerTests.cs`
- [x] T058 [US4] Add dispatcher failure logging and retry coverage in `tests/Bondstone.Hosting.Tests/IncomingInbox/DurableIncomingInboxWorkerTests.cs`
- [x] T059 [US4] Add clean cancellation coverage in `tests/Bondstone.Hosting.Tests/IncomingInbox/DurableIncomingInboxWorkerTests.cs`
- [x] T060 [US4] Add invalid constructor option coverage in `tests/Bondstone.Hosting.Tests/IncomingInbox/DurableIncomingInboxWorkerTests.cs`
- [x] T061 [US4] Add missing dispatcher startup failure coverage in `tests/Bondstone.Hosting.Tests/IncomingInbox/DurableIncomingInboxWorkerTests.cs`

### Implementation for User Story 4

- [x] T062 [US4] Implement incoming inbox worker option validation for worker id, lease duration, batch size, polling interval, failure delay, max attempts, and retry delays
- [x] T063 [US4] Validate incoming inbox worker options during worker construction
- [x] T064 [US4] Verify `IDurableIncomingInboxDispatcher` registration during incoming inbox worker startup
- [x] T065 [US4] Resolve `IDurableIncomingInboxDispatcher` from a new async scope for each batch
- [x] T066 [US4] Call `ProcessAsync(...)` with configured worker id, lease duration, and batch size
- [x] T067 [US4] Immediately process another batch when claimed rows are returned
- [x] T068 [US4] Wait for polling interval when no rows are claimed
- [x] T069 [US4] Log processing batch failures with event id `2001` and consecutive failure count
- [x] T070 [US4] Wait for failure delay after unexpected dispatcher failure and continue
- [x] T071 [US4] Stop the loop when host cancellation is requested

**Checkpoint**: Hosted incoming inbox worker loops over durable incoming inbox processing batches using configured options.

---

## Phase N: Polish & Cross-Cutting Concerns

**Purpose**: Documentation and compatibility support for the migrated feature.

- [x] T072 [P] Document hosted worker package scope in `src/Bondstone.Hosting/README.md`
- [x] T073 [P] Link package docs to setup, package discovery, operations, observability, packaging policy, architecture, and tests
- [x] T074 [P] Add test routing notes in `tests/Bondstone.Hosting.Tests/README.md` and `tests/Bondstone.Hosting.Tests/AGENTS.md`
- [x] T075 [P] Document `Bondstone.Hosting` package ID and dependency direction in `docs/packaging.md`
- [x] T076 [P] Document hosted worker setup in `docs/setup.md` and `docs/package-discovery.md`
- [x] T077 [P] Document hosted worker operational failure behavior in `docs/operations.md`
- [x] T078 [P] Document hosted worker log event ids in `docs/observability.md`
- [x] T079 [P] Document public hosting setup API classification in `docs/public-api.md`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies.
- **Foundational (Phase 2)**: Depends on package and test project setup.
- **User Stories (Phase 3+)**: Depend on worker options and shared hosting infrastructure.
- **Polish**: Depends on setup and worker behavior being stable.

### User Story Dependencies

- **User Story 1 (P1)**: Can be implemented after foundational outbox worker options and service registration helpers.
- **User Story 2 (P2)**: Depends on outbox worker registration and provider-neutral durable outbox contracts from `Bondstone.Persistence`.
- **User Story 3 (P3)**: Can be implemented after foundational incoming inbox worker options and service registration helpers.
- **User Story 4 (P4)**: Depends on incoming inbox worker registration and provider-neutral durable incoming inbox contracts from `Bondstone.Persistence`.

## Gaps Identified

- Outbox setup lacks focused duplicate-hosted-worker registration coverage.
- `UseDurableDispatcher(...)` exists in source but has less direct coverage than `UseWorker(...)`.
- Outbox worker cancellation support exists in source but lacks the explicit clean cancellation/blocked dispatcher test that incoming inbox worker has.
- Option validation exists but focused tests do not cover every positive-duration field.
- Incoming inbox option validation lacks focused tests for `MaxAttempts <= 0` and `RetryDelays = null`.

## Verification Commands

The migrated feature is covered by existing tests. Use these commands after building:

```bash
pnpm backend:build
dotnet test tests/Bondstone.Hosting.Tests/Bondstone.Hosting.Tests.csproj --configuration Release --no-build --filter "Category=Unit"
```
