---
description: "Migrated task list for existing RabbitMQ transport feature"
---

# Tasks: RabbitMQ Transport

**Input**: Migrated design documents from `specs/002-rabbitmq-transport/`

**Prerequisites**: Existing implementation in `src/Bondstone.Transport.RabbitMq`; existing tests in `tests/Bondstone.Transport.RabbitMq.Tests`

**Tests**: Existing xUnit tests use `Category=Unit` and `Category=Integration`.

**Organization**: Tasks are grouped by migrated user story. All implementation tasks are marked complete because the feature existed before SpecKit adoption.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Could have been implemented in parallel at the time because it touched different files or concerns.
- **[Story]**: Migrated user story identifier.
- All paths refer to existing source-controlled files.

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Establish the package and test project for RabbitMQ transport.

- [x] T001 Create packable package project `src/Bondstone.Transport.RabbitMq/Bondstone.Transport.RabbitMq.csproj`
- [x] T002 Add package references to `Bondstone`, `Bondstone.Persistence`, `RabbitMQ.Client`, Microsoft hosting/logging/options abstractions, and DI abstractions
- [x] T003 Create test project `tests/Bondstone.Transport.RabbitMq.Tests/Bondstone.Transport.RabbitMq.Tests.csproj`
- [x] T004 Add test dependencies for xUnit, Microsoft test SDK, DI/logging, RabbitMQ Testcontainers, and coverlet
- [x] T005 Add scoped package and test documentation in `src/Bondstone.Transport.RabbitMq/README.md`, `src/Bondstone.Transport.RabbitMq/AGENTS.md`, `tests/Bondstone.Transport.RabbitMq.Tests/README.md`, and `tests/Bondstone.Transport.RabbitMq.Tests/AGENTS.md`
- [x] T006 Expose internals to `Bondstone.Transport.RabbitMq.Tests` through `src/Bondstone.Transport.RabbitMq/Properties/AssemblyInfo.cs`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Build the shared dispatcher and receive worker infrastructure required by all user stories.

- [x] T007 [P] Implement public setup extensions in `src/Bondstone.Transport.RabbitMq/BondstoneRabbitMqBuilderExtensions.cs`
- [x] T008 [P] Implement dispatcher destination record in `src/Bondstone.Transport.RabbitMq/RabbitMqEnvelopeDestination.cs`
- [x] T009 [P] Implement dispatcher options in `src/Bondstone.Transport.RabbitMq/RabbitMqEnvelopeDispatcherOptions.cs`
- [x] T010 Implement native-driver envelope dispatcher in `src/Bondstone.Transport.RabbitMq/RabbitMqEnvelopeDispatcher.cs`
- [x] T011 [P] Implement receive worker options and immutable registration in `src/Bondstone.Transport.RabbitMq/RabbitMqReceiveWorkerOptions.cs` and `src/Bondstone.Transport.RabbitMq/RabbitMqReceiveWorkerRegistration.cs`
- [x] T012 [P] Implement receive failure log event metadata in `src/Bondstone.Transport.RabbitMq/RabbitMqReceiveWorkerLogEvents.cs`
- [x] T013 Implement hosted receive worker in `src/Bondstone.Transport.RabbitMq/RabbitMqReceiveWorker.cs`

**Checkpoint**: RabbitMQ dispatcher and receive worker services can be configured and resolved.

---

## Phase 3: User Story 1 - Register RabbitMQ Dispatcher And Worker Setup (Priority: P1)

**Goal**: Hosts can opt into RabbitMQ dispatcher and receive worker composition without Bondstone owning RabbitMQ topology.

**Independent Test**: `dotnet test tests/Bondstone.Transport.RabbitMq.Tests/Bondstone.Transport.RabbitMq.Tests.csproj --configuration Release --filter "Category=Unit"`

### Tests for User Story 1

- [x] T014 [US1] Add dispatcher registration coverage in `tests/Bondstone.Transport.RabbitMq.Tests/RabbitMqBuilderTests.cs`
- [x] T015 [US1] Add missing queue validation coverage in `tests/Bondstone.Transport.RabbitMq.Tests/RabbitMqBuilderTests.cs`
- [x] T016 [US1] Add receive worker hosted service registration coverage in `tests/Bondstone.Transport.RabbitMq.Tests/RabbitMqBuilderTests.cs`
- [x] T017 [US1] Add durable incoming inbox command ingestion registration coverage in `tests/Bondstone.Transport.RabbitMq.Tests/RabbitMqBuilderTests.cs`
- [x] T018 [US1] Add durable incoming inbox event subscriber binding registration coverage in `tests/Bondstone.Transport.RabbitMq.Tests/RabbitMqBuilderTests.cs`
- [x] T019 [US1] Add default no-requeue behavior coverage in `tests/Bondstone.Transport.RabbitMq.Tests/RabbitMqBuilderTests.cs`

### Implementation for User Story 1

- [x] T020 [US1] Expose `UseRabbitMqDispatcher(...)` from `BondstoneBuilder` and `BondstoneOutboxBuilder`
- [x] T021 [US1] Register `RabbitMqEnvelopeDispatcher` as `IDurableEnvelopeDispatcher` and mark transport name `RabbitMq`
- [x] T022 [US1] Expose `UseRabbitMqReceiveWorker(...)` from `BondstoneBuilder`
- [x] T023 [US1] Require `QueueName` and build receive worker registrations from `RabbitMqReceiveWorkerOptions`
- [x] T024 [US1] Support `ReceiveCommand()`, `ReceiveEvent(...)`, `IngestCommandToDurableIncomingInbox()`, and `IngestEventToDurableIncomingInbox(...)`

**Checkpoint**: RabbitMQ setup APIs compose dispatcher and receive worker services.

---

## Phase 4: User Story 2 - Publish Durable Envelopes To RabbitMQ (Priority: P2)

**Goal**: Claimed durable outbox records can be serialized and published through a host-provided RabbitMQ channel.

**Independent Test**: `dotnet test tests/Bondstone.Transport.RabbitMq.Tests/Bondstone.Transport.RabbitMq.Tests.csproj --configuration Release --filter "Category=Integration"`

### Tests for User Story 2

- [x] T025 [US2] Add RabbitMQ Testcontainers fixture in `tests/Bondstone.Transport.RabbitMq.Tests/RabbitMqFixture.cs`
- [x] T026 [US2] Add queue publish integration coverage in `tests/Bondstone.Transport.RabbitMq.Tests/RabbitMqIntegrationTests.cs`
- [x] T027 [US2] Add topic exchange/routing-key publish integration coverage in `tests/Bondstone.Transport.RabbitMq.Tests/RabbitMqIntegrationTests.cs`

### Implementation for User Story 2

- [x] T028 [US2] Validate dispatcher destinations with exchange and routing key in `RabbitMqEnvelopeDispatcherOptions`
- [x] T029 [US2] Serialize durable envelopes in `RabbitMqEnvelopeDispatcher`
- [x] T030 [US2] Publish to `IChannel.BasicPublishAsync(...)` using resolved exchange, routing key, mandatory flag, body, and cancellation token

**Checkpoint**: RabbitMQ receives serialized durable command and event envelopes through app-owned topology.

---

## Phase 5: User Story 3 - Receive Native RabbitMQ Deliveries Through Bondstone Receiver Boundary (Priority: P3)

**Goal**: RabbitMQ receive workers consume native deliveries with manual acknowledgement and direct them into Bondstone receive boundaries.

**Independent Test**: `dotnet test tests/Bondstone.Transport.RabbitMq.Tests/Bondstone.Transport.RabbitMq.Tests.csproj --configuration Release --filter "Category=Unit|Category=Integration"`

### Tests for User Story 3

- [x] T031 [US3] Add integration coverage for command delivery direct receive in `tests/Bondstone.Transport.RabbitMq.Tests/RabbitMqIntegrationTests.cs`
- [x] T032 [US3] Add integration coverage for event delivery direct receive with binding in `tests/Bondstone.Transport.RabbitMq.Tests/RabbitMqIntegrationTests.cs`
- [x] T033 [US3] Add unit coverage proving acknowledgement waits for receive completion in `tests/Bondstone.Transport.RabbitMq.Tests/RabbitMqReceiveWorkerTests.cs`
- [x] T034 [US3] Add unit coverage proving receive failure logs and nacks using the requeue option in `tests/Bondstone.Transport.RabbitMq.Tests/RabbitMqReceiveWorkerTests.cs`

### Implementation for User Story 3

- [x] T035 [US3] Start one manual-ack consumer per receive worker registration in `RabbitMqReceiveWorker`
- [x] T036 [US3] Delegate direct receive deliveries to `IDurableEnvelopeReceiver.ReceiveAsync(...)`
- [x] T037 [US3] Acknowledge successful deliveries with `BasicAckAsync(...)`
- [x] T038 [US3] Log receive failures with event id `2001` and nack failed deliveries with `BasicNackAsync(...)`
- [x] T039 [US3] Cancel active consumers during worker shutdown

**Checkpoint**: Direct receive handoff honors manual native settlement behavior.

---

## Phase 6: User Story 4 - Ingest RabbitMQ Deliveries Into Durable Incoming Inbox (Priority: P4)

**Goal**: RabbitMQ receive workers ingest native deliveries into Bondstone's durable incoming inbox ledger before acknowledgement.

**Independent Test**: `dotnet test tests/Bondstone.Transport.RabbitMq.Tests/Bondstone.Transport.RabbitMq.Tests.csproj --configuration Release --filter "Category=Unit"`

### Tests for User Story 4

- [x] T040 [US4] Add unit coverage proving durable incoming inbox save completes before ack in `tests/Bondstone.Transport.RabbitMq.Tests/RabbitMqReceiveWorkerTests.cs`
- [x] T041 [US4] Add unit coverage proving already-ingested records ack without handler execution in `tests/Bondstone.Transport.RabbitMq.Tests/RabbitMqReceiveWorkerTests.cs`
- [x] T042 [US4] Add unit coverage proving command ingestion uses the receiver module boundary in `tests/Bondstone.Transport.RabbitMq.Tests/RabbitMqReceiveWorkerTests.cs`
- [x] T043 [US4] Add unit coverage proving command inbox keys use target module and handler identity in `tests/Bondstone.Transport.RabbitMq.Tests/RabbitMqReceiveWorkerTests.cs`
- [x] T044 [US4] Add unit coverage proving event inbox keys use subscriber module and subscriber identity in `tests/Bondstone.Transport.RabbitMq.Tests/RabbitMqReceiveWorkerTests.cs`
- [x] T045 [US4] Add unit coverage proving missing event bindings and missing subscribers nack with `BondstoneSetupCodes.MissingReceiveBinding` in `tests/Bondstone.Transport.RabbitMq.Tests/RabbitMqReceiveWorkerTests.cs`
- [x] T046 [US4] Add unit coverage proving failed ingestion nacks using existing failure behavior in `tests/Bondstone.Transport.RabbitMq.Tests/RabbitMqReceiveWorkerTests.cs`
- [x] T047 [US4] Add unit coverage proving durable incoming inbox records preserve envelope fields in `tests/Bondstone.Transport.RabbitMq.Tests/RabbitMqReceiveWorkerTests.cs`

### Implementation for User Story 4

- [x] T048 [US4] Deserialize RabbitMQ delivery bodies with `IDurableMessageEnvelopeSerializer`
- [x] T049 [US4] Create command incoming inbox records from command route handler identities
- [x] T050 [US4] Create event incoming inbox records from explicit subscriber bindings and subscriber registrations
- [x] T051 [US4] Resolve durable incoming inbox ingestion boundary by receiver module
- [x] T052 [US4] Save durable incoming inbox ingestion before acknowledging RabbitMQ delivery
- [x] T053 [US4] Include source transport name in durable incoming inbox records

**Checkpoint**: RabbitMQ native deliveries become durable incoming inbox records before native acknowledgement.

---

## Phase N: Polish & Cross-Cutting Concerns

**Purpose**: Documentation and compatibility support for the migrated feature.

- [x] T054 [P] Document thin adapter positioning in `src/Bondstone.Transport.RabbitMq/README.md`
- [x] T055 [P] Link package docs to setup, package discovery, operations, observability, packaging policy, and architecture
- [x] T056 [P] Add test routing notes in `tests/Bondstone.Transport.RabbitMq.Tests/README.md` and `tests/Bondstone.Transport.RabbitMq.Tests/AGENTS.md`
- [x] T057 [P] Document RabbitMQ package ID and dependency direction in `docs/packaging.md`
- [x] T058 [P] Document RabbitMQ setup and receive ingestion behavior in `docs/setup.md` and `docs/package-discovery.md`
- [x] T059 [P] Document RabbitMQ operational ownership and settlement behavior in `docs/operations.md`
- [x] T060 [P] Document RabbitMQ receive failure log event id in `docs/observability.md`

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

- Dispatcher option validation exists in `RabbitMqEnvelopeDispatcherOptions` but lacks focused unit tests for missing `ResolveDestination`, null exchange, empty routing key, and `Mandatory` forwarding.
- Receive worker lifecycle behavior for multiple registrations, configured consumer tags, and cancel-on-stop exists in source but has limited direct focused coverage.
- Unsupported durable message kind rejection exists in `RabbitMqReceiveWorker.CreateIncomingInboxRecord(...)` but lacks a focused unit test.

## Verification Commands

The migrated feature is covered by existing tests. Use these commands after building:

```bash
pnpm backend:build
dotnet test tests/Bondstone.Transport.RabbitMq.Tests/Bondstone.Transport.RabbitMq.Tests.csproj --configuration Release --no-build --filter "Category=Unit"
dotnet test tests/Bondstone.Transport.RabbitMq.Tests/Bondstone.Transport.RabbitMq.Tests.csproj --configuration Release --no-build --filter "Category=Integration"
```
