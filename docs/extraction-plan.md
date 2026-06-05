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
  - `AddBondstoneEntityFrameworkCorePersistence<TDbContext>`
- EF Core store implementations:
  - outbox writer
  - inbox store
  - operation state store
- PostgreSQL provider helpers:
  - `AddBondstonePostgreSqlPersistence<TDbContext>`
  - `PostgreSqlPersistenceExceptionClassifier`
  - `PostgreSqlDurableOutboxClaimer<TDbContext>`
- PostgreSQL outbox claiming:
  - provider-neutral `IDurableOutboxClaimer` contract
  - PostgreSQL `FOR UPDATE SKIP LOCKED` claim implementation
  - scheduled pending row eligibility
  - expired processing lease reclaim
  - active processing lease exclusion
  - schema-aware service registration
- PostgreSQL outbox dispatch lifecycle:
  - provider-neutral `IDurableOutboxDispatchRecorder` contract
  - dispatch success recording
  - retry scheduling after failure
  - dead-letter outcome recording
  - stale claimant and expired lease rejection
- PostgreSQL inbox registration:
  - provider-neutral `IDurableInboxRegistrar` contract
  - `DurableInboxRegistrationResult`
  - newly registered, already received, and already processed outcomes
  - transaction-safe duplicate registration
  - schema-aware service registration
- Inbox handle-once orchestration:
  - provider-neutral `IDurableInboxHandlerExecutor` contract
  - `DurableInboxHandleResult`
  - delegate-based handler execution for newly registered records
  - explicit caller-supplied commit boundary
  - conservative skip for already received or already processed rows
- EF Core persistence scope:
  - provider-neutral EF `IEntityFrameworkCorePersistenceScope` contract
  - transaction start/join behavior for consumer-owned DbContexts
  - explicit `SaveChangesAsync` commit delegate for lower-level durable
    primitives
  - PostgreSQL verification for commit, rollback, and existing transaction
    ownership
- Architecture docs split into topic pages under `docs/architecture/`.
- Neutral unit tests cover message identity registration, trace context capture,
  durable command send result semantics, durable operation state/status
  semantics, durable message envelope validation, persistence record and
  dispatch-state validation, and inbox handle-once control flow.
- EF Core unit tests cover entity-to-core mapping, provider-neutral model
  metadata, provider-neutral service registration, outbox writer staging, inbox
  store staging, and operation state store behavior.
- PostgreSQL integration tests cover provider-neutral EF Core schema creation
  against PostgreSQL, outbox transaction commit/rollback behavior, and inbox
  duplicate unique-constraint behavior. They also cover PostgreSQL registration
  helper behavior, primary-key constraint names, inbox processed timestamps,
  operation-state updates, outbox claim lease columns, savepoint rollback after
  duplicate inbox inserts, `FOR UPDATE SKIP LOCKED` outbox row selection
  against a real database, and public PostgreSQL outbox claim behavior
  including scheduled rows and schema-aware service registration. They also
  cover PostgreSQL outbox dispatch lifecycle outcomes and public PostgreSQL
  inbox registration outcomes.

## Active Rename Notes

- Historical source material used `DurableOperationSnapshot` for the read-model
  DTO and `DurableOperationState` for the enum. The extracted public contract
  uses `DurableOperationState` for the read-model DTO and
  `DurableOperationStatus` for the enum because callers read the current
  operation state and inspect its status, while `snapshot` sounds like a
  persisted projection or event-sourcing term.

## Next Candidate Slice

Continue PostgreSQL provider extraction with real database verification before
adding broader provider APIs.

Candidate concepts:

- outbox lease renewal, retry-delay calculation, max-attempt policy,
  dead-letter routing, and stale-claim recovery orchestration built around the
  shared claim lease state and verified PostgreSQL claim and lifecycle
  implementations;
- inbox handler discovery, receive retry policy, stale receive recovery,
  transport acknowledgement coordination, module identity scopes, and
  higher-level transaction helper APIs only if required by real transport or
  sample behavior;
- fresh `dotnet restore` timeout investigation for the PostgreSQL provider
  dependency graph; no-restore build/test/pack and integration tests pass from
  current restored assets.

Review pressure:

- Keep EF Core package concerns out of `Bondstone` unless they are pure public
  contracts.
- Keep provider-specific locking, claiming, SQL, schema, and migration details
  inside provider packages.
- Preserve atomic source-state-plus-outbox semantics without recreating a
  generic mediator.
- Create or amend an ADR before introducing public conflict, claiming, leasing,
  unit-of-work, or migration-helper APIs.

Verification:

- Add Testcontainers-backed PostgreSQL `Integration` tests for provider
  behavior.
- Run `pnpm check` for the default gate and
  `pnpm backend:test:integration` or targeted integration tests for provider
  behavior.

## Deferred Work

- `send and wait` helper behavior and timeout/polling policy.
- Trace context and causation propagation through inbox, outbox, and transport
  adapters.
- Envelope content type if non-JSON payloads or explicit JSON contracts become
  necessary.
- Neutral envelope headers if multiple adapters need cross-cutting metadata.
- Scheduling, TTL, priority, reply-to, tenant, or transport-native metadata if
  a later durable scenario justifies it.
- PostgreSQL `jsonb` payload mapping if provider-specific payload storage
  becomes useful; keep generic EF mappings provider-neutral unless an ADR
  accepts the cross-provider migration cost.
- Retry-delay calculation, max-attempt, and dead-letter routing ownership.
- Partition-key ordering and scaling semantics.
- Outbox dispatcher loop, transport send implementation, lease renewal,
  stale-claim recovery, retry-delay calculation, and max-attempt semantics.
- Inbox handler discovery, stale receive recovery, receive-side retry policy,
  transport acknowledgement coordination, module identity scopes, and
  higher-level transaction helper APIs.
- Bondstone-owned migration helpers or provider-specific migration
  conventions.
- Operation-state transition policy and optimistic concurrency.
- Provider-specific dispatch behavior beyond PostgreSQL claiming.
- Rebus transport adapter behavior.
- Additional integration tests with neutral Bondstone fixtures.
- Samples validating modular-monolith and service-split usage.

## ADR Checkpoints

Create or amend an ADR before widening implementation if any of these become
necessary:

- public API changes to durable send/result/trace contracts;
- operation-state transition policy beyond the current store contract;
- inbox/outbox entity shape and migration policy;
- retry/dead-letter ownership;
- provider-specific locking or claiming strategy beyond the current
  PostgreSQL claim contract;
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
