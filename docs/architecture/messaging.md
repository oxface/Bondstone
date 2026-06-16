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
`ICommand<TResult>` extends `ICommand` for commands that produce an application
result when executed locally through the module command executor.
`IDurableCommand` extends `ICommand` for commands accepted for durable outbox
delivery and transport receive. A durable result command implements both
`IDurableCommand` and `ICommand<TResult>`.

`IDurableCommandSender` accepts a durable command, a required target module,
and optional metadata such as partition key, durable operation id, trace
context, and causation id. The default sender requires a current module
execution context, uses the executing module as the source module, serializes
the command through `IDurablePayloadSerializer`, and stages a command envelope
through `IDurableOutboxWriter`. When a durable operation id is supplied, the
send result carries a `DurableOperationHandle` with the operation id, source
module, and target module. It is not a general background-work API and does
not expose a public source-module override.

Module command execution is registered through module command routes and
executed through `IModuleCommandExecutor`. The executor runs typed
`ICommandHandler<TCommand>` and `ICommandHandler<TCommand, TResult>` handlers
through direct internal orchestration: provider transaction runners,
operation completion, receive inbox handling, module execution context,
validation, the handler, then provider post-handler actions.

Local execution of `ICommand<TResult>` returns a typed
`ModuleCommandExecutionResult<TResult>` from `ExecuteResultAsync`. Durable send
does not return `TResult` directly; it accepts work and returns send metadata.

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
then executes the typed handler through direct internal orchestration:
provider transaction runners, receive inbox handling, module execution
context, the handler, then provider post-handler actions.

Event-driven orchestration composes commands and events rather than erasing
their distinction. A subscriber, saga, process manager, or orchestrator can
react to an integration event and send durable commands as additional module
work.

## Domain Events

Domain events are module-local facts. They are distinct from integration
events and must not be treated as durable cross-module contracts by default.

Bondstone owns a small module-local domain event contract in the core
`Bondstone` package under the `Bondstone.DomainEvents` namespace:

- `IDomainEvent` marks a module-local domain fact;
- `DomainEventIdentityAttribute` provides a stable module-local identity for
  persisted domain event records;
- `IDomainEventSource` lets aggregate roots or entities expose
  `PendingDomainEvents` and clear them through
  `ClearPendingDomainEvents()` after successful collection.

Domain events are not transport messages, durable messages, or event-delivery
contracts. They do not implement `IMessage`, `IIntegrationEvent`, or
`IDurableCommand`; do not use `MessageKind`, `MessageTypeRegistry`,
`DurableMessageEnvelope`, durable message topology, command targets, or
event subscribers; and are not automatically staged in the outgoing outbox.
Publishing a public integration event from a domain event must remain an
explicit module-code mapping step that calls `IDurableEventPublisher` for a
registered `IIntegrationEvent`.

Bondstone does not expose an active domain event handler contract and does not
dispatch domain events automatically.

The accepted persistence direction is optional module-local records. A
persisted domain event record is private to the owning module unless module
code publishes a separate integration event.

Runtime behavior for domain event persistence is EF-owned, not a public domain
event bus. EF Core is the first runtime implementation, provided by
`Bondstone.Persistence.EntityFrameworkCore`. It activates only for EF-backed
modules that call `UseEntityFrameworkCoreDomainEventPersistence()` and map the
EF domain event record shape explicitly with `ApplyBondstoneDomainEvents()`.
The implementation participates through Bondstone's hidden provider
post-handler action contract.

EF Core collection persists module-local domain event records and clears
sources only after the EF module transaction saves and commits successfully.
The EF behavior does not resolve local domain event handlers and does not map
domain events to integration events. Mapping a domain event to an integration
event is explicit module code.

## Module Execution Context

Module command execution and module event subscriber execution establish the
current source module through Bondstone's module execution context. The
current implementation uses an ambient `AsyncLocal` accessor owned by
`AddBondstone`; command and event subscriber runtime execution pushes the
executing module before handlers run and restores the previous context when
execution completes.

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
the target module persistence boundary. `IDurableOperationReader` reads across
the configured module-owned operation stores and returns the highest-precedence
state: terminal statuses outrank `Running`, which outranks `Pending`; states
with equal precedence use the newer update timestamp. If no module-owned
operation stores are configured, the default reader returns no state.

The command loop writes `Pending` and `Completed`. When a durable command also
implements `ICommand<TResult>` and is received with a durable operation id,
Bondstone serializes the handler result with the configured durable payload
JSON options and stores it as the completed operation state's result payload
inside the target module receive transaction. The same completed operation
state also stores optional diagnostic context from the receive route: module
name, durable message type name, and handler identity. Callers can observe
durable results through `IDurableOperationResultReader`:
`GetResultAsync<TResult>()` reads current state once, while
`WaitForResultAsync<TResult>()` performs explicit timeout-bounded polling
until the operation reaches a terminal state. `IDurableOperationReader`
remains available as the lower-level state reader. When available, pass
`DurableCommandSendResult.Operation` to these readers. Handle-based reads query
the target module's operation-state store. Operation-id-only reads remain
available as the global aggregate compatibility path.

Applications can mark explicit terminal non-success outcomes through
`IDurableOperationFinalizer`. The finalizer writes `Failed` or `Cancelled` to
the named module's operation-state store when the operation is unknown,
`Pending`, or `Running`. It returns the existing state without overwriting when
the operation is already terminal. This API is for application-owned timeout,
expiry, cancellation, and workflow policy; it does not infer failure from
outbox dispatch, inbox idempotency, broker retry, or dead-letter behavior.

Applications that need a repeatable expiry job can call
`IDurableOperationExpirationProcessor`. The processor asks the named module's
operation-state store for stale `Pending` or `Running` candidates before a UTC
cutoff, then finalizes those candidates as `Failed` or `Cancelled` through the
same finalizer. Bondstone does not schedule this job automatically; the app
owns cadence, cutoff calculation, terminal status, reason text, and
operational policy.

`DurableOperationResult<TResult>` preserves the operation-state flags and adds
a single `State` classification for consumer diagnostics:

- `IsKnown` means an operation state row was found.
- `IsCompleted` means the operation status is `Completed`.
- `IsTerminal` means the operation status is `Completed`, `Failed`, or
  `Cancelled`.
- `HasResult` means a completed result payload was successfully deserialized
  as `TResult`.

`State` distinguishes unknown operations, pending operations, running
operations, completed operations with a deserialized result, completed
operations without a result payload, failed operations, cancelled operations,
and completed operations whose result payload could not be deserialized as the
requested result type. Deserialization failures are returned as
`ResultDeserializationFailed` with a `DeserializationFailure` value that
includes the operation id, target result type name, diagnostic message, and
underlying exception type name when available. When operation state carries
diagnostic context, the result and deserialization failure diagnostics also
include module name, durable message type name, and handler identity.

Operation diagnostic context is optional. It may be absent for old rows,
manually-created operation states, operations created before this feature, or
provider schemas that have not been migrated. Result diagnostics remain useful
without it by including the operation id and requested result type when those
values are known.

`Running` remains a storage/read-model value for application-owned operation
policies. `Failed` and `Cancelled` can be written explicitly by application
policy through the finalizer. Bondstone's default command loop does not infer
terminal failure from broker retry or handler exceptions. Callers choose
polling cadence, timeout, and API endpoint policy when using the result
reader. Generated operation ids, retry state, stale receive recovery, default
timeout policies, and provider-specific operation concurrency are outside the
current operation-state contract.

## Transport Boundary

Bondstone does not currently ship a broker adapter package. Rebus,
RabbitMQ.Client, Azure Service Bus, Kafka, and other broker runtimes are
app-owned choices.

`Bondstone.Transport.Local` is the only active transport package. It provides
explicit local queue routing for samples, tests, and local development. It
uses the neutral receive helper and preserves outbox/inbox semantics, but it
is not a broker durability layer or fallback.

Outbound broker integrations call
`UseDurableEnvelopeDispatcher<TDispatcher>()` and implement
`IDurableEnvelopeDispatcher` to publish claimed outbox records through the
chosen native transport. Advanced integrations may also compose
`IDurableEnvelopeDispatchRoute` with `RoutedDurableEnvelopeDispatcher`, which
dispatches a claimed outbox record only when exactly one route matches the
message. Zero matches and ambiguous matches are loud dispatch-time errors.

Inbound broker integrations should use `IDurableMessageEnvelopeSerializer` to
read the durable envelope body and `IDurableEnvelopeReceiver` to execute
module receive. Command envelopes route by `TargetModule`. Event receive is
explicit: the app supplies the subscriber module and stable subscriber
identity selected by its native subscription.

Bondstone owns persisted outbox retry and terminal failure state. The outgoing
outbox terminal status is documented in
[persistence-core.md](persistence-core.md). Native broker consumers own
settlement ordering and operational diagnostics. Broker retry schedules,
delivery counts, dead-letter policy, topology, prefetch, concurrency, and
client retry configuration remain application-owned and provider-native.

Bondstone does not create a provider-neutral receive retry policy, receive
DLQ abstraction, subscription store, receive topology DSL, or broker worker
runtime.

## Diagnostics

Durable-message diagnostics should specialize by message kind and provider.
Command diagnostics should describe target-module routing. Event diagnostics
should describe publish subjects, subscriber identities, and missing-subscriber
outcomes. Core keeps shared durable-message vocabulary; app-owned broker code
should log provider-native delivery and settlement details where needed.

Bondstone no longer has a provider-neutral startup topology diagnostics layer.
Validation does not create exchanges, queues, topics, subscriptions, bindings,
or broker topology.

Receive recovery diagnostics should explain the native settlement handoff after
failed Bondstone dispatch when an app-owned broker consumer performs that
handoff. These logs document what the app did around Bondstone; they do not
imply Bondstone owns broker retry or dead-letter policy.
