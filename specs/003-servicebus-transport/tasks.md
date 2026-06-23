---
description: "Migrated task list for existing Service Bus transport feature"
---

# Tasks: Service Bus Transport

**Input**: Migrated design documents from `specs/003-servicebus-transport/`

**Prerequisites**: Existing implementation in `src/Bondstone.Transport.ServiceBus`; existing tests in `tests/Bondstone.Transport.ServiceBus.Tests`

**Tests**: Existing xUnit tests use `Category=Unit` and `Category=Integration`.

**Organization**: Tasks are grouped by migrated user story. All implementation tasks are marked complete because the feature existed before SpecKit adoption.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Could have been implemented in parallel at the time because it touched different files or concerns.
- **[Story]**: Migrated user story identifier.
- All paths refer to existing source-controlled files.

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Establish the package and test project for Service Bus transport.

- [x] T001 Create packable package project `src/Bondstone.Transport.ServiceBus/Bondstone.Transport.ServiceBus.csproj`
- [x] T002 Add package references to `Bondstone`, `Bondstone.Persistence`, `Azure.Messaging.ServiceBus`, Microsoft hosting/logging/options abstractions, and DI abstractions
- [x] T003 Create test project `tests/Bondstone.Transport.ServiceBus.Tests/Bondstone.Transport.ServiceBus.Tests.csproj`
- [x] T004 Add test dependencies for xUnit, Microsoft test SDK, DI/logging, Azure Service Bus Testcontainers, and coverlet
- [x] T005 Add scoped package and test documentation in `src/Bondstone.Transport.ServiceBus/README.md`, `src/Bondstone.Transport.ServiceBus/AGENTS.md`, `tests/Bondstone.Transport.ServiceBus.Tests/README.md`, and `tests/Bondstone.Transport.ServiceBus.Tests/AGENTS.md`
- [x] T006 Expose internals to `Bondstone.Transport.ServiceBus.Tests` through `src/Bondstone.Transport.ServiceBus/Properties/AssemblyInfo.cs`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Build the shared dispatcher and receive worker infrastructure required by all user stories.

- [x] T007 [P] Implement public setup extensions in `src/Bondstone.Transport.ServiceBus/BondstoneServiceBusBuilderExtensions.cs`
- [x] T008 [P] Implement dispatcher options in `src/Bondstone.Transport.ServiceBus/ServiceBusEnvelopeDispatcherOptions.cs`
- [x] T009 Implement native-driver envelope dispatcher in `src/Bondstone.Transport.ServiceBus/ServiceBusEnvelopeDispatcher.cs`
- [x] T010 [P] Implement receive worker options and immutable registration in `src/Bondstone.Transport.ServiceBus/ServiceBusReceiveWorkerOptions.cs` and `src/Bondstone.Transport.ServiceBus/ServiceBusReceiveWorkerRegistration.cs`
- [x] T011 [P] Implement receive failure log event metadata in `src/Bondstone.Transport.ServiceBus/ServiceBusReceiveWorkerLogEvents.cs`
- [x] T012 Implement hosted receive worker in `src/Bondstone.Transport.ServiceBus/ServiceBusReceiveWorker.cs`

**Checkpoint**: Service Bus dispatcher and receive worker services can be configured and resolved.

---

## Phase 3: User Story 1 - Register Service Bus Dispatcher And Worker Setup (Priority: P1)

**Goal**: Hosts can opt into Service Bus dispatcher and receive worker composition without Bondstone owning Service Bus topology.

**Independent Test**: `dotnet test tests/Bondstone.Transport.ServiceBus.Tests/Bondstone.Transport.ServiceBus.Tests.csproj --configuration Release --filter "Category=Unit"`

### Tests for User Story 1

- [x] T013 [US1] Add dispatcher registration coverage in `tests/Bondstone.Transport.ServiceBus.Tests/ServiceBusBuilderTests.cs`
- [x] T014 [US1] Add missing entity validation coverage in `tests/Bondstone.Transport.ServiceBus.Tests/ServiceBusBuilderTests.cs`
- [x] T015 [US1] Add default manual completion option coverage in `tests/Bondstone.Transport.ServiceBus.Tests/ServiceBusBuilderTests.cs`
- [x] T016 [US1] Add `AutoCompleteMessages` rejection coverage in `tests/Bondstone.Transport.ServiceBus.Tests/ServiceBusBuilderTests.cs`
- [x] T017 [US1] Add `ReceiveAndDelete` rejection coverage in `tests/Bondstone.Transport.ServiceBus.Tests/ServiceBusBuilderTests.cs`
- [x] T018 [US1] Add processor option clone coverage in `tests/Bondstone.Transport.ServiceBus.Tests/ServiceBusBuilderTests.cs`
- [x] T019 [US1] Add receive worker hosted service registration coverage in `tests/Bondstone.Transport.ServiceBus.Tests/ServiceBusBuilderTests.cs`

### Implementation for User Story 1

- [x] T020 [US1] Expose `UseServiceBusDispatcher(...)` from `BondstoneBuilder` and `BondstoneOutboxBuilder`
- [x] T021 [US1] Register `ServiceBusEnvelopeDispatcher` as `IDurableEnvelopeDispatcher` and mark transport name `ServiceBus`
- [x] T022 [US1] Expose `UseServiceBusReceiveWorker(...)` from `BondstoneBuilder`
- [x] T023 [US1] Require exactly one queue or topic/subscription receive entity in `ServiceBusReceiveWorkerOptions`
- [x] T024 [US1] Support `ReceiveCommand()` and `ReceiveEvent(...)` durable incoming inbox bindings
- [x] T025 [US1] Clone and validate manual-completion processor options before storing registrations

**Checkpoint**: Service Bus setup APIs compose dispatcher and receive worker services.

---

## Phase 4: User Story 2 - Publish Durable Envelopes To Service Bus (Priority: P2)

**Goal**: Claimed durable outbox records can be serialized and published through a host-provided `ServiceBusClient`.

**Independent Test**: `dotnet test tests/Bondstone.Transport.ServiceBus.Tests/Bondstone.Transport.ServiceBus.Tests.csproj --configuration Release --filter "Category=Integration"`

### Tests for User Story 2

- [x] T026 [US2] Add Service Bus Testcontainers fixture in `tests/Bondstone.Transport.ServiceBus.Tests/ServiceBusFixture.cs`
- [x] T027 [US2] Add queue publish integration coverage in `tests/Bondstone.Transport.ServiceBus.Tests/ServiceBusIntegrationTests.cs`
- [x] T028 [US2] Add topic/subscription publish integration coverage in `tests/Bondstone.Transport.ServiceBus.Tests/ServiceBusIntegrationTests.cs`

### Implementation for User Story 2

- [x] T029 [US2] Validate dispatcher entity names with `ServiceBusEnvelopeDispatcherOptions.ResolveEntityName`
- [x] T030 [US2] Serialize durable envelopes in `ServiceBusEnvelopeDispatcher`
- [x] T031 [US2] Create native Service Bus messages with durable envelope id, subject, content type, correlation id, and Bondstone application properties
- [x] T032 [US2] Cache `ServiceBusSender` instances by resolved entity name
- [x] T033 [US2] Send native messages through `ServiceBusSender.SendMessageAsync(...)`
- [x] T034 [US2] Dispose cached senders from `ServiceBusEnvelopeDispatcher.DisposeAsync()`

**Checkpoint**: Service Bus receives serialized durable command and event envelopes through app-owned topology.

---

## Phase 5: User Story 3 - Run Manual Service Bus Receive Processors (Priority: P3)

**Goal**: Service Bus receive workers run native processors with manual message completion.

**Independent Test**: `dotnet test tests/Bondstone.Transport.ServiceBus.Tests/Bondstone.Transport.ServiceBus.Tests.csproj --configuration Release --filter "Category=Unit|Category=Integration"`

### Tests for User Story 3

- [x] T035 [US3] Add receive failure logging coverage in `tests/Bondstone.Transport.ServiceBus.Tests/ServiceBusReceiveWorkerTests.cs`
- [x] T036 [US3] Add command receive worker integration coverage in `tests/Bondstone.Transport.ServiceBus.Tests/ServiceBusIntegrationTests.cs`
- [x] T037 [US3] Add event receive worker integration coverage in `tests/Bondstone.Transport.ServiceBus.Tests/ServiceBusIntegrationTests.cs`

### Implementation for User Story 3

- [x] T038 [US3] Create one Service Bus processor per receive worker registration in `ServiceBusReceiveWorker`
- [x] T039 [US3] Support queue processors and topic/subscription processors
- [x] T040 [US3] Attach message and error callbacks to every processor
- [x] T041 [US3] Start processors during hosted worker execution
- [x] T042 [US3] Stop and dispose processors during hosted worker shutdown
- [x] T043 [US3] Log processor failures with event id `3001`

**Checkpoint**: Native Service Bus receive processors preserve manual settlement behavior.

---

## Phase 6: User Story 4 - Ingest Service Bus Deliveries Into Durable Incoming Inbox (Priority: P4)

**Goal**: Service Bus receive workers ingest native deliveries into Bondstone's durable incoming inbox ledger before native completion.

**Independent Test**: `dotnet test tests/Bondstone.Transport.ServiceBus.Tests/Bondstone.Transport.ServiceBus.Tests.csproj --configuration Release --filter "Category=Unit"`

### Tests for User Story 4

- [x] T044 [US4] Add unit coverage proving durable incoming inbox save completes before native completion in `tests/Bondstone.Transport.ServiceBus.Tests/ServiceBusReceiveWorkerTests.cs`
- [x] T045 [US4] Add unit coverage proving failed ingestion does not complete the native message in `tests/Bondstone.Transport.ServiceBus.Tests/ServiceBusReceiveWorkerTests.cs`
- [x] T046 [US4] Add unit coverage proving already-ingested records complete without handler execution in `tests/Bondstone.Transport.ServiceBus.Tests/ServiceBusReceiveWorkerTests.cs`
- [x] T047 [US4] Add unit coverage proving event inbox keys use subscriber module and subscriber identity in `tests/Bondstone.Transport.ServiceBus.Tests/ServiceBusReceiveWorkerTests.cs`
- [x] T048 [US4] Add unit coverage proving missing event bindings and missing subscribers fail with `BondstoneSetupCodes.MissingReceiveBinding` in `tests/Bondstone.Transport.ServiceBus.Tests/ServiceBusReceiveWorkerTests.cs`
- [x] T049 [US4] Add integration coverage proving command receive worker ingestion stores the expected source transport name in `tests/Bondstone.Transport.ServiceBus.Tests/ServiceBusIntegrationTests.cs`
- [x] T050 [US4] Add integration coverage proving event receive worker ingestion stores the expected source transport name in `tests/Bondstone.Transport.ServiceBus.Tests/ServiceBusIntegrationTests.cs`

### Implementation for User Story 4

- [x] T051 [US4] Deserialize Service Bus message bodies with `IDurableMessageEnvelopeSerializer`
- [x] T052 [US4] Create command incoming inbox records from command route handler identities
- [x] T053 [US4] Create event incoming inbox records from explicit subscriber bindings and subscriber registrations
- [x] T054 [US4] Resolve durable incoming inbox ingestion boundary by receiver module
- [x] T055 [US4] Save durable incoming inbox ingestion before completing the native Service Bus message
- [x] T056 [US4] Include source transport name in durable incoming inbox records

**Checkpoint**: Service Bus native deliveries become durable incoming inbox records before native completion.

---

## Phase N: Polish & Cross-Cutting Concerns

**Purpose**: Documentation and compatibility support for the migrated feature.

- [x] T057 [P] Document thin adapter positioning in `src/Bondstone.Transport.ServiceBus/README.md`
- [x] T058 [P] Link package docs to setup, package discovery, operations, observability, packaging policy, and architecture
- [x] T059 [P] Add test routing notes in `tests/Bondstone.Transport.ServiceBus.Tests/README.md` and `tests/Bondstone.Transport.ServiceBus.Tests/AGENTS.md`
- [x] T060 [P] Document Service Bus package ID and dependency direction in `docs/packaging.md`
- [x] T061 [P] Document Service Bus setup and receive ingestion behavior in `docs/setup.md` and `docs/package-discovery.md`
- [x] T062 [P] Document Service Bus operational ownership and settlement behavior in `docs/operations.md`
- [x] T063 [P] Document Service Bus receive failure log event id in `docs/observability.md`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies.
- **Foundational (Phase 2)**: Depends on package and test project setup.
- **User Stories (Phase 3+)**: Depend on dispatcher and receive worker infrastructure.
- **Polish**: Depends on package behavior and ownership boundaries being stable.

### User Story Dependencies

- **User Story 1 (P1)**: Can be implemented after foundational package setup.
- **User Story 2 (P2)**: Depends on dispatcher options and dispatcher registration.
- **User Story 3 (P3)**: Depends on receive worker registration and hosted worker infrastructure.
- **User Story 4 (P4)**: Depends on receive worker infrastructure plus durable incoming inbox contracts from `Bondstone.Persistence`.

## Gaps Identified

- Dispatcher option validation exists in `ServiceBusEnvelopeDispatcherOptions` but lacks focused unit tests for missing `ResolveEntityName`, empty entity names, default/custom `ContentType`, `CorrelationId`, application properties, sender caching, and sender disposal.
- Receive worker entity validation has coverage for missing entities but lacks focused coverage for the "both queue and topic/subscription configured" case.
- Receive worker lifecycle behavior for multiple registrations, queue versus topic processor creation, processor start/stop and disposal, and full processor option cloning exists in source but has limited direct focused coverage.
- Unsupported durable message kind rejection exists in `ServiceBusReceiveWorker.CreateIncomingInboxRecord(...)` but lacks a focused unit test.

## Verification Commands

The migrated feature is covered by existing tests. Use these commands after building:

```bash
pnpm backend:build
dotnet test tests/Bondstone.Transport.ServiceBus.Tests/Bondstone.Transport.ServiceBus.Tests.csproj --configuration Release --no-build --filter "Category=Unit"
dotnet test tests/Bondstone.Transport.ServiceBus.Tests/Bondstone.Transport.ServiceBus.Tests.csproj --configuration Release --no-build --filter "Category=Integration"
```
