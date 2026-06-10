# MVP Plan

Archived: this document is preserved as the completed MVP implementation
history. It is not active operating guidance.

Use [../README.md](../README.md) for current documentation navigation,
[../architecture/README.md](../architecture/README.md) for runtime contracts,
and [../backlog/04-future-work.md](../backlog/04-future-work.md) for
non-current follow-up ideas.

Historical source notes are archived in this folder and should be treated as
source archaeology, not current direction.

## Purpose

Bondstone is a focused .NET library for durable module boundaries:

- outbox-backed durable commands;
- explicit durable integration events;
- module-owned inbox/outbox persistence;
- module-owned command and event subscriber execution;
- direct transport adapters that keep provider infrastructure configuration
  native;
- a low-friction path from modular monolith to split services.

This plan recorded the completed MVP surface, phase outcomes, and deferred
post-MVP decisions at the time. Use ADRs for durable technical decision
history, architecture docs for the current operating contract, and backlog docs
for non-current follow-up ideas.

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
- module-owned operation-state reads aggregate across module stores with
  explicit status precedence;
- module-owned persistence provider registrations reject duplicate
  module-scoped outbox writer, outbox dispatcher, inbox handler executor, and
  operation-state store bindings with clear diagnostics, and missing
  module-owned services name the declared provider;
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
- startup topology validation for aggregate outbound durable route ownership
  across Local, RabbitMQ, and Service Bus, plus RabbitMQ and Service Bus
  receive bindings and queue-destination event fan-out mismatches;
- provider-neutral module receive pipelines:
  `IModuleCommandReceivePipeline` and `IModuleEventReceivePipeline`;
- EF Core entity mappings, outbox writer, inbox store, operation state store,
  EF persistence scope, and module-owned EF transaction behaviors;
- PostgreSQL EF Core registration, duplicate classification, inbox
  registration, outbox claiming, outbox lease renewal, and dispatch recording;
- PostgreSQL-specific non-EF persistence proof for durable outbox, inbox,
  operation state, module transactions, and mixed-persistence sample pressure;
- outgoing Azure Service Bus and RabbitMQ direct transport proof packages;
- RabbitMQ receive queue topology and dispatcher proof over the neutral
  receive pipelines;
- Service Bus receive source topology and dispatcher proof over the neutral
  receive pipelines;
- native received message mappers for RabbitMQ and Service Bus into
  Bondstone transport message shapes;
- receive settlement handler helpers for RabbitMQ and Service Bus that settle
  native messages only after Bondstone dispatch succeeds;
- opt-in hosted receive lifecycle helpers for RabbitMQ consumers and Service
  Bus processors over configured receive topology;
- Phase 7 provider retry/recovery boundary: Bondstone owns persisted outbox
  retry and terminal failure state, while direct provider receive adapters own
  settlement ordering and diagnostics without owning broker retry/dead-letter
  policy;
- modular monolith sample using EF-backed ordering/fulfillment modules,
  `Bondstone.Persistence.Postgres` billing, explicit integration events, a durable
  outbox worker, and explicit local queue transport over the neutral receive
  pipelines.

Rebus has been removed by
[ADR 0036](../adr/0036-direct-transport-adapters-and-rebus-removal.md). Existing
Rebus ADRs are retained as historical decision trail only.

## MVP Priority Phases

### Phase 0: Durable Message Shape Guardrail

Status: **Complete**.

Accepted decision: [ADR 0026](../adr/0026-event-shape-guardrail.md).

Outcome:

- commands and integration events are distinct durable message kinds;
- integration events use explicit source facts, not target modules;
- subscriber identity is stable consumer-owned identity;
- domain events remain module-local/private.

### Phase 1: Durable Payload Serialization

Status: **Complete**.

Accepted decision:
[ADR 0029](../adr/0029-durable-payload-serialization-boundary.md).

Outcome:

- command send, event publish, and receive pipelines use the shared durable
  payload serializer;
- JSON options and converters use one Bondstone configuration surface.

### Phase 2: Optional Persistence Mapping

Status: **Complete for the current MVP surface**.

Accepted decision:
[ADR 0027](../adr/0027-optional-ef-core-persistence-mapping.md).

Outcome:

- `ApplyBondstonePersistence` remains the full EF convenience mapping;
- granular outbox, inbox, and operation-state mappings are available;
- durable-messaging EF modules validate required outbox/inbox mappings.

### Phase 3: Usable Durable Command Loop

Status: **Complete for the current MVP surface**.

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
[ADR 0032](../adr/0032-module-owned-durable-ef-persistence.md).

Outcome:

- modules own persistence declarations;
- modular monolith sample uses module-owned `DbContext` types and schemas;
- source outbox writes, target inbox handling, handler state, operation state,
  and outgoing messages resolve through module-owned persistence boundaries.

### Phase 5: First-Class Events

Status: **Complete for the current MVP surface**.

Accepted decision:
[ADR 0033](../adr/0033-first-class-event-publish-subscribe-topology.md).

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
non-EF persistence proof**.

Accepted decisions:

- [ADR 0034](../adr/0034-adapter-diversity-proof-transports.md)
- [ADR 0035](../adr/0035-postgresql-dapper-persistence-proof.md)

Outcome:

- `Bondstone.Transport.ServiceBus` sends claimed command records to queues and
  event records to topic or queue destinations using provider-native
  vocabulary;
- `Bondstone.Transport.RabbitMq` publishes claimed command and event records
  through exchange/routing-key or queue topology using provider-native
  vocabulary;
- `Bondstone.Persistence.Postgres` proves durable module persistence
  without EF Core and participates in the mixed-persistence sample.

### Phase 6.5: Direct Adapter Reset

Status: **Complete**.

Accepted decisions:

- [ADR 0036](../adr/0036-direct-transport-adapters-and-rebus-removal.md)
- [ADR 0037](../adr/0037-postgresql-persistence-package-identity.md)

Goal: remove adapter-on-adapter design pressure and make direct provider
adapters the reference architecture before Phase 7 hardening.

Outcome:

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
- add RabbitMQ receive queue bindings and
  `IRabbitMqReceivedMessageDispatcher` as the first direct receive proof. This
  maps received Bondstone RabbitMQ messages into the neutral command/event
  receive pipelines.
- add Service Bus receive source bindings and
  `IServiceBusReceivedMessageDispatcher` as the first Service Bus receive
  proof. This maps received Bondstone Service Bus messages from queues or
  topic subscriptions into the neutral command/event receive pipelines but
  does not own broker administration.
- add native receive message mappers so RabbitMQ `BasicDeliverEventArgs` and
  Service Bus `ServiceBusReceivedMessage` can be converted into Bondstone
  transport messages before dispatch while leaving native acknowledgement
  timing app-owned.
- add receive settlement handler helpers so app-owned RabbitMQ consumers and
  Service Bus processors can compose native mapping, Bondstone dispatch, and
  caller-supplied acknowledgement/completion in the correct order.
- add opt-in hosted receive lifecycle helpers so applications can start
  RabbitMQ consumers and Service Bus processors over configured receive
  topology without Bondstone owning broker administration or retry policy.
- add the first RabbitMQ broker-backed receive worker integration test,
  proving real queue delivery through the opt-in worker and broker
  acknowledgement after successful command receive dispatch.
- deepen RabbitMQ broker-backed receive worker coverage to prove failed
  dispatch is negatively acknowledged with `requeue: false` and handed to
  application-owned RabbitMQ dead-letter topology.
- deepen RabbitMQ broker-backed receive worker coverage to prove one event
  queue delivery is dispatched once per configured subscriber identity before
  broker acknowledgement.
- add Service Bus emulator-backed receive worker integration tests, proving
  queue command completion, failed dispatch handoff to the Service Bus
  dead-letter subqueue, and topic subscription fan-out to configured
  subscriber identities.
- rename the non-EF PostgreSQL package from the Dapper implementation identity
  to `Bondstone.Persistence.Postgres`, while keeping Dapper internal and
  leaving EF package identities unchanged.
- supplement the modular monolith sample with an explicit
  `AddModularMonolithSampleWithRabbitMq(...)` provider-backed path and a
  RabbitMQ sample smoke test.

Deferred beyond Phase 6.5:

- broker recovery diagnostics, covered in Phase 7 by ADR 0038;
- broker topology declaration helpers, if accepted by ADR;
- any broader EF persistence package-family rename.

### Phase 7: Direct Provider And Persistence API Hardening

Status: **Complete for the current MVP surface**.

Accepted decisions:

- [ADR 0038](../adr/0038-provider-retry-recovery-and-settlement-boundaries.md)
- [ADR 0039](../adr/0039-startup-transport-topology-validation.md)
- [ADR 0040](../adr/0040-event-queue-fanout-diagnostics.md)

Goal: harden direct provider transport and persistence APIs before broader
public polish.

Outcome:

- define the provider retry/recovery boundary: core owns persisted outbox
  retry and terminal failure state; direct provider receive adapters own
  settlement ordering and diagnostics; broker retry/dead-letter policy remains
  app-owned and provider-native.
- clarify the MVP receive registration contract: Bondstone binds to
  provider-native receive entities and exposes bounded worker options, while
  RabbitMQ DLX/retry topology and Service Bus max-delivery/dead-letter policy
  stay app-owned.
- improve RabbitMQ and Service Bus receive failure diagnostics around native
  settlement handoff.
- add RabbitMQ and Service Bus startup topology validation against registered
  durable command handlers, published events, and event subscribers while
  keeping broker provisioning deferred.
- add aggregate outbound route ownership validation across Local, RabbitMQ,
  and Service Bus transport diagnostics so zero-route and ambiguous-route
  durable command/event topology failures are loud during startup.
- harden operation-state reader coverage and document current module-store
  status precedence without adding default running/failure transitions.
- harden module-owned persistence provider contracts so duplicate
  `IDurableModule*` registrations for the same module fail with clear
  diagnostics.
- extend module-owned persistence diagnostics to outbox dispatchers and missing
  module service registrations so errors name the declared provider.
- cover missing single-transport event subscriber receive bindings in RabbitMQ
  and Service Bus topology validation tests.
- add queue-destination event fan-out diagnostics so RabbitMQ and Service Bus
  fail startup when direct queue event routes are paired with split subscriber
  receive bindings, while preserving same-queue in-process fan-out.

Deferred beyond Phase 7:

- refine multi-transport diagnostics into a public report shape if accepted by
  ADR;
- improve operation-state failure/running/retry semantics if a safe model is
  accepted by ADR;
- add stale inbox receive recovery if a safe model is accepted by ADR;
- add broker topology declaration helpers if accepted by ADR;
- broaden provider-backed integration tests beyond the current command
  success, failure handoff, and event fan-out coverage.

## Verification Policy

Default verification should stay fast:

- `dotnet restore Bondstone.slnx --disable-build-servers -p:NuGetAudit=false`
- `dotnet build Bondstone.slnx --configuration Release --no-restore --disable-build-servers`
- `dotnet test Bondstone.slnx --configuration Release --no-build --filter "Category=Unit|Category=Application" --disable-build-servers`
- `pnpm format:check`
- `git diff --check`

Infrastructure-backed tests remain explicit:

- PostgreSQL and sample tests use `Category=Integration`;
- RabbitMQ and Service Bus provider-backed receive tests must also use
  `Category=Integration`.
