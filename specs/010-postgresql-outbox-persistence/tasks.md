---
description: "Migrated task list for existing PostgreSQL outbox persistence feature"
---

# Tasks: PostgreSQL Outbox Persistence

**Input**: Migrated design documents from `specs/010-postgresql-outbox-persistence/`

**Prerequisites**: Existing PostgreSQL provider implementation in `src/Bondstone.Persistence.EntityFrameworkCore.Postgres`; existing PostgreSQL unit and integration tests in `tests/Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests`

**Tests**: Existing xUnit tests use `Category=Unit` for validation/setup behavior and `Category=Integration` for provider behavior.

**Organization**: Tasks are grouped by migrated user story. All implementation tasks are marked complete because the feature existed before SpecKit adoption.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Could have been implemented in parallel at the time because it touched different files or concerns.
- **[Story]**: Migrated user story identifier.
- All paths refer to existing source-controlled files.

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Establish PostgreSQL provider package support for durable outbox mutation.

- [x] T001 Create PostgreSQL provider package project `src/Bondstone.Persistence.EntityFrameworkCore.Postgres/Bondstone.Persistence.EntityFrameworkCore.Postgres.csproj`
- [x] T002 Add package documentation in `src/Bondstone.Persistence.EntityFrameworkCore.Postgres/README.md`
- [x] T003 Add scoped package guidance in `src/Bondstone.Persistence.EntityFrameworkCore.Postgres/AGENTS.md`
- [x] T004 Add PostgreSQL provider test project `tests/Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests/Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests.csproj`
- [x] T005 Add PostgreSQL provider test guidance in `tests/Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests/README.md` and `tests/Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests/AGENTS.md`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Register PostgreSQL provider setup and table identifier support used by outbox mutation.

- [x] T006 [P] Implement PostgreSQL outbox table identifier helper in `src/Bondstone.Persistence.EntityFrameworkCore.Postgres/Outbox/PostgreSqlOutboxTableIdentifier.cs`
- [x] T007 [P] Implement PostgreSQL service registration surface in `src/Bondstone.Persistence.EntityFrameworkCore.Postgres/Persistence/BondstonePostgreSqlServiceCollectionExtensions.cs`
- [x] T008 [P] Implement PostgreSQL builder setup surface in `src/Bondstone.Persistence.EntityFrameworkCore.Postgres/Persistence/BondstonePostgreSqlBuilderExtensions.cs`
- [x] T009 [P] Add PostgreSQL setup tests in `tests/Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests/Persistence/BondstonePostgreSqlServiceCollectionExtensionsTests.cs`
- [x] T010 [P] Add PostgreSQL persistence registration tests in `tests/Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests/Persistence/PostgreSqlPersistenceRegistrationTests.cs`

**Checkpoint**: PostgreSQL package setup can register provider-owned outbox mutation primitives and module dispatcher registration hooks.

---

## Phase 3: User Story 1 - Claim Due Outbox Rows With PostgreSQL (Priority: P1)

**Goal**: PostgreSQL atomically claims due outbox rows for dispatch.

**Independent Test**: `dotnet test tests/Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests/Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests.csproj --configuration Release --filter "Category=Integration"`

### Tests for User Story 1

- [x] T011 [US1] Add claim validation coverage in `tests/Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests/Outbox/PostgreSqlDurableOutboxClaimerTests.cs`
- [x] T012 [US1] Add raw skip-locked query coverage in `tests/Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests/Persistence/PostgreSqlOutboxClaimTests.cs`
- [x] T013 [US1] Add pending-row deterministic claim coverage in `tests/Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests/Persistence/PostgreSqlOutboxClaimTests.cs`
- [x] T014 [US1] Add due scheduled row claim coverage in `tests/Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests/Persistence/PostgreSqlOutboxClaimTests.cs`
- [x] T015 [US1] Add locked row skip coverage in `tests/Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests/Persistence/PostgreSqlOutboxClaimTests.cs`
- [x] T016 [US1] Add stale processing reclaim coverage in `tests/Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests/Persistence/PostgreSqlOutboxClaimTests.cs`
- [x] T017 [US1] Add active processing lease exclusion coverage in `tests/Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests/Persistence/PostgreSqlOutboxClaimTests.cs`
- [x] T018 [US1] Add source-module claim scoping coverage in `tests/Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests/Persistence/PostgreSqlOutboxDispatcherTests.cs`

### Implementation for User Story 1

- [x] T019 [US1] Implement `PostgreSqlDurableOutboxClaimer<TDbContext>`
- [x] T020 [US1] Normalize and validate claim owner
- [x] T021 [US1] Validate positive lease duration and max count
- [x] T022 [US1] Select pending due rows and stale processing rows as claim candidates
- [x] T023 [US1] Restrict candidates by source module when a module-specific claimer is configured
- [x] T024 [US1] Use PostgreSQL `FOR UPDATE SKIP LOCKED` and `LIMIT @maxCount`
- [x] T025 [US1] Update claimed rows to `Processing`, increment attempt count, and set claim fields
- [x] T026 [US1] Return claimed rows as `DurableOutboxRecord` values

**Checkpoint**: PostgreSQL can safely claim due durable outbox rows for workers.

---

## Phase 4: User Story 2 - Renew Active Outbox Dispatch Leases (Priority: P2)

**Goal**: PostgreSQL extends only active matching processing claims.

**Independent Test**: PostgreSQL integration tests after build.

### Tests for User Story 2

- [x] T027 [US2] Add lease renewal validation coverage in `tests/Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests/Outbox/PostgreSqlDurableOutboxLeaseRenewerTests.cs`
- [x] T028 [US2] Add active lease renewal coverage in `tests/Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests/Persistence/PostgreSqlOutboxLeaseTests.cs`
- [x] T029 [US2] Add wrong-owner renewal rejection coverage in `tests/Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests/Persistence/PostgreSqlOutboxLeaseTests.cs`
- [x] T030 [US2] Add expired-lease renewal rejection coverage in `tests/Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests/Persistence/PostgreSqlOutboxLeaseTests.cs`
- [x] T031 [US2] Add non-processing renewal rejection coverage in `tests/Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests/Persistence/PostgreSqlOutboxLeaseTests.cs`

### Implementation for User Story 2

- [x] T032 [US2] Implement `PostgreSqlDurableOutboxLeaseRenewer<TDbContext>`
- [x] T033 [US2] Validate claim owner and positive lease duration
- [x] T034 [US2] Update `ClaimedUntilUtc` only for matching message id, processing status, matching owner, and active lease
- [x] T035 [US2] Return `true` only when exactly one row is renewed

**Checkpoint**: PostgreSQL exposes the provider primitive needed to extend active outbox dispatch claims.

---

## Phase 5: User Story 3 - Record Dispatched, Retry, And Terminal Outcomes With PostgreSQL (Priority: P3)

**Goal**: PostgreSQL records outbox dispatch outcomes only for active matching claims.

**Independent Test**: PostgreSQL integration tests after build.

### Tests for User Story 3

- [x] T036 [US3] Add dispatch recorder validation coverage in `tests/Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests/Outbox/PostgreSqlDurableOutboxDispatchRecorderTests.cs`
- [x] T037 [US3] Add dispatched outcome coverage in `tests/Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests/Persistence/PostgreSqlOutboxDispatchTests.cs`
- [x] T038 [US3] Add retry scheduled outcome coverage in `tests/Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests/Persistence/PostgreSqlOutboxDispatchTests.cs`
- [x] T039 [US3] Add terminal failed outcome coverage in `tests/Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests/Persistence/PostgreSqlOutboxDispatchTests.cs`
- [x] T040 [US3] Add wrong-owner outcome rejection coverage in `tests/Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests/Persistence/PostgreSqlOutboxDispatchTests.cs`
- [x] T041 [US3] Add expired-lease outcome rejection coverage in `tests/Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests/Persistence/PostgreSqlOutboxDispatchTests.cs`

### Implementation for User Story 3

- [x] T042 [US3] Implement `PostgreSqlDurableOutboxDispatchRecorder<TDbContext>`
- [x] T043 [US3] Validate claim owner, UTC timestamps, failure reason, and retry timestamp ordering
- [x] T044 [US3] Implement `MarkDispatchedAsync(...)` guarded by message id, processing status, claim owner, and active lease
- [x] T045 [US3] Clear retry/failure/claim fields when marking dispatched
- [x] T046 [US3] Implement `ScheduleRetryAsync(...)` guarded by message id, processing status, claim owner, and active lease
- [x] T047 [US3] Record failed timestamp, failure reason, next attempt timestamp, and clear claim fields when scheduling retry
- [x] T048 [US3] Implement `MarkTerminalFailedAsync(...)` guarded by message id, processing status, claim owner, and active lease
- [x] T049 [US3] Record failed timestamp and failure reason and clear retry/claim fields when marking terminal failed
- [x] T050 [US3] Return `false` when guarded outcome updates affect no rows

**Checkpoint**: PostgreSQL records durable dispatch outcomes without allowing stale workers to overwrite newer state.

---

## Phase 6: User Story 4 - Dispatch Outbox Rows Through PostgreSQL Module Dispatchers (Priority: P4)

**Goal**: PostgreSQL setup wires provider-specific mutation into module-scoped outbox dispatchers.

**Independent Test**: PostgreSQL integration and setup tests after build.

### Tests for User Story 4

- [x] T051 [US4] Add provider service resolution coverage in `tests/Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests/Persistence/PostgreSqlPersistenceRegistrationTests.cs`
- [x] T052 [US4] Add schema-specific registered claimer coverage in `tests/Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests/Persistence/PostgreSqlPersistenceRegistrationTests.cs`
- [x] T053 [US4] Add root and module setup coverage in `tests/Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests/Persistence/BondstonePostgreSqlServiceCollectionExtensionsTests.cs`
- [x] T054 [US4] Add dispatcher aggregator replacement/preservation coverage in `tests/Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests/Persistence/BondstonePostgreSqlServiceCollectionExtensionsTests.cs`
- [x] T055 [US4] Add successful transport dispatch coverage in `tests/Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests/Persistence/PostgreSqlOutboxDispatcherTests.cs`
- [x] T056 [US4] Add module-shared-table source scoping coverage in `tests/Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests/Persistence/PostgreSqlOutboxDispatcherTests.cs`
- [x] T057 [US4] Add transport retry and terminal failure dispatch coverage in `tests/Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests/Persistence/PostgreSqlOutboxDispatcherTests.cs`
- [x] T058 [US4] Add cross-module DbContext non-resolution coverage in `tests/Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests/Persistence/BondstonePostgreSqlServiceCollectionExtensionsTests.cs`

### Implementation for User Story 4

- [x] T059 [US4] Register `IDurableOutboxClaimer` in `AddBondstonePostgreSqlPersistence<TDbContext>()`
- [x] T060 [US4] Register `IDurableOutboxLeaseRenewer` in `AddBondstonePostgreSqlPersistence<TDbContext>()`
- [x] T061 [US4] Register `IDurableOutboxDispatchRecorder` in `AddBondstonePostgreSqlPersistence<TDbContext>()`
- [x] T062 [US4] Enable module outbox dispatcher aggregator in `AddBondstonePostgreSqlModulePersistence<TDbContext>()`
- [x] T063 [US4] Register `DurableModuleOutboxDispatcherRegistration` for PostgreSQL modules
- [x] T064 [US4] Implement `PostgreSqlModuleDurableOutboxDispatcher<TDbContext>`
- [x] T065 [US4] Compose PostgreSQL claimer, lease renewer, and dispatch recorder with `DurableOutboxDispatcher`
- [x] T066 [US4] Scope module dispatcher claims by source module name

**Checkpoint**: PostgreSQL-backed modules can dispatch durable outbox rows through normal Bondstone setup.

---

## Phase N: Polish & Cross-Cutting Concerns

**Purpose**: Documentation, packaging, and operational support for the migrated feature.

- [x] T067 [P] Document PostgreSQL provider package purpose in `src/Bondstone.Persistence.EntityFrameworkCore.Postgres/README.md`
- [x] T068 [P] Document PostgreSQL as supported production durable persistence path in `docs/architecture.md`
- [x] T069 [P] Document PostgreSQL provider package discovery in `docs/package-discovery.md`
- [x] T070 [P] Document outbox terminal failure and stale claim inspection in `docs/operations.md`
- [x] T071 [P] Document application-owned migrations and schema rollout in `docs/packaging.md`
- [x] T072 [P] Document real PostgreSQL integration-test requirement in `docs/testing.md`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies.
- **Foundational (Phase 2)**: Depends on PostgreSQL package and generic EF Core outbox mapping.
- **User Story 1 (Phase 3)**: Depends on mapped outbox table and provider-neutral durable outbox records.
- **User Story 2 (Phase 4)**: Depends on claimed processing rows.
- **User Story 3 (Phase 5)**: Depends on claimed processing rows.
- **User Story 4 (Phase 6)**: Depends on PostgreSQL claimer, lease renewer, and dispatch recorder plus provider-neutral dispatcher.
- **Polish**: Depends on stable provider behavior and setup surface.

### User Story Dependencies

- **User Story 1 (P1)**: Can be implemented once outbox mapping exists.
- **User Story 2 (P2)**: Depends on processing claim state.
- **User Story 3 (P3)**: Depends on processing claim state.
- **User Story 4 (P4)**: Depends on provider-specific mutation primitives and module setup.

## Gaps Identified

- Cleanup, purge, replay, retention, and operator repair flows are not implemented by this PostgreSQL outbox feature.
- Long-running dispatch lease heartbeat is not implemented here; this feature exposes lease renewal, but no worker heartbeat loop.
- Broker-native dead-letter movement and retry policy remain application or transport-owned.
- Applications still own EF migrations and operational rollout for the mapped outbox table.
- Provider tests cover behavior through PostgreSQL integration tests plus validation unit tests, but do not isolate every generated SQL branch as unit tests.

## Verification Commands

The migrated feature is covered by existing tests. Use these commands after building and with PostgreSQL test infrastructure available:

```bash
pnpm backend:build
dotnet test tests/Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests/Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests.csproj --configuration Release --no-build --filter "Category=Unit|Category=Integration"
```
