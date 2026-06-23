---
description: "Migrated task list for existing core module model and execution feature"
---

# Tasks: Core Module Model And Execution

**Input**: Migrated design documents from `specs/017-core-module-model-execution/`

## Phase 1: Module Registration

- [x] T001 Implement module registry and module builder
- [x] T002 Normalize and validate module names
- [x] T003 Register durable messaging capability
- [x] T004 Add module registration tests

## Phase 2: Command And Query Routes

- [x] T005 Implement command route registration and lookup
- [x] T006 Implement query route registration and lookup
- [x] T007 Register command validators
- [x] T008 Validate duplicate and missing routes
- [x] T009 Add command/query route tests

## Phase 3: Execution Runtime

- [x] T010 Implement module command executor
- [x] T011 Implement module query executor
- [x] T012 Flow module execution context
- [x] T013 Run command validators before handlers
- [x] T014 Execute transaction runners and post-handler actions

## Phase 4: Event Subscriber Runtime

- [x] T015 Implement published-event registration
- [x] T016 Implement subscriber registration with durable identities
- [x] T017 Implement event subscriber executor
- [x] T018 Add subscriber execution tests

## Phase 5: Receive Pipelines

- [x] T019 Implement command receive pipeline
- [x] T020 Implement event receive pipeline
- [x] T021 Deserialize durable envelopes
- [x] T022 Resolve target routes/subscribers
- [x] T023 Integrate direct inbox handling
- [x] T024 Add receive pipeline tests

## Gaps Identified

- Durable send/publish persistence and transport delivery are separate features.

## Verification Commands

```bash
pnpm backend:build
dotnet test tests/Bondstone.Tests/Bondstone.Tests.csproj --configuration Release --no-build --filter "Category=Unit"
```
