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
  destination resolution, wire-envelope mapping, durable headers, and W3C
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
  inbox behavior, validation, and execution results;
- default outbox-backed `IDurableCommandSender` that requires current module
  execution context and uses the executing module as source module;
- default outbox-backed `IDurableEventPublisher` that requires current module
  execution context, uses the executing module as source module, and stages
  `MessageKind.Event` envelopes without target modules;
- module event registration metadata for published integration events,
  subscriber handler identity, per-subscriber inbox-key naming, and
  command/event topology diagnostic vocabulary;
- module-owned EF Core persistence opt-in and EF-specific module command
  transaction behavior over `IEntityFrameworkCorePersistenceScope`;
- fluent `AddBondstone` composition and outbox capability validation for
  hosted or dispatcher-based processing.

## MVP Priority Groups

### Group 0: Durable Message Shape Guardrail

Status: **Complete**.

Accepted decision: [ADR 0026](adr/0026-event-shape-guardrail.md).

This group exists to prevent command-only naming and topology assumptions from
hardening before serializer configuration, receive listener binding, and
diagnostics are built.

The near-term goal is guardrail, not full event transport implementation.
Bondstone should acknowledge events early because they are essential to
modular-monolith workflows, while keeping actual event fan-out, Rebus
publish/subscribe, and event choreography samples for later groups. The same
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

Remaining in Group 0: **complete**.

### Group 1: Durable Payload Serialization

Current group: **Group 1**.

Current/next slice: **durable payload serialization ADR/design**.

Goal: commands and events use one durable payload serialization configuration
surface so consumers can configure JSON options and converters without
transport-specific duplication.

Slices:

1. ADR/design for durable payload serialization:
   - shared JSON options or serializer abstraction for send and receive;
   - command and event payload parity;
   - provider-neutral core contract versus transport-specific registration;
   - compatibility expectations for stored payloads.
2. Implement the accepted serializer configuration shape:
   - use it in `IDurableCommandSender`;
   - use it in Rebus command receive pipelines;
   - add focused tests for custom converters/options.

Remaining in Group 1: **small-medium, 1-2 slices**.

### Group 2: Optional Persistence Mapping

Goal: modules that only need module-owned persistence should not be forced to
map durable messaging tables. Durable messaging modules should still get clear
validation that required inbox/outbox/operation-state pieces exist.
Proposed decision: [ADR 0027](adr/0027-optional-ef-core-persistence-mapping.md).

Slices:

1. ADR/design for optional persistence mapping:
   - keep `ApplyBondstonePersistence` as the convenience mapping;
   - add granular mapping such as outbox, inbox, and operation-state mappings;
   - define how module durable-messaging capability validates required
     mappings and provider registrations.
2. Implement granular EF Core mapping helpers and focused tests if accepted.
3. Update setup and architecture docs so persistence-only modules and durable
   messaging modules have distinct examples.

Remaining in Group 2: **small-medium, 2-3 slices**.

### Group 3: Usable Durable Command Loop

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

Remaining slices:

1. Durable-messaging capability validation:
   - durable modules have persistence when durable messaging is enabled;
   - Rebus receive topology targets registered local modules;
   - durable command handlers are registered on modules that opt into durable
     messaging;
   - missing persistence, transport, or route pieces produce useful errors.
2. Command topology diagnostics:
   - explicit route versus receive binding versus convention fallback versus
     missing route;
   - diagnostic result object before or alongside logging.
3. Rebus endpoint dispatcher:
   - `HandleOnceAsync(endpointName, envelope, ct)`;
   - validate endpoint exists;
   - validate envelope target module is accepted by the endpoint;
   - call `IRebusModuleCommandReceivePipeline`.
4. Rebus listener binding helper:
   - Rebus-native handler or adapter;
   - application still owns Rebus infrastructure configuration;
   - in-memory Rebus transport test proving `SendLocal` dispatches into
     `IModuleCommandExecutor`.
5. Operation-state integration:
   - make `IDurableOperationReader` meaningful beyond current contracts;
   - record durable send and receive state transitions consistently.

Remaining in Group 3: **about 4-5 slices**.

### Group 4: Domain Event Persistence Capability

Goal: support transactional collection and persistence of module-local domain
events without forcing consumers to adopt a Bondstone aggregate model or
automatically publishing integration events.
Proposed decision:
[ADR 0028](adr/0028-domain-event-persistence-capability.md).

This group is a boundary capability, not a DDD framework. It should use narrow
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

Remaining in Group 4: **medium, 3-4 slices**.

### Group 5: First-Class Events Implementation

Goal: publish/subscribe is first-class and durable without turning Bondstone
into a generic bus.

Slices:

1. Event outbox staging and dispatch after the guardrail abstractions settle.
2. Module event handler/subscriber registration execution path.
3. Event subscriber identity and inbox behavior.
4. Rebus event publish topology with topic/exchange naming.
5. Rebus event subscription topology with subscriber endpoint/subscription
   binding.
6. Event diagnostics and transport-backed tests.

Remaining in Group 5: **large, 5-6 slices**.

### Group 6: Reliability And Recovery

Goal: the happy path is operationally resilient.

Slices:

1. Receive retry policy ownership.
2. Stale receive recovery.
3. Stale outbox claim recovery.
4. Cleanup and maintenance workers.
5. Dead-letter routing ownership.
6. Advanced dispatcher and worker options.

Remaining in Group 6: **medium-large, 5-6 slices**.

### Group 7: Provider And Migration Usefulness

Goal: persistence providers are usable in real applications without relying on
hidden project conventions.

Slices:

1. Provider-specific module persistence validation.
2. PostgreSQL `jsonb` payload decision and implementation if accepted.
3. Migration helper strategy.
4. Broader provider fixtures.
5. Operation-state optimistic concurrency policy.

Remaining in Group 7: **medium, 4-5 slices**.

### Group 8: Samples And Adoption Proof

Goal: prove the library works for modular-monolith and service-split usage.

Slices:

1. Modular monolith sample after the command loop and optional persistence
   mapping are usable.
2. Split-service Rebus sample.
3. Event choreography sample after events exist.
4. Setup and production topology documentation cleanup.

Remaining in Group 8: **medium, 3-4 slices**.

## Verification Surface

Current automated coverage includes:

- neutral unit tests for core messaging and persistence contracts;
- neutral unit tests for module command registration, startup scanning,
  validation, route lookup, module execution context, source-module scoped
  durable sending, source-module scoped durable event publishing, module event
  registration metadata, event subscriber inbox-key naming, receive inbox
  behavior, and direct handler execution;
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
  deserialization, Activity creation, registry mismatch failures, topology
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
