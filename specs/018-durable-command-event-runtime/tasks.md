---
description: "Migrated task list for existing durable command and event runtime feature"
---

# Tasks: Durable Command And Event Runtime

**Input**: Migrated design documents from `specs/018-durable-command-event-runtime/`

## Phase 1: Durable Command Sending

- [x] T001 Implement `IDurableCommandSender`
- [x] T002 Implement `DurableCommandSender`
- [x] T003 Resolve message type identity and target module
- [x] T004 Serialize command payloads
- [x] T005 Stage command envelope through source outbox writer
- [x] T006 Create operation handle and pending state when operation id is supplied
- [x] T007 Add command sender tests

## Phase 2: Durable Event Publishing

- [x] T008 Implement `IDurableEventPublisher`
- [x] T009 Implement `DurableEventPublisher`
- [x] T010 Resolve published event identity and subscriber destinations
- [x] T011 Stage one envelope per subscriber
- [x] T012 Add event publisher tests

## Phase 3: Result Models

- [x] T013 Implement durable command send result/status
- [x] T014 Implement durable event publish result/status
- [x] T015 Add result model validation tests

## Phase 4: Envelope Receive Handoff

- [x] T016 Implement `IDurableEnvelopeReceiver`
- [x] T017 Implement receive bindings
- [x] T018 Route command envelopes to command receive pipeline
- [x] T019 Route event envelopes to event receive pipeline
- [x] T020 Add envelope receiver tests

## Gaps Identified

- Concrete transport dispatch and broker settlement are separate package features.

## Verification Commands

```bash
pnpm backend:build
dotnet test tests/Bondstone.Tests/Bondstone.Tests.csproj --configuration Release --no-build --filter "Category=Unit"
```
