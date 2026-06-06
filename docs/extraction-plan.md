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
  - `ICommand`
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
- Neutral hosted outbox worker:
  - `DurableOutboxWorker` hosted-service loop over
    `IDurableOutboxDispatcher`
  - worker id, lease duration, batch size, polling interval, and failure delay
    options
  - immediate backlog draining while rows are claimed
  - failure logging and delayed retry after unexpected dispatch failures
  - DI registration in `Bondstone.Hosting` for the worker and default
    dispatcher composition
- Fluent composition guardrails:
  - core `AddBondstone` builder
  - outbox persistence, transport, dispatcher, and worker capability tracking
  - PostgreSQL, Rebus, and Hosting builder extension methods
  - validation that hosted or dispatcher-based outbox processing has both
    persistence and transport capability
- Module command execution boundary:
  - `IBondstoneModule` for module-provided registration
  - module registration through `AddBondstone`
  - module metadata registry through `IBondstoneModuleRegistry`
  - `UseDurableMessaging` capability metadata on `BondstoneModuleBuilder`
  - `ICommand` base marker for module command pipeline execution
  - `IDurableCommand` as durable command specialization
  - `ICommandHandler<TCommand>` direct typed handlers
  - `ICommandValidator<TCommand>` validators
  - `IModuleCommandPipelineBehavior<TCommand>` pipeline hook
  - startup reflection registration for command handlers and validators
  - cached module command route metadata and scoped `IModuleCommandExecutor`
  - validation pipeline behavior before direct handler execution
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
- Cross-package AddBondstone host wiring smoke test:
  - preferred fluent composition with PostgreSQL persistence, Rebus transport,
    and hosted outbox worker
  - scoped dispatcher graph resolution without requiring live PostgreSQL or
    broker infrastructure
- Rebus receive-side inbox integration design:
  - command-only receive direction for Bondstone-owned Rebus wire envelopes
  - explicit handler identity and target-module inbox keys
  - handle-once execution through core inbox executor and caller-supplied
    commit delegate
  - Rebus acknowledgement after successful handled or already-processed
    outcomes
  - unresolved already-received rows surface through Rebus retry/dead-letter
    policy
- Rebus receive-side inbox adapter:
  - `IRebusDurableInboxHandlerExecutor` and default implementation
  - command wire-envelope mapping back to durable envelopes
  - low-level DI registration without persistence, EF Core, handler discovery,
    or Rebus endpoint configuration
  - fluent `AddBondstone` registration through `UseRebusInbox`
  - .NET `ActivityContext` validation for W3C `traceparent`
  - already-received exception for unresolved unprocessed inbox rows
  - unit coverage for handled, already processed, already received, unsupported
    events, missing target module, mapping, traceparent validation, and
    registration
- Cross-package Rebus receive composition smoke test:
  - preferred fluent composition with PostgreSQL persistence and
    `UseRebusInbox`
  - real Rebus receive adapter and EF persistence scope resolution
  - explicit `SaveChangesAsync` commit delegate without live PostgreSQL or
    broker infrastructure
- Rebus typed command receive pipeline design:
  - message-registry resolution from stable message type name to command CLR
    type
  - `System.Text.Json` payload deserialization
  - explicit stable handler identity at registration
  - receive-pipeline .NET/OTel consumer Activity from accepted W3C context
  - composition over the low-level Rebus inbox adapter and caller-supplied
    commit delegates
- Rebus typed command receive pipeline:
  - `IRebusTypedCommandReceivePipeline` and default implementation
  - message-registry validation for registered durable command types
  - `System.Text.Json` payload deserialization
  - receive ActivitySource and stable initial Activity tags
  - low-level and fluent DI registration
  - unit coverage for successful typed handling, missing registration,
    event-kind mismatch, generic type mismatch, invalid JSON, Activity tags,
    and registration
- Cross-package typed Rebus receive composition smoke test:
  - preferred fluent composition with PostgreSQL persistence and
    `UseRebusTypedCommandReceivePipeline`
  - default message registry, typed receive pipeline, Rebus inbox executor,
    and EF persistence scope resolution
  - explicit `SaveChangesAsync` commit delegate without live PostgreSQL or
    broker infrastructure
- Rebus receive transport-backed verification:
  - in-memory Rebus transport tests for `SendLocal` delivery of
    `RebusDurableMessageEnvelope`
  - typed receive pipeline execution through a real Rebus worker and handler
    activator
  - successful command deserialization, handler invocation, inbox delegation,
    commit delegate execution, and queue drain behavior
  - unknown message identity failure surfacing through Rebus retry/dead-letter
    behavior into the in-memory error queue
- Rebus PostgreSQL receive transport verification:
  - Testcontainers-backed PostgreSQL transport tests for real Rebus transport
    persistence
  - successful `SendLocal` delivery, typed receive handling, acknowledgement,
    and PostgreSQL transport-table drain behavior
  - unknown message identity failure moved to the PostgreSQL-backed Rebus
    error queue with one delivery attempt
- Rebus receive usage sketch:
  - current explicit receive wiring documented with normal Rebus handler,
    message registry setup, typed receive pipeline call, stable handler
    identity, and EF persistence-scope commit delegate
  - ergonomic pressure resolved into the module command execution boundary in
    ADR 0025 rather than more transport-local handler registration helpers
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
  claim, lease renewal, and outcome-recording behavior. Hosting unit tests
  cover hosted worker options, hosted worker loop behavior, DI registration,
  and builder guardrails. Rebus unit tests cover outgoing command transport
  routing, wire envelope mapping, header mapping, unsupported event envelopes,
  destination resolution, builder transport registration, and DI registration.

## Active Rename Notes

- Historical source material used `DurableOperationSnapshot` for the read-model
  DTO and `DurableOperationState` for the enum. The extracted public contract
  uses `DurableOperationState` for the read-model DTO and
  `DurableOperationStatus` for the enum because callers read the current
  operation state and inspect its status, while `snapshot` sounds like a
  persisted projection or event-sourcing term.

## Next Candidate Slice

The next work should continue the module command execution direction instead
of adding more transport-local receive ergonomics.

Candidate concepts:

- add module persistence registration that makes the module-owned DbContext
  and EF persistence scope explicit without coupling module code to host
  transport topology;
- add EF transaction pipeline behavior around `IModuleCommandExecutor` so
  validators, handlers, outbox staging, inbox markers, operation state,
  `SaveChangesAsync`, and commit happen in one module boundary;
- validate module durable-messaging capability where transport or persistence
  adapters require it;
- add source-module execution scope so `IDurableCommandSender` can derive the
  durable envelope source module from the active module context;
- bind host-owned Rebus receive topology to module command routes so Rebus
  dispatches into `IModuleCommandExecutor` instead of requiring per-command
  handler and commit delegates;
- keep durable commands outbox/inbox-backed; do not add a default local
  durable in-memory queue unless a later transport/testing adapter decision
  proves the need;
- use Wolverine as a useful comparison point for routing design: handler
  discovery can imply local handling, explicit routing should override
  convention, transport listening endpoints should be separate from handler
  registration, and diagnostics should explain why a command routes where it
  does;
- keep Rebus receive endpoint names, retry policy, and explicit
  local-vs-remote receive exposure in host topology configuration rather than
  module registration;
- keep outbox stale-claim recovery orchestration or advanced worker policy
  behind real sample or transport-backed usage.

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
- Stale-claim recovery, cleanup/maintenance workers, and advanced hosted
  worker policy.
- Inbox handler discovery, stale receive recovery, receive-side retry policy,
  transport acknowledgement coordination, module identity scopes, and
  higher-level transaction helper APIs.
- Bondstone-owned migration helpers or provider-specific migration
  conventions.
- Operation-state transition policy and optimistic concurrency.
- Provider-specific dispatch behavior beyond PostgreSQL claiming.
- Broader Rebus transport-level receive tests, higher-level typed handler
  registration helpers, and event publish/subscribe behavior.
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
