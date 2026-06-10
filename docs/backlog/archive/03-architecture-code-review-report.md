# Phase 03 Architecture And Code Review Report

Date: 2026-06-10

## Status

Phase 03 completed the first architecture and code sweep. The source tree
matches the stable documentation direction: direct provider adapters replaced
Rebus source packages, core owns provider-neutral durability contracts,
providers own SQL/native broker behavior, and the sample proves mixed module
persistence with explicit local and RabbitMQ transport paths.

This report is a human review plan for the next round. It records the current
shape, safe fixes made during the sweep, review questions, risks, and
verification commands by architecture slice.

## Repository Map

### Package Projects

- `src/Bondstone`: core configuration, messaging identity/serialization,
  module registration/execution, durable inbox/outbox/operation contracts, and
  provider-neutral dispatch orchestration.
- `src/Bondstone.Hosting`: reusable hosted outbox worker over
  `IDurableOutboxDispatcher`.
- `src/Bondstone.EntityFrameworkCore`: provider-neutral EF Core mappings,
  durable stores, module transaction behaviors, and EF persistence scope.
- `src/Bondstone.EntityFrameworkCore.Postgres`: PostgreSQL-specific EF Core
  registration, inbox registrar, outbox claiming, lease renewal, and dispatch
  recording.
- `src/Bondstone.Persistence.Postgres`: PostgreSQL-specific non-EF persistence
  backed by Npgsql/Dapper internally.
- `src/Bondstone.Transport.Local`: explicit local queue route adapter for
  samples, tests, and local development.
- `src/Bondstone.Transport.RabbitMq`: direct RabbitMQ outbox, receive topology,
  mapper, dispatcher, settlement helper, topology validation, and opt-in
  hosted receive worker.
- `src/Bondstone.Transport.ServiceBus`: direct Azure Service Bus outbox,
  receive topology, mapper, dispatcher, settlement helper, topology
  validation, and opt-in hosted receive worker.

### Test Projects

- `tests/Bondstone.Tests`: core messaging, module, persistence, configuration,
  and utility behavior.
- `tests/Bondstone.Composition.Tests`: composed `AddBondstone` validation and
  cross-package registration checks.
- `tests/Bondstone.Hosting.Tests`: outbox worker registration, options, and
  loop behavior.
- `tests/Bondstone.EntityFrameworkCore.Tests`: EF mapping, stores, operation
  state, transaction behavior, and EF scope tests.
- `tests/Bondstone.EntityFrameworkCore.Postgres.Tests`: PostgreSQL EF provider
  integration tests for inbox/outbox SQL and provider registration.
- `tests/Bondstone.Persistence.Postgres.Tests`: non-EF PostgreSQL integration
  tests for schema, transactions, inbox, outbox, and operation state.
- `tests/Bondstone.Transport.Local.Tests`: local route adapter behavior.
- `tests/Bondstone.Transport.RabbitMq.Tests`: RabbitMQ routing, dispatch, and
  provider-backed receive worker tests.
- `tests/Bondstone.Transport.ServiceBus.Tests`: Service Bus destination,
  dispatch, and emulator-backed receive worker tests.
- `tests/Bondstone.Samples.Tests`: modular monolith PostgreSQL and RabbitMQ
  adoption smoke tests.

### Samples

- `samples/ModularMonolith`: minimal API host and app composition.
- `samples/ModularMonolith.Ordering` and `.Contracts`: EF-backed ordering
  module, command sender, event publisher, and subscriber projection.
- `samples/ModularMonolith.Fulfillment` and `.Contracts`: EF-backed
  fulfillment command handler, event publisher, and subscriber projection.
- `samples/ModularMonolith.Billing`: non-EF PostgreSQL billing subscriber
  module.

### Stable Docs

- `docs/architecture/`: current runtime contracts for messaging, modules,
  hosting, persistence, transports, topology validation, retry boundaries, and
  diagnostics.
- `docs/packaging.md`: package IDs, dependency direction, target framework,
  release, and publishing policy.
- `docs/setup.md`: user-facing host composition examples.
- `docs/testing.md`: category policy and verification entrypoints.
- `docs/samples.md`: current sample ownership and constraints.
- `docs/backlog/15-future-work.md`: non-current follow-up ideas.

## High-Level Sweep

Package dependencies match `docs/packaging.md`: core has no provider,
transport, or hosting dependency; hosting depends only on core plus hosting
abstractions; provider and transport packages depend on core and their native
SDKs; samples compose packages without pushing sample domain behavior into
library projects.

No Rebus source package or active Rebus project reference remains. Rebus text
appears in ADR decision history and generated local build output only. The
old generated `tests/Bondstone.Transport.Rebus.Tests/obj` directory should
stay uncommitted and may be cleaned locally when convenient.

The public API surface is useful but broad. Many concrete implementation
classes are public so advanced consumers and tests can compose low-level
pieces directly. Any tightening of public surface, namespace ownership, or
compatibility guarantees should go through ADR work because packages have
already been published.

## Reviewer Feedback Intake

The 2026-06-10 human review raised the following design questions. These are
not implementation instructions by themselves; use them to drive backlog
refinement or ADR work before broad code changes.

### Module Persistence Metadata

- `PersistenceProviderName` currently does more than diagnostics. It marks
  that the module uses persistence, lets provider transaction behaviors decide
  whether they own the current module execution, and improves missing-service
  diagnostics. This is workable but stringly typed.
- `PersistenceContextType` is mostly EF-specific metadata stored on the
  provider-neutral module registration. It exists so the EF transaction runner
  can resolve and validate the module DbContext. Non-EF providers do not need
  equivalent CLR context metadata, which makes the general module registration
  shape leaky.
- Follow-up: consider a module capability registry or provider-owned
  persistence metadata model keyed by module name. Resolvers could expose
  segregated interfaces while sharing one backing registry. This would reduce
  repeated "get module, find registration, throw module-aware diagnostic"
  code and keep EF-specific metadata out of general module state.
- ADR: required if the public module registration shape or provider metadata
  contract changes.

### Fallback Persistence Services

- `DurableModuleOutboxWriterResolver`, inbox executor resolution, and
  operation-state resolution still accept fallback non-module services. This
  supports low-level and older single-store composition paths used by tests and
  advanced setup. It is not the preferred module-owned path.
- Follow-up: decide whether fallback services remain a supported advanced API
  or should be retired. Removing them is a compatibility decision and needs
  ADR review.

### Module Outbox Dispatch Aggregation

- `DurableModuleOutboxDispatchAggregator` dispatches module outboxes
  sequentially, sharing a single batch budget across module dispatchers.
  Claiming, locking, lease renewal, transport send, and outcome recording are
  performed inside each module dispatcher.
- Risks: one slow or noisy module can consume the shared worker loop. A hung
  provider call relies on cancellation and provider timeouts. Parallel module
  dispatch and per-module workers were deliberately deferred, but the current
  aggregate worker is not a fairness mechanism.
- Follow-up: keep the current simple aggregate worker for now, but capture
  per-module workers, per-module concurrency, dispatch timeouts, and noisy
  neighbor isolation as future worker-design work.
- ADR: required before changing default worker topology, concurrency, or
  fairness semantics.

### Registration Validators And Organization

- `DurableModulePersistenceRegistrationValidator` is a small uniqueness
  helper. The repository has several validators with different lifecycles:
  argument guards, options validators, composed `AddBondstone` configuration
  validators, provider topology validators, and service-registration
  validators.
- Follow-up: consider folders or naming conventions that group validators by
  lifecycle rather than by generic "validator" terminology. This is safe
  cleanup if it stays internal and mechanical; public type movement requires
  compatibility review.

### Outbox Retry, Dead-Letter, And Infrastructure Boundaries

- Bondstone's outbox retry/dead-letter state is for outgoing records that have
  been persisted locally and claimed for dispatch. It handles "we could not
  hand this claimed outbox record to the configured transport" cases, such as
  broker outage, route failure, or transport send exception.
- Provider receive retry and broker dead-letter policy are separate. RabbitMQ
  negative acknowledgement and Service Bus abandon/complete hand off receive
  retry and DLQ behavior to broker-native infrastructure.
- The persisted outbox `DeadLettered` status means Bondstone stopped retrying
  the outgoing local record. It does not mean Bondstone created or wrote to a
  broker DLQ.
- Follow-up: docs and names should keep this distinction extremely explicit.
  Consider whether `DeadLettered` is the right public word for outbox terminal
  failure, or whether `TerminalFailure`/`FailedTerminally` would avoid broker
  DLQ confusion. Renaming persisted/public statuses requires ADR review.
- ADR: required before changing outbox retry/dead-letter terminology,
  schedule, attempt accounting, or terminal failure policy.

### Direct SQL Versus DbContext State

- PostgreSQL EF provider claim, inbox registration, lease renewal, and
  dispatch-record updates use direct SQL because they rely on PostgreSQL
  semantics such as `INSERT ... ON CONFLICT DO NOTHING`, `FOR UPDATE SKIP
LOCKED`, claim-owner and lease-aware conditional updates, and immediate row
  count outcomes. These operations intentionally do not wait for EF
  `SaveChangesAsync`.
- This is separate from handler state changes. Handler code may use EF and can
  call `SaveChangesAsync`, but Bondstone's preferred durable boundary is still
  the module transaction behavior committing handler state, inbox markers,
  operation state, and outgoing outbox rows together.
- Follow-up: compare EF-backed and non-EF PostgreSQL behavior for mid-handler
  savepoints, explicit `SaveChangesAsync`, transaction reuse, and failure
  rollback. Document any intentional differences.

### Inbox Commit Delegate And Naming

- `DurableInboxHandlerExecutor.HandleOnceAsync` accepts a commit delegate so
  the core primitive can stage the processed marker and let the provider
  commit its transaction boundary at the right point. Current module pipeline
  behaviors often pass a no-op because provider transaction behaviors own the
  commit at the outer boundary.
- This shape is flexible but confusing because commit ownership is split
  between a low-level primitive and module transaction behaviors.
- `AlreadyReceived` means an inbox row exists without a processed timestamp;
  operationally it is "received/in progress or stale" and remains loud because
  Bondstone has no accepted stale receive recovery model.
- Follow-up: review whether commit should move out of the core inbox executor
  into provider/module transaction behaviors, or whether the API should make
  commit ownership clearer. Also consider better names for `Registrar`; it is
  neither just a registry nor just a store. "Store" is acceptable for durable
  persistence operations, but registrar might become "InboxReceiptRecorder" or
  similar if the concept is renamed.
- ADR: required before changing inbox recovery, stale receive handling, or
  public persistence contract names.

### Commands, Events, And Shared Message Mechanics

- Keep commands and integration events separate at the public behavior level.
  The explicit split is a Bondstone advantage over more magical bus/mediator
  surfaces because commands target one module and events publish facts to
  independently identified subscribers.
- There is real duplication in neutral receive pipelines, transport dispatch,
  local transport, send/publish staging, and provider envelope mapping. Some
  shared "message mechanics" helpers may be useful, as long as they do not
  erase command/event semantics.
- Follow-up: look for internal helpers such as a message-kind resolver,
  envelope dispatch helper, or provider route abstraction that centralize
  switch statements without creating a generic message bus. Do this only where
  duplication is genuinely maintenance-heavy.

### Receive Pipeline And Executor Naming

- `ModuleCommandReceivePipeline` and `ModuleEventReceivePipeline` can be
  confused with `PipelineBehavior` types. They are provider-neutral receive
  entrypoints, while pipeline behaviors are handler execution middleware.
- `ModuleCommandExecutor` always resolves a module command route. That is
  currently compatible with HTTP command execution if HTTP commands are
  registered module commands, but it is intentionally not a generic mediator
  for arbitrary in-process calls.
- Follow-up: consider names such as receive "dispatcher" or "orchestrator" for
  provider-neutral receive entrypoints. If HTTP command routing becomes a
  first-class scenario, decide whether it uses the same module command route
  registry or needs a distinct non-durable command execution contract.
- ADR: required before adding mediator-like HTTP command routing as a default
  Bondstone feature.

### Execution Context Accessor

- `ModuleExecutionContextAccessor` uses `AsyncLocal` to make the current module
  available to durable send/publish APIs without threading context through
  every handler call. This is common in .NET request/execution context code,
  but it is ambient state and should be treated carefully.
- Risks: ambient context can surprise code that starts parallel work, queues
  work outside the handler flow, suppresses execution context, or calls
  send/publish after the pipeline scope is disposed.
- Follow-up: keep the current accessor for ergonomic handler APIs, but
  evaluate explicit context alternatives before expanding it. Options include
  injecting a module-scoped command/event client, passing source-module context
  explicitly into lower-level APIs, or exposing a provider-owned execution
  context object to handlers.
- ADR: required before changing public send/publish context semantics.

### Transport Receive Settlement Helpers

- `IRabbitMqReceivedMessageHandler` and `IServiceBusReceivedMessageHandler`
  accept settlement delegates so app-owned consumers can keep native settlement
  ownership while Bondstone guarantees "settle only after dispatch succeeds."
  This is useful, but the delegate shape reads like a workaround.
- Follow-up: consider an extra lower-level receive service, separate from
  `BackgroundService`, that app code or custom schedulers can call with native
  message abstractions. The hosted worker would become one consumer of that
  service. Keep native provider types visible rather than inventing a generic
  bus abstraction.

### Local Transport Positioning

- Local transport can be useful in non-production environments beyond a
  developer laptop. It can technically run in production, but it provides no
  broker durability, network isolation, broker retry, or broker DLQ behavior.
- Follow-up: when package README files are added or expanded, make
  `Bondstone.Transport.Local` carry a prominent warning that it is an explicit
  in-process adapter, not production broker durability or a hidden fallback.

### Operation State, Diagnostics, And Observability

- Operation-state defaults beyond `Pending` and successful command
  `Completed` remain intentionally unaccepted. Handler failure, running state,
  cancellation, timeout, result payloads, and retry-state projection need ADR
  work.
- Diagnostics and telemetry should grow from real failure modes. Do not make
  tracing/logging/metrics a stable compatibility contract until Bondstone has
  used the library in a real project and learned which diagnostics are useful
  rather than noisy.
- Follow-up: add diagnostics when they answer a concrete operational question.
  Tests can then lock the behavior that matters.

### Easy Deploy And Provisioning Helpers

- Easy deploy helpers for persistence and transport setup remain possible, but
  they should be explicit opt-in helpers, not default behavior. Production
  schema migration and broker topology ownership stay app/provider-native
  under current docs.
- ADR: required before adding production-oriented migrators, broker
  provisioning, or deployment automation as supported Bondstone behavior.

## Decision Resolution Pass 1

This pass classifies the feedback above into current decisions, small
documentation cleanups, ADR candidates, and deferred discovery work. The
intent is to keep implementation agents from treating every review question as
an immediate code-change instruction.

### Resolve Now In Docs Or Backlog

- Outbox retry boundary: keep Bondstone-owned retry and terminal failure for
  outgoing persisted outbox records. Keep broker receive retry and DLQ policy
  provider/app-owned. Clarify wording wherever `DeadLettered` could be read as
  a broker DLQ operation.
- Direct PostgreSQL SQL: keep direct SQL for provider-owned concurrency,
  idempotency, claim, lease, and row-count outcomes. EF `DbContext` state
  remains the app handler state mechanism; provider SQL remains the durable
  infrastructure mechanism.
- Local transport positioning: keep local transport as an explicit in-process
  adapter that may be useful outside a developer laptop but is not production
  broker durability. Add prominent package README warning when package READMEs
  are introduced.
- Diagnostics discipline: keep telemetry/logging growth demand-driven. Add
  diagnostics when a real failure mode or consumer confusion appears; do not
  stabilize a broad observability contract yet.
- Validator organization: treat folder/naming cleanup as internal repository
  hygiene unless it moves public types or changes validation lifecycle.

### Keep Current Behavior For Now

- Fallback persistence services: keep fallback non-module services as an
  advanced/compatibility path for now. Preferred setup remains module-owned
  persistence. Removing fallback behavior requires ADR review.
- Sequential module outbox aggregation: keep the aggregate worker simple and
  sequential for now. It is acceptable as the default first worker, but it is
  not a fairness or isolation story.
- Inbox commit delegate: keep the current low-level commit delegate until a
  broader inbox contract cleanup is accepted. Provider transaction behaviors
  can continue passing no-op commits when the outer module transaction owns the
  final commit.
- Command/event split: keep commands and integration events explicitly
  separate in public behavior. Internal helpers may remove real duplication
  only when they do not introduce a generic bus/mediator abstraction.
- Ambient module execution context: keep `AsyncLocal` execution context for
  ergonomic durable send/publish APIs. Treat it as a scoped execution-context
  mechanism, not a general background-work context.
- Native receive settlement delegates: keep settlement delegates in the small
  handler helpers because they preserve app-owned native settlement while
  enforcing "settle only after successful dispatch."

### ADR Candidates

- Module persistence metadata registry: decide whether to replace
  `PersistenceProviderName`/`PersistenceContextType` on general module
  registration with provider-owned module capability metadata and shared
  resolver infrastructure.
- Outbox terminal failure terminology: decide whether public/persisted
  `DeadLettered` should remain, be documented as Bondstone terminal outbox
  failure, or be renamed before stronger compatibility promises.
- Inbox stale receive recovery: decide if Bondstone should add inbox leases,
  stale receive recovery, maintenance workers, or app-owned recovery hooks for
  `AlreadyReceived` rows.
- Module worker topology: decide whether to add per-module workers,
  per-module concurrency settings, dispatch timeouts, or noisy-neighbor
  isolation beyond the aggregate worker.
- Execution context alternatives: decide whether durable send/publish should
  gain explicit source-module APIs, module-scoped clients, or another
  non-ambient path before broadening usage.
- Public API tightening: decide which concrete implementation classes remain
  intentionally public and which should be hidden or marked advanced before
  compatibility expectations harden.
- Easy deploy helpers: decide if explicit opt-in schema/broker provisioning
  helpers are part of Bondstone's supported surface, while keeping default
  production setup app-owned.

### Defer Until Real-Project Pressure

- Stable tracing/logging/metrics names, tags, event ids, and counters.
- Whether provider-neutral receive entrypoints should be renamed away from
  "pipeline" terminology.
- Whether `Registrar`, `Store`, or a new term should become the persistence
  vocabulary for inbox receipt/idempotency operations.
- Whether shared internal message-kind dispatch helpers are worth the
  abstraction cost.
- Whether Service Bus needs a full setup snippet parallel to the RabbitMQ
  example.

### Suggested ADR Order

1. Outbox terminal failure terminology and retry boundary.
2. Module persistence metadata and capability registry.
3. Inbox stale receive recovery and commit ownership.
4. Module outbox worker topology and noisy-neighbor isolation.
5. Execution context alternatives for durable send/publish.
6. Public API surface and advanced composition policy.
7. Explicit deploy/provisioning helpers.

## Review Plan By Slice

### Modular Persistence And Package Boundaries

Purpose and design: Modules declare durable messaging and a persistence
provider. Core validates duplicate module-owned persistence registrations and
resolves writers, inbox executors, dispatchers, and operation stores by module
name.

Important files and tests: `BondstoneModuleRegistration.cs`,
`DurableModuleOutboxWriterResolver.cs`,
`DurableModuleInboxHandlerExecutorResolver.cs`,
`DurableModuleOutboxDispatchAggregator.cs`,
`DurableModuleOperationStateStoreResolver.cs`,
`DurableModulePersistenceRegistrationValidator.cs`,
`tests/Bondstone.Tests/Persistence`, and
`tests/Bondstone.Composition.Tests`.

Review questions: Are diagnostics clear enough when one module is missing a
provider-owned service? Should advanced low-level registration remain public
as-is now that packages are published?

Risks or smells: Public implementation surface is wider than a minimal user
API. Changing it is a compatibility decision.

Recommended next actions: Add an API baseline/review artifact before public
surface tightening. Route compatibility decisions through ADR.

Verification: `pnpm backend:test -- --filter` is not a current script shape;
use `pnpm backend:test:fast` for fast coverage and `pnpm backend:pack` for
package surface smoke.

### Outbox Writing, Claiming, Dispatch, Retry, And Terminal Failure

Purpose and design: Sends/publishes stage `DurableMessageEnvelope` records in
the source module outbox. Provider claimers mark due rows as `Processing`,
increment attempts at claim time, lease rows, renew before send, and record
success, retry, or `DeadLettered` terminal Bondstone state with claim-owner
and lease checks.

Important files and tests: `DurableOutboxDispatcher.cs`,
`DurableOutboxFailurePolicy.cs`, `DurableOutboxDispatchState.cs`,
`EntityFrameworkCoreDurableOutboxWriter.cs`,
`PostgreSqlDurableOutboxClaimer.cs`,
`PostgreSqlDurableOutboxDispatchRecorder.cs`,
`PostgreSqlDurableOutboxLeaseRenewer.cs`,
`PostgresDurableOutboxClaimer.cs`,
`PostgresDurableOutboxDispatchRecorder.cs`,
`PostgresDurableOutboxLeaseRenewer.cs`,
`tests/Bondstone.Tests/Persistence/DurableOutboxDispatcherTests.cs`,
`tests/Bondstone.EntityFrameworkCore.Postgres.Tests/Outbox`, and
`tests/Bondstone.Persistence.Postgres.Tests`.

Review questions: Is claim-time attempt increment acceptable for records whose
transport route is stale? Should stale claim counts be surfaced in logs or
metrics beyond dispatch result counts?

Risks or smells: Outbox failure reasons can include exception messages; review
whether sensitive broker details need redaction guidance before production
hardening.

Recommended next actions: Add focused observability tests once logging/metrics
surface is accepted. Consider an ADR before changing attempt accounting or
terminal failure semantics.

Verification: `pnpm backend:test:fast`; `pnpm backend:test:integration` for
real PostgreSQL claim, lease, and dispatch SQL.

### Inbox Registration, Idempotency, And Recovery

Purpose and design: Inbox keys use Bondstone message id, module, and stable
handler/subscriber identity. Already processed rows skip handling; already
received but unprocessed rows throw through the receive pipeline because there
is no accepted stale receive recovery or inbox lease policy.

Important files and tests: `DurableInboxHandlerExecutor.cs`,
`DurableInboxMessageKey.cs`, `DurableInboxAlreadyReceivedException.cs`,
`PostgreSqlDurableInboxRegistrar.cs`, `PostgresDurableInboxRegistrar.cs`,
`EntityFrameworkCoreDurableInboxStore.cs`,
`tests/Bondstone.Tests/Persistence/DurableInboxHandlerExecutorTests.cs`,
`tests/Bondstone.Tests/Modules/ModuleReceivePipelineTests.cs`,
`tests/Bondstone.EntityFrameworkCore.Postgres.Tests/Persistence/PostgreSqlPersistenceInboxTests.cs`.

Review questions: What operational playbook should users follow for
already-received/unprocessed rows? Is a future inbox lease/stale recovery
model needed before production recommendation?

Risks or smells: The current loud behavior is correct for safety but can
strand a message until operator intervention or a later recovery feature.

Recommended next actions: Keep current behavior. If stale receive recovery is
desired, create an ADR covering idempotency proof, transaction boundary, and
provider behavior.

Verification: `pnpm backend:test:fast`; PostgreSQL integration tests for
concurrency and unique constraint behavior.

### Provider-Neutral Receive Pipelines

Purpose and design: `IModuleCommandReceivePipeline` and
`IModuleEventReceivePipeline` deserialize neutral durable envelopes, derive
inbox records, execute typed module handlers/subscribers, and surface inbox
outcomes to provider adapters.

Important files and tests: `ModuleCommandReceivePipeline.cs`,
`ModuleEventReceivePipeline.cs`, `ModuleCommandExecutor.cs`,
`ModuleEventSubscriberExecutor.cs`, `ModuleReceiveTelemetry.cs`,
`tests/Bondstone.Tests/Modules/ModuleReceivePipelineTests.cs`, and
`tests/Bondstone.Tests/Modules/ModuleEventSubscriberExecutionTests.cs`.

Review questions: Should receive activities add more tags for module, handler,
subscriber, and inbox outcome? Are deserialization failures observable enough
for provider workers?

Risks or smells: Diagnostics are present but still minimal. This is a good
next observability target.

Recommended next actions: Add telemetry/logging contract tests after deciding
the supported diagnostic surface.

Verification: `pnpm backend:test:fast`.

### Command Sending And Integration Event Publishing

Purpose and design: Command sending requires a current module execution
context and a target module; event publishing requires the source module to
register the published event. Both serialize payloads and stage neutral
envelopes in the source module outbox.

Important files and tests: `DurableCommandSender.cs`,
`DurableEventPublisher.cs`, `IDurableCommandSender.cs`,
`IDurableEventPublisher.cs`, `ModulePublishedEventRegistry.cs`,
`tests/Bondstone.Tests/Messaging/DurableCommandSenderTests.cs`, and
`tests/Bondstone.Tests/Messaging/DurableEventPublisherTests.cs`.

Review questions: Should command sends validate registered target module
routes at send time, or remain topology/startup/runtime dispatch concerns?
Should event publish results expose zero-subscriber topology diagnostics?

Risks or smells: Send result only means outbox acceptance. Docs say this
clearly; API consumers may still expect target handling unless examples keep
reinforcing the distinction.

Recommended next actions: Keep send/publish narrow. Consider API/result
changes only through ADR.

Verification: `pnpm backend:test:fast`.

### Local Transport

Purpose and design: Local transport explicitly maps durable outbox records to
provider-neutral receive pipelines through configured queues. It proves the
durable loop without broker durability.

Important files and tests: `LocalDurableOutboxTransportRoute.cs`,
`LocalTransportTopology.cs`, `LocalTransportTopologyDiagnosticSource.cs`,
`BondstoneLocalTransportBuilder.cs`,
`tests/Bondstone.Transport.Local.Tests`, and sample tests.

Review questions: Should local transport expose additional diagnostics that
make it harder to mistake for production broker durability?

Risks or smells: Docs are clear, but examples can still normalize local
transport. Keep production transport docs visible beside sample docs.

Recommended next actions: No code change now. Revisit only if consumer docs
or package descriptions blur the boundary.

Verification: `pnpm backend:test:fast`; sample integration smoke.

### RabbitMQ Transport

Purpose and design: RabbitMQ maps commands to a command exchange/routing key,
events to exchange/routing-key or default-exchange queue destinations, and
receive queues to command modules or event subscribers. Worker uses manual
ack and negative acknowledgement with configured requeue.

Important files and tests: `RabbitMqDurableOutboxTransport.cs`,
`RabbitMqDurableEnvelopeMapper.cs`, `RabbitMqReceivedMessageDispatcher.cs`,
`RabbitMqReceivedMessageHandler.cs`, `RabbitMqReceiveWorker.cs`,
`RabbitMqTopologyConfigurationValidator.cs`,
`tests/Bondstone.Transport.RabbitMq.Tests`, and
`tests/Bondstone.Samples.Tests/ModularMonolithRabbitMqSampleTests.cs`.

Review questions: Is `PrefetchCount` sufficient as the only worker-level
throughput option? Should connection/channel lifetime errors produce more
specific diagnostics?

Risks or smells: Broker topology remains app-owned, so documentation must stay
explicit that Bondstone does not declare exchanges, queues, bindings, DLX, or
retry queues.

Recommended next actions: Run broker-backed tests in CI-like infrastructure
and add any missing worker error-path tests that can be deterministic.

Verification: `pnpm backend:test:fast`; `pnpm backend:test:integration` with
Docker/Testcontainers.

### Azure Service Bus Transport

Purpose and design: Service Bus maps commands to queues and events to queues
or topics. Receive sources represent queues or subscriptions. Worker disables
auto-complete, completes after dispatch, and abandons after failure.

Important files and tests: `ServiceBusDurableOutboxTransport.cs`,
`ServiceBusDurableEnvelopeMapper.cs`,
`ServiceBusReceivedMessageDispatcher.cs`,
`ServiceBusReceivedMessageHandler.cs`, `ServiceBusReceiveWorker.cs`,
`ServiceBusTopologyConfigurationValidator.cs`, and
`tests/Bondstone.Transport.ServiceBus.Tests`.

Review questions: Are emulator-backed tests close enough to Azure behavior
for lock renewal and dead-letter handoff? Should worker options expose more
native processor settings?

Risks or smells: Native Service Bus behavior is provider-owned; any expanded
abstraction over retry/DLQ would be a broad durable behavior decision.

Recommended next actions: Keep worker narrow. Add documented examples for
app-owned max delivery count and subscription rules if users need more setup
guidance.

Verification: `pnpm backend:test:fast`; `pnpm backend:test:integration` with
the Service Bus test fixture.

### Hosted Workers And Receive Lifecycle Helpers

Purpose and design: `Bondstone.Hosting` owns the outbox worker; transport
packages own optional receive workers. Workers resolve scoped services inside
processing boundaries and validate configured topology at startup.

Important files and tests: `DurableOutboxWorker.cs`,
`DurableOutboxWorkerOptions.cs`,
`BondstoneHostingServiceCollectionExtensions.cs`,
`RabbitMqReceiveWorker.cs`, `ServiceBusReceiveWorker.cs`,
`tests/Bondstone.Hosting.Tests`, and transport worker integration tests.

Review questions: Should workers expose metrics for claimed, dispatched,
retry, dead-letter, stale, ack, nack, complete, and abandon counts? Should
receive workers share a common lifecycle abstraction or stay provider-native?

Risks or smells: Shared receive-worker abstraction could drift toward a
generic bus layer. Keep provider-native unless an ADR accepts a narrow common
diagnostic contract.

Recommended next actions: Prefer common observability vocabulary over common
receive control flow.

Verification: `pnpm backend:test:fast`; provider-backed integration tests for
settlement.

### Topology Validation And Startup Diagnostics

Purpose and design: Core aggregates transport route diagnostics and fails
startup for missing or ambiguous outbound routes. RabbitMQ and Service Bus
also validate provider receive bindings and direct queue event fan-out shape.

Important files and tests: `DurableTransportTopologyConfigurationValidator.cs`,
`DurableTransportTopologyRouteDiagnostic.cs`,
`RabbitMqTopologyConfigurationValidator.cs`,
`ServiceBusTopologyConfigurationValidator.cs`,
`tests/Bondstone.Composition.Tests`, `RabbitMqTopologyConfigurationValidatorTests.cs`,
and `ServiceBusTopologyConfigurationValidatorTests.cs`.

Review questions: In multi-transport hosts, is it acceptable that missing
subscriber receive bindings are not validated globally? Should diagnostics
distinguish app-owned external subscribers from local module subscribers?

Risks or smells: Multi-transport receive validation is intentionally narrower
than single-transport validation. Expanding it may need a topology model
decision.

Recommended next actions: Add follow-up tests for multi-transport receive
topology expectations before changing behavior.

Verification: `pnpm backend:test:fast`.

### Diagnostics, Tracing, Logging, And Observability

Purpose and design: Message trace context is captured and copied into durable
envelopes and transport metadata. Receive pipelines start activities; workers
log native failure handoff details.

Important files and tests: `MessageTraceContext.cs`,
`ModuleReceiveTelemetry.cs`, `RabbitMqReceiveWorker.cs`,
`ServiceBusReceiveWorker.cs`, transport envelope mappers, and current message
trace tests.

Review questions: What observable contract should be guaranteed across
providers: activity names, tags, log event ids, counters, or metrics? Should
outbox dispatcher have structured logging?

Risks or smells: Observability is useful but not yet a stable contract.
Adding it casually can create accidental compatibility pressure.

Recommended next actions: Create an ADR or small design note before promoting
logs/metrics/tracing tags to stable API.

Verification: Fast tests for trace context; future diagnostics tests once a
contract exists.

### Serialization And Durable Message Identity

Purpose and design: Stable message identity comes from explicit attributes or
registration. The neutral durable envelope contains explicit message kind,
type name, module names, trace, causation, partition key, payload, and
metadata.

Important files and tests: `MessageIdentityAttributes.cs`,
`MessageTypeRegistry.cs`, `DurableMessageEnvelope.cs`,
`SystemTextJsonDurablePayloadSerializer.cs`, provider durable envelope
mappers, and `tests/Bondstone.Tests/Messaging`.

Review questions: Should payload serializer options be frozen after service
provider build? Should metadata become structured instead of an opaque string?

Risks or smells: Serializer options are mutable through exposed
`JsonSerializerOptions`, which is normal for .NET options but worth reviewing
before compatibility promises harden.

Recommended next actions: Keep current shape unless real consumer pressure
requires versioned serialization policy.

Verification: `pnpm backend:test:fast`; provider integration tests for native
body handoff.

### Operation-State Semantics

Purpose and design: Send with caller-supplied operation id stages `Pending`
if unknown; successful durable command receive writes `Completed`. The reader
aggregates module stores by terminal/running/pending precedence.

Important files and tests: `DurableOperationState.cs`,
`DurableOperationStatus.cs`, `DurableModuleOperationReader.cs`,
`ModuleCommandOperationStatePipelineBehavior.cs`, EF/Postgres operation
stores, `tests/Bondstone.Tests/Messaging/DurableOperationReaderTests.cs`, and
provider operation-state tests.

Review questions: Should handler failure write `Failed`? Should long-running
handlers write `Running`? Should cancellation/timeouts be library-owned or
app-owned?

Risks or smells: The current narrow semantics are documented, but consumers
may expect operation state to mirror broker retry or handler failure. That
would be a durable behavior change.

Recommended next actions: Any default `Running`, `Failed`, `Cancelled`,
timeout, retry, or result payload behavior needs ADR work.

Verification: `pnpm backend:test:fast`; PostgreSQL integration tests for
state persistence and reader aggregation.

### EF Core Persistence And PostgreSQL Non-EF Persistence

Purpose and design: EF Core package owns generic mappings and transaction
scope. EF PostgreSQL owns provider SQL and duplicate/lease behavior.
`Bondstone.Persistence.Postgres` owns a PostgreSQL-specific non-EF provider
with app SQL available through `IPostgresModuleSession`.

Important files and tests: `BondstoneModelBuilderExtensions.cs`,
`EntityFrameworkCoreModuleTransactionRunner.cs`,
`EntityFrameworkCorePersistenceScope.cs`,
`BondstonePostgreSqlBuilderExtensions.cs`,
`BondstonePostgresBuilderExtensions.cs`, `PostgresSchema.cs`, and EF/Postgres
test projects.

Review questions: Should `PostgresSchema.EnsureDurableMessagingTablesAsync`
remain only a proof/test/sample helper, or is a migration story needed? Are
schema names validated consistently across EF and non-EF providers?

Risks or smells: Two PostgreSQL package names are easy to confuse:
`EntityFrameworkCore.Postgres` and `Persistence.Postgres`. Stable docs explain
the difference, but package descriptions and setup examples should keep doing
so.

Recommended next actions: Keep production schema/migrations app-owned. Add
docs only if users struggle with the two PostgreSQL paths.

Verification: `pnpm backend:test:fast`; `pnpm backend:test:integration`.

### Samples, Setup Guidance, And Developer Ergonomics

Purpose and design: Setup docs show package composition. The sample uses
module-owned registrations, mixed EF/non-EF persistence, local default
transport, and one preferred RabbitMQ provider path.

Important files and tests: `docs/setup.md`, `docs/samples.md`,
`samples/ModularMonolith`, module projects under `samples/`, and
`tests/Bondstone.Samples.Tests`.

Review questions: Does setup show enough app-owned broker topology without
becoming a deployment guide? Should Service Bus get a full parallel setup
snippet or stay summarized?

Risks or smells: Too much sample topology can read like Bondstone provisions
brokers. Too little can leave first-time users with an undefined native client
or topology.

Recommended next actions: Keep setup examples minimal but executable. Expand
only where a snippet currently leaves an undefined symbol or missing required
package.

Verification: `pnpm format:check`; `pnpm backend:test:integration` for sample
smoke tests.

### Tests, Integration Infrastructure, And CI Verification

Purpose and design: Fast tests cover public contracts and in-process behavior.
Integration tests cover real PostgreSQL, RabbitMQ, Service Bus, samples,
concurrency, provider SQL, transport settlement, and durable behavior.

Important files and tests: `docs/testing.md`, `package.json`,
all `tests/*/*.csproj`, provider fixtures, sample fixtures, and CI workflows.

Review questions: Should provider-backed integration tests be split by
database versus broker to make local runs cheaper? Should generated build
artifacts be cleaned from workspaces more aggressively?

Risks or smells: Fast tests can pass while infrastructure-specific behavior
regresses if integration tests are not run regularly. Local generated output
can preserve stale project names, such as the old Rebus test `obj` folder,
even after source cleanup.

Recommended next actions: Keep integration tests explicit, but make CI
expectations for infrastructure-backed coverage visible wherever branch
protection is configured.

Verification: `pnpm check`; `pnpm backend:test:integration` when Docker or
provider fixtures are available.

## Fixes Made

- Updated `docs/setup.md` so the RabbitMQ setup snippet creates the
  `IConnection` passed to `AddBondstoneRabbitMqConnection`.
- Renamed the lone remaining test `CancellationToken cancellationToken`
  parameter to `ct` in
  `tests/Bondstone.EntityFrameworkCore.Tests/Persistence/EntityFrameworkCoreModuleTransactionBehaviorTests.cs`.

## ADRs Needed

No ADR was needed for the small fixes in this pass.

Create or update ADRs before making any of these larger changes:

- public API surface reduction, compatibility guarantees, or implementation
  type visibility changes;
- operation-state defaults for `Running`, `Failed`, `Cancelled`, timeouts,
  retries, or result payloads;
- inbox lease or stale receive recovery;
- provider-neutral receive retry/dead-letter policy;
- shared receive-worker abstraction across providers;
- stable observability contract for activity tags, log event ids, counters, or
  metrics;
- migration/provisioning ownership changes for PostgreSQL or broker topology.

## Verification

Run during this pass:

- `pnpm backend:test:fast`

Recommended after this report and doc/test edits:

- `pnpm format:check`
- `pnpm backend:build`
- `pnpm backend:test:fast`

Recommended before release or major transport/persistence changes:

- `pnpm backend:test:integration`
- `pnpm check`

## Remaining Review Work

- Run the full integration suite in an environment with Docker/Testcontainers.
- Decide whether Phase 04 should start with observability, operation-state
  semantics, inbox stale recovery, or API baseline work.
- Review package descriptions and setup docs after any public API tightening.
- Clean local generated artifacts if they are noisy in developer workspaces;
  keep them out of committed source.
