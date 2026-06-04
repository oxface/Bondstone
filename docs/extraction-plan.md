# Extraction Plan

This is the tactical extraction backlog. It should stay current and disposable:
use [extraction.md](extraction.md) for the durable extraction strategy, and use
ADRs for durable technical decisions.

## Rules

- Keep slices small enough to review independently.
- Do not preserve compatibility with the historical template as a design
  constraint.
- Do not bulk-copy implementation code.
- Do not extract the historical generic mediator or message-bus layer as a
  default Bondstone feature.
- Record deferred work explicitly so `deferred` does not become `forgotten`.
- Create or amend ADRs when a slice changes public API direction, package
  boundaries, durable behavior, provider strategy, transport strategy, or
  compatibility policy.

## Completed Slices

- Core message identity contracts:
  - `IMessage`
  - `IDurableCommand`
  - `IIntegrationEvent`
  - message identity attributes
  - `MessageKind`
  - `MessageTypeRegistration`
  - `IMessageTypeRegistry`
  - `MessageTypeRegistry`
- Durable command send contracts:
  - `IDurableCommandSender`
  - `DurableCommandSendResult`
  - `DurableCommandSendStatus`
  - `MessageTraceContext`
- Durable operation read contracts:
  - `DurableOperationState`
  - `DurableOperationStatus`
  - `IDurableOperationReader`
- Durable message envelope contracts:
  - `DurableMessageEnvelope`
- Persistence-neutral boundary contracts:
  - `IDurableOutboxWriter`
  - `DurableOutboxRecord`
  - `DurableOutboxDispatchState`
  - `DurableOutboxStatus`
  - `DurableInboxMessageKey`
  - `DurableInboxRecord`
  - `IDurableInboxStore`
  - `IDurableOperationStateStore`
- EF Core provider-neutral persistence entities and mappings:
  - outbox message entity and configuration
  - inbox message entity and configuration
  - operation state entity and configuration
  - `ApplyBondstonePersistence`
- Architecture docs split into topic pages under `docs/architecture/`.
- Neutral unit tests cover message identity registration, trace context capture,
  durable command send result semantics, durable operation state/status
  semantics, durable message envelope validation, and persistence record and
  dispatch-state validation.
- EF Core unit tests cover entity-to-core mapping and provider-neutral model
  metadata.

## Active Rename Notes

- Historical source material used `DurableOperationSnapshot` for the read-model
  DTO and `DurableOperationState` for the enum. The extracted public contract
  uses `DurableOperationState` for the read-model DTO and
  `DurableOperationStatus` for the enum because callers read the current
  operation state and inspect its status, while `snapshot` sounds like a
  persisted projection or event-sourcing term.

## Next Candidate Slice

Continue persistence extraction without provider-specific behavior.

Candidate concepts:

- implement EF Core outbox writer, inbox store, and operation state store
  against the current entities;
- unit-of-work or module persistence boundary only if required by outbox
  atomicity.

Review pressure:

- Keep EF Core package concerns out of `Bondstone` unless they are pure public
  contracts.
- Avoid provider-specific locking, claiming, SQL, schema, and migration details.
- Preserve atomic source-state-plus-outbox semantics without recreating a
  generic mediator.
- Create or amend an ADR if persistence boundaries imply a public unit-of-work
  or migration policy.

Verification:

- Add neutral unit tests for value semantics and required-field validation.
- Run `pnpm check`.

## Deferred Work

- `send and wait` helper behavior and timeout/polling policy.
- Trace context and causation propagation through inbox, outbox, and transport
  adapters.
- Envelope content type if non-JSON payloads or explicit JSON contracts become
  necessary.
- Neutral envelope headers if multiple adapters need cross-cutting metadata.
- Scheduling, TTL, priority, reply-to, tenant, or transport-native metadata if
  a later durable scenario justifies it.
- Retry, max-attempt, and dead-letter policy ownership.
- Partition-key ordering and scaling semantics.
- Outbox claiming, leases, and transport acknowledgement semantics.
- EF Core store implementations and DbContext registration helpers.
- Bondstone-owned migration helpers or provider-specific migration
  conventions.
- Operation-state transition policy and optimistic concurrency.
- PostgreSQL claiming, locking, and provider-specific behavior.
- Rebus transport adapter behavior.
- Integration tests with neutral Bondstone fixtures.
- Samples validating modular-monolith and service-split usage.

## ADR Checkpoints

Create or amend an ADR before widening implementation if any of these become
necessary:

- public API changes to durable send/result/trace contracts;
- operation-state transition policy beyond the current store contract;
- inbox/outbox entity shape and migration policy;
- retry/dead-letter ownership;
- provider-specific locking or claiming strategy;
- transport header contract;
- sample topology or service-extraction workflow.

## Historical Source Pointers

Use `/workspaces/dotnet-modular-react-template` as source material only. The
most relevant current areas are:

- `template/server/src/Bondstone/Bondstone/Messaging`
- `template/server/src/Bondstone/Bondstone.EntityFrameworkCore/Outbox`
- `template/server/src/Bondstone/Bondstone.EntityFrameworkCore/Inbox`
- `template/server/src/Bondstone/Bondstone.EntityFrameworkCore/Persistence`
- `template/server/src/Bondstone/Bondstone.EntityFrameworkCore.Postgres`
- `template/server/src/Bondstone/Bondstone.Transport.Rebus`
- `tests/ModularTemplate.Framework.Tests`
