# 0033 First-Class Event Publish/Subscribe Topology

Status: Amended
Application: Applied
Date: 2026-06-09

## Context

Phase 4 proved the durable command loop through module-owned EF persistence,
outbox-backed command send, Rebus dispatch, Rebus module receive endpoint
binding, module command execution, receive inbox handling, and one target
module transaction.

Phase 5 starts first-class integration events. ADR 0026 intentionally created
the event guardrail early, and Phase 0 applied the minimal core shape:
`IDurableEventPublisher`, `IIntegrationEventHandler<TEvent>`, module event
registration metadata, event envelopes without target modules, and
per-subscriber inbox-key naming. It did not decide the runtime receive path,
Rebus publish/subscribe topology, subscription binding, diagnostics, or
transport-backed verification.

Those pieces affect public API shape, durable behavior, transport behavior,
module runtime behavior, and sample architecture. They need a durable decision
before broad implementation. The decision must keep Bondstone focused on
durable module boundaries rather than turning it into a generic mediator or
generic message bus.

## Decision

Bondstone will implement first-class integration events as explicit
outbox-backed publish/subscribe, separate from durable command routing and
separate from module-local domain events.

`IDurableEventPublisher` remains the source-side API. Publishing an
integration event requires current module execution context, serializes the
event through `IDurablePayloadSerializer`, stages a `MessageKind.Event`
envelope in the source module outbox, and returns an accepted publish result.
It does not return subscriber results, wait for subscribers, or infer
publication from domain events.

Event outbox dispatch publishes claimed `MessageKind.Event` records to
transport event topology. A dispatch attempt is successful when the transport
accepts the publish operation. Fan-out delivery, subscriber retry, and
dead-letter outcomes belong to each subscriber endpoint or subscription that
receives a copy. Event envelopes must not carry `TargetModule`.

Modules declare event subscribers through module-owned registration:
`module.Events.RegisterSubscriber<TEvent, THandler>(subscriberIdentity)`.
The handler implements `IIntegrationEventHandler<TEvent>`. Subscriber
registration records:

- stable event identity;
- subscriber module;
- stable consumer-owned subscriber identity;
- handler type.

Subscriber execution will use a module event execution path, not the module
command executor and not a generic mediator. The event execution path must
resolve the event CLR type by stable message identity, deserialize through
`IDurablePayloadSerializer`, establish source and subscriber metadata needed
for tracing and durable sends, invoke the typed event handler, and compose
module-owned persistence behavior so handler state, inbox markers, operation
state when applicable, and outgoing outbox messages commit in the subscriber
module boundary. Exact pipeline names and behavior ordering are a later
implementation slice, but the path must preserve module-owned persistence
boundaries.

Event inbox identity is per subscriber. The inbox key for a delivered event is
the Bondstone message id, subscriber module, and stable subscriber identity.
It must not use command target module, Rebus endpoint name, handler CLR name,
or transport subscription id as the durable identity. Already-processed
duplicates are acknowledged. Already-received but unprocessed rows remain
operationally loud until a later stale receive recovery decision says
otherwise.

For Rebus, Bondstone event topology uses publish/subscribe vocabulary:

- an event topic is the durable publish subject for one stable integration
  event identity;
- a topic name may be mapped explicitly or derived by convention from the
  stable event identity;
- a subscription binding connects an event topic to a subscriber module,
  stable subscriber identity, and Rebus endpoint that will receive that
  subscriber's copy.

Rebus event publish dispatch uses the raw Rebus topics API with the resolved
topic name and the existing Bondstone wire envelope. It preserves Bondstone
headers, W3C trace headers, message id, source module, causation id, durable
operation id, partition key, and payload metadata. Bondstone must not rely on
Rebus CLR type headers as durable event identity.

Rebus-native infrastructure remains application-owned: broker transport,
connection string, serializer, input queue, workers, retry/dead-letter policy,
subscription storage, auto-subscription startup, and broker-specific topic,
exchange, queue, or routing-key creation. Bondstone topology describes the
durable message relationship; it does not own the entire Rebus host.

Diagnostics should be event-specific. Event publish diagnostics should report
event identity, resolved topic, whether resolution came from an explicit
mapping or convention, and missing-topic failures. Event subscription
diagnostics should report event identity, topic, subscriber module,
subscriber identity, endpoint or subscription binding, and zero-subscriber or
missing-binding outcomes. Diagnostics should be useful before dispatch or
receive starts.

Testing must stay layered. Fast unit/application tests should cover topic
resolution, event outbox dispatch, subscriber registration and validation,
inbox key derivation, and diagnostics. Transport-backed event tests should be
explicit `Integration` tests, using Rebus in-memory transport for the broad
publish/subscribe path and provider-backed tests only where persistence,
acknowledgement, retry, dead-letter, or subscription storage semantics require
real infrastructure.

This decision explicitly defers:

- automatic domain-event persistence;
- automatic domain-event-to-integration-event publication;
- broad event choreography samples;
- subscriber execution pipeline implementation beyond the contract above;
- event operation result payloads, failure states, retry state, and stale
  receive recovery;
- provider-specific migration helpers and broker-specific topology creation.

## Amendment 2026-06-09: Durable Messages And Provider-Native Topology

Commands and integration events are both durable messages. They share stable
message identity, outbox staging, payload serialization, trace/causation
metadata, and receive-side inbox concepts. The distinction is behavioral and
operational, not an argument for separate generic infrastructure.

Commands represent directed work for one target module. A command envelope
requires `TargetModule`, uses point-to-point delivery, is handled by one
module command route, and can drive command-specific operation-state
completion. Integration events represent facts from one source module. An
event envelope has no `TargetModule`, may have zero or more subscribers, and
uses per-subscriber inbox identity and retry/dead-letter outcomes.

Bondstone should keep provider-neutral core concepts at the durable-message
level and let transport adapters expose provider-native topology. Rebus can
use Rebus endpoint, route, topic, and subscription vocabulary. A future Azure
Service Bus adapter should use Service Bus queue/topic/subscription
vocabulary. A RabbitMQ-focused adapter may need exchange, routing-key, queue,
and binding vocabulary. Core should not introduce a lowest-common-denominator
topic/queue abstraction unless multiple adapters prove one is useful.

Rebus event publish dispatch should use the Rebus bus as the native
application-owned integration point. The transport resolves command sends
through `IBus.Advanced.Routing` and event publishes through
`IBus.Advanced.Topics`. Bondstone should not support a special partial setup
where command dispatch works because only `IRoutingApi` was registered while
event dispatch depends on an optional `ITopicsApi` branch.

External event handoff remains valid but is separate from Bondstone
subscriber fan-out. A later provider-specific slice may add an event publish
destination that sends to a queue/address for external infrastructure to fan
out. That should be explicit topology vocabulary, not an accidental fallback
from integration events to command routing.

Rebus event publish dispatch keeps the Bondstone wire envelope for internal
Bondstone durable delivery. External interop may later add an explicit
external event wire format, such as payload plus headers or CloudEvents, but
the first-class Bondstone event loop should not implicitly unwrap durable
envelopes.

## Amendment 2026-06-09: Orchestration And Adapter Diversity

Event-driven orchestration does not erase the command/event distinction.
An integration event can trigger a subscriber, saga, process manager, or
orchestrator. That handler can then send durable commands to target modules.
The event still has source-fact semantics and per-subscriber inbox identity;
the follow-up commands still have target-module work-request semantics.

Outbox and inbox are the default durable messaging spine. Bondstone should not
present a local queue that bypasses outbox/inbox as equivalent durable module
messaging. A future in-memory or local adapter may be useful for tests, demos,
or explicitly non-durable development loops, but it must be named and
documented as a weaker contract unless it preserves the same outbox/inbox
semantics.

Adapter diversity should be used as design pressure before Bondstone hardens
too deeply around Rebus, EF Core, or PostgreSQL. After the first-class event
loop has enough shape, add thin proof slices for additional transport and
persistence adapters. Good candidates are Azure Service Bus and RabbitMQ
transport adapters, plus one non-EF persistence adapter such as direct ADO.NET
or Dapper. These slices should validate provider-native topology vocabulary,
durable message routing, event subscription shape, transaction boundaries,
and test fixtures without trying to make every adapter production-complete in
one pass.

## Consequences

Event publication can now move beyond outbox staging without changing command
routing. Commands keep queue-backed point-to-point destinations; events get
topic-backed fan-out.

Subscriber identity becomes part of the durable receive contract before the
receive implementation exists. This protects event inbox keys from accidental
dependency on handler CLR names or Rebus endpoint details.

The Rebus adapter can add event publish dispatch in a small slice by resolving
topics and calling the Rebus topics API, while leaving subscription binding
and subscriber execution for later slices.

Applications keep direct control of Rebus infrastructure and operational
policy. Bondstone describes durable event topology but does not hide
subscription storage, broker setup, retry/dead-letter policy, or workers.

Stable docs can now describe Phase 5 as an accepted contract with partial
implementation, rather than as only future vocabulary.

## Related Decisions

- [0004 Positioning And Service Extraction Path](0004-positioning-and-service-extraction-path.md)
- [0015 Inbox Handle-Once Orchestration](0015-inbox-handle-once-orchestration.md)
- [0018 Rebus Outbox Transport Adapter](0018-rebus-outbox-transport-adapter.md)
- [0023 Rebus Receive-Side Inbox Integration](0023-rebus-receive-side-inbox-integration.md)
- [0025 Module Command Execution Boundary](0025-module-command-execution-boundary.md)
- [0026 Event Shape Guardrail](0026-event-shape-guardrail.md)
- [0029 Durable Payload Serialization Boundary](0029-durable-payload-serialization-boundary.md)
- [0032 Module-Owned Durable EF Persistence](0032-module-owned-durable-ef-persistence.md)

## Application Notes

- 2026-06-09 reset: Rebus-specific transport application has
  been superseded by
  [ADR 0036](0036-direct-transport-adapters-and-rebus-removal.md). The current
  durable event contract that remains applied is core event publish staging,
  module subscriber registration/execution, per-subscriber inbox identity,
  EF/PostgreSQL persistence behavior, and the sample proof over the
  provider-neutral receive pipelines. Direct RabbitMQ and Service Bus receive
  adapters are now applied by later ADRs.
- Current contract: Accepted Phase 5 event topology and execution direction.
  Core event publish staging, module event registration metadata, subscriber
  execution, per-subscriber inbox orchestration, EF event subscriber
  transactions, direct transport event routing, direct receive dispatch, and
  sample event proof are applied. RabbitMQ uses exchange/routing-key or queue
  event destinations. Service Bus uses topic or queue event destinations.
  Queue-style event destinations validate receive binding fan-out according
  to ADR 0040.
- Stable docs: Current Phase 5 event direction is described in
  [docs/architecture/README.md](../architecture/README.md),
  [docs/architecture/messaging.md](../architecture/messaging.md),
  [docs/architecture/modules.md](../architecture/modules.md),
  [docs/architecture/persistence.md](../architecture/persistence.md),
  [docs/architecture/persistence-core.md](../architecture/persistence-core.md),
  [docs/architecture/transport-local.md](../architecture/transport-local.md),
  [docs/architecture/transport-rabbitmq.md](../architecture/transport-rabbitmq.md),
  [docs/architecture/transport-servicebus.md](../architecture/transport-servicebus.md),
  [docs/packaging.md](../packaging.md), [docs/setup.md](../setup.md),
  [docs/testing.md](../testing.md), [docs/samples.md](../samples.md), and
  [docs/mvp-plan.md](../mvp-plan.md).
- Agent guidance: Root [AGENTS.md](../../AGENTS.md) already requires ADR
  review before public API, durable behavior, provider, transport, module
  runtime, topology, and sample architecture changes. No new repository skill
  is required for this one-off implementation slice.
- Application evidence: Core event shape, event publish staging, event
  subscriber registration/execution, per-subscriber inbox identity, EF event
  subscriber transactions, direct transport event dispatch, direct receive
  topology validation, queue fan-out diagnostics, local transport sample
  execution, RabbitMQ sample path, and provider-backed receive integration
  tests are applied.
- Pending or deferred: None for the first-class event loop decision. External
  event wire formats, event choreography samples, domain event persistence,
  automatic integration-event publication, retry state, failure state, stale
  receive recovery, broker-specific topology creation, and any public
  cross-provider diagnostic report object remain separate future decisions.

## Verification

Read back this ADR and affected stable docs. Verified the first event publish
topology implementation slice with:

- `dotnet test tests/Bondstone.Transport.Rebus.Tests/Bondstone.Transport.Rebus.Tests.csproj --configuration Release --filter "Category=Unit|Category=Application" --disable-build-servers`
- `dotnet test Bondstone.slnx --configuration Release --filter "Category=Unit|Category=Application" --disable-build-servers`

After the native Rebus bus clarification, verification was rerun with:

- `dotnet test tests/Bondstone.Transport.Rebus.Tests/Bondstone.Transport.Rebus.Tests.csproj --configuration Release --filter "Category=Unit|Category=Application" --disable-build-servers`

After the core event subscriber execution and per-subscriber inbox slice,
verification was rerun with:

- `dotnet test tests/Bondstone.Tests/Bondstone.Tests.csproj --configuration Release --filter "Category=Unit" --disable-build-servers`

After the EF Core event subscriber transaction slice, verification was rerun
with:

- `dotnet test tests/Bondstone.EntityFrameworkCore.Tests/Bondstone.EntityFrameworkCore.Tests.csproj --configuration Release --filter "Category=Unit|Category=Application" --disable-build-servers`

After the Rebus event receive binding slice, verification was rerun with:

- `dotnet test tests/Bondstone.Transport.Rebus.Tests/Bondstone.Transport.Rebus.Tests.csproj --configuration Release --filter "Category=Unit|Category=Application" --disable-build-servers`
- `dotnet test Bondstone.slnx --configuration Release --filter "Category=Unit|Category=Application" --disable-build-servers`
- `dotnet build Bondstone.slnx --configuration Release --no-restore --disable-build-servers`
- `pnpm format:check`
- `git diff --check`

After documenting orchestration, durable local-queue boundaries, and adapter
diversity priority, read back the ADR and affected stable docs.

After the Rebus in-memory transport-backed event receive proof, verification
was rerun with:

- `dotnet test tests/Bondstone.Transport.Rebus.Tests/Bondstone.Transport.Rebus.Tests.csproj --configuration Release --filter "Category=Integration" --disable-build-servers`

After the Rebus event subscription diagnostics and modular monolith event
sample adoption proof, verification was rerun with:

- `dotnet test tests/Bondstone.Transport.Rebus.Tests/Bondstone.Transport.Rebus.Tests.csproj --configuration Release --filter "Category=Unit|Category=Application" --disable-build-servers`
- `dotnet build Bondstone.slnx --configuration Release --no-restore --disable-build-servers`
- `dotnet test tests/Bondstone.Samples.Tests/Bondstone.Samples.Tests.csproj --configuration Release --no-build --filter "Category=Integration" --disable-build-servers`

Final Phase 5 closeout verification was rerun with:

- `dotnet test Bondstone.slnx --configuration Release --filter "Category=Unit|Category=Application" --disable-build-servers`
- `dotnet test tests/Bondstone.Transport.Rebus.Tests/Bondstone.Transport.Rebus.Tests.csproj --configuration Release --filter "Category=Integration" --disable-build-servers`
- `dotnet test tests/Bondstone.Samples.Tests/Bondstone.Samples.Tests.csproj --configuration Release --filter "Category=Integration" --disable-build-servers`
- `dotnet build Bondstone.slnx --configuration Release --no-restore --disable-build-servers`
- `pnpm format:check`
- `git diff --check`
