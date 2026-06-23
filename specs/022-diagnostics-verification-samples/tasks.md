---
description: "Migrated task list for existing diagnostics, verification, and samples feature"
---

# Tasks: Diagnostics, Verification, And Samples

**Input**: Migrated design documents from `specs/022-diagnostics-verification-samples/`

## Phase 1: Diagnostics

- [x] T001 Implement messaging diagnostics helpers
- [x] T002 Implement persistence diagnostics helpers
- [x] T003 Define low-cardinality activity and metric tags
- [x] T004 Add diagnostic helper tests

## Phase 2: Public API And Package Verification

- [x] T005 Add public API baseline tests
- [x] T006 Maintain baseline files for packable packages
- [x] T007 Add package artifact tests
- [x] T008 Document pack-before-package-test requirement

## Phase 3: Samples

- [x] T009 Add modular monolith sample
- [x] T010 Add PostgreSQL-backed sample proof
- [x] T011 Add RabbitMQ broker adapter sample proof
- [x] T012 Add Service Bus broker adapter sample proof
- [x] T013 Keep infrastructure-backed sample tests in integration gate

## Gaps Identified

- Release publishing and package deprecation are not automated by these tests.

## Verification Commands

```bash
pnpm backend:test
pnpm backend:pack
pnpm backend:test:integration
```
