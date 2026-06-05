# Messaging Architecture

## Command Boundaries

`IDurableCommand` is reserved for asynchronous commands accepted for durable
outbox delivery. It is not the general in-process command abstraction for every
module call.

Direct in-process calls between modules can use consumer-owned `.Contracts`
references without Bondstone mediation. Bondstone should add in-process command
or dispatch abstractions only when they protect a durable boundary concern such
as outbox persistence, inbox handling, tracing, or service-extraction
continuity.

Bondstone should avoid generic mediator or message-bus APIs for ordinary
in-process calls. They often hide call graphs, weaken discoverability, add
reflection or dispatch overhead, and provide little durable-boundary value when
normal typed contracts are sufficient.

Durable command sending is represented by `IDurableCommandSender`. The sender
accepts a durable command, a required target module, and optional explicit
metadata parameters. It returns a send result and does not promise an
immediate command result.

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
Receive-side Rebus inbox integration and event publish/subscribe remain
deferred.

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
- hosted worker, receive adapter, and transport integration.

## Message Identity Names

Bondstone keeps durable message identity strings free-form for compatibility
with existing systems and consumer naming policies. It should not derive
identities from CLR names.

Docs, tests, and samples should prefer lowercase dotted identities with an
explicit version suffix. A good default shape is
`{module}.{aggregate}.{message}.v{major}`, such as
`sales.customer.registered.v1`.
