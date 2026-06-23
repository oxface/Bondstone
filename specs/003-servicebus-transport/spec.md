# Feature Specification: Service Bus Transport

**Feature Branch**: `N/A - migrated existing functionality`

**Created**: 2026-06-23

**Status**: migrated

**Input**: Existing implementation in `src/Bondstone.Transport.ServiceBus` and tests in `tests/Bondstone.Transport.ServiceBus.Tests`

## Scope And Boundaries _(mandatory)_

**Bondstone Capability**: Azure Service Bus transport adapter

**Affected Packages/Areas**:

- `src/Bondstone.Transport.ServiceBus`
- `tests/Bondstone.Transport.ServiceBus.Tests`
- `docs/setup.md`
- `docs/package-discovery.md`
- `docs/operations.md`
- `docs/observability.md`
- `docs/packaging.md`

**Out Of Scope**:

- Azure Service Bus queue, topic, subscription, rule, credential, retry,
  dead-letter, concurrency, lock renewal policy, and monitoring ownership.
- Durable outbox persistence, durable incoming inbox persistence, and hosted
  durable inbox processing behavior owned by persistence and hosting packages.
- RabbitMQ and local transport behavior.

## User Scenarios & Testing _(mandatory)_

### User Story 1 - Register Service Bus Dispatcher And Worker Setup (Priority: P1)

As a Bondstone host maintainer, I want Service Bus dispatcher and receive worker
composition hooks so a host can opt into Azure Service Bus envelope plumbing
while keeping Service Bus topology and client configuration application-owned.

**Why this priority**: Setup APIs are the entry point for all Service Bus
adapter behavior and define the package's public surface.

**Independent Test**: Run `dotnet test tests/Bondstone.Transport.ServiceBus.Tests/Bondstone.Transport.ServiceBus.Tests.csproj --configuration Release --filter "Category=Unit"` after build.

**Acceptance Scenarios**:

1. **Given** a Bondstone service collection, **When** `UseServiceBusDispatcher(...)` is configured, **Then** the service collection registers `ServiceBusEnvelopeDispatcher` as the `IDurableEnvelopeDispatcher`.
2. **Given** a Service Bus receive worker configuration without exactly one queue or topic/subscription entity, **When** the host registers the worker, **Then** registration fails with an actionable Service Bus entity setup error.
3. **Given** a Service Bus receive worker configuration, **When** `ProcessorOptions.AutoCompleteMessages` is true, **Then** registration fails because Bondstone requires manual completion.
4. **Given** a Service Bus receive worker configuration, **When** `ProcessorOptions.ReceiveMode` is `ReceiveAndDelete`, **Then** registration fails because Bondstone requires `PeekLock`.
5. **Given** event ingestion options with subscriber module and subscriber identity, **When** the host registers the worker, **Then** it registers `ServiceBusReceiveWorker` as an `IHostedService` and preserves the durable subscriber binding.

---

### User Story 2 - Publish Durable Envelopes To Service Bus (Priority: P2)

As a Bondstone host maintainer, I want claimed durable outbox envelopes to be
serialized and published through an application-provided Service Bus client so
commands and events can be routed through host-owned Service Bus entities.

**Why this priority**: Outbound dispatch is the adapter's core transport
responsibility.

**Independent Test**: Run `dotnet test tests/Bondstone.Transport.ServiceBus.Tests/Bondstone.Transport.ServiceBus.Tests.csproj --configuration Release --filter "Category=Integration"` with Service Bus Testcontainers available after build.

**Acceptance Scenarios**:

1. **Given** a durable command envelope and an entity resolver returning a queue name, **When** the dispatcher sends the outbox record, **Then** Service Bus receives a serialized Bondstone envelope with the same message id, subject, and payload.
2. **Given** a durable integration event envelope and an entity resolver returning a topic name, **When** the dispatcher sends the outbox record, **Then** a Service Bus subscription receives the event envelope with the same message id, subject, and payload.
3. **Given** a durable envelope with a durable operation id and module metadata, **When** the dispatcher creates a native message, **Then** it sets the configured content type, correlation id, message kind, source module, and optional target module properties.

---

### User Story 3 - Run Manual Service Bus Receive Processors (Priority: P3)

As a Bondstone maintainer, I want Service Bus receive workers to create native
processors with manual settlement so Service Bus completion happens only after
Bondstone durable receive ingestion succeeds.

**Why this priority**: This preserves the adapter boundary and prevents native
completion before Bondstone-owned durable receive work.

**Independent Test**: Run `dotnet test tests/Bondstone.Transport.ServiceBus.Tests/Bondstone.Transport.ServiceBus.Tests.csproj --configuration Release --filter "Category=Unit|Category=Integration"` after build.

**Acceptance Scenarios**:

1. **Given** a queue receive worker registration, **When** the hosted worker starts, **Then** it creates a queue processor with cloned manual-completion options.
2. **Given** a topic/subscription receive worker registration, **When** the hosted worker starts, **Then** it creates a subscription processor with cloned manual-completion options.
3. **Given** a native Service Bus processor error, **When** the worker receives the error callback, **Then** it logs Service Bus receive failure event id `3001`.
4. **Given** the worker is stopping, **When** cancellation is requested, **Then** active processors are stopped and disposed.

---

### User Story 4 - Ingest Service Bus Deliveries Into Durable Incoming Inbox (Priority: P4)

As a Bondstone host maintainer, I want Service Bus receive workers to ingest
deliveries into the durable incoming inbox ledger so command and event handling
can be processed later by Bondstone's durable inbox worker.

**Why this priority**: Durable incoming inbox ingestion is the public receive
behavior and protects restart-safe receive semantics.

**Independent Test**: Run `dotnet test tests/Bondstone.Transport.ServiceBus.Tests/Bondstone.Transport.ServiceBus.Tests.csproj --configuration Release --filter "Category=Unit"` after build.

**Acceptance Scenarios**:

1. **Given** a command envelope delivery, **When** durable incoming inbox ingestion succeeds and saves, **Then** the worker completes the Service Bus message only after save and records the receiver module, handler identity, envelope fields, ingestion time, and source transport name.
2. **Given** an already-ingested command delivery, **When** ingestion reports `AlreadyIngested`, **Then** the worker completes the native message without executing the handler.
3. **Given** durable incoming inbox ingestion fails, **When** the worker handles the delivery, **Then** it does not complete the native message and does not call module handlers.
4. **Given** an event envelope with a configured subscriber binding, **When** durable incoming inbox ingestion runs, **Then** the worker resolves the registered subscriber and records a durable event subscriber inbox key without executing the handler.
5. **Given** event ingestion without a binding or without a registered subscriber, **When** the worker handles the delivery, **Then** it fails with `BondstoneSetupCodes.MissingReceiveBinding` and does not complete the native message.

### Edge Cases

- A receive worker registration requires either `QueueName` or `TopicName` plus
  `SubscriptionName`, but not both.
- `SourceTransportName` defaults to `servicebus:{QueueName}` for queue workers
  and `servicebus:{TopicName}/{SubscriptionName}` for subscription workers.
- Service Bus receive requires `AutoCompleteMessages = false`.
- Service Bus receive requires `ReceiveMode = PeekLock`.
- Event ingestion requires both subscriber module and subscriber identity.
- Service Bus topology, broker retry, dead-letter handling, credentials,
  rules, concurrency, lock renewal policy, and monitoring are application-owned.
- Unsupported durable message kinds are rejected during durable incoming inbox
  ingestion.

## Requirements _(mandatory)_

### Functional Requirements

- **FR-001**: Service Bus transport MUST provide `UseServiceBusDispatcher(...)` from both `BondstoneBuilder` and `BondstoneOutboxBuilder`.
- **FR-002**: Service Bus transport MUST register `ServiceBusEnvelopeDispatcher` as the active `IDurableEnvelopeDispatcher` and mark the outbox transport as `ServiceBus`.
- **FR-003**: Service Bus dispatch MUST serialize `DurableMessageEnvelope` payloads through Bondstone's durable envelope serializer before publishing.
- **FR-004**: Service Bus dispatch MUST resolve a queue or topic entity name through configured `ServiceBusEnvelopeDispatcherOptions.ResolveEntityName`.
- **FR-005**: Service Bus dispatch MUST set native `MessageId`, `Subject`, `ContentType`, and `CorrelationId` from the durable envelope and dispatcher options.
- **FR-006**: Service Bus dispatch MUST set application properties for Bondstone message kind, source module, and optional target module.
- **FR-007**: Service Bus dispatch MUST cache native `ServiceBusSender` instances per resolved entity name and dispose cached senders when the dispatcher is disposed.
- **FR-008**: Service Bus receive worker registration MUST require exactly one receive entity: `QueueName`, or `TopicName` plus `SubscriptionName`.
- **FR-009**: Service Bus receive workers MUST clone processor options when registering and creating processors so later host mutations do not affect registrations.
- **FR-010**: Service Bus receive workers MUST reject `AutoCompleteMessages = true`.
- **FR-011**: Service Bus receive workers MUST reject `ReceiveMode = ReceiveAndDelete` and require `PeekLock`.
- **FR-012**: Service Bus receive workers MUST complete native messages only after durable incoming inbox ingestion succeeds.
- **FR-013**: Service Bus receive workers MUST NOT complete native messages when durable incoming inbox ingestion fails.
- **FR-014**: Service Bus receive workers MUST log processor errors with event id `3001` and event name `ReceiveFailed`.
- **FR-015**: Service Bus receive workers MUST support durable incoming inbox command ingestion through `ReceiveCommand()`.
- **FR-016**: Service Bus receive workers MUST support durable incoming inbox event ingestion through `ReceiveEvent(...)`.
- **FR-017**: Durable incoming inbox command ingestion MUST resolve command handler identity from the module command route registry.
- **FR-018**: Durable incoming inbox event ingestion MUST resolve subscriber identity from the module event subscriber registry and surface missing bindings with `BondstoneSetupCodes.MissingReceiveBinding`.
- **FR-019**: Service Bus transport MUST NOT create, bind, configure, or monitor Azure Service Bus topology or broker policy.

### Compatibility And Public API

- **API-001**: Public setup APIs are `UseServiceBusDispatcher(...)`, `UseServiceBusReceiveWorker(...)`, `ServiceBusEnvelopeDispatcherOptions`, and `ServiceBusReceiveWorkerOptions`.
- **API-002**: The package ID is `Bondstone.Transport.ServiceBus`, and the public namespace is `Bondstone.Transport.ServiceBus`.
- **API-003**: `ServiceBusReceiveWorkerOptions` exposes queue/topic/subscription entity properties, `ProcessorOptions`, `ReceiveCommand()`, and `ReceiveEvent(...)`.

### Durable Semantics

- **DS-001**: Service Bus native message completion MUST happen after durable incoming inbox ingestion succeeds.
- **DS-002**: Command inbox keys MUST use the target module and durable handler identity resolved from registered command routes.
- **DS-003**: Event inbox keys MUST use explicitly configured subscriber module and durable subscriber identity.
- **DS-004**: Service Bus receive must not execute module handlers during durable incoming inbox ingestion; processing is owned by the durable inbox worker.

### Documentation Requirements

- **DOC-001**: Package README and consumer docs MUST state that Service Bus topology, rules, retry, dead-letter, credentials, concurrency, lock renewal policy, and monitoring remain application-owned.
- **DOC-002**: Operations and setup docs MUST describe manual completion after durable incoming inbox ingestion.
- **DOC-003**: Observability docs MUST record Service Bus receive failure logging event id `3001`.

### Key Entities

- **ServiceBusEnvelopeDispatcherOptions**: Public dispatcher configuration with entity resolver and content type.
- **ServiceBusEnvelopeDispatcher**: Internal `IDurableEnvelopeDispatcher` that serializes Bondstone envelopes and publishes through an application-provided `ServiceBusClient`.
- **ServiceBusReceiveWorkerOptions**: Public receive worker options for queue/topic/subscription entity selection, subscriber binding, and native processor options.
- **ServiceBusReceiveWorkerRegistration**: Internal immutable registration used by the hosted worker.
- **ServiceBusReceiveWorker**: Internal hosted worker that creates Service Bus processors, ingests native deliveries into the durable incoming inbox, and completes messages after ingestion succeeds.

## Success Criteria _(mandatory)_

### Measurable Outcomes

- **SC-001**: Unit tests prove dispatcher registration, worker registration, missing entity validation, manual completion validation, `PeekLock` validation, processor option cloning, hosted worker registration, receive failure logging, save-before-complete ordering, failed-ingestion non-completion, already-ingested completion, subscriber binding ingestion, and missing binding diagnostics.
- **SC-002**: Service Bus Testcontainers integration tests prove command envelopes publish to Service Bus queues and event envelopes publish through topics/subscriptions.
- **SC-003**: Service Bus Testcontainers integration tests prove the receive worker consumes command and event deliveries and ingests durable incoming inbox records with expected source transport names.
- **SC-004**: Package README states the adapter is thin native-driver plumbing and not Azure Service Bus topology ownership.

## Assumptions

- The migrated feature describes behavior already implemented before SpecKit adoption.
- Hosts register and own `ServiceBusClient` instances and Azure Service Bus topology.
- Durable persistence behavior is owned by `Bondstone.Persistence` and concrete persistence packages.
- Durable incoming inbox processing after ingestion is owned by `Bondstone.Hosting`.
- The source of truth for transport ownership boundaries is `../../docs/architecture.md`.

## Review Notes

- Source scope: `src/Bondstone.Transport.ServiceBus`.
- Test scope: `tests/Bondstone.Transport.ServiceBus.Tests`.
- Known gaps are listed in `tasks.md`; they are candidates for future specs, not behavior claimed as fully covered here.
