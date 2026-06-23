---
description: "Migrated task list for existing PostgreSQL direct inbox persistence feature"
---

# Tasks: PostgreSQL Direct Inbox Persistence

**Input**: Migrated design documents from `specs/013-postgresql-direct-inbox-persistence/`

**Prerequisites**: Existing PostgreSQL EF Core provider implementation in `src/Bondstone.Persistence.EntityFrameworkCore.Postgres`; existing PostgreSQL provider tests in `tests/Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests`

**Tests**: Existing xUnit tests use `Category=Unit`, `Category=Application`, and Testcontainers-backed `Category=Integration`.

**Organization**: Tasks are grouped by migrated user story. All implementation tasks are marked complete because the feature existed before SpecKit adoption.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Could have been implemented in parallel at the time because it touched different files or concerns.
- **[Story]**: Migrated user story identifier.
- All paths refer to existing source-controlled files.

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Establish PostgreSQL EF Core provider package, Testcontainers-backed tests, and provider setup entry points.

- [x] T001 Create PostgreSQL EF Core provider package `src/Bondstone.Persistence.EntityFrameworkCore.Postgres/Bondstone.Persistence.EntityFrameworkCore.Postgres.csproj`
- [x] T002 Create PostgreSQL provider test project `tests/Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests/Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests.csproj`
- [x] T003 Add PostgreSQL test fixture and DbContexts for default and schema-qualified durable persistence tests
- [x] T004 Document PostgreSQL provider package scope in `src/Bondstone.Persistence.EntityFrameworkCore.Postgres/README.md`
- [x] T005 Document real PostgreSQL/Testcontainers requirements in `docs/testing.md`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Define provider helpers used by PostgreSQL direct inbox registration and setup.

- [x] T006 [P] Implement PostgreSQL identifier quoting helper in `src/Bondstone.Persistence.EntityFrameworkCore.Postgres/Persistence/PostgreSqlTableIdentifier.cs`
- [x] T007 [P] Implement PostgreSQL exception classifier in `src/Bondstone.Persistence.EntityFrameworkCore.Postgres/Persistence/PostgreSqlPersistenceExceptionClassifier.cs`
- [x] T008 [P] Add direct inbox duplicate classification for `PK_inbox_messages`
- [x] T009 [P] Add PostgreSQL service collection setup in `src/Bondstone.Persistence.EntityFrameworkCore.Postgres/Persistence/BondstonePostgreSqlServiceCollectionExtensions.cs`
- [x] T010 [P] Add PostgreSQL builder setup in `src/Bondstone.Persistence.EntityFrameworkCore.Postgres/Persistence/BondstonePostgreSqlBuilderExtensions.cs`

**Checkpoint**: PostgreSQL provider setup and duplicate classification helpers exist.

---

## Phase 3: User Story 1 - Atomically Register Direct Inbox Receives In PostgreSQL (Priority: P1)

**Goal**: PostgreSQL direct inbox registration inserts new rows or returns existing rows without aborting the current transaction.

**Independent Test**: `dotnet test tests/Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests/Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests.csproj --configuration Release --filter "Category=Integration"`

### Tests for User Story 1

- [x] T011 [US1] Add new-row registration coverage in `tests/Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests/Persistence/PostgreSqlPersistenceInboxTests.cs`
- [x] T012 [US1] Add existing-unprocessed duplicate coverage in `tests/Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests/Persistence/PostgreSqlPersistenceInboxTests.cs`
- [x] T013 [US1] Add existing-processed duplicate coverage in `tests/Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests/Persistence/PostgreSqlPersistenceInboxTests.cs`
- [x] T014 [US1] Add duplicate-inside-transaction coverage in `tests/Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests/Persistence/PostgreSqlPersistenceInboxTests.cs`

### Implementation for User Story 1

- [x] T015 [US1] Implement `PostgreSqlDurableInboxRegistrar<TDbContext>`
- [x] T016 [US1] Build schema-aware `inbox_messages` table identifier
- [x] T017 [US1] Implement `INSERT ... ON CONFLICT ON CONSTRAINT "PK_inbox_messages" DO NOTHING`
- [x] T018 [US1] Return inserted row when registration succeeds
- [x] T019 [US1] Return existing row when registration conflicts
- [x] T020 [US1] Map inserted/existing row to direct inbox registration status
- [x] T021 [US1] Enlist registration command in current EF Core transaction
- [x] T022 [US1] Preserve connection state around provider-owned SQL

**Checkpoint**: Direct inbox registration is atomic and duplicate-safe in PostgreSQL.

---

## Phase 4: User Story 2 - Classify PostgreSQL Direct Inbox Duplicates (Priority: P2)

**Goal**: Provider code recognizes direct inbox duplicate primary-key violations.

**Independent Test**: PostgreSQL unit and integration tests after build.

### Tests for User Story 2

- [x] T023 [US2] Add unique-violation classifier coverage in `tests/Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests/Persistence/PostgreSqlPersistenceExceptionClassifierTests.cs`
- [x] T024 [US2] Add direct inbox duplicate classifier coverage in `tests/Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests/Persistence/PostgreSqlPersistenceExceptionClassifierTests.cs`
- [x] T025 [US2] Add EF store duplicate save coverage in `tests/Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests/Persistence/PostgreSqlPersistenceInboxTests.cs`
- [x] T026 [US2] Add savepoint rollback duplicate coverage in `tests/Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests/Persistence/PostgreSqlPersistenceInboxTests.cs`

### Implementation for User Story 2

- [x] T027 [US2] Implement nested `PostgresException` discovery
- [x] T028 [US2] Implement unique-violation classification by SQL state
- [x] T029 [US2] Implement optional constraint-name matching
- [x] T030 [US2] Implement `IsInboxMessageDuplicate(...)`

**Checkpoint**: Direct inbox duplicate provider exceptions can be classified reliably.

---

## Phase 5: User Story 3 - Use Schema-Aware Direct Inbox SQL And Provider Setup (Priority: P3)

**Goal**: Root and module PostgreSQL persistence setup register direct inbox services and respect configured schemas.

**Independent Test**: PostgreSQL setup unit tests and schema integration tests.

### Tests for User Story 3

- [x] T031 [US3] Add root service-registration coverage in `tests/Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests/Persistence/BondstonePostgreSqlServiceCollectionExtensionsTests.cs`
- [x] T032 [US3] Add module persistence registration coverage in `tests/Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests/Persistence/BondstonePostgreSqlServiceCollectionExtensionsTests.cs`
- [x] T033 [US3] Add module-only root executor absence coverage in `tests/Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests/Persistence/BondstonePostgreSqlServiceCollectionExtensionsTests.cs`
- [x] T034 [US3] Add schema-qualified root persistence coverage in `tests/Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests/Persistence/PostgreSqlPersistenceRegistrationTests.cs`
- [x] T035 [US3] Add `inbox_messages` table and primary-key schema coverage in `tests/Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests/Persistence/PostgreSqlPersistenceSchemaTests.cs`

### Implementation for User Story 3

- [x] T036 [US3] Register `IDurableInboxRegistrar` in root PostgreSQL persistence setup
- [x] T037 [US3] Register root `IDurableInboxHandlerExecutor` in root PostgreSQL persistence setup
- [x] T038 [US3] Register module direct inbox handler executor in module PostgreSQL persistence setup
- [x] T039 [US3] Register module direct inbox inspection store in module PostgreSQL persistence setup
- [x] T040 [US3] Thread optional schema into PostgreSQL direct inbox registrar and module executor
- [x] T041 [US3] Expose root and module setup through PostgreSQL builder extensions

**Checkpoint**: PostgreSQL setup routes direct inbox behavior to the intended root or module persistence boundary.

---

## Phase 6: User Story 4 - Execute Direct Inbox Handlers Through PostgreSQL Module Persistence (Priority: P4)

**Goal**: PostgreSQL module direct inbox handler executors compose provider registration and EF Core store behavior.

**Independent Test**: PostgreSQL integration tests after build.

### Tests for User Story 4

- [x] T042 [US4] Add schema-qualified handler execution coverage in `tests/Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests/Persistence/PostgreSqlPersistenceRegistrationTests.cs`
- [x] T043 [US4] Add single-root fallback command receive coverage in `tests/Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests/Persistence/PostgreSqlSingleRootFallbackTests.cs`
- [x] T044 [US4] Add processed direct inbox row verification for fallback receive in `tests/Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests/Persistence/PostgreSqlSingleRootFallbackTests.cs`

### Implementation for User Story 4

- [x] T045 [US4] Implement `PostgreSqlModuleDurableInboxHandlerExecutor<TDbContext>`
- [x] T046 [US4] Compose PostgreSQL registrar with generic EF Core direct inbox store
- [x] T047 [US4] Pass optional `TimeProvider` to provider-neutral direct inbox handler executor
- [x] T048 [US4] Normalize and expose module name on module executor

**Checkpoint**: Direct inbox receive execution can use PostgreSQL root fallback or module-specific persistence.

---

## Phase N: Polish & Cross-Cutting Concerns

**Purpose**: Documentation, diagnostics, and compatibility support for the migrated feature.

- [x] T049 [P] Document EF Core plus PostgreSQL as the supported production durable persistence path in `docs/architecture.md`
- [x] T050 [P] Document PostgreSQL provider package ownership in `docs/packaging.md`
- [x] T051 [P] Document PostgreSQL integration-test expectations in `docs/testing.md`
- [x] T052 [P] Keep public API compatibility covered by `tests/Bondstone.PublicApi.Tests`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies.
- **Foundational (Phase 2)**: Depends on PostgreSQL package and EF Core direct inbox mapping.
- **User Stories (Phase 3+)**: Depend on provider-neutral direct inbox contracts and generic EF Core inbox mapping/store behavior.
- **Polish**: Depends on stable public API, schema shape, and provider behavior.

### User Story Dependencies

- **User Story 1 (P1)**: Depends on EF Core direct inbox mapping and PostgreSQL connection access.
- **User Story 2 (P2)**: Depends on PostgreSQL exception classifier and direct inbox primary key.
- **User Story 3 (P3)**: Depends on registrar, generic EF Core store, and service registration conventions.
- **User Story 4 (P4)**: Depends on direct inbox registrar, generic EF Core store, and provider-neutral handler executor.

## Gaps Identified

- Consumer EF migration generation and rollout are application-owned.
- Direct inbox cleanup, purge, replay, retention, and operator repair flows are not implemented.
- Generic EF Core direct inbox mapping/store behavior is outside this migration and tracked under `specs/012-efcore-direct-inbox-persistence`.
- Durable incoming inbox PostgreSQL behavior is separate from direct inbox idempotency.
- Transport receive, broker settlement, topology, and hosted workers are outside this feature.

## Verification Commands

The migrated feature is covered by existing tests. Use these commands after building:

```bash
pnpm backend:build
dotnet test tests/Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests/Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests.csproj --configuration Release --no-build --filter "Category=Unit|Category=Integration"
```
