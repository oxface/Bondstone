---
description: "Migrated task list for existing domain events feature"
---

# Tasks: Domain Events

**Input**: Migrated design documents from `specs/020-domain-events/`

## Phase 1: Core Contracts

- [x] T001 Implement domain event marker contract
- [x] T002 Implement domain event source contract
- [x] T003 Implement domain event identity attribute
- [x] T004 Add domain event contract tests

## Phase 2: EF Core Collection

- [x] T005 Implement EF Core domain event collector
- [x] T006 Implement module behavior for domain event collection
- [x] T007 Register EF domain event module behavior
- [x] T008 Add EF Core collection tests

## Phase 3: Optional Persistence

- [x] T009 Implement domain event record entity
- [x] T010 Implement domain event record mapping
- [x] T011 Add model-builder extension
- [x] T012 Add module builder opt-in
- [x] T013 Add mapping and persistence tests

## Phase 4: PostgreSQL Proof

- [x] T014 Add PostgreSQL transaction test for persisted domain events
- [x] T015 Keep consumer migrations application-owned

## Gaps Identified

- Domain events are not automatically published as integration events.

## Verification Commands

```bash
pnpm backend:build
pnpm backend:test:integration
```
