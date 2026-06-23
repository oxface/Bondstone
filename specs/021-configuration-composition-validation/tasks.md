---
description: "Migrated task list for existing configuration and composition validation feature"
---

# Tasks: Configuration And Composition Validation

**Input**: Migrated design documents from `specs/021-configuration-composition-validation/`

## Phase 1: Core Composition

- [x] T001 Implement `AddBondstone`
- [x] T002 Implement `BondstoneBuilder`
- [x] T003 Register module registries, runtime services, serializers, persistence resolvers, and observation services
- [x] T004 Add composition smoke tests

## Phase 2: Builder APIs

- [x] T005 Implement module builder entry points
- [x] T006 Implement outbox builder entry points
- [x] T007 Implement envelope dispatcher builder extensions
- [x] T008 Implement durable payload JSON option configuration

## Phase 3: Validation

- [x] T009 Implement durable messaging configuration validator
- [x] T010 Implement outbox configuration validator
- [x] T011 Implement durable module persistence configuration validator
- [x] T012 Add validation diagnostic tests

## Gaps Identified

- Provider-specific setup remains in provider package migrations.

## Verification Commands

```bash
pnpm backend:build
dotnet test Bondstone.slnx --configuration Release --no-build --filter "Category=Unit|Category=Application"
```
