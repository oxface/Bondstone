# MVP Plan

This document is the active implementation plan for making Bondstone useful as
a standalone library. It replaces the old tactical backlog. Historical source
notes are archived under [archive/](archive/) and should be treated as source
archaeology, not current direction.

## Purpose

Bondstone is no longer trying to preserve compatibility with the historical
template repository. The product goal is a focused .NET library for durable
module boundaries:

- outbox-backed durable commands;
- explicit durable integration events;
- EF Core backed inbox/outbox persistence;
- module-owned command execution and transactions;
- transport adapters that keep provider infrastructure configuration native;
- a low-friction path from modular monolith to split services.

Use this plan for current scope, priority, and slice sequencing. Use ADRs for
durable technical decisions and architecture docs for the current operating
contract.

## Current Position

Bondstone has the core durable-command spine in place but does not yet have a
complete app-facing receive loop or first-class event handling.

Implemented surface includes:

- core message identity, message type registration, trace context, durable
  command send result, durable event publish result, durable operation read,
  and durable envelope contracts;
- provider-neutral persistence records and boundaries for outbox, inbox,
  operation state, outbox claiming, outbox lease renewal, outbox dispatch
  recording, inbox registration, and delegate-based inbox handle-once
  execution;
- EF Core entity mappings, outbox writer, inbox store, operation state store,
  and EF persistence scope;
- PostgreSQL registration, duplicate classification, inbox registration,
  outbox claiming, outbox lease renewal, and dispatch recording;
- hosted outbox worker composition over `IDurableOutboxDispatcher`;
- Rebus outgoing command transport for claimed outbox records, including
  destination resolution, command destination diagnostics, wire-envelope
  mapping, durable headers, and W3C trace headers;
- Rebus outgoing event publish transport for claimed event outbox records,
  including explicit event-topic mapping, event-topic convention fallback,
  event-topic diagnostics, wire-envelope mapping, durable headers, and W3C
  trace headers;
- Rebus low-level receive inbox adapter and typed command receive pipeline;
- Rebus module command receive pipeline groundwork that dispatches wire
  envelopes into `IModuleCommandExecutor` with explicit durable inbox records;
- host-owned Rebus command topology with conventional module queue naming,
  convention fallback routing, receive endpoint bindings, and explicit
  destination overrides;
- module registration and command execution in core, including
  `IBondstoneModule`, `ICommand`, `IDurableCommand`, direct typed handlers,
  validators, startup scanning, cached routes, scoped execution, module
  metadata, durable-messaging capability metadata, module persistence
  metadata, source-module execution context, ordered system behaviors, receive
  inbox behavior, durable-messaging capability validation, and execution
  results;
- default outbox-backed `IDurableCommandSender` that requires current module
  execution context and uses the executing module as source module;
- default outbox-backed `IDurableEventPublisher` that requires current module
  execution context, uses the executing module as source module, and stages
  `MessageKind.Event` envelopes without target modules;
- shared durable payload serialization through a core
  `IDurablePayloadSerializer` with a default System.Text.Json implementation
  and one durable JSON configuration surface for current command send, event
  publish, and Rebus command receive paths;
- granular provider-neutral EF Core mapping helpers for outbox, inbox, and
  operation state, with `ApplyBondstonePersistence` retained as the full
  convenience mapping helper;
- module event registration metadata for published integration events,
  subscriber handler identity, per-subscriber inbox-key naming, and
  command/event topology diagnostic vocabulary;
- module-owned EF Core persistence opt-in and EF-specific module command
  transaction behavior over `IEntityFrameworkCorePersistenceScope`;
- module-aware durable EF persistence resolution for source-module outbox
  sends, target-module receive inbox handling, target-module operation
  completion, aggregate operation reading, and aggregate local module outbox
  dispatching;
- fluent `AddBondstone` composition and outbox capability validation for
  hosted or dispatcher-based processing.

## MVP Priority Phases

### Phase 0: Durable Message Shape Guardrail

Status: **Complete**.

Accepted decision: [ADR 0026](adr/0026-event-shape-guardrail.md).

This phase exists to prevent command-only naming and topology assumptions from
hardening before serializer configuration, receive listener binding, and
diagnostics are built.

The near-term goal is guardrail, not full event transport implementation.
Bondstone should acknowledge events early because they are essential to
modular-monolith workflows, while keeping actual event fan-out, Rebus
publish/subscribe, and event choreography samples for later phases. The same
guardrail should also protect shared durable-message concepts such as payload
serialization, diagnostics, inbox identity, topology naming, and envelope
semantics across commands and events.

Slices:

1. Create a durable message/event ADR/design note. **Done by ADR 0026.**
   - event publisher shape;
   - event handler/subscriber shape;
   - outbox-backed event publish;
   - per-subscriber inbox identity;
   - fan-out versus point-to-point semantics;
   - Rebus topic/subscription vocabulary;
   - durable payload serialization boundaries;
   - shared command/event diagnostics implications.
2. Update stable docs for the current command/event/domain-event split.
   **Done.**
3. Add only low-risk core shape if accepted. **Done.**
   - explicit durable event publisher contract;
   - event subscriber registration metadata;
   - stable subscriber identity metadata for inbox identity;
   - diagnostics vocabulary that can describe command routes and event
     subscriptions without assuming a command-only topology.

Remaining in Phase 0: **complete**.

### Phase 1: Durable Payload Serialization

Status: **Complete for the current MVP surface**.

Current phase: **Phase 1**.

Current/next slice: **complete; future event receive applies the same boundary
when Phase 5 event subscriber execution is implemented**.

Accepted decision:
[ADR 0029](adr/0029-durable-payload-serialization-boundary.md).

Goal: commands and events use one durable payload serialization configuration
surface so consumers can configure JSON options and converters without
transport-specific duplication.

Slices:

1. ADR/design for durable payload serialization. **Done by ADR 0029.**
   - shared JSON options or serializer abstraction for send and receive;
   - command and event payload parity;
   - provider-neutral core contract versus transport-specific registration;
   - compatibility expectations for stored payloads.
2. Implement the accepted serializer configuration shape. **Done.**
   - use it in `IDurableCommandSender`;
   - use it in `IDurableEventPublisher`;
   - use it in Rebus command receive pipelines;
   - add focused tests for custom converters/options.

Remaining in Phase 1: **complete for the current command send, event publish,
and command receive surface; future event receive remains in Phase 5**.

### Phase 2: Optional Persistence Mapping

Status: **Complete for the current MVP surface**.

Accepted decision:
[ADR 0027](adr/0027-optional-ef-core-persistence-mapping.md).

Current/next slice: **complete; move to Phase 3 durable command loop work**.

Goal: modules that only need module-owned persistence should not be forced to
map durable messaging tables. Durable messaging modules should still get clear
validation that required inbox/outbox/operation-state pieces exist.

Slices:

1. ADR/design for optional persistence mapping. **Done by ADR 0027.**
   - keep `ApplyBondstonePersistence` as the convenience mapping;
   - add granular mapping such as outbox, inbox, and operation-state mappings;
   - define how module durable-messaging capability validates required
     mappings and provider registrations.
2. Implement granular EF Core mapping helpers and focused tests. **Done.**
3. Update setup and architecture docs for the current full and granular
   mapping surface. **Done.**
4. Implement durable-messaging capability validation that ties configured
   module capabilities to required mapped persistence pieces. **Done for the
   current EF `UseDurableMessaging` surface by requiring outbox and inbox
   mappings.**

Remaining in Phase 2: **complete for the current MVP surface. Provider-specific
schema validation remains later provider work.**

### Phase 3: Usable Durable Command Loop

Goal: a consumer can register a module, send a durable command through outbox
and Rebus, receive it through module command execution, persist handler state
and inbox markers in one EF transaction, and understand why routing worked or
failed.

Already done:

- command contracts, identities, handlers, validators, and module executor;
- source-module scoped durable sender;
- EF transaction behavior for modules that opt into EF persistence;
- outbox writer, dispatcher, hosted worker, and Rebus outgoing transport;
- Rebus module command receive pipeline groundwork;
- Rebus command topology conventions and explicit overrides.
- build-time validation that modules using durable messaging also declare
  persistence, durable command handlers belong to durable-messaging modules,
  and Rebus receive endpoints bind only registered local durable-messaging
  modules with durable command handlers.
- Rebus command destination diagnostics that report explicit route, receive
  endpoint binding, module queue convention, or missing destination outcomes.
- Rebus endpoint dispatcher that validates a receive endpoint name and target
  module before delegating to the module command receive pipeline.
- Rebus module command endpoint handler that binds a configured Rebus receive
  endpoint name to the endpoint dispatcher while leaving broker, worker, retry,
  dead-letter, serializer, and input queue setup Rebus-native.
- durable operation-state integration for caller-supplied operation ids:
  command send stages `Pending`, successful module command receive stages
  `Completed`, and `IDurableOperationReader` can observe those states.

Remaining slices:

1. Durable-messaging capability validation:
   - **Done for build-time module and Rebus receive topology checks.**
   - Remaining: validate missing outbound command destination pieces through
     topology diagnostics, and add deeper operation-state/provider-specific
     validation when those capabilities are implemented.
2. Command topology diagnostics:
   - **Done for Rebus command destination diagnostics.**
   - Remaining: endpoint-dispatch diagnostics remain deferred until listener
     binding needs a preflight surface, and event topic/subscription
     diagnostics remain deferred until first-class events are implemented.
3. Rebus endpoint dispatcher:
   - **Done for explicit `DispatchAsync(endpointName, envelope, ct)` over
     configured receive topology.**
4. Rebus listener binding helper:
   - **Done for a Rebus-native command endpoint handler and DI registration
     helper.**
5. Operation-state integration:
   - **Done for caller-supplied operation ids in the current command loop.**
   - Remaining: failure states, running states, cancellation states, result
     payloads, retry state, stale receive recovery, and provider-specific
     concurrency policy remain deferred.

Remaining in Phase 3: **complete for the current MVP surface**.

### Phase 4: Samples And Adoption Proof

Goal: prove the implemented command-loop and optional persistence mapping are
pleasant enough to adopt before adding more durable capabilities.

This phase should exercise the current Phase 1-3 implementation without
changing the completed phase scope. The first `samples/ModularMonolith`
project is an adoption-proof sample rather than the final polished
consumer-style sample: it should send a durable command through outbox and
Rebus, receive it through module command execution, persist handler state and
inbox markers in one EF transaction, and expose API friction while the MVP
surface is still settling. Once the MVP API is stable, polish or replace it
with a sample that shows the public setup path an application should copy.

Slices:

1. Create the modular monolith sample according to [samples.md](samples.md):
   - **Initial adoption-proof sample added in `samples/ModularMonolith`.**
   - The sample uses `ordering` and `fulfillment` modules with module-owned
     `DbContext` types and PostgreSQL schemas, sends a durable command across
     module persistence ownership, uses Rebus in-memory transport, and uses
     PostgreSQL-backed EF Core persistence for outbox claiming, inbox handling,
     operation state, and module state.
   - It binds one Rebus receive endpoint, `fulfillment-commands`, to the
     fulfillment module command backlog.
2. Add a focused sample verification entrypoint or test shape:
   - **Initial smoke test added in `tests/Bondstone.Samples.Tests`.**
   - It is categorized as `Integration`, so default verification remains fast
     and PostgreSQL/Testcontainers sample verification is explicit.
3. Update [setup.md](setup.md) with the full preferred receive wiring once the
   sample settles the exact shape.
4. Record API friction found by the sample as narrow follow-up slices before
   moving deeper into events or domain-event persistence.
   - Module-owned durable EF persistence was accepted in
     [ADR 0032](adr/0032-module-owned-durable-ef-persistence.md) and applied
     to the sample after the first sample slice exposed the one-`DbContext`
     limitation.
5. Add module-bound PostgreSQL provider registration:
   - **Done.** ADR 0032 was amended to prefer
     `module.UsePostgreSqlPersistence<TDbContext>(...)` for module-owned
     durable EF persistence.
   - Root-level provider registration remains available for compatibility or
     advanced composition, while setup docs and the sample prefer the
     module-bound shape.
6. Reshape the adoption-proof sample into a normal host-style API project:
   - **Done.** The sample is a minimal ASP.NET Core API project using normal
     app registration, Rebus service-provider registration, the Rebus module
     command endpoint handler, and the durable outbox worker. Verification-only
     hosted-service startup, database reset, and polling live in the integration
     test instead of the app entrypoint.
7. Use existing command handler assembly registration in the host-shaped
   sample:
   - **Done.** The sample now splits ordering, fulfillment contracts, and
     fulfillment implementation into module-owned assemblies and uses
     `RegisterFromAssemblyContaining<TMarker>()` for module command handlers.
   - Follow-up: replace or polish the Phase 4 adoption-proof sample after the
     MVP public API settles.
8. Move sample module setup into module-owned registration extensions:
   - **Done.** Ordering and fulfillment assemblies now expose module-owned
     `IBondstoneModule` registration objects plus thin `Add...Module`
     registration methods so the API host composes modules without owning
     module persistence and command-handler details.
9. Fold default Rebus module endpoint handler binding into receive topology:
   - **Done.** Configuring the current single receive endpoint through
     `ReceiveModule(...)` registers the durable module receive pipeline,
     endpoint dispatcher, and matching Rebus envelope handler. Low-level
     explicit endpoint-handler registration remains available for tests and
     advanced composition.

Remaining in Phase 4: **complete for the current adoption-proof surface**.

### Phase 5: First-Class Events Implementation

Goal: publish/subscribe is first-class and durable without turning Bondstone
into a generic bus.

Accepted decision:
[ADR 0033](adr/0033-first-class-event-publish-subscribe-topology.md).

Current/next slice: **module event subscriber execution path**. The first
publish-side implementation slice is applied: Rebus can resolve event
identities to topics and dispatch claimed event outbox records through Rebus
publish/subscribe. Subscriber execution, subscription binding, event receive
inbox behavior, choreography samples, and transport-backed event tests remain
follow-up slices.

Slices:

1. Event outbox staging and dispatch after the guardrail abstractions settle:
   - event staging through `IDurableEventPublisher` is already applied from
     Phase 0;
   - Rebus event topic resolution, diagnostics, and publish dispatch are
     applied with focused unit coverage.
2. Module event handler/subscriber registration execution path:
   - subscriber registration metadata is already applied from Phase 0;
   - subscriber execution remains the next Phase 5 slice.
3. Event subscriber identity and inbox behavior.
4. Rebus event publish topology with topic/exchange naming.
5. Rebus event subscription topology with subscriber endpoint/subscription
   binding.
6. Event diagnostics and transport-backed tests.

Remaining in Phase 5: **large, 4-5 slices after publish-side Rebus dispatch**.

### Phase 6: Adapter Diversity Proof

Goal: test Bondstone's abstractions against more than Rebus, EF Core, and
PostgreSQL before hardening reliability policy too deeply around the first
implementation path.

This phase should be proof-oriented, not a broad production-support promise.
Each adapter slice should be thin enough to reveal API, topology,
transaction-boundary, testing-fixture, and package-boundary gaps while leaving
deep reliability, migration, and operational polish to later work.

Expected slices:

1. ADR/design for adapter-diversity proof scope:
   - supported proof adapters and package names;
   - provider-native topology vocabulary;
   - minimum command/event send and receive behavior to prove;
   - integration-test expectations and required external infrastructure.
2. Azure Service Bus transport proof:
   - command send to queue;
   - event publish/subscription shape using Service Bus topics/subscriptions;
   - provider-native setup remains app-owned.
3. RabbitMQ transport proof:
   - command send through RabbitMQ-native exchange/routing-key/queue
     vocabulary;
   - event publish/subscription shape through exchange/binding semantics;
   - provider-native setup remains app-owned.
4. Non-EF persistence proof, such as direct ADO.NET or Dapper:
   - implement core outbox/inbox/operation-state contracts directly;
   - prove module transaction boundaries without `DbContext`;
   - keep migration/schema strategy narrow and explicit.
5. Compare API friction found by the adapter proofs and record narrow follow-up
   slices before broad reliability hardening.

Remaining in Phase 6: **medium-large, proof-oriented 4-5 slices after Phase 5
has enough command/event loop shape**.

### Phase 7: Reliability And Recovery

Goal: the happy path is operationally resilient.

Slices:

1. Receive retry policy ownership.
2. Stale receive recovery.
3. Stale outbox claim recovery.
4. Cleanup and maintenance workers.
5. Dead-letter routing ownership.
6. Module-targeted outbox workers to avoid noisy-neighbor dispatch across
   independently owned module outboxes.
7. Advanced dispatcher and worker options.

Remaining in Phase 7: **medium-large, 6-7 slices**.

### Phase 8: Provider And Migration Usefulness

Goal: persistence providers are usable in real applications without relying on
hidden project conventions.

Slices:

1. Provider-specific module persistence validation.
2. PostgreSQL `jsonb` payload decision and implementation if accepted.
3. Migration helper strategy.
4. Broader provider fixtures.
5. Operation-state optimistic concurrency policy.

Remaining in Phase 8: **medium, 4-5 slices**.

### Phase 9: Domain Event Persistence Capability

Goal: support transactional collection and persistence of module-local domain
events without forcing consumers to adopt a Bondstone aggregate model or
automatically publishing integration events.
Proposed decision:
[ADR 0028](adr/0028-domain-event-persistence-capability.md).

This phase is a boundary capability, not a DDD framework. It should use narrow
provider-neutral abstractions and provider-specific collectors/stores, then
compose through the module command pipeline. Domain events remain private to a
module unless module code explicitly publishes integration events.

Slices:

1. ADR/design for provider-neutral domain event collection and persistence:
   - collector/store abstractions;
   - naming for the small event-source/buffer/accessor interface or adapter;
   - pipeline ordering relative to validation, transaction, inbox, outbox, and
     `SaveChangesAsync`;
   - clear or mark-collected behavior after successful persistence.
2. EF Core collector/store implementation:
   - discover domain event sources from the EF `ChangeTracker` or configured
     adapters;
   - stage domain event records in the same module transaction;
   - avoid custom DbContext inheritance or `SaveChangesAsync` hijacking.
3. Module opt-in and validation:
   - `UseDomainEventPersistence` or equivalent capability;
   - provider-specific registration validation.
4. Optional mapping helper from domain events to integration events only after
   first-class integration events exist, keeping publication explicit.

Remaining in Phase 9: **medium, 3-4 slices**.

## Verification Surface

Current automated coverage includes:

- neutral unit tests for core messaging and persistence contracts;
- neutral unit tests for module command registration, startup scanning,
  validation, route lookup, module execution context, source-module scoped
  durable sending, source-module scoped durable event publishing, module event
  registration metadata, event subscriber inbox-key naming, receive inbox
  behavior, and direct handler execution;
- neutral unit tests for durable payload JSON converter configuration flowing
  through command send and event publish;
- EF Core unit and application tests for mapping, service registration, store
  staging, persistence-scope validation, module persistence opt-in, and module
  transaction/save behavior;
- PostgreSQL Testcontainers integration tests for real schema creation,
  transactions, savepoints, unique constraints, inbox registration, outbox
  claiming, lease renewal, dispatch recording, dispatcher composition, EF
  persistence-scope behavior, and schema-aware provider registration;
- hosting unit tests for outbox worker options, hosted worker loop behavior,
  DI registration, and builder guardrails;
- Rebus unit tests for outgoing command transport routing, wire-envelope
  mapping, durable headers, trace headers, unsupported event envelopes,
  destination resolution, command receive-side inbox mapping,
  already-received behavior, traceparent validation, typed command
  deserialization, shared durable payload converter configuration, Activity
  creation, registry mismatch failures, topology
  conventions, and DI registration;
- Rebus in-memory transport tests for receive-side `SendLocal` delivery
  through a real Rebus worker, typed receive pipeline execution, queue drain,
  and unknown message identity dead-letter behavior;
- Rebus PostgreSQL transport tests for receive-side `SendLocal` delivery,
  typed receive handling, PostgreSQL-backed acknowledgement/queue drain, and
  PostgreSQL-backed dead-letter behavior;
- cross-package application smoke tests for preferred `AddBondstone`
  composition with PostgreSQL persistence, Rebus transport, hosted outbox
  worker, Rebus inbox execution, typed Rebus command receive execution, and
  explicit EF persistence commit boundaries.

Default gate: `pnpm check`.

Integration gate: `pnpm backend:test:integration` or targeted provider and
transport integration tests when a slice changes provider behavior.

## Deferred Work

- `send and wait` helper behavior and timeout/polling policy.
- Trace context and causation propagation through inbox, outbox, and transport
  adapters beyond the currently implemented Rebus send/receive pieces.
- Envelope content type if non-JSON payloads or explicit JSON contracts become
  necessary.
- Neutral envelope headers if multiple adapters need cross-cutting metadata.
- Scheduling, TTL, priority, reply-to, tenant, or transport-native metadata if
  a later durable scenario justifies it.
- Partition-key ordering and scaling semantics.
- Adapter-diversity proof beyond the current Rebus, EF Core, and PostgreSQL
  path remains scheduled after first-class event loop work has enough shape.
- Provider-specific dispatch behavior beyond PostgreSQL claiming.
- Additional integration tests with neutral Bondstone fixtures.

## Historical Source Notes

The historical template repository is source material only. It is useful for
source archaeology around persistence, inbox/outbox lifecycle, Rebus adapter
reference points, and terminology pressure, but it no longer defines the
Bondstone route.

Do not preserve compatibility with the historical template as a design
constraint. Do not bulk-copy implementation code. If historical source is
consulted, extract only the idea that still fits current ADRs and stable docs.

Archived extraction docs:

- [archive/extraction.md](archive/extraction.md)
- [archive/extraction-plan.md](archive/extraction-plan.md)

## Terminology Notes

Historical source material used `DurableOperationSnapshot` for the read-model
DTO and `DurableOperationState` for the enum. The extracted public contract
uses `DurableOperationState` for the read-model DTO and
`DurableOperationStatus` for the enum because callers read the current
operation state and inspect its status, while `snapshot` sounds like a
persisted projection or event-sourcing term.
