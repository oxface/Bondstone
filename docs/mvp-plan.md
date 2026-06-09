# MVP Plan

This document is the active implementation plan for making Bondstone useful as
a standalone library. Historical source notes are archived under
[archive/](archive/) and should be treated as source archaeology, not current
direction.

## Purpose

Bondstone is a focused .NET library for durable module boundaries:

- outbox-backed durable commands;
- explicit durable integration events;
- module-owned inbox/outbox persistence;
- module-owned command and event subscriber execution;
- direct transport adapters that keep provider infrastructure configuration
  native;
- a low-friction path from modular monolith to split services.

Use this plan for current scope, priority, and slice sequencing. Use ADRs for
durable technical decisions and architecture docs for the current operating
contract.

## Current Position

Implemented surface includes:

- core message identity, message type registration, trace context, durable
  command send result, durable event publish result, durable operation read,
  and durable envelope contracts;
- shared durable payload serialization through `IDurablePayloadSerializer`;
- provider-neutral persistence records and boundaries for outbox, inbox,
  operation state, outbox claiming, outbox lease renewal, outbox dispatch
  recording, inbox registration, and delegate-based inbox handle-once
  execution;
- default durable outbox dispatcher and hosted worker composition;
- module registration and command execution in core, including
  `IBondstoneModule`, `ICommand`, `IDurableCommand`, typed handlers,
  validators, cached routes, scoped execution, module metadata,
  source-module execution context, ordered system behaviors, receive inbox
  behavior, and durable-messaging capability validation;
- default outbox-backed `IDurableCommandSender` and
  `IDurableEventPublisher`;
- module event registration metadata for published integration events,
  subscriber handler identity, per-subscriber inbox-key naming, and
  command/event topology diagnostic vocabulary;
- provider-neutral module receive pipelines:
  `IModuleCommandReceivePipeline` and `IModuleEventReceivePipeline`;
- EF Core entity mappings, outbox writer, inbox store, operation state store,
  EF persistence scope, and module-owned EF transaction behaviors;
- PostgreSQL EF Core registration, duplicate classification, inbox
  registration, outbox claiming, outbox lease renewal, and dispatch recording;
- PostgreSQL Dapper-assisted persistence proof for durable outbox, inbox,
  operation state, module transactions, and mixed-persistence sample pressure;
- outgoing Azure Service Bus and RabbitMQ direct transport proof packages;
- modular monolith sample using EF-backed ordering/fulfillment modules,
  PostgreSQL Dapper-assisted billing, explicit integration events, a durable
  outbox worker, and explicit local queue transport over the neutral receive
  pipelines.

Rebus has been removed by
[ADR 0036](adr/0036-direct-transport-adapters-and-rebus-removal.md). Existing
Rebus ADRs are retained as historical decision trail only.

## MVP Priority Phases

### Phase 0: Durable Message Shape Guardrail

Status: **Complete**.

Accepted decision: [ADR 0026](adr/0026-event-shape-guardrail.md).

Outcome:

- commands and integration events are distinct durable message kinds;
- integration events use explicit source facts, not target modules;
- subscriber identity is stable consumer-owned identity;
- domain events remain module-local/private.

### Phase 1: Durable Payload Serialization

Status: **Complete**.

Accepted decision:
[ADR 0029](adr/0029-durable-payload-serialization-boundary.md).

Outcome:

- command send, event publish, and receive pipelines use the shared durable
  payload serializer;
- JSON options and converters use one Bondstone configuration surface.

### Phase 2: Optional Persistence Mapping

Status: **Complete for the current MVP surface**.

Accepted decision:
[ADR 0027](adr/0027-optional-ef-core-persistence-mapping.md).

Outcome:

- `ApplyBondstonePersistence` remains the full EF convenience mapping;
- granular outbox, inbox, and operation-state mappings are available;
- durable-messaging EF modules validate required outbox/inbox mappings.

### Phase 3: Usable Durable Command Loop

Status: **Complete for the core and EF/PostgreSQL path; direct provider
receive adapters remain Phase 6.5/7 work**.

Outcome:

- modules register durable command handlers and execute through
  `IModuleCommandExecutor`;
- durable command sends stage command envelopes in the source module outbox;
- receive pipelines execute command envelopes through target module inbox and
  transaction behavior;
- caller-supplied operation ids are staged as `Pending` on send and
  `Completed` on successful command receive;
- EF/PostgreSQL module-owned persistence proves one transaction boundary per
  receiving module.

### Phase 4: Module-Owned Persistence

Status: **Complete**.

Accepted decision:
[ADR 0032](adr/0032-module-owned-durable-ef-persistence.md).

Outcome:

- modules own persistence declarations;
- modular monolith sample uses module-owned `DbContext` types and schemas;
- source outbox writes, target inbox handling, handler state, operation state,
  and outgoing messages resolve through module-owned persistence boundaries.

### Phase 5: First-Class Events

Status: **Complete for core event publishing, subscriber execution, inbox
identity, and sample proof; direct provider receive workers remain later
transport work**.

Accepted decision:
[ADR 0033](adr/0033-first-class-event-publish-subscribe-topology.md).

Outcome:

- `IDurableEventPublisher` stages integration event envelopes;
- modules register published events and typed subscribers;
- `IModuleEventSubscriberExecutor` executes subscribers through event
  pipeline behaviors;
- event inbox identity is per subscriber;
- EF module event subscriber transaction behavior saves handler state, inbox
  marker, and outgoing messages together;
- the sample proves explicit event publication in both module directions.

### Phase 6: Adapter Diversity Proof

Status: **Complete for outgoing direct transport proofs and PostgreSQL
Dapper-assisted persistence proof**.

Accepted decisions:

- [ADR 0034](adr/0034-adapter-diversity-proof-transports.md)
- [ADR 0035](adr/0035-postgresql-dapper-persistence-proof.md)

Outcome:

- `Bondstone.Transport.ServiceBus` sends claimed command records to queues and
  event records to topic or queue destinations using provider-native
  vocabulary;
- `Bondstone.Transport.RabbitMq` publishes claimed command and event records
  through exchange/routing-key or queue topology using provider-native
  vocabulary;
- `Bondstone.Persistence.Dapper.Postgres` proves durable module persistence
  without EF Core and participates in the mixed-persistence sample.

### Phase 6.5: Direct Adapter Reset

Status: **In progress**.

Accepted decision:
[ADR 0036](adr/0036-direct-transport-adapters-and-rebus-removal.md).

Goal: remove adapter-on-adapter design pressure and make direct provider
adapters the reference architecture before Phase 7 hardening.

Applied in this slice:

- remove the Rebus package and tests from the solution;
- remove Rebus package versions and sample references;
- move reusable module receive behavior into core neutral receive pipelines;
- update composition tests around RabbitMQ and core receive;
- update stable docs and agent guidance to direct provider adapters;
- keep the sample durable loop alive through explicit
  `Bondstone.Transport.Local` queue routing.
- allow event publish routes to target provider-native destinations instead of
  assuming every event route is a topic: Service Bus supports topics and
  queues; RabbitMQ supports exchange/routing-key routes and direct queue
  routes through the default exchange.
- add explicit multi-transport outbox selection through
  `IDurableOutboxTransportRoute` and `RoutedDurableOutboxTransport`, so a
  claimed durable message is sent only when exactly one direct provider route
  matches.

Remaining slices:

1. Implement RabbitMQ receive worker proof:
   command queue receive, event queue binding receive, acknowledgement,
   retry/dead-letter handoff, diagnostics, and provider-backed tests.
2. Implement Service Bus receive worker proof:
   command queue processor, event topic subscription or event queue processor,
   acknowledgement, retry/dead-letter handoff, diagnostics, and
   provider-backed tests.
3. Decide persistence package naming:
   whether `Bondstone.Persistence.Dapper.Postgres` becomes
   `Bondstone.Persistence.Postgres`, and whether EF packages gain
   `Persistence` in their public package identity.
4. Replace or supplement local transport in the sample with one preferred
   direct provider receive path once a direct receive adapter is ready.

### Phase 7: Hardening And Public API Tightening

Status: **Not started**.

Start Phase 7 only after Phase 6.5 no longer leaves the repository in a
transition state.

Likely goals:

- harden direct provider transport receive semantics;
- refine multi-transport routing and diagnostics;
- tighten persistence package naming and provider contracts;
- improve operation-state failure/running/retry semantics;
- add stale inbox receive recovery if a safe model is accepted by ADR;
- add provider-backed integration tests for receive reliability;
- polish the sample into the preferred public API path.

## Verification Policy

Default verification should stay fast:

- `dotnet restore Bondstone.slnx --disable-build-servers -p:NuGetAudit=false`
- `dotnet build Bondstone.slnx --configuration Release --no-restore --disable-build-servers`
- `dotnet test Bondstone.slnx --configuration Release --no-build --filter "Category=Unit|Category=Application" --disable-build-servers`
- `pnpm format:check`
- `git diff --check`

Infrastructure-backed tests remain explicit:

- PostgreSQL and sample tests use `Category=Integration`;
- future RabbitMQ and Service Bus provider-backed receive tests must also use
  `Category=Integration`.
