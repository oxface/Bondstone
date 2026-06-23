---
description: "Migrated task list for existing EF Core operation-state persistence feature"
---

# Tasks: EF Core Operation State Persistence

**Input**: Migrated design documents from `specs/015-efcore-operation-state-persistence/`

**Tests**: Existing tests use `Category=Unit` and `Category=Application`.

## Phase 1: Setup

- [x] T001 Create EF Core operation-state entity and configuration files
- [x] T002 Add package-local operation-state tests
- [x] T003 Include operation-state mapping in generic EF Core persistence setup

## Phase 2: Mapping And Entity

- [x] T004 Implement `OperationStateEntity`
- [x] T005 Implement `OperationStateEntityConfiguration`
- [x] T006 Map operation id primary key and operation-state columns
- [x] T007 Add entity round-trip tests
- [x] T008 Add model-builder metadata tests

## Phase 3: Store Behavior

- [x] T009 Implement `EntityFrameworkCoreDurableOperationStateStore<TDbContext>`
- [x] T010 Read operation state by durable operation id
- [x] T011 Insert new operation state rows
- [x] T012 Update existing operation state rows
- [x] T013 Avoid calling `SaveChanges` from store operations
- [x] T014 Add store save/read/update tests

## Phase 4: Expiration And Module Scope

- [x] T015 Implement expiration candidate lookup for stale pending/running rows
- [x] T016 Implement `EntityFrameworkCoreModuleDurableOperationStateStore<TDbContext>`
- [x] T017 Keep module-scoped state associated with configured module
- [x] T018 Add expiration filtering and ordering tests

## Phase 5: Setup And Documentation

- [x] T019 Register `IDurableOperationStateStore` in generic EF Core setup
- [x] T020 Apply operation-state mapping in `ApplyBondstonePersistence(...)`
- [x] T021 Document EF Core operation-state ownership in durable architecture docs

## Gaps Identified

- PostgreSQL transaction/schema behavior is a separate provider concern.
- Consumer EF migrations are application-owned.

## Verification Commands

```bash
pnpm backend:build
dotnet test tests/Bondstone.Persistence.EntityFrameworkCore.Tests/Bondstone.Persistence.EntityFrameworkCore.Tests.csproj --configuration Release --no-build --filter "Category=Unit|Category=Application"
```
