# Messaging Architecture

## Command Boundaries

`IDurableCommand` is reserved for asynchronous commands accepted for durable
outbox delivery. It is not the general in-process command abstraction for every
module call.

Direct in-process calls between modules can use consumer-owned `.Contracts`
references without Bondstone mediation. Bondstone adds command execution
abstractions only when they protect a durable boundary concern such as outbox
persistence, inbox handling, validation, tracing, module persistence, or
service-extraction continuity.

Bondstone should avoid generic mediator or message-bus APIs for ordinary
in-process calls. They often hide call graphs, weaken discoverability, add
dispatch overhead, and provide little durable-boundary value when normal typed
contracts are sufficient.

Durable commands are different from ordinary in-process collaboration. When a
workflow changes state across distinct module persistence boundaries, direct
calls should be reserved for operations that tolerate failure without a durable
retry/deduplication boundary. The usual shape should be a durable command to a
target module or event choreography when multiple subscribers react to a
completed fact.

Module command execution is the narrow exception. Commands handled by a
Bondstone module can be registered through module command routes and executed
through `IModuleCommandExecutor`. The executor uses startup reflection
registration and cached runtime route metadata to run typed
`ICommandHandler<TCommand>` handlers through pipeline behaviors such as
validation. Bondstone-owned runtime concerns use ordered system pipeline
behaviors that wrap application behaviors; current system behaviors cover
source-module execution context, receive-side inbox handling, and EF
transaction ownership for EF-backed modules. Later pipeline behaviors will add
operation-state updates and receive tracing.

`ICommand` is the base marker for module command pipeline execution.
`IDurableCommand` extends `ICommand` for commands accepted for durable outbox
delivery and transport receive. The currently applied durable identity,
outbox, inbox, and operation-state behavior is command-first; integration
event publish/subscribe will apply those durable concepts through separate
event-specific runtime slices.

`IIntegrationEvent` is reserved for durable cross-module facts. Integration
events are not commands: they do not target one module and should eventually
fan out to independently identified subscribers. First-class event
publish/subscribe is not implemented yet. The accepted guardrail for this
shape is tracked in
[ADR 0026](../adr/0026-event-shape-guardrail.md), and implementation
sequencing is tracked in [../mvp-plan.md](../mvp-plan.md).

Durable commands and integration events are the durable boundary for
cross-persistence state changes. Direct `.Contracts` calls remain appropriate
for reads, local composition, and operations that tolerate failure without a
retry or deduplication boundary. Bondstone should not introduce a generic
mediator or message-bus layer for ordinary module collaboration.

The durable event publisher is a narrow core contract. `IDurableEventPublisher`
accepts an `IIntegrationEvent`, requires current source-module context, stages
a `MessageKind.Event` envelope through the outbox, and returns
`DurableEventPublishResult` instead of subscriber results. Event envelopes do
not specify `TargetModule`. Transport dispatch for event fan-out remains
deferred, so publishing is an outbox-backed core shape rather than a complete
broker publish/subscribe path.

Event handlers are typed integration event handlers registered as module
subscriber metadata through `IIntegrationEventHandler<TEvent>` and
`module.Events.RegisterSubscriber`. A subscriber belongs to a module and
carries stable consumer-owned subscriber identity. Subscriber identity must
not be derived from handler CLR names. Subscriber execution remains deferred.

Event inbox identity is per subscriber: durable message id, subscriber module,
and stable subscriber identity. It is not based on command target module
because events intentionally have no target module. Each subscriber receives
its own handle-once boundary and retry/dead-letter outcome through the
transport endpoint or subscription that delivers that subscriber's copy.

Domain events are module-local facts raised inside a module's domain model.
They are not automatically integration events, and Bondstone does not
currently collect, persist, dispatch, or publish them. Proposed future domain
event persistence should remain an explicit module capability and should not
force consumers into a Bondstone aggregate model. That proposal is tracked in
[ADR 0028](../adr/0028-domain-event-persistence-capability.md).

Durable command sending is represented by `IDurableCommandSender`. The sender
accepts a durable command, a required target module, and optional explicit
metadata parameters. It returns a send result and does not promise an
immediate command result.

The default sender stages a `DurableMessageEnvelope` through
`IDurableOutboxWriter`. It requires a current module execution context and uses
the executing module as the envelope source module. That context is established
by `IModuleCommandExecutor`, so durable sends from HTTP endpoints or local
application code should normally happen inside a module command handler rather
than from arbitrary services.

The common send overload accepts only the command, target module, and
cancellation token. The advanced overload adds parameters in the order callers
are most likely to override them: `partitionKey`, `durableOperationId`,
`traceContext`, and `causationId`.

`partitionKey` is an optional ordering or sharding key, commonly an aggregate
id or tenant id. Bondstone should not infer it from arbitrary command property
names through reflection. If derivation becomes useful later, prefer an
explicit interface or mapping policy.

`durableOperationId` is an optional logical operation id for idempotent
operation tracking or later operation-status lookup. A sender implementation
can generate it when absent, but callers that need retry-safe operation
tracking can provide it explicitly.

`traceContext` carries distributed tracing metadata, such as W3C `traceparent`,
`tracestate`, and baggage. It can be captured from `Activity.Current` in normal
.NET execution and can be mapped to or from transport adapters such as Rebus.
`MessageTraceContext` exposes W3C trace-id parsing through .NET
`ActivityContext` APIs so transport adapters do not need handwritten
traceparent parsers. This replaces loose correlation-id parameters for
cross-layer tracing.

`causationId` identifies the immediate Bondstone message that caused the send.
It is separate from distributed tracing: trace context follows the workflow,
while causation points to the direct message parent when one exists.

Durable operation tracking is represented by `DurableOperationState`,
`DurableOperationStatus`, and `IDurableOperationReader`. The reader returns
`null` when an operation id is unknown. Operation states are persistence-neutral
read models that expose a `Status` enum and do not define polling, timeout,
result deserialization, or `send and wait` behavior.

Receive-side handle-once execution is represented by
`IDurableInboxHandlerExecutor`. The executor is not a mediator and does not
discover handlers. It accepts a durable inbox record, a caller-supplied handler
delegate, and a caller-supplied commit delegate. It runs the handler only when
the inbox record is newly registered, stages the processed marker after the
handler completes, invokes the commit delegate, and returns a
`DurableInboxHandleResult`. Already processed records are skipped. Already
received but unprocessed records are also skipped because Bondstone does not
yet have an inbox lease or stale receive recovery model that can prove a second
handler execution is safe.

`DurableMessageEnvelope` represents the persistence- and transport-neutral
shape of a durable message before EF Core entities, provider claiming, or
transport headers are involved. Command envelopes require a target module;
event envelopes do not specify one. Envelope metadata remains explicit:
operation ids, trace context, causation, partition key, payload, and optional
metadata are stored as separate boundary fields instead of being inferred from
CLR names or transport details.

`Bondstone.Transport.Rebus` currently maps outgoing command envelopes to Rebus
explicit routing sends. The adapter preserves Bondstone message identity in
Bondstone-specific headers and maps trace context to W3C transport headers.
Receive-side Rebus inbox integration is command-only. Rebus handlers can
receive Bondstone wire envelopes, derive inbox keys from message id, target
module, and explicit handler identity, compose
`IRebusDurableInboxHandlerExecutor`, and acknowledge only after handle-once
execution and its commit boundary succeed. Event publish/subscribe remains
deferred.

Durable payload serialization is shared command/event infrastructure. Current
command send, event publish, Rebus typed command receive, and Rebus module
command receive use `IDurablePayloadSerializer`. The default implementation
uses `System.Text.Json` with durable-payload-specific
`DurablePayloadJsonOptions`, so consumers configure payload JSON options and
converters once instead of repeating transport-specific serializer options.
Applications can call `ConfigureBondstoneDurablePayloadJson` for the default
JSON implementation or replace `IDurablePayloadSerializer` when a custom
serializer is needed. Future event receive must use the same boundary.
Transport adapters must not rely on transport CLR type headers for Bondstone
durable identity. Content type, non-JSON payloads, schema registries, payload
encryption, compression, and stored-payload migration remain deferred.

The typed Rebus command receive pipeline uses `IMessageTypeRegistry` to
resolve stable message type names to durable command CLR types, deserializes
payloads through `IDurablePayloadSerializer`, starts a .NET/OTel consumer
`Activity` from accepted W3C trace context, and invokes caller-registered
typed command handlers with explicit stable handler identity.

The current typed Rebus pipeline is a lower-level transport primitive. The
preferred app-facing receive shape binds host Rebus topology to
`IModuleCommandExecutor` so application code does not repeat handler and
commit delegates per command.

Rebus module command receive groundwork now resolves a Bondstone wire envelope
to a registered module command route, derives the stable handler identity from
route metadata, passes the durable inbox record into `IModuleCommandExecutor`,
and receives the inbox result from `ModuleCommandExecutionResult`. Host
receive topology can now bind Rebus endpoint names to accepted local modules
and registers the module command receive pipeline. Receive bindings also
derive outgoing command destinations for accepted modules, with explicit
`RouteModule` mappings reserved as overrides and module queue conventions as
fallback routing for extracted or otherwise remote target modules. Actual Rebus
listener binding to that topology remains future work.

For durable commands, prefer queue-backed point-to-point delivery. A Rebus
receive endpoint should usually represent a module-level command backlog so
that scaling, retry, dead-letter handling, and service extraction can be
managed per module. Multiple modules may share an endpoint when they share the
same operational profile, but a general inbox queue is not the default command
topology. Future event publish/subscribe work can use topic or subscription
topology because events intentionally fan out to multiple subscribers.

Durable-message diagnostics should specialize by message kind. Command
diagnostics can report target module, destination, route source, and receive
endpoint binding. Event diagnostics can report event identity, topic,
subscriber module, subscriber identity, subscription binding, and zero
subscriber outcomes. Shared diagnostics should include stable message
identity, message kind, source module, payload serialization policy, trace
context, and causation information without assuming every durable message is a
command. Core now has `DurableMessageTopologyDiagnosticKind` values for
command routes, command destinations, command receive endpoints, event topics,
and event subscriptions.

Modules that send or receive durable commands should opt into one durable
messaging capability, such as `UseDurableMessaging`, rather than making normal
applications choose separate inbox and outbox toggles. The capability should
represent the inbox/outbox-backed path for durable commands. Plain direct
module collaboration should use `.Contracts` references or regular
`ICommand` execution, not a local durable queue substitute.

Future envelope fields remain open. Content type is the most likely next
addition if Bondstone needs to support non-JSON payloads or make JSON explicit.
Neutral headers may be added if multiple adapters need cross-cutting metadata
that does not deserve a first-class field. Scheduling, TTL, priority, reply-to,
tenant id, and transport-native headers should stay deferred until persistence,
transport, or samples prove a durable need.

Deferred durable-command work remains tracked:

- any `send and wait` helper and timeout/polling policy;
- trace context and causation propagation rules;
- dispatcher configuration, advanced retry policy, and dead-letter routing
  ownership;
- transport-specific receive adapters and publish/subscribe behavior;
- inbox handler discovery, receive retry policy, stale receive recovery, and
  transport acknowledgement coordination;
- deeper partition-key ordering and scaling semantics;
- content type or neutral header support if adapters need it;
- scheduling, TTL, priority, reply-to, tenant, or transport-native metadata if
  a later durable scenario justifies it;
- receive adapter, receive-side transport integration, and additional
  transport-backed verification;
- durable-messaging capability validation, transaction behaviors,
  Rebus host-topology binding to module command routes, and service-shaped
  samples.

## Message Identity Names

Bondstone keeps durable message identity strings free-form for compatibility
with existing systems and consumer naming policies. It should not derive
identities from CLR names.

Docs, tests, and samples should prefer lowercase dotted identities with an
explicit version suffix. A good default shape is
`{module}.{aggregate}.{message}.v{major}`, such as
`sales.customer.registered.v1`.
