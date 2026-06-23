---
description: "Migrated task list for existing PostgreSQL incoming inbox persistence feature"
---

# Tasks: PostgreSQL Incoming Inbox Persistence

**Input**: Migrated design documents from `specs/008-postgresql-incoming-inbox-persistence/`

**Prerequisites**: Existing PostgreSQL provider implementation in `src/Bondstone.Persistence.EntityFrameworkCore.Postgres`; existing PostgreSQL integration tests in `tests/Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests`

**Tests**: Existing xUnit tests use `Category=Integration` for provider behavior and `Category=Unit` for setup behavior.

**Organization**: Tasks are grouped by migrated user story. All implementation tasks are marked complete because the feature existed before SpecKit adoption.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Could have been implemented in parallel at the time because it touched different files or concerns.
- **[Story]**: Migrated user story identifier.
- All paths refer to existing source-controlled files.

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Establish PostgreSQL provider package support for durable incoming inbox mutation.

- [x] T001 Create PostgreSQL provider package project `src/Bondstone.Persistence.EntityFrameworkCore.Postgres/Bondstone.Persistence.EntityFrameworkCore.Postgres.csproj`
- [x] T002 Add package documentation in `src/Bondstone.Persistence.EntityFrameworkCore.Postgres/README.md`
- [x] T003 Add scoped package guidance in `src/Bondstone.Persistence.EntityFrameworkCore.Postgres/AGENTS.md`
- [x] T004 Add PostgreSQL provider test project `tests/Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests/Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests.csproj`
- [x] T005 Add PostgreSQL provider test guidance in `tests/Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests/README.md` and `tests/Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests/AGENTS.md`
- [x] T006 Add PostgreSQL incoming inbox test DbContext in `tests/Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests/PostgreSqlIncomingInboxTestDbContext.cs`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Register PostgreSQL provider setup and table identifier support used by incoming inbox mutation.

- [x] T007 [P] Implement PostgreSQL table identifier helper in `src/Bondstone.Persistence.EntityFrameworkCore.Postgres/IncomingInbox/PostgreSqlIncomingInboxTableIdentifier.cs`
- [x] T008 [P] Implement PostgreSQL service registration surface in `src/Bondstone.Persistence.EntityFrameworkCore.Postgres/Persistence/BondstonePostgreSqlServiceCollectionExtensions.cs`
- [x] T009 [P] Implement PostgreSQL builder setup surface in `src/Bondstone.Persistence.EntityFrameworkCore.Postgres/Persistence/BondstonePostgreSqlBuilderExtensions.cs`
- [x] T010 [P] Add PostgreSQL setup tests in `tests/Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests/Persistence/BondstonePostgreSqlServiceCollectionExtensionsTests.cs`
- [x] T011 [P] Add PostgreSQL persistence registration tests in `tests/Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests/Persistence/PostgreSqlPersistenceRegistrationTests.cs`

**Checkpoint**: PostgreSQL package setup can register provider-owned incoming inbox mutation primitives and module dispatcher registration hooks.

---

## Phase 3: User Story 1 - Claim Due Incoming Inbox Rows With PostgreSQL (Priority: P1)

**Goal**: PostgreSQL atomically claims due incoming inbox rows for processing.

**Independent Test**: `dotnet test tests/Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests/Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests.csproj --configuration Release --filter "Category=Integration"`

### Tests for User Story 1

- [x] T012 [US1] Add missing mapping setup error coverage in `tests/Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests/Persistence/PostgreSqlIncomingInboxMutationTests.cs`
- [x] T013 [US1] Add pending-row deterministic claim coverage in `tests/Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests/Persistence/PostgreSqlIncomingInboxMutationTests.cs`
- [x] T014 [US1] Add due retry row claim coverage in `tests/Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests/Persistence/PostgreSqlIncomingInboxMutationTests.cs`
- [x] T015 [US1] Add not-due retry exclusion coverage in `tests/Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests/Persistence/PostgreSqlIncomingInboxMutationTests.cs`
- [x] T016 [US1] Add stale processing reclaim coverage in `tests/Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests/Persistence/PostgreSqlIncomingInboxMutationTests.cs`
- [x] T017 [US1] Add active processing lease exclusion coverage in `tests/Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests/Persistence/PostgreSqlIncomingInboxMutationTests.cs`
- [x] T018 [US1] Add receiver-module claim scoping coverage in `tests/Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests/Persistence/PostgreSqlIncomingInboxProcessingTests.cs`

### Implementation for User Story 1

- [x] T019 [US1] Implement `PostgreSqlDurableIncomingInboxClaimer<TDbContext>`
- [x] T020 [US1] Validate incoming inbox EF mapping before claiming
- [x] T021 [US1] Normalize and validate claim owner
- [x] T022 [US1] Validate positive lease duration and max count
- [x] T023 [US1] Select pending, due retry, and stale processing rows as claim candidates
- [x] T024 [US1] Restrict candidates by receiver module when a module-specific claimer is configured
- [x] T025 [US1] Use PostgreSQL `FOR UPDATE SKIP LOCKED` and `LIMIT @maxCount`
- [x] T026 [US1] Update claimed rows to `Processing`, increment attempt count, clear prior outcomes, and set claim fields
- [x] T027 [US1] Return claimed rows as `DurableIncomingInboxRecord` values

**Checkpoint**: PostgreSQL can safely claim due durable incoming inbox rows for workers.

---

## Phase 4: User Story 2 - Renew Active Incoming Inbox Processing Leases (Priority: P2)

**Goal**: PostgreSQL extends only active matching processing claims.

**Independent Test**: PostgreSQL integration tests after build.

### Tests for User Story 2

- [x] T028 [US2] Add missing mapping setup error coverage for lease renewal in `tests/Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests/Persistence/PostgreSqlIncomingInboxMutationTests.cs`
- [x] T029 [US2] Add active lease renewal coverage in `tests/Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests/Persistence/PostgreSqlIncomingInboxMutationTests.cs`
- [x] T030 [US2] Add stale lease renewal rejection coverage in `tests/Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests/Persistence/PostgreSqlIncomingInboxMutationTests.cs`

### Implementation for User Story 2

- [x] T031 [US2] Implement `PostgreSqlDurableIncomingInboxLeaseRenewer<TDbContext>`
- [x] T032 [US2] Validate incoming inbox EF mapping before renewal
- [x] T033 [US2] Validate incoming inbox key, claim owner, and positive lease duration
- [x] T034 [US2] Update `ClaimedUntilUtc` only for matching key, processing status, matching owner, and active lease
- [x] T035 [US2] Return `true` only when exactly one row is renewed

**Checkpoint**: PostgreSQL exposes the provider primitive needed to extend active incoming inbox processing claims.

---

## Phase 5: User Story 3 - Record Processed, Retry, And Terminal Outcomes With PostgreSQL (Priority: P3)

**Goal**: PostgreSQL records incoming inbox processing outcomes only for active matching claims.

**Independent Test**: PostgreSQL integration tests after build.

### Tests for User Story 3

- [x] T036 [US3] Add missing mapping setup error coverage for outcome recording in `tests/Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests/Persistence/PostgreSqlIncomingInboxMutationTests.cs`
- [x] T037 [US3] Add processed outcome coverage in `tests/Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests/Persistence/PostgreSqlIncomingInboxMutationTests.cs`
- [x] T038 [US3] Add stale processed outcome rejection coverage in `tests/Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests/Persistence/PostgreSqlIncomingInboxMutationTests.cs`
- [x] T039 [US3] Add retry scheduled outcome coverage in `tests/Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests/Persistence/PostgreSqlIncomingInboxMutationTests.cs`
- [x] T040 [US3] Add stale retry outcome rejection coverage in `tests/Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests/Persistence/PostgreSqlIncomingInboxMutationTests.cs`
- [x] T041 [US3] Add terminal failed outcome coverage in `tests/Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests/Persistence/PostgreSqlIncomingInboxMutationTests.cs`
- [x] T042 [US3] Add stale terminal outcome rejection coverage in `tests/Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests/Persistence/PostgreSqlIncomingInboxMutationTests.cs`

### Implementation for User Story 3

- [x] T043 [US3] Implement `PostgreSqlDurableIncomingInboxOutcomeRecorder<TDbContext>`
- [x] T044 [US3] Validate incoming inbox EF mapping before outcome recording
- [x] T045 [US3] Validate key, claim owner, UTC timestamps, failure reason, and retry timestamp ordering
- [x] T046 [US3] Implement `MarkProcessedAsync(...)` guarded by key, processing status, claim owner, and active lease
- [x] T047 [US3] Clear retry/failure/claim fields when marking processed
- [x] T048 [US3] Implement `ScheduleRetryAsync(...)` guarded by key, processing status, claim owner, and active lease
- [x] T049 [US3] Record failed timestamp, failure reason, next attempt timestamp, and clear claim fields when scheduling retry
- [x] T050 [US3] Implement `MarkTerminalFailedAsync(...)` guarded by key, processing status, claim owner, and active lease
- [x] T051 [US3] Record failed timestamp and failure reason and clear retry/claim fields when marking terminal failed
- [x] T052 [US3] Return `false` when guarded outcome updates affect no rows

**Checkpoint**: PostgreSQL records durable receive outcomes without allowing stale workers to overwrite newer state.

---

## Phase 6: User Story 4 - Process Incoming Inbox Rows Through PostgreSQL Module Dispatchers (Priority: P4)

**Goal**: PostgreSQL setup wires provider-specific mutation into module-scoped incoming inbox dispatchers.

**Independent Test**: PostgreSQL integration and setup tests after build.

### Tests for User Story 4

- [x] T053 [US4] Add provider service resolution coverage in `tests/Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests/Persistence/PostgreSqlPersistenceRegistrationTests.cs`
- [x] T054 [US4] Add schema-specific registered claimer coverage in `tests/Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests/Persistence/PostgreSqlPersistenceRegistrationTests.cs`
- [x] T055 [US4] Add root and module setup coverage in `tests/Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests/Persistence/BondstonePostgreSqlServiceCollectionExtensionsTests.cs`
- [x] T056 [US4] Add dispatcher aggregator replacement/preservation coverage in `tests/Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests/Persistence/BondstonePostgreSqlServiceCollectionExtensionsTests.cs`
- [x] T057 [US4] Add command processing and processed outcome coverage in `tests/Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests/Persistence/PostgreSqlIncomingInboxProcessingTests.cs`
- [x] T058 [US4] Add duplicate delivery no-rerun coverage in `tests/Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests/Persistence/PostgreSqlIncomingInboxProcessingTests.cs`
- [x] T059 [US4] Add module-shared-table receiver scoping coverage in `tests/Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests/Persistence/PostgreSqlIncomingInboxProcessingTests.cs`
- [x] T060 [US4] Add handler retry and terminal failure processing coverage in `tests/Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests/Persistence/PostgreSqlIncomingInboxProcessingTests.cs`
- [x] T061 [US4] Add stale processing inspection evidence coverage in `tests/Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests/Persistence/PostgreSqlIncomingInboxProcessingTests.cs`

### Implementation for User Story 4

- [x] T062 [US4] Register `IDurableIncomingInboxClaimer` in `AddBondstonePostgreSqlPersistence<TDbContext>()`
- [x] T063 [US4] Register `IDurableIncomingInboxLeaseRenewer` in `AddBondstonePostgreSqlPersistence<TDbContext>()`
- [x] T064 [US4] Register `IDurableIncomingInboxOutcomeRecorder` in `AddBondstonePostgreSqlPersistence<TDbContext>()`
- [x] T065 [US4] Enable module incoming inbox dispatcher aggregator in `AddBondstonePostgreSqlModulePersistence<TDbContext>()`
- [x] T066 [US4] Register `DurableModuleIncomingInboxDispatcherRegistration` for PostgreSQL modules
- [x] T067 [US4] Implement `PostgreSqlModuleDurableIncomingInboxDispatcher<TDbContext>`
- [x] T068 [US4] Compose PostgreSQL claimer and outcome recorder with `DurableIncomingInboxDispatcher`
- [x] T069 [US4] Scope module dispatcher claims by receiver module name

**Checkpoint**: PostgreSQL-backed modules can process durable incoming inbox rows through normal Bondstone setup.

---

## Phase N: Polish & Cross-Cutting Concerns

**Purpose**: Documentation, packaging, and operational support for the migrated feature.

- [x] T070 [P] Document PostgreSQL provider package purpose in `src/Bondstone.Persistence.EntityFrameworkCore.Postgres/README.md`
- [x] T071 [P] Document PostgreSQL as supported production durable persistence path in `docs/architecture.md`
- [x] T072 [P] Document PostgreSQL provider package discovery in `docs/package-discovery.md`
- [x] T073 [P] Document incoming inbox terminal failure and stale claim inspection in `docs/operations.md`
- [x] T074 [P] Document application-owned migrations and schema rollout in `docs/packaging.md`
- [x] T075 [P] Document real PostgreSQL integration-test requirement in `docs/testing.md`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies.
- **Foundational (Phase 2)**: Depends on PostgreSQL package and generic EF Core incoming inbox mapping.
- **User Story 1 (Phase 3)**: Depends on mapped incoming inbox table and provider-neutral incoming inbox records.
- **User Story 2 (Phase 4)**: Depends on claimed processing rows.
- **User Story 3 (Phase 5)**: Depends on claimed processing rows.
- **User Story 4 (Phase 6)**: Depends on PostgreSQL claimer and outcome recorder plus provider-neutral dispatcher.
- **Polish**: Depends on stable provider behavior and setup surface.

### User Story Dependencies

- **User Story 1 (P1)**: Can be implemented once incoming inbox mapping exists.
- **User Story 2 (P2)**: Depends on processing claim state.
- **User Story 3 (P3)**: Depends on processing claim state.
- **User Story 4 (P4)**: Depends on provider-specific mutation primitives and module setup.

## Gaps Identified

- Cleanup, purge, replay, retention, and operator repair flows are not implemented by this PostgreSQL incoming inbox feature.
- Long-running handler lease heartbeat is not implemented here; this feature exposes lease renewal, but no dispatcher heartbeat loop.
- Broker-native dead-letter movement and retry policy remain application or transport-owned.
- Tests cover provider behavior through PostgreSQL integration tests, but do not isolate every SQL validation branch as unit tests.
- Applications still own EF migrations and operational rollout for the mapped incoming inbox table.

## Verification Commands

The migrated feature is covered by existing tests. Use these commands after building and with PostgreSQL test infrastructure available:

```bash
pnpm backend:build
dotnet test tests/Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests/Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests.csproj --configuration Release --no-build --filter "Category=Integration"
```
