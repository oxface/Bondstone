---
description: "Migrated task list for existing local transport feature"
---

# Tasks: Local Transport

**Input**: Migrated design documents from `specs/001-local-transport/`

**Prerequisites**: Existing implementation in `src/Bondstone.Transport.Local`; existing tests in `tests/Bondstone.Transport.Local.Tests`

**Tests**: Existing xUnit tests use `Category=Unit` and `Category=Integration`.

**Organization**: Tasks are grouped by migrated user story. All implementation tasks are marked complete because the feature existed before SpecKit adoption.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Could have been implemented in parallel at the time because it touched different files or concerns
- **[Story]**: Migrated user story identifier
- All paths refer to existing source-controlled files

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Establish the package and test project for local transport.

- [x] T001 Create packable package project `src/Bondstone.Transport.Local/Bondstone.Transport.Local.csproj`
- [x] T002 Add package dependency references to `Bondstone`, `Bondstone.Persistence`, and `Microsoft.Extensions.DependencyInjection.Abstractions`
- [x] T003 Create test project `tests/Bondstone.Transport.Local.Tests/Bondstone.Transport.Local.Tests.csproj`
- [x] T004 Add test dependencies for xUnit, Microsoft test SDK, DI, EF relational APIs, PostgreSQL test containers, and coverlet
- [x] T005 Add scoped package and test documentation in `src/Bondstone.Transport.Local/README.md` and `tests/Bondstone.Transport.Local.Tests/README.md`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Build local topology and dispatch infrastructure required by all user stories.

- [x] T006 [P] Implement local setup extensions in `src/Bondstone.Transport.Local/Outbox/BondstoneLocalBuilderExtensions.cs`
- [x] T007 [P] Register local envelope dispatcher services in `src/Bondstone.Transport.Local/Outbox/BondstoneLocalServiceCollectionExtensions.cs`
- [x] T008 [P] Implement required and optional string normalization helpers in `src/Bondstone.Transport.Local/Utility/StringExtensions.cs`
- [x] T009 Implement local topology snapshot in `src/Bondstone.Transport.Local/Outbox/Topology/LocalTransportTopology.cs`
- [x] T010 Implement queue registration and binding records in `src/Bondstone.Transport.Local/Outbox/Topology/LocalQueueRegistration.cs`, `LocalCommandQueueBinding.cs`, and `LocalEventSubscription.cs`
- [x] T011 Implement user-facing topology builders in `src/Bondstone.Transport.Local/Outbox/Topology/BondstoneLocalTransportBuilder.cs`, `BondstoneLocalQueueBuilder.cs`, `BondstoneLocalModuleRouteBuilder.cs`, and `BondstoneLocalEventRouteBuilder.cs`
- [x] T012 Implement the local durable envelope dispatch route in `src/Bondstone.Transport.Local/Outbox/LocalDurableEnvelopeDispatchRoute.cs`

**Checkpoint**: Local transport can be configured and can decide whether command or event outbox records are sendable.

---

## Phase 3: User Story 1 - Route Durable Commands Through Local Queues (Priority: P1)

**Goal**: Durable command envelopes can be routed to target module receive pipelines through explicit or convention-based local queues.

**Independent Test**: `dotnet test tests/Bondstone.Transport.Local.Tests/Bondstone.Transport.Local.Tests.csproj --configuration Release --filter "Category=Unit|Category=Integration"`

### Tests for User Story 1

- [x] T013 [US1] Add unit coverage for explicit command route dispatch in `tests/Bondstone.Transport.Local.Tests/LocalDurableEnvelopeDispatcherTests.cs`
- [x] T014 [US1] Add unit coverage for `UseModuleQueueConvention()` command dispatch in `tests/Bondstone.Transport.Local.Tests/LocalDurableEnvelopeDispatcherTests.cs`
- [x] T015 [US1] Add startup validation coverage for module queue convention setup in `tests/Bondstone.Transport.Local.Tests/LocalDurableEnvelopeDispatcherTests.cs`
- [x] T016 [US1] Add PostgreSQL-backed integration coverage for local command dispatch, durable inbox persistence, and duplicate delivery idempotency in `tests/Bondstone.Transport.Local.Tests/LocalTransportInboxPersistenceTests.cs`

### Implementation for User Story 1

- [x] T017 [US1] Expose `RouteModule(...).ToQueue(...)` and `Queue(...).AcceptModule(...)` local command binding APIs
- [x] T018 [US1] Expose `UseModuleQueueConvention()` and custom module queue convention APIs
- [x] T019 [US1] Resolve command queue bindings by target module in `LocalTransportTopology`
- [x] T020 [US1] Dispatch local command envelopes through `IDurableEnvelopeReceiver.ReceiveCommandAsync(...)`

**Checkpoint**: Command envelopes can be dispatched locally and duplicate durable inbox handling is preserved.

---

## Phase 4: User Story 2 - Fan Out Integration Events To Local Subscribers (Priority: P2)

**Goal**: Integration event envelopes can be routed to all configured local subscribers with explicit subscriber identities.

**Independent Test**: `dotnet test tests/Bondstone.Transport.Local.Tests/Bondstone.Transport.Local.Tests.csproj --configuration Release --filter "Category=Unit|Category=Integration"`

### Tests for User Story 2

- [x] T021 [US2] Add unit coverage for event fan-out to multiple subscribers in `tests/Bondstone.Transport.Local.Tests/LocalDurableEnvelopeDispatcherTests.cs`
- [x] T022 [US2] Add PostgreSQL-backed integration coverage for subscriber inbox persistence and duplicate delivery idempotency in `tests/Bondstone.Transport.Local.Tests/LocalTransportInboxPersistenceTests.cs`

### Implementation for User Story 2

- [x] T023 [US2] Expose `RouteEvent(...).ToQueue(...)` local event route API
- [x] T024 [US2] Expose `Queue(...).SubscribeEvent(...)` subscriber binding API
- [x] T025 [US2] Resolve event subscriptions by message type in `LocalTransportTopology`
- [x] T026 [US2] Dispatch event envelopes through `IDurableEnvelopeReceiver.ReceiveEventAsync(...)` once per subscriber binding

**Checkpoint**: Event envelopes fan out locally and subscriber durable inbox idempotency is preserved.

---

## Phase 5: User Story 3 - Report Missing Local Receive Bindings (Priority: P3)

**Goal**: Missing local receive setup fails with actionable Bondstone setup diagnostics.

**Independent Test**: `dotnet test tests/Bondstone.Transport.Local.Tests/Bondstone.Transport.Local.Tests.csproj --configuration Release --filter "Category=Unit"`

### Tests for User Story 3

- [x] T027 [US3] Add unit coverage for missing dispatcher route diagnostics in `tests/Bondstone.Transport.Local.Tests/LocalDurableEnvelopeDispatcherTests.cs`
- [x] T028 [US3] Add unit coverage for missing command receive binding diagnostics in `tests/Bondstone.Transport.Local.Tests/LocalDurableEnvelopeDispatcherTests.cs`
- [x] T029 [US3] Add unit coverage for missing event subscriber binding diagnostics in `tests/Bondstone.Transport.Local.Tests/LocalDurableEnvelopeDispatcherTests.cs`

### Implementation for User Story 3

- [x] T030 [US3] Return false from `CanSend(...)` when command queue or event subscriber bindings are absent
- [x] T031 [US3] Throw `BondstoneSetupCodes.MissingReceiveBinding` for missing local command and event bindings
- [x] T032 [US3] Preserve dispatcher-level missing-route diagnostics when no route can send the outbox record

**Checkpoint**: Local transport setup failures are observable and actionable.

---

## Phase 6: User Story 4 - Preserve Handler Failure Semantics (Priority: P4)

**Goal**: Handler exceptions raised through local receive pipelines remain visible to callers.

**Independent Test**: `dotnet test tests/Bondstone.Transport.Local.Tests/Bondstone.Transport.Local.Tests.csproj --configuration Release --filter "Category=Integration"`

### Tests for User Story 4

- [x] T033 [US4] Add integration coverage proving local command handler exceptions propagate in `tests/Bondstone.Transport.Local.Tests/LocalTransportInboxPersistenceTests.cs`

### Implementation for User Story 4

- [x] T034 [US4] Dispatch through the normal command receive pipeline without swallowing handler exceptions
- [x] T035 [US4] Preserve failed receive behavior so a throwing handler does not create a target inbox row

**Checkpoint**: Local transport exposes handler failures consistently with durable dispatch semantics.

---

## Phase N: Polish & Cross-Cutting Concerns

**Purpose**: Documentation and compatibility support for the migrated feature.

- [x] T036 [P] Document local/dev/test positioning in `src/Bondstone.Transport.Local/README.md`
- [x] T037 [P] Link package docs to setup, package discovery, operations, observability, packaging policy, architecture, and tests
- [x] T038 [P] Add test routing notes in `tests/Bondstone.Transport.Local.Tests/README.md` and `tests/Bondstone.Transport.Local.Tests/AGENTS.md`
- [x] T039 [P] Expose test-only internals to `Bondstone.Transport.Local.Tests` through `src/Bondstone.Transport.Local/Properties/AssemblyInfo.cs`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies.
- **Foundational (Phase 2)**: Depends on package and test project setup.
- **User Stories (Phase 3+)**: Depend on topology and dispatch infrastructure.
- **Polish**: Depends on the package shape and behavior being stable.

### User Story Dependencies

- **User Story 1 (P1)**: Can be implemented after foundational topology and dispatch services.
- **User Story 2 (P2)**: Can be implemented after foundational topology and dispatch services; independent from command routing except for shared dispatch route infrastructure.
- **User Story 3 (P3)**: Depends on command and event route resolution behavior.
- **User Story 4 (P4)**: Depends on command receive pipeline dispatch.

## Gaps Identified

- Event queue convention behavior exists in `BondstoneLocalTransportBuilder.UseEventQueueConvention(...)` but does not have direct focused tests.
- Duplicate or conflicting route registration errors exist in source but do not have focused tests.
- Required value normalization is used across route and binding APIs but does not have focused edge-case tests for every public entrypoint.
- Unsupported message kind rejection exists in `LocalDurableEnvelopeDispatchRoute` but does not have a focused unit test.

## Verification Commands

The migrated feature is covered by existing tests. Use these commands after building:

```bash
pnpm backend:build
dotnet test tests/Bondstone.Transport.Local.Tests/Bondstone.Transport.Local.Tests.csproj --configuration Release --no-build --filter "Category=Unit|Category=Integration"
```
