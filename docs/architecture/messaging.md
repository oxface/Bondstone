# Messaging Architecture

## Durable Message Kinds

Commands and integration events are both durable messages. They share stable
message identity, outbox staging, durable payload serialization, trace and
causation metadata, and receive-side inbox concepts.

The distinction is behavioral:

- commands represent directed work for one target module;
- integration events represent facts from one source module and may have zero
  or more subscribers.

Bondstone should not collapse this into a generic mediator or generic message
bus. Ordinary in-process module collaboration can use typed `.Contracts`
references. Durable commands and integration events are for cross-persistence
state changes that need retry, deduplication, and service-extraction
continuity.

## Commands

`ICommand` is the base marker for module command pipeline execution.
`IDurableCommand` extends `ICommand` for commands accepted for durable outbox
delivery and transport receive.

`IDurableCommandSender` accepts a durable command, a required target module,
and optional metadata such as partition key, durable operation id, trace
context, and causation id. The default sender requires a current module
execution context, uses the executing module as the source module, serializes
the command through `IDurablePayloadSerializer`, and stages a command envelope
through `IDurableOutboxWriter`. It is not a general background-work API and
does not expose a public source-module override.

Module command execution is registered through module command routes and
executed through `IModuleCommandExecutor`. The executor runs typed
`ICommandHandler<TCommand>` handlers through an internal runtime plan:
ordered system behavior steps, application behavior steps in DI registration
order, then the handler. System behaviors currently cover source-module
execution context, receive-side inbox handling, durable operation completion,
and module-owned persistence behavior supplied by provider packages.

## Integration Events

`IIntegrationEvent` is reserved for durable cross-module facts. Integration
events are not commands: they do not target one module and they fan out to
independently identified subscribers.

`IDurableEventPublisher` accepts an integration event, requires current
source-module context, serializes the event through
`IDurablePayloadSerializer`, stages a `MessageKind.Event` envelope without
`TargetModule`, and returns a publish result. The current source module must
have registered that event through `RegisterPublishedEvent`. It does not wait
for subscriber results.

Published events are registered as module-owned publish metadata through
`module.Events.RegisterPublishedEvent`. Transport topology validation uses
that metadata to check configured event destinations without treating
subscriber-only event types as outbound publications.

Event subscribers are typed `IIntegrationEventHandler<TEvent>` handlers
registered as module-owned subscriber metadata through
`module.Events.RegisterSubscriber`. A subscriber belongs to a module and
carries a stable consumer-owned subscriber identity. Subscriber identity must
not be derived from handler CLR names.

Core subscriber execution uses `IModuleEventSubscriberExecutor`. It resolves a
subscriber by module, stable event identity, and stable subscriber identity,
then executes the typed handler through an internal runtime plan: ordered
system behavior steps, application behavior steps in DI registration order,
then the handler. System behaviors provide per-subscriber inbox handling and
set the module execution context for the subscriber module.

Event-driven orchestration composes commands and events rather than erasing
their distinction. A subscriber, saga, process manager, or orchestrator can
react to an integration event and send durable commands as follow-up work.

## Domain Events

Domain events are module-local facts. They are distinct from integration
events and must not be treated as durable cross-module contracts by default.

ADR 0028 accepts a small Bondstone-owned domain event contract. Core currently
provides:

- `IDomainEvent` marks a module-local domain fact in the
  `Bondstone.DomainEvents` namespace;
- `DomainEventIdentityAttribute` provides a stable module-local identity for
  persisted domain event records;
- `IDomainEventSource` lets aggregate roots or entities expose
  `PendingDomainEvents` and clear them through
  `ClearPendingDomainEvents()` after successful collection;
- `IDomainEventHandler<TDomainEvent>` is the local handler contract for
  module-local handlers and is constrained to `IDomainEvent`.

Domain events are not transport messages, durable messages, or event-delivery
contracts. They do not implement `IMessage`, `IIntegrationEvent`, or
`IDurableCommand`; do not use `MessageKind`, `MessageTypeRegistry`,
`DurableMessageEnvelope`, durable message topology, command targets, or
event subscribers; and are not automatically staged in the outgoing outbox.
Publishing a public integration event from a domain event must remain an
explicit module-code mapping step that calls `IDurableEventPublisher` for a
registered `IIntegrationEvent`.

The accepted persistence direction is optional module-local records. A
persisted domain event record may support auditing, local projections, later
in-module dispatch, or explicit mapping to integration events, but it remains
private to the owning module unless module code publishes a separate
integration event.

Runtime pipeline behavior for domain event persistence is provider-owned, not
a public domain event bus. EF Core is the first runtime provider. It activates
only for EF-backed modules that call
`UseEntityFrameworkCoreDomainEventPersistence()` and map the EF domain event
record shape explicitly with `ApplyBondstoneDomainEvents()`. Bondstone does not
have a public
capability-step registry, public named pipeline slots, or a separate
`Bondstone.DomainEvents` package.

EF Core collection persists module-local domain event records and clears
sources only after the EF module transaction saves and commits successfully.
Integration-event mapping remains pending and must stay explicit module code.
Domain event follow-up ideas are tracked in
[../backlog/00-plans.md](../backlog/00-plans.md).

## Module Execution Context

Module command execution and module event subscriber execution establish the
current source module through Bondstone's module execution context. The
current implementation uses an ambient `AsyncLocal` accessor owned by
`AddBondstone`; command and event subscriber system pipeline behaviors push
the executing module before application handlers run and restore the previous
context when the pipeline completes.

Durable command sending and integration event publishing require that current
module execution flow. Calls from queued work after the handler returns, code
that suppresses execution-context flow, or arbitrary background services do
not have a supported source module. HTTP endpoints, schedulers, and other
custom app-owned entrypoints that need module command behavior should execute
registered module commands through `IModuleCommandExecutor`; the handler then
gets the normal durable send/publish context. Bondstone does not currently
provide module-scoped durable sender/publisher clients or public APIs for
arbitrary source-module selection.

## Inbox Identity

Receive-side handle-once execution is represented by
`IDurableInboxHandlerExecutor`. It accepts a durable inbox record, a handler
delegate, and a cancellation token. It runs the handler only when the inbox
record is newly registered, stages the processed marker after the handler
completes, and returns a `DurableInboxHandleResult`.

The executor does not commit persistence. Module transaction behaviors own the
surrounding commit so handler state, inbox markers, operation state, and
outgoing outbox rows persist together. A low-level caller that uses the
executor directly must place it inside the caller's transaction or save
boundary and commit outside the executor.

Command inbox identity is:

- Bondstone message id;
- target module;
- stable command handler identity.

Event inbox identity is per subscriber:

- Bondstone message id;
- subscriber module;
- stable subscriber identity.

Already processed records are skipped. Already received but unprocessed
records are operationally loud through `DurableInboxAlreadyReceivedException`
when using the module receive pipeline, because Bondstone has no inbox lease or
stale receive recovery model that can prove a second handler execution is
safe. Recovery for those rows is currently operator-owned or application-owned:
the application must inspect its persisted inbox and provider state, decide
whether and how to mutate the row or broker message, and own the safety proof.
Bondstone does not provide a default stale-row sweeper, recovery hook, failed
receive state, or provider-neutral receive dead-letter abstraction.

## Neutral Receive Pipeline

Core exposes provider-neutral receive pipelines over
`DurableMessageEnvelope`:

- `IModuleCommandReceivePipeline`
- `IModuleEventReceivePipeline`

These pipelines resolve stable message identities, deserialize payloads
through the shared durable payload serializer, derive inbox records, execute
the module command or event subscriber executor, and surface stale receive
state. They do not configure broker listeners, discover handlers from broker
messages, own retry/dead-letter policy, or replace provider-native
acknowledgement behavior.

Direct transport receive adapters should parse their provider-native body into
the neutral durable envelope and call these pipelines inside the provider
message acknowledgement boundary. An already-received unprocessed inbox row is
a dispatch failure for normal module receive, so provider settlement must
follow the same failure handoff as other dispatch exceptions rather than
acknowledging the message as handled.

## Durable Envelope

`DurableMessageEnvelope` represents the persistence- and transport-neutral
shape of a durable message before provider stores or transport headers are
involved. Command envelopes require `TargetModule`; event envelopes must not
specify one. Envelope metadata remains explicit: operation ids, trace context,
causation, partition key, payload, and optional metadata are stored as
separate boundary fields instead of being inferred from CLR names or transport
details.

Durable payload serialization is shared through `IDurablePayloadSerializer`.
The default implementation uses System.Text.Json options configured through
Bondstone's durable payload JSON surface.

## Operation State

Durable operation tracking is represented by `DurableOperationState`,
`DurableOperationStatus`, and `IDurableOperationReader`. Operation state is a
caller-visible logical handle; it is not the delivery ledger. Outbox records
track staged and dispatched durable messages, and inbox records track receive
idempotency and processed markers.

The current command loop records operation state only for caller-supplied
operation ids. Sending a command with an operation id stages `Pending` if the
operation is unknown. Successful module command receive stages `Completed` in
the target module persistence boundary. When a host has module-owned operation
stores, `IDurableOperationReader` reads across the configured module stores
and returns the highest-precedence state: terminal statuses outrank
`Running`, which outranks `Pending`; states with equal precedence use the
newer update timestamp.

The command loop writes `Pending` and `Completed`. `Running`, `Failed`, and
`Cancelled` remain storage/read-model values for application-owned operation
policies; Bondstone's default command loop does not infer them from broker
retry or handler exceptions. Polling, timeout, result deserialization, retry
state, stale receive recovery, and default failure/cancellation transitions are
outside the current operation-state contract.

## Transport Adapters

Direct provider adapters are the supported transport architecture. Rebus is
not a supported transport package.

Current direct transport packages:

- `Bondstone.Transport.RabbitMq`
- `Bondstone.Transport.ServiceBus`
- `Bondstone.Transport.Local`

RabbitMQ and Azure Service Bus are the production-oriented direct provider
adapters. Their implemented scope includes outgoing durable outbox dispatch,
provider-native receive topology, opt-in hosted receive workers, and
provider-backed receive tests. RabbitMQ uses exchange, routing-key, and queue
vocabulary. Azure Service Bus uses queue, topic, subscription, and event
destination vocabulary. Provider connection, credentials,
queue/topic/exchange/binding creation, retry, dead-letter, serializer, and
administration remain app-owned or provider-native.

`Bondstone.Transport.Local` is explicit local queue routing for samples, tests,
and local development. It uses the neutral receive pipelines and preserves
outbox/inbox semantics, but it is not a broker durability layer or fallback.

Direct transport packages contribute `IDurableOutboxTransportRoute` entries.
`RoutedDurableOutboxTransport` sends a claimed outbox record only when exactly
one provider route matches the message. Zero matches and ambiguous matches are
loud configuration errors.

Local, RabbitMQ, and Service Bus contribute startup topology diagnostics for
outbound durable route ownership. When transport diagnostic sources are
configured, registered durable command target modules and registered published
events must have exactly one outbound transport route. Zero matches and
multiple matches fail during `AddBondstone`, before outbox dispatch.

RabbitMQ and Service Bus also validate their provider receive bindings.
Provider receive bindings always validate that accepted command modules have
registered durable command handlers and event subscription bindings have
matching registered subscribers. In a single-transport host, provider
validators also fail when registered event subscribers have no receive binding.
They also validate queue-style event destinations against receive bindings:
same-queue in-process fan-out remains valid, but direct queue event routing
fails startup when receive bindings for that event are on another receive
entity or spread across multiple receive entities. Split subscribers should use
provider-native broker fan-out, such as RabbitMQ exchange bindings or Service
Bus topic subscriptions.

RabbitMQ and Service Bus map received provider-native messages into the neutral
receive pipelines. Provider packages also expose native received message
mappers so app-owned consumers/processors can convert broker messages before
dispatch, plus handler helpers that invoke caller-supplied native settlement
only after dispatch succeeds. RabbitMQ and Service Bus also expose opt-in
hosted receive lifecycle helpers over configured receive topology.
Provider-backed integration tests cover successful receive settlement,
dead-letter handoff after failed dispatch, and event subscriber fan-out.

Bondstone owns persisted outbox retry and terminal failure state. The outgoing
outbox terminal status is documented in
[persistence-core.md](persistence-core.md). Direct provider receive adapters
own settlement ordering and operational diagnostics, but broker retry
schedules, delivery counts, dead-letter policy, and provider client retry
configuration remain application-owned and provider-native.

Applications bind Bondstone receive topology to provider-native queues and
subscriptions where their retry/DLQ policy is already configured. RabbitMQ
exposes the worker failure `requeue` decision. Service Bus exposes processor
concurrency and lock renewal settings. Bondstone does not create a
provider-neutral receive retry policy or receive DLQ abstraction.

## Diagnostics

Durable-message diagnostics should specialize by message kind and provider.
Command diagnostics should describe target-module routing. Event diagnostics
should describe publish subjects, subscriber bindings, and missing-subscriber
outcomes. Core keeps shared durable-message vocabulary, while provider
packages expose provider-native diagnostic details.

Startup topology validation should use those diagnostics for fail-fast
configuration errors while remaining separate from broker provisioning.
Validation does not create RabbitMQ exchanges, queues, or bindings, and does
not create Service Bus queues, topics, subscriptions, or rules.

Receive recovery diagnostics should explain the native settlement handoff after
failed Bondstone dispatch. RabbitMQ diagnostics should include the queue,
delivery identity, routing facts, and configured negative-acknowledgement
requeue decision. Service Bus diagnostics should include the receive source,
message identity, delivery count when available, and abandon handoff. These
logs document what Bondstone did; they do not imply Bondstone owns broker
retry or dead-letter policy.
