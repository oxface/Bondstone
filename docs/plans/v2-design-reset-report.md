# V2 Design Reset Report

Date: 2026-06-18
Status: ADR package accepted; durable inbox completion slice in progress

## Purpose

This report is the pre-implementation design reset for the v2 MVP. It is a
planning artifact, not a stable architecture contract. The durable decisions
are carried by ADRs 0018-0020, and the current behavior should later be
applied into stable docs as implementation lands.

The goal is a clean library model for module execution, durable messaging, and
one durable receive ledger. The current implementation slice is transitional:
RabbitMQ durable receive ingests into `incoming_inbox_messages`, but processing
still writes the older `inbox_messages` idempotency marker until a follow-up
cleanup removes that dependency. Compatibility with the transitional v1 public
surface is not a constraint for the v2 reset.

## Product Model

Bondstone v2 should be a .NET library for durable module boundaries, not a
general transport framework, workflow engine, code generator, or application
framework.

The product model has six cooperating surfaces:

- Module command pipeline: immediate same-process execution of registered
  module commands through Bondstone's module boundary. It owns handler
  resolution, module execution context, validation, provider transaction
  behavior, domain-event persistence hooks, outgoing outbox staging, and typed
  local results. It is not durable receive.
- Module query pipeline: immediate same-process read execution through a
  separate module boundary. It should respect module registration and
  persistence ownership, but it should not write inbox rows, outbox rows,
  operation state, or integration events. Queries are reads, not messages.
- Durable inbox: the target durable receive ledger in v2. Transport deliveries
  are ingested into this ledger before native settlement. A Bondstone worker
  later claims durable inbox rows and invokes the module command or event
  subscriber pipeline. The old tiny direct receive inbox should be removed,
  collapsed into the durable inbox implementation, or hidden until deleted.
- Durable outbox: the source-module ledger for outgoing durable commands and
  integration events. Source state and outgoing envelopes commit together in
  the source module persistence boundary. The outbox worker dispatches claimed
  rows through host-configured transport dispatchers and records dispatched,
  retry, stale, or terminal failure state.
- Operation observation: caller-visible operation status and optional result
  payloads for durable commands. Operation APIs support accepted-work
  responses, status reads, result reads, and short edge-facing waits. They are
  not orchestration, saga state, or durable continuations.
- Host-owned transport infrastructure with module-aware bindings: the host
  owns queues, topics, subscriptions, exchanges, rules, credentials, prefetch,
  broker retry, dead-letter policy, workers, and deployment topology.
  Bondstone owns durable module semantics: contracts, stable identities,
  module persistence boundaries, outbox rows, durable inbox rows, command
  handlers, event subscriber handlers, and operation finalization semantics.

## Execution Sequences

### HTTP Immediate Command Execution

Use this when the HTTP request should execute a module command now and return
a typed result or immediate command outcome.

```text
HTTP endpoint
  -> IModuleCommandExecutor.ExecuteAsync/ExecuteResultAsync(module, command)
  -> module command route lookup
  -> provider transaction runner for the target module
  -> module execution context = target module
  -> command validators for that module and command
  -> ICommandHandler<TCommand> or ICommandHandler<TCommand, TResult>
  -> optional outgoing durable sends/publishes staged in target module outbox
  -> provider post-handler actions
  -> commit target module state and staged outbox rows
  -> return ModuleCommandExecutionResult<TResult> or command result metadata
```

No durable inbox row is written. If code calls the handler directly instead of
the executor, Bondstone is bypassed.

### HTTP Durable Command Ingress

Use this when the HTTP request should accept durable work and return an
operation id or accepted-work metadata before handler execution.

```text
HTTP endpoint
  -> create or accept durable operation id
  -> build durable command envelope for target module
  -> resolve target module command route and stable handler identity
  -> durable inbox ingestion boundary for the target module
  -> insert or return idempotent durable inbox row in Pending state
  -> optionally stage Pending operation state according to accepted ADR/API
  -> commit durable inbox ingestion
  -> return 202 Accepted with operation id and observation link/metadata

Durable inbox worker
  -> claim due durable inbox row
  -> invoke module command receive/command execution pipeline
  -> handler commits target state, outgoing outbox rows, and operation result
  -> record durable inbox processed/retry/terminal outcome
```

This is durable receive without pretending HTTP is a broker, and it is not a
source module outbox send. The host owns the HTTP endpoint shape; Bondstone may
provide small helpers, but should not own the web application model.

### Module-To-Module Direct Command Execution Through `.Contracts`

Use this only for explicit immediate same-process collaboration that should
cross Bondstone's module command boundary.

```text
Source module or host code references Target.Contracts
  -> constructs target command DTO
  -> calls IModuleCommandExecutor for the target module
  -> Bondstone runs target module command pipeline
  -> target module state commits or rolls back in its own boundary
  -> typed result returns to caller
```

Contracts projects share DTOs and stable identities. They do not imply raw
handler construction. Direct handler calls bypass Bondstone and therefore
bypass module execution context, validation, durable send/publish context,
provider transactions, operation finalization, diagnostics, and inbox/outbox
coordination.

Cross-module direct execution from inside an active handler is allowed only
through Bondstone's module command pipeline. It is immediate same-process work,
not durable receive, not durable orchestration, and not atomic across source
and target module persistence boundaries. Handlers that need restart-safe
coordination should use durable commands or integration events with app-owned
process state until a future saga/process-manager ADR accepts a native
abstraction.

### Module-To-Module Durable Command Send

Use this when the target module work must survive process restart, transport
retry, or service extraction.

```text
Source module command/event handler
  -> IDurableCommandSender.SendAsync(command, targetModule, operationId?)
  -> source module execution context supplies source module
  -> serialize payload and stable message identity
  -> stage command envelope in source module outbox
  -> source handler state and outbox row commit atomically

Outbox worker
  -> claim source outbox row
  -> dispatch envelope through host-configured transport route
  -> record dispatched, retry, stale, or terminal failure

Transport receive/ingestion
  -> native delivery parsed as DurableMessageEnvelope
  -> resolve target module command route and stable handler identity
  -> ingest into target module durable inbox
  -> settle native delivery after durable ingestion succeeds

Durable inbox worker
  -> claim target durable inbox row
  -> execute target module command pipeline
  -> commit target state, outgoing outbox rows, and operation result
  -> record durable inbox processed/retry/terminal outcome
```

Durable send accepts work. It does not return the target handler result
directly. Results are observed through operation APIs when the command and
caller use an operation id.

### Integration Event Fanout

Use this when a module publishes a durable fact that zero or more subscribers
may consume independently.

```text
Source module handler
  -> IDurableEventPublisher.PublishAsync(event)
  -> serialize event with stable integration event identity
  -> stage event envelope in source module outbox
  -> source state and outbox row commit atomically

Outbox worker
  -> claim event outbox row
  -> dispatch envelope to host-owned event topology
     (exchange/topic/routing chosen by host transport configuration)
  -> record outbox outcome

Broker/native infrastructure
  -> fans out event to configured queues/subscriptions
  -> each queue/subscription represents one app-owned subscriber binding

Transport receive/ingestion per subscriber
  -> parse envelope
  -> host binding supplies subscriber module and subscriber identity
  -> ingest one durable inbox row for that subscriber
  -> settle native delivery after durable ingestion succeeds

Durable inbox worker
  -> claim subscriber durable inbox row
  -> execute registered IIntegrationEventHandler<TEvent>
  -> commit subscriber module state and outgoing outbox rows
  -> record durable inbox processed/retry/terminal outcome
```

Fanout is provider-native topology, not a Bondstone subscription store.
Subscriber module and subscriber identity are Bondstone durable receive
identity. Exchanges, topics, subscriptions, queues, rules, and bindings remain
host-owned.

### Durable Inbox Ingestion And Processing

The v2 durable receive ledger should be a single state machine around receive
attempts:

```text
native delivery
  -> deserialize DurableMessageEnvelope
  -> validate message kind and stable message type identity
  -> resolve receive binding:
       command: target module + stable handler identity
       event: subscriber module + stable subscriber identity
  -> insert durable inbox row if absent
       status = Pending
       key = message id + module + handler/subscriber identity
       envelope fields stored structurally
  -> commit receiver module durable inbox persistence
  -> settle native delivery

durable inbox worker
  -> claim due Pending/Retry/expired Processing rows
  -> mark Processing with claimant, lease, and attempt count
  -> invoke command receive or event subscriber receive pipeline
  -> record Processed when module pipeline succeeds or reports duplicate done
  -> schedule Retry when policy says the failure is retryable
  -> record TerminalFailed when policy exhausts receive attempts
  -> report stale outcome when claim ownership or lease no longer matches
```

The durable inbox row is operational evidence. Terminal receive failure should
not automatically write operation `Failed`; application policy should decide
whether that evidence is sufficient to finalize a caller-visible operation.

### Operation Status/Result Observation

Operation observation answers "what is known about accepted durable work?"
It should not be used as a durable control-flow primitive inside handlers.

```text
durable ingress or durable send
  -> caller supplies or receives operation id
  -> operation starts as accepted/pending when configured

target module durable command processing
  -> command handler runs through target module pipeline
  -> result command returns TResult
  -> operation state/result is stored in target module transaction

edge/API/UI/test observer
  -> read by DurableOperationHandle or operation id + module hint
  -> receive status: unknown, pending, running, completed, failed, cancelled
  -> receive result classification:
       completed with result
       completed without result
       deserialization failure
       not terminal yet
  -> optionally perform short timeout-bounded wait
```

Waiting is caller patience, not stored operation state. Durable inter-module
continuations should use explicit integration events and app-owned process
state until a future saga/process-manager ADR exists.

### Query Execution

Queries should be a separate, synchronous, boundary-respecting read path.

```text
HTTP endpoint, UI service, test, or module-facing app code
  -> IModuleQueryExecutor.ExecuteAsync<TQuery, TResult>(module, query)
  -> module query route lookup
  -> optional provider read scope for the target module
  -> module execution context = target module only if needed for diagnostics
  -> IQueryHandler<TQuery, TResult>
  -> return TResult
```

Queries should not be durable messages. They should not stage outbox rows,
write inbox rows, create operation state, publish integration events, or imply
local read projections. Query handlers should be allowed to use ordinary
read-optimized persistence patterns chosen by the module.

## Explicit Boundaries

The v2 MVP should explicitly exclude:

- Bondstone-owned sagas, process managers, durable workflows, automatic
  command chaining, and persisted continuations.
- Broker topology ownership, including exchange, queue, topic, subscription,
  binding, rule, retry, dead-letter, credential, prefetch, and concurrency
  configuration.
- Polling another module's operation store as a supported orchestration
  mechanism. Operation reads are for observation, not control flow.
- Code generation for modules, endpoints, contracts, handlers, migrations, or
  broker topology.
- A default cleanup or retention worker. Cleanup mutates durable evidence and
  should remain app-owned unless a later ADR accepts an explicit mutation
  surface.
- Direct handler calls as a Bondstone path. They are application code and
  bypass Bondstone.
- Treating local transport as broker durability or production topology
  guidance.
- Automatic operation failure inference from outbox terminal failure, durable
  inbox terminal failure, broker retry exhaustion, or dead-letter state.

## Proposed Implementation Chunks

### 1. Durable Inbox Completion

Status: complete for the v2 MVP durable-ingestion path as of 2026-06-18.

- Make durable inbox the only v2 durable receive ledger target.
- Collapse or remove the tiny direct receive inbox path from public product
  language.
- Finish ingestion, claim, lease renewal, retry, terminal failure, inspection,
  and processing semantics around the single ledger.
- Ensure handler state, operation completion/result, outgoing outbox rows, and
  receive completion commit coherently inside the target module boundary where
  provider capabilities allow.
- Decide whether current `DurableIncomingInbox*` API names remain as internal
  implementation vocabulary, are renamed, or are aliased for v2.
- Update samples and migrations after the ledger shape is final.

### 2. Command/Query Pipeline Cleanup

- Keep module command execution as the direct immediate command path.
- Add the module query contract, registration, handler, executor, diagnostics,
  and tests in one coherent slice.
- Implement the accepted direct cross-module command rule: pipeline-only,
  immediate, non-durable, non-orchestrating, and not atomic across module
  persistence boundaries.
- Keep application middleware concerns outside Bondstone's public runtime
  pipeline.
- Add small HTTP helper APIs only if they stay library-shaped and do not own
  endpoint architecture.

### 3. Operation Observation Cleanup

- Reposition operation docs and API naming around observation.
- Keep short waits as edge-facing convenience, not workflow composition.
- Prefer handle-based or module-hinted reads over global store scans in new
  examples.
- Remove examples that suggest operation waiting inside module handlers.
- Add troubleshooting language that ties pending operations to source outbox,
  durable inbox, target operation state, and app-owned finalization policy.

### 4. Transport/Fanout Ergonomics

- Keep topology host-owned and provider-native.
- Improve receive worker registration so command queues and event subscriber
  queues/subscriptions read as module-aware bindings.
- Keep event fanout on native exchanges/topics/subscriptions.
- Decide Service Bus durable inbox ingestion parity with RabbitMQ.
- Keep route-aware outbound dispatch below the topology line: one envelope
  must match exactly one route, but Bondstone should not provision or validate
  provider-native topology.

### 5. Worker Operations/Retention

- Finish durable inbox worker lease, retry, stale, and terminal-failure
  behavior.
- Add inspection ergonomics and operations recipes for durable inbox pending,
  processing, retry, stale, and terminal rows.
- Keep operation expiration scheduling app-owned.
- Keep cleanup and retention app-owned; document example policies rather than
  registering a default cleanup worker.
- Add health/readiness recipes that distinguish accepting traffic from
  operator investigation.

### 6. Final Docs/API/Sample Cleanup

- Treat ADRs 0018-0020 as accepted and pending before broad implementation.
- Apply accepted decisions into stable docs only after behavior exists.
- Remove stale direct-receive-as-default language from setup, architecture,
  operations, samples, and public API docs once implementation changes land.
- Reset sample migrations before v2 publication.
- Re-run public API review, package validation or ApiCompat, full tests, docs
  review, and sample verification.

## Accepted ADR Boundaries

### ADR 0018: V2 Module Execution And Durable Inbox Reset

ADR 0018 accepts the single durable inbox receive model, the immediate module
command pipeline, the separate synchronous query pipeline, and HTTP durable
ingress into the target module durable inbox. Direct cross-module command
execution is pipeline-only immediate work and is not durable orchestration or
an atomic multi-module transaction.

### ADR 0019: Operation Observation Not Orchestration

ADR 0019 accepts operation ids supplied by callers or generated at the
accepting edge. Operation observation APIs are for edge endpoints, UI refresh
loops, operator tools, tests, and app-owned schedulers. Module handlers should
not poll or wait on another module's operation state as durable business flow.

### ADR 0020: Host-Owned Transport With Module-Aware Bindings

ADR 0020 accepts host-owned transport registration with module-aware receive
bindings. Module registrations declare commands, published events,
subscribers, stable identities, and persistence boundaries; they do not declare
broker endpoints or active native workers. Bondstone will not add a
provider-neutral subscription store for integration event fanout in the v2 MVP.
Local transport routing helpers remain limited to samples, tests, and local
development.

## Maintainer Review Checklist

- Treat Service Bus durable inbox handoff as outside the built-in Service Bus
  worker for the v2 MVP. Apps can own native Service Bus receive loops and call
  durable inbox ingestion explicitly.
- Confirm that no default cleanup worker, code generation, broker topology DSL,
  or saga/process-manager work enters the v2 MVP implementation chunks.

## Implementation Progress

2026-06-18 durable inbox completion slice:

- `ApplyBondstonePersistence(...)` now maps the durable incoming inbox table so
  normal EF durable module mappings can host durable receive rows.
- PostgreSQL module persistence now contributes module incoming inbox
  dispatchers behind the app-facing `IDurableIncomingInboxDispatcher`, allowing
  the hosted incoming worker to process module-owned incoming rows.
- RabbitMQ `ReceiveCommand()` and `ReceiveEvent(...)` now ingest broker
  deliveries into the durable incoming inbox before ack; the explicit
  `Ingest*ToDurableIncomingInbox(...)` methods remain aliases.
- Provider-backed tests now prove durable inbox ingestion/processing through
  PostgreSQL module persistence, including command handler execution,
  operation result completion, outgoing outbox staging, duplicate ingestion
  without handler rerun, retry outcome, and terminal failure outcome.
- Stable docs now describe durable inbox as the incoming durable receive
  ledger for implemented durable-ingestion paths and explicitly call out that
  the old direct receive `inbox_messages` row remains a temporary
  implementation-detail idempotency marker during processing.

Residual behavior:

- Incoming-ledger `Processed`, `RetryScheduled`, and `TerminalFailed` outcomes
  are still recorded after the module receive pipeline returns. If the process
  crashes after the module transaction commits handler state, operation result,
  outgoing outbox rows, and the implementation-detail idempotency marker, but
  before the incoming row is marked processed, a later retry catches the
  incoming ledger up through the idempotency marker's already-processed
  result.
- Durable incoming inbox processing still depends on the older
  `inbox_messages` table for module receive idempotency. This is accepted for
  v2 MVP as an implementation-detail marker. Removing it later requires a
  smaller internal execution path where the claimed incoming inbox row is the
  receive idempotency boundary while preserving the module EF transaction for
  handler state, operation state, domain events, and outgoing outbox rows.
- The built-in Azure Service Bus receive worker remains a direct receive
  adapter. Durable inbox ingestion with Service Bus is app-owned through a
  native receive loop and the durable inbox ingestion boundary.
- Public `DurableIncomingInbox*` names remain the current provider/runtime
  vocabulary for the v2 durable inbox ledger; no rename is required for the v2
  MVP.

Final design check:

- The three-worker topology is accepted for durable receive: outbox dispatch,
  transport receive/ingestion, and incoming inbox processing.
- Host-owned transport and module-aware durable runtime behavior remain the
  guiding split.
- The durable inbox model is clean enough to build the next v2 features on top
  of it. The known internal complexity is the temporary receive idempotency
  marker used during module processing.

## Verification

`pnpm format:check` passed on 2026-06-18.

Durable inbox completion focused verification passed on 2026-06-18:
`dotnet test tests/Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests/Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests.csproj --configuration Release --filter "FullyQualifiedName~PostgreSqlOutboxDispatcherTests|FullyQualifiedName~PostgreSqlPersistenceTests"`.

Broader backend verification passed on 2026-06-18: `pnpm backend:test`.
