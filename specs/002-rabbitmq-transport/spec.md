# Feature Specification: RabbitMQ Transport

**Feature Branch**: `N/A - migrated existing functionality`

**Created**: 2026-06-23

**Status**: migrated

**Input**: Existing implementation in `src/Bondstone.Transport.RabbitMq` and tests in `tests/Bondstone.Transport.RabbitMq.Tests`

## Scope And Boundaries _(mandatory)_

**Bondstone Capability**: RabbitMQ transport adapter

**Affected Packages/Areas**:

- `src/Bondstone.Transport.RabbitMq`
- `tests/Bondstone.Transport.RabbitMq.Tests`
- `docs/setup.md`
- `docs/package-discovery.md`
- `docs/operations.md`
- `docs/observability.md`
- `docs/packaging.md`

**Out Of Scope**:

- RabbitMQ exchange, queue, binding, credential, retry, dead-letter, prefetch,
  concurrency, and monitoring ownership.
- Durable outbox persistence, durable incoming inbox persistence, and hosted
  durable inbox processing behavior owned by persistence and hosting packages.
- Service Bus and local transport behavior.

## User Scenarios & Testing _(mandatory)_

### User Story 1 - Register RabbitMQ Dispatcher And Worker Setup (Priority: P1)

As a Bondstone host maintainer, I want RabbitMQ dispatcher and receive worker
composition hooks so a host can opt into RabbitMQ envelope plumbing while
keeping RabbitMQ topology and connection lifecycle application-owned.

**Why this priority**: Setup APIs are the entry point for all RabbitMQ adapter
behavior and define the package's public surface.

**Independent Test**: Run `dotnet test tests/Bondstone.Transport.RabbitMq.Tests/Bondstone.Transport.RabbitMq.Tests.csproj --configuration Release --filter "Category=Unit"` after build.

**Acceptance Scenarios**:

1. **Given** a Bondstone service collection, **When** `UseRabbitMqDispatcher(...)` is configured, **Then** the service collection registers `RabbitMqEnvelopeDispatcher` as the `IDurableEnvelopeDispatcher`.
2. **Given** a RabbitMQ receive worker configuration without `QueueName`, **When** the host registers the worker, **Then** registration fails with an actionable `QueueName` setup error.
3. **Given** a RabbitMQ receive worker with command ingestion configured, **When** the host registers the worker, **Then** it registers `RabbitMqReceiveWorker` as an `IHostedService` and stores durable incoming inbox ingestion mode.
4. **Given** event ingestion options with subscriber module and subscriber identity, **When** the host registers the worker, **Then** the registration preserves that durable subscriber binding.

---

### User Story 2 - Publish Durable Envelopes To RabbitMQ (Priority: P2)

As a Bondstone host maintainer, I want claimed durable outbox envelopes to be
serialized and published through a host-provided RabbitMQ channel so the
application can route commands and events through its own RabbitMQ topology.

**Why this priority**: Outbound dispatch is the adapter's core transport
responsibility.

**Independent Test**: Run `dotnet test tests/Bondstone.Transport.RabbitMq.Tests/Bondstone.Transport.RabbitMq.Tests.csproj --configuration Release --filter "Category=Integration"` with RabbitMQ Testcontainers available after build.

**Acceptance Scenarios**:

1. **Given** a durable command envelope and a destination resolving to the default exchange plus a queue routing key, **When** the dispatcher sends the outbox record, **Then** RabbitMQ receives a serialized Bondstone envelope with the same message id and payload.
2. **Given** a durable integration event envelope and a destination resolving to a topic exchange and routing key, **When** the dispatcher sends the outbox record, **Then** a RabbitMQ queue bound to that exchange and routing key receives the event envelope.

---

### User Story 3 - Receive Native RabbitMQ Deliveries Through Bondstone Receiver Boundary (Priority: P3)

As a Bondstone maintainer, I want the RabbitMQ receive worker to consume native
deliveries with manual acknowledgement so native settlement happens only after
Bondstone receive work succeeds.

**Why this priority**: This preserves the adapter boundary and prevents native
acknowledgement before Bondstone-owned receive processing.

**Independent Test**: Run `dotnet test tests/Bondstone.Transport.RabbitMq.Tests/Bondstone.Transport.RabbitMq.Tests.csproj --configuration Release --filter "Category=Unit|Category=Integration"` after build.

**Acceptance Scenarios**:

1. **Given** a running RabbitMQ receive worker in direct receive mode, **When** a command envelope is published to its queue, **Then** the worker calls `IDurableEnvelopeReceiver` with the same envelope and no event binding.
2. **Given** a running RabbitMQ receive worker configured for an event binding, **When** an event envelope is published to its bound queue, **Then** the worker calls `IDurableEnvelopeReceiver` with the same envelope and the configured subscriber module and subscriber identity.
3. **Given** a receiver failure, **When** the worker handles the delivery, **Then** it logs RabbitMQ receive failure event id `2001` and negatively acknowledges using the configured requeue option.
4. **Given** receiver processing that has started but not completed, **When** the delivery is in progress, **Then** the worker does not acknowledge until receive processing completes.

---

### User Story 4 - Ingest RabbitMQ Deliveries Into Durable Incoming Inbox (Priority: P4)

As a Bondstone host maintainer, I want RabbitMQ receive workers to ingest
deliveries into the durable incoming inbox ledger so command and event handling
can be processed later by Bondstone's durable inbox worker.

**Why this priority**: Durable incoming inbox ingestion is the current public
receive mode and protects restart-safe receive semantics.

**Independent Test**: Run `dotnet test tests/Bondstone.Transport.RabbitMq.Tests/Bondstone.Transport.RabbitMq.Tests.csproj --configuration Release --filter "Category=Unit"` after build.

**Acceptance Scenarios**:

1. **Given** a command envelope delivery, **When** durable incoming inbox ingestion succeeds and saves, **Then** the worker acknowledges only after save and records the receiver module, handler identity, envelope fields, ingestion time, and source transport name.
2. **Given** an already-ingested command delivery, **When** ingestion reports `AlreadyIngested`, **Then** the worker acknowledges without executing the handler.
3. **Given** command ingestion with multiple module boundaries, **When** the command targets `fulfillment`, **Then** the worker resolves and saves through the `fulfillment` incoming inbox boundary only.
4. **Given** an event envelope with a configured subscriber binding, **When** durable incoming inbox ingestion runs, **Then** the worker resolves the registered subscriber and records a durable event subscriber inbox key without executing the handler.
5. **Given** event ingestion without a binding or without a registered subscriber, **When** the worker handles the delivery, **Then** it negatively acknowledges and logs `BondstoneSetupCodes.MissingReceiveBinding`.
6. **Given** durable incoming inbox ingestion fails, **When** the worker handles the delivery, **Then** it negatively acknowledges using the configured requeue option and does not call module handlers.

### Edge Cases

- `QueueName` is required for worker registration.
- `SourceTransportName` defaults to `rabbitmq:{QueueName}` and custom values are trimmed.
- Event ingestion requires both subscriber module and subscriber identity.
- RabbitMQ delivery acknowledgement is manual; `autoAck` is false.
- RabbitMQ topology, broker retry, dead-letter handling, prefetch/concurrency,
  credentials, and monitoring are application-owned.
- Unsupported durable message kinds are rejected during durable incoming inbox
  ingestion.

## Requirements _(mandatory)_

### Functional Requirements

- **FR-001**: RabbitMQ transport MUST provide `UseRabbitMqDispatcher(...)` from both `BondstoneBuilder` and `BondstoneOutboxBuilder`.
- **FR-002**: RabbitMQ transport MUST register `RabbitMqEnvelopeDispatcher` as the active `IDurableEnvelopeDispatcher` and mark the outbox transport as `RabbitMq`.
- **FR-003**: RabbitMQ dispatch MUST serialize `DurableMessageEnvelope` payloads through Bondstone's durable envelope serializer before publishing.
- **FR-004**: RabbitMQ dispatch MUST resolve an exchange and routing key through configured `RabbitMqEnvelopeDispatcherOptions.ResolveDestination`.
- **FR-005**: RabbitMQ dispatch MUST publish with the configured `Mandatory` option.
- **FR-006**: RabbitMQ receive worker registration MUST require `QueueName`.
- **FR-007**: RabbitMQ receive workers MUST consume with manual acknowledgement.
- **FR-008**: RabbitMQ receive workers MUST acknowledge only after direct receive or durable incoming inbox ingestion succeeds.
- **FR-009**: RabbitMQ receive workers MUST negatively acknowledge failed deliveries and use `RequeueOnFailure` for the native requeue flag.
- **FR-010**: RabbitMQ receive workers MUST log failed deliveries with event id `2001` and event name `ReceiveFailed`.
- **FR-011**: RabbitMQ receive workers MUST support durable incoming inbox command ingestion through `ReceiveCommand()` and `IngestCommandToDurableIncomingInbox()`.
- **FR-012**: RabbitMQ receive workers MUST support durable incoming inbox event ingestion through `ReceiveEvent(...)` and `IngestEventToDurableIncomingInbox(...)`.
- **FR-013**: Durable incoming inbox ingestion MUST resolve command handler identity from the module command route registry.
- **FR-014**: Durable incoming inbox event ingestion MUST resolve subscriber identity from the module event subscriber registry and surface missing bindings with `BondstoneSetupCodes.MissingReceiveBinding`.
- **FR-015**: RabbitMQ transport MUST NOT create, bind, configure, or monitor RabbitMQ broker topology.

### Compatibility And Public API

- **API-001**: Public setup APIs are `UseRabbitMqDispatcher(...)`, `UseRabbitMqReceiveWorker(...)`, `RabbitMqEnvelopeDispatcherOptions`, `RabbitMqEnvelopeDestination`, and `RabbitMqReceiveWorkerOptions`.
- **API-002**: The package ID is `Bondstone.Transport.RabbitMq`, and the public namespace is `Bondstone.Transport.RabbitMq`.
- **API-003**: `RabbitMqReceiveWorkerOptions` exposes command/event receive methods and ingestion aliases that map to durable incoming inbox ingestion mode.

### Durable Semantics

- **DS-001**: RabbitMQ native delivery settlement MUST happen after durable incoming inbox ingestion succeeds.
- **DS-002**: Command inbox keys MUST use the target module and durable handler identity resolved from registered command routes.
- **DS-003**: Event inbox keys MUST use explicitly configured subscriber module and durable subscriber identity.
- **DS-004**: RabbitMQ receive must not execute module handlers during durable incoming inbox ingestion; processing is owned by the durable inbox worker.

### Documentation Requirements

- **DOC-001**: Package README and consumer docs MUST state that RabbitMQ topology, retry, dead-letter, credentials, prefetch/concurrency, and monitoring remain application-owned.
- **DOC-002**: Operations and setup docs MUST describe manual acknowledgement after durable incoming inbox ingestion.
- **DOC-003**: Observability docs MUST record RabbitMQ receive failure logging event id `2001`.

### Key Entities

- **RabbitMqEnvelopeDestination**: Public destination record containing RabbitMQ exchange and routing key.
- **RabbitMqEnvelopeDispatcherOptions**: Public dispatcher configuration with destination resolver and mandatory publish flag.
- **RabbitMqEnvelopeDispatcher**: Internal `IDurableEnvelopeDispatcher` that serializes Bondstone envelopes and publishes through an application-provided `IChannel`.
- **RabbitMqReceiveWorkerOptions**: Public receive worker options for queue name, binding, requeue behavior, consumer tag, source transport name, and command/event ingestion mode.
- **RabbitMqReceiveWorkerRegistration**: Internal immutable registration used by the hosted worker.
- **RabbitMqReceiveWorker**: Internal hosted worker that consumes RabbitMQ deliveries, delegates direct receive or durable incoming inbox ingestion, and performs manual acknowledgement or negative acknowledgement.

## Success Criteria _(mandatory)_

### Measurable Outcomes

- **SC-001**: Unit tests prove dispatcher registration, worker registration, queue name validation, source transport naming, subscriber binding registration, ack/nack ordering, requeue behavior, missing binding diagnostics, module-boundary ingestion, and envelope-field preservation.
- **SC-002**: RabbitMQ Testcontainers integration tests prove command envelopes publish to RabbitMQ queues and event envelopes publish through exchange/routing-key bindings.
- **SC-003**: RabbitMQ Testcontainers integration tests prove the receive worker consumes command and event deliveries and forwards expected receive bindings.
- **SC-004**: Package README states the adapter is thin native-driver plumbing and not RabbitMQ topology ownership.

## Assumptions

- The migrated feature describes behavior already implemented before SpecKit adoption.
- Hosts register and own RabbitMQ `IChannel` instances and RabbitMQ topology.
- Durable persistence behavior is owned by `Bondstone.Persistence` and concrete persistence packages.
- Durable incoming inbox processing after ingestion is owned by `Bondstone.Hosting`.
- The source of truth for transport ownership boundaries is `../../docs/architecture.md`.

## Review Notes

- Source scope: `src/Bondstone.Transport.RabbitMq`.
- Test scope: `tests/Bondstone.Transport.RabbitMq.Tests`.
- Known gaps are listed in `tasks.md`; they are candidates for future specs, not behavior claimed as fully covered here.
