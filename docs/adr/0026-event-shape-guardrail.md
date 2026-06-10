# 0026 Event Shape Guardrail

Status: Amended
Application: Applied
Date: 2026-06-07

## Context

Bondstone is built around durable module boundaries for modular monoliths with
a low-friction path to service extraction. The current implemented spine is
command-first: durable commands have stable identity, outbox-backed send,
Rebus outgoing transport, receive-side inbox groundwork, module command
execution, and EF-backed module transactions.

Events are also essential to modular-monolith workflows. They represent facts
that other modules can react to, and they will need fan-out semantics,
subscriber-owned inbox identity, and transport topology that differs from
point-to-point commands. If Bondstone keeps building command-only routing,
diagnostics, and naming assumptions before events have a basic shape, later
event support may require broad rewrites or awkward compatibility seams.

At the same time, full event implementation is larger than the current command
receive-loop gap. Bondstone needs an early guardrail that prevents command-only
drift without pulling Rebus publish/subscribe, event choreography samples, and
all event runtime behavior into the immediate slice.

The guardrail also needs to protect shared durable-message concepts before
payload serialization, command listener binding, and diagnostics harden around
command-specific terms. Durable commands and integration events both cross
module persistence boundaries, both stage payloads through the outbox, both
need stable message identities, and both need receive-side idempotency. Their
topology and handler semantics differ enough that Bondstone should name those
differences now.

## Decision

Bondstone will use a durable message shape guardrail before completing broad
receive listener binding, payload serialization configuration, and topology
diagnostics.

The accepted conceptual split is:

- commands as directed work requests to one target module;
- integration events as durable cross-module facts that may have multiple
  subscribers;
- domain events as module-local facts that are not automatically public
  integration contracts.

The first event slice should focus on design and minimal core API shape. It
formalizes these rules:

- durable commands and integration events are the durable boundary for
  cross-persistence state changes;
- direct `.Contracts` calls are mainly for reads, local composition, or
  operations that tolerate failure without retry and deduplication;
- Bondstone must not introduce a generic mediator or generic message-bus
  layer as the default abstraction for module collaboration;
- transport infrastructure configuration remains provider-native;
- Bondstone transport topology should describe durable message topology, not
  wrap broker connection, worker, serializer, retry, or dead-letter setup;
- domain events are module-local/private and distinct from durable integration
  events.

The durable event publisher shape is a future `Bondstone` core contract,
expected to be named like `IDurableEventPublisher`. It will accept an
`IIntegrationEvent`, stage a `MessageKind.Event` envelope through the durable
outbox, require the current source module, and return an accepted-for-publish
result rather than an immediate subscriber result. Event envelopes must not
specify `TargetModule`. Publication remains explicit module code; Bondstone
must not automatically turn domain events into integration events.

The event handler/subscriber shape is future module registration metadata over
typed integration event handlers, expected to be named like
`IIntegrationEventHandler<TEvent>` or equivalent. A subscriber registration
belongs to a module and carries a stable subscriber identity. Subscriber
identity must be consumer-owned text and must not be derived from handler CLR
names. Registration helpers may default from explicit event identity plus
module/subscriber metadata only when that default remains stable and visible.

Receive-side event idempotency is per subscriber. Event inbox identity is
derived from the durable message id, the subscriber module, and the stable
subscriber identity. It is not derived from command `TargetModule`, because
events intentionally have no target module. Each subscriber gets its own
handle-once boundary and its own retry/dead-letter outcome through the
transport endpoint or subscription that delivers that subscriber's copy.

Commands keep queue-backed point-to-point semantics: one durable command is
addressed to one target module. Integration events use fan-out semantics: one
published event may be delivered to zero or more subscribers, and each
subscription receives its own copy. Bondstone diagnostics and topology names
must distinguish command routes/destinations from event topics,
subscriptions, and subscribers.

For Rebus, later event work should use Rebus publish/subscribe vocabulary.
Bondstone may describe durable event topology with event topics and subscriber
endpoint or subscription bindings. Applications still configure Rebus-native
transport, subscription storage, serializers, workers, retries, dead-letter
policy, and broker connection details through Rebus.

Durable payload serialization is shared command/event infrastructure. The
event guardrail does not decide the concrete serializer API, but it requires a
single durable payload serialization boundary for command send, command
receive, event publish, and event receive. Transport adapters must not rely on
transport CLR type headers as Bondstone durable identity. Content type and
non-JSON payload support remain deferred unless the serialization ADR accepts
them.

Diagnostics should use durable-message vocabulary and specialize by message
kind. Command diagnostics may report target module, destination, route source,
and receive endpoint binding. Event diagnostics may report event identity,
topic, subscriber module, subscriber identity, subscription binding, and
whether there are zero subscribers. Shared diagnostics should include stable
message identity, message kind, source module, payload serialization policy,
and trace/causation information without assuming every message is a command.

The minimal core abstractions that are safe now are the existing
`IIntegrationEvent` marker, `IntegrationEventIdentityAttribute`,
`MessageKind.Event`, kind-aware `MessageTypeRegistry` registrations, and
`DurableMessageEnvelope` validation that event envelopes have no target
module. Do not add event publisher, event handler, subscriber registration, or
event topology public APIs until the next small design/API slice can validate
names against the accepted guardrail.

The guardrail should not implement full event fan-out, Rebus
publish/subscribe, event subscription workers, event choreography samples, or
automatic domain-event-to-integration-event publication.

Event publication should remain explicit. Later mapping helpers may reduce
ceremony when converting a module-local domain event into an integration
event, but they must preserve the visible step where private module state
becomes a durable public contract.

## Amendment 2026-06-07: Minimal Core Guardrail APIs

The follow-up Phase 0 API slice validated the minimal names and applied them
in core without implementing event fan-out or subscriber execution.

`Bondstone` now includes `IDurableEventPublisher`,
`DurableEventPublishResult`, and `DurableEventPublishStatus`. The publisher
uses the current module execution context, stages `MessageKind.Event`
envelopes through `IDurableOutboxWriter`, and leaves `TargetModule` empty.
This is an outbox-backed durable event publish shape, not transport-level
publish/subscribe.

`Bondstone` now also includes module event registration metadata:
`module.Events.RegisterPublishedEvent`,
`module.Events.RegisterSubscriber<TEvent, THandler>`,
`IIntegrationEventHandler<TEvent>`, `IModuleEventSubscriberRegistry`, and
`ModuleEventSubscriberRegistration`. This metadata records stable event
identity, subscriber module, subscriber identity, and handler type. It does
not execute subscribers or bind transport subscriptions.

`DurableInboxMessageKey.ForEventSubscriber` names the per-subscriber inbox-key
shape for future receive paths. `DurableMessageTopologyDiagnosticKind` names
command route/destination/receive-endpoint and event topic/subscription
diagnostic categories so later diagnostics do not assume command-only
topology.

## Consequences

Bondstone can keep finishing the usable durable command loop while ensuring
public APIs, diagnostics, and topology vocabulary do not harden around
commands only.

The first event work stays small enough to fit before command listener
completion. It creates design pressure but deliberately defers the expensive
runtime path.

The command/event/domain-event split becomes clearer for consumers. Commands
remain point-to-point. Integration events are durable cross-module facts.
Domain events remain local to a module unless module code explicitly publishes
integration events.

Future event implementation will still need its own runtime slices for outbox
staging, subscriber execution, Rebus topology, event inbox behavior,
diagnostics, and transport-backed verification.

The first accepted guardrail intentionally names likely future public API
shapes without committing those public contracts in code. This gives command
listener binding and diagnostics a durable-message vocabulary now while
leaving implementation details for smaller reviewed slices.

## Related Decisions

- [0004 Positioning And Service Extraction Path](0004-positioning-and-service-extraction-path.md)
- [0018 Rebus Outbox Transport Adapter](0018-rebus-outbox-transport-adapter.md)
- [0023 Rebus Receive-Side Inbox Integration](0023-rebus-receive-side-inbox-integration.md)
- [0024 Rebus Typed Command Receive Pipeline](0024-rebus-typed-command-receive-pipeline.md)
- [0025 Module Command Execution Boundary](0025-module-command-execution-boundary.md)
- [0028 Domain Event Persistence Capability](0028-domain-event-persistence-capability.md)

## Application Notes

- Current contract: Accepted guardrail. Current runtime implementation remains
  explicit about command/event differences, with core support for
  `IIntegrationEvent`, `IntegrationEventIdentityAttribute`,
  `IDurableEventPublisher`, outbox-backed event envelope staging,
  `MessageKind.Event`, kind-aware message registrations, event subscriber
  registration metadata, event subscriber inbox-key factory naming, and event
  envelopes without target modules. First-class subscriber execution and
  direct transport event routing are now applied by later ADRs.
- Stable docs: Current command/event/domain-event split, durable event
  publisher/subscriber shape, per-subscriber inbox identity, serialization
  boundary, diagnostics vocabulary, and direct transport event vocabulary are
  described in [docs/architecture/messaging.md](../architecture/messaging.md),
  [docs/architecture/modules.md](../architecture/modules.md),
  [docs/architecture/transport-rabbitmq.md](../architecture/transport-rabbitmq.md),
  and [docs/architecture/transport-servicebus.md](../architecture/transport-servicebus.md).
  Sequencing is tracked in [docs/mvp-plan.md](../mvp-plan.md).
- Agent guidance: Root [AGENTS.md](../../AGENTS.md) requires ADR review before
  broad public API, durable behavior, provider, transport, module runtime, or
  topology changes. It now points agents at the accepted command/event/domain
  event split.
- Application evidence: Core messaging tests cover event message identity
  registration and envelope validation. Later event-loop tests cover
  outbox-backed durable event publish staging, event subscriber registration,
  per-subscriber inbox-key naming, command/event topology diagnostics,
  subscriber execution, direct transport event routing, and sample event
  publication/subscription.
- Pending or deferred: None for the event-shape guardrail. Choreography
  samples, automatic domain-event-to-integration-event helpers, and external
  event wire formats remain separate future decisions.

## Verification

Read back the amended ADR and related stable docs. Ran `pnpm check` and
stale-reference scans for ADR 0026, event guardrail status, Phase 0 closeout,
and old command-only/event proposal language.
