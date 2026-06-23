---
description: "Migrated task list for existing message identity, envelope, and serialization feature"
---

# Tasks: Message Identity, Envelope, And Serialization

**Input**: Migrated design documents from `specs/019-message-identity-envelope-serialization/`

## Phase 1: Identity

- [x] T001 Implement durable command and integration event identity attributes
- [x] T002 Implement message type registration
- [x] T003 Implement message type registry
- [x] T004 Add identity and registry tests

## Phase 2: Envelope And Trace Context

- [x] T005 Implement durable message envelope
- [x] T006 Implement message trace context
- [x] T007 Validate envelope ids, modules, kind, type name, payload, and operation id
- [x] T008 Add envelope and trace context tests

## Phase 3: Serialization

- [x] T009 Implement durable payload JSON options
- [x] T010 Implement system text JSON durable payload serializer
- [x] T011 Implement durable message envelope serializer
- [x] T012 Add serializer round-trip tests

## Gaps Identified

- Transport-native serialization remains adapter-owned.

## Verification Commands

```bash
pnpm backend:build
dotnet test tests/Bondstone.Tests/Bondstone.Tests.csproj --configuration Release --no-build --filter "Category=Unit"
```
