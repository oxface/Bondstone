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
- Outbox lease renewal:
  - provider-neutral `IDurableOutboxLeaseRenewer` contract
  - PostgreSQL claim-owner and lease-aware renewal implementation
  - active claim renewal, wrong-owner rejection, expired-lease rejection, and
    non-processing row rejection
- PostgreSQL outbox dispatch lifecycle:
  - provider-neutral `IDurableOutboxDispatchRecorder` contract
  - dispatch success recording
  - retry scheduling after failure
  - dead-letter outcome recording
  - stale claimant and expired lease rejection
- Outbox failure decision policy:
  - provider-neutral `IDurableOutboxFailurePolicy` contract
  - default `DurableOutboxFailurePolicy`
  - deterministic retry versus dead-letter decisions from attempt count,
    maximum attempts, retry delays, and failure timestamp
- Outbox dispatcher composition:
  - provider-neutral `IDurableOutboxDispatcher` contract
  - provider-neutral `IDurableOutboxTransport` contract
  - default plain `DurableOutboxDispatcher` class for one claimed batch
  - `DurableOutboxDispatchResult` count summary
  - composition of claimer, lease renewer, transport sender, failure policy,
    and dispatch recorder
  - PostgreSQL integration coverage for success, retry, and dead-letter
    outcomes with real provider persistence and fake transport
- Rebus outgoing command transport:
  - `RebusDurableOutboxTransport` implementation of
    `IDurableOutboxTransport`
  - target-module to Rebus destination-address resolution
  - Bondstone-owned Rebus wire envelope for durable command payloads
  - Bondstone identity headers and W3C trace header mapping
  - DI registration for outgoing Rebus outbox transport
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
  cover PostgreSQL outbox dispatch lifecycle outcomes, public PostgreSQL inbox
  registration outcomes, and dispatcher composition against real PostgreSQL
  claim, lease renewal, and outcome-recording behavior. Rebus unit tests cover
  outgoing command transport routing, wire envelope mapping, header mapping,
  unsupported event envelopes, destination resolution, and DI registration.

## Active Rename Notes

- Historical source material used `DurableOperationSnapshot` for the read-model
  DTO and `DurableOperationState` for the enum. The extracted public contract
  uses `DurableOperationState` for the read-model DTO and
  `DurableOperationStatus` for the enum because callers read the current
  operation state and inspect its status, while `snapshot` sounds like a
  persisted projection or event-sourcing term.

## Next Candidate Slice

Continue worker and transport extraction with real verification before adding
broader provider APIs.

Candidate concepts:

- outbox stale-claim recovery orchestration and hosted worker composition built
  around the shared claim lease state, lease renewal, failure decision policy,
  dispatcher, outgoing Rebus command transport, and verified PostgreSQL claim
  and lifecycle implementations;
- inbox handler discovery, receive retry policy, stale receive recovery,
  transport acknowledgement coordination, module identity scopes, and
  higher-level transaction helper APIs only if required by real transport or
  sample behavior;
- fresh `dotnet restore` timeout investigation for the PostgreSQL provider
  dependency graph; no-restore build/test/pack and integration tests pass from
  current restored assets.

Worker design notes:

- Default to competitive workers that claim rows with provider-specific
  skip-locked or equivalent semantics. Do not require leader election or a
  singleton sweeper until load testing or real usage proves the need.
- Keep hosted-service registration separate from the plain outbox dispatcher.
- Outbox workers should compose the existing primitives: claimer, lease
  renewer, transport sender, failure policy, and dispatch recorder.
- Consider Brighter-style worker options when the hosted worker ADR is written:
  polling interval, batch size, lease duration, worker identity, and minimum
  message age.
- Keep archiving and cleanup separate from dispatch. Consider route or
  destination circuit breaking only after transport routing exists.
- Keep at-least-once delivery explicit. Consumers must use an inbox or
  idempotent handlers because duplicate sends remain possible.

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
- Add Rebus unit or transport-backed tests for adapter behavior.
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
- Dispatcher configuration binding, advanced retry policy, and dead-letter
  routing ownership.
- Partition-key ordering and scaling semantics.
- Hosted outbox worker loop and stale-claim recovery.
- Inbox handler discovery, stale receive recovery, receive-side retry policy,
  transport acknowledgement coordination, module identity scopes, and
  higher-level transaction helper APIs.
- Bondstone-owned migration helpers or provider-specific migration
  conventions.
- Operation-state transition policy and optimistic concurrency.
- Provider-specific dispatch behavior beyond PostgreSQL claiming.
- Rebus receive-side inbox integration and event publish/subscribe behavior.
- Additional integration tests with neutral Bondstone fixtures.
- Samples validating modular-monolith and service-split usage.

## ADR Checkpoints

Create or amend an ADR before widening implementation if any of these become
necessary:

- public API changes to durable send/result/trace contracts;
- operation-state transition policy beyond the current store contract;
- inbox/outbox entity shape and migration policy;
- advanced retry policy, dead-letter routing, or dispatcher ownership;
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
