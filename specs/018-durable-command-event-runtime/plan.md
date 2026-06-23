# Implementation Plan: Durable Command And Event Runtime

**Branch**: `N/A - migrated existing functionality` | **Date**: 2026-06-23 | **Spec**: [spec.md](spec.md)

## Summary

This migrated feature captures runtime durable send/publish APIs and receive handoff: command sender, event publisher, send/publish result models, durable envelope receiver, and receive bindings.

## Technical Context

**Language/Version**: C# on `net10.0`

**Primary Dependencies**: `Bondstone`, `Bondstone.Persistence`, module runtime, message type registry, durable payload serializer

**Scale/Scope**: 2,041 lines across source and focused messaging tests.

## Constitution Check

_GATE: Passed._

- Runtime APIs stay in `Bondstone`.
- Durable messages use explicit identities.
- Persistence/transport implementations stay separate.

## Project Structure

```text
src/Bondstone/Messaging/
├── Contracts/
├── Sending/
├── Publishing/
└── Receiving/

tests/Bondstone.Tests/Messaging/
├── DurableCommandSenderTests.cs
├── DurableCommandSendResultTests.cs
├── DurableEventPublisherTests.cs
├── DurableEventPublishResultTests.cs
└── DurableEnvelopeReceiverTests.cs
```

## Reconstructed Implementation Approach

1. Resolve source/target module and message identity.
2. Serialize command/event payloads into durable envelopes.
3. Stage envelopes through source outbox writer.
4. Create operation handle and pending operation state when operation id is supplied.
5. Route inbound envelopes to command/event receive pipelines.

## Verification Strategy

```bash
pnpm backend:build
dotnet test tests/Bondstone.Tests/Bondstone.Tests.csproj --configuration Release --no-build --filter "Category=Unit"
```

## Gaps And Follow-Up Candidates

- Outbox dispatch and concrete transport behavior are separate migrations.
