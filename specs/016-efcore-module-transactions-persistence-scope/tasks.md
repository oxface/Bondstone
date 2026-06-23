---
description: "Migrated task list for existing EF Core module transaction and persistence-scope feature"
---

# Tasks: EF Core Module Transactions And Persistence Scope

**Input**: Migrated design documents from `specs/016-efcore-module-transactions-persistence-scope/`

## Phase 1: Setup

- [x] T001 Create generic EF Core persistence setup extensions
- [x] T002 Create EF Core persistence scope contract and implementation
- [x] T003 Create module transaction runner and runtime registry support

## Phase 2: Persistence Scope

- [x] T004 Implement `IEntityFrameworkCorePersistenceScope`
- [x] T005 Implement `EntityFrameworkCorePersistenceScope<TDbContext>`
- [x] T006 Save changes through current DbContext
- [x] T007 Add persistence-scope tests

## Phase 3: Module Transactions

- [x] T008 Implement `EntityFrameworkCoreModuleTransactionRunner`
- [x] T009 Wrap module execution in EF Core transaction behavior
- [x] T010 Ensure post-handler state participates in transaction boundary
- [x] T011 Add module transaction behavior tests

## Phase 4: Module Runtime Registration

- [x] T012 Implement module EF Core builder extensions
- [x] T013 Record module persistence provider name and context type
- [x] T014 Register module runtime descriptors
- [x] T015 Add service registration tests

## Phase 5: Mapping Validation

- [x] T016 Implement model-builder aggregation
- [x] T017 Validate required Bondstone mappings
- [x] T018 Emit clear missing mapping diagnostics
- [x] T019 Add missing mapping tests

## Gaps Identified

- Provider-specific transaction behavior is covered by PostgreSQL migrations.

## Verification Commands

```bash
pnpm backend:build
dotnet test tests/Bondstone.Persistence.EntityFrameworkCore.Tests/Bondstone.Persistence.EntityFrameworkCore.Tests.csproj --configuration Release --no-build --filter "Category=Unit|Category=Application"
```
