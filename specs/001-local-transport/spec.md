# Feature Specification: Local Transport

**Feature Branch**: `N/A - migrated existing functionality`

**Created**: 2026-06-23

**Status**: migrated

**Input**: Existing implementation in `src/Bondstone.Transport.Local` and tests in `tests/Bondstone.Transport.Local.Tests`

## User Scenarios & Testing _(mandatory)_

### User Story 1 - Route Durable Commands Through Local Queues (Priority: P1)

As a Bondstone maintainer or sample author, I want a local in-process transport
adapter so durable commands staged in the outbox can be routed into a target
module's receive pipeline without requiring a production broker.

**Why this priority**: Local command routing is the core purpose of this
package and supports samples, tests, and local development.

**Independent Test**: Run `dotnet test tests/Bondstone.Transport.Local.Tests/Bondstone.Transport.Local.Tests.csproj --configuration Release --filter "Category=Unit|Category=Integration"` after build.

**Acceptance Scenarios**:

1. **Given** a durable command envelope targeting `fulfillment` and an explicit local route from `fulfillment` to `fulfillment.commands`, **When** the envelope is dispatched, **Then** the command receive pipeline handles the same envelope once.
2. **Given** a durable command envelope targeting `fulfillment` and `UseModuleQueueConvention()`, **When** the envelope is dispatched, **Then** the command receive pipeline handles the same envelope once.
3. **Given** a module command that stages a durable command in the outbox, **When** the durable outbox dispatcher runs through local transport, **Then** the outbox row is dispatched, the target inbox row is persisted and processed, and duplicate delivery is skipped.

---

### User Story 2 - Fan Out Integration Events To Local Subscribers (Priority: P2)

As a Bondstone maintainer or sample author, I want local transport to fan out
integration events to explicitly configured subscriber module identities so
event handling follows the same durable inbox semantics as broker-backed
transport.

**Why this priority**: Event fan-out is the second durable messaging shape the
adapter supports and preserves subscriber identity as part of durable inbox
keys.

**Independent Test**: Run `dotnet test tests/Bondstone.Transport.Local.Tests/Bondstone.Transport.Local.Tests.csproj --configuration Release --filter "Category=Unit|Category=Integration"` after build.

**Acceptance Scenarios**:

1. **Given** an integration event envelope and two subscribers bound to one local event queue, **When** the envelope is dispatched, **Then** each subscriber receive pipeline is invoked with the same envelope and its configured subscriber module and subscriber identity.
2. **Given** a module command that publishes an integration event and two event subscribers, **When** the durable outbox dispatcher runs through local transport, **Then** the event outbox row is dispatched and one processed inbox row exists per subscriber.
3. **Given** a previously processed event inbox row, **When** the same event is handled again for that subscriber, **Then** the receive pipeline reports `AlreadyProcessed` and does not duplicate handler side effects.

---

### User Story 3 - Report Missing Local Receive Bindings (Priority: P3)

As a package maintainer, I want missing local routing setup to fail with
Bondstone setup diagnostics so consumers can distinguish configuration errors
from handler or persistence failures.

**Why this priority**: Missing binding diagnostics keep local transport
failures actionable and aligned with the repository constitution's evidence and
diagnostics principles.

**Independent Test**: Run the unit tests in `LocalDurableEnvelopeDispatcherTests`.

**Acceptance Scenarios**:

1. **Given** no durable dispatch route can send the envelope, **When** the dispatcher runs, **Then** it reports `BondstoneSetupCodes.MissingDispatcher`.
2. **Given** the local route is asked to dispatch a command without a command queue binding, **When** dispatch runs, **Then** it reports `BondstoneSetupCodes.MissingReceiveBinding`.
3. **Given** the local route is asked to dispatch an event without subscriber bindings, **When** dispatch runs, **Then** it reports `BondstoneSetupCodes.MissingReceiveBinding`.

---

### User Story 4 - Preserve Handler Failure Semantics (Priority: P4)

As a maintainer, I want local transport to propagate handler failures from the
receive pipeline so tests and local development see the same failure that would
drive outbox retry behavior at the durable dispatcher layer.

**Why this priority**: This is not the primary routing feature, but it protects
failure visibility and prevents local transport from swallowing handler
exceptions.

**Independent Test**: Run the integration test `DispatchAsync_WhenLocalCommandHandlerThrows_PropagatesHandlerFailure`.

**Acceptance Scenarios**:

1. **Given** a local command handler throws during receive pipeline execution, **When** local transport dispatches the durable command envelope, **Then** the handler exception is propagated and no target inbox row is persisted.

### Edge Cases

- A command route must not count as sendable unless the resolved queue accepts the target module.
- An event route must not count as sendable unless the resolved queue has at least one matching subscriber binding.
- Unsupported message kinds are rejected by the local route.
- Local transport is not a production broker fallback and does not own retry, dead-letter handling, topology provisioning, or broker durability.

## Requirements _(mandatory)_

### Functional Requirements

- **FR-001**: Local transport MUST register an `IDurableEnvelopeDispatchRoute` with transport name `Local`.
- **FR-002**: Local transport MUST be configurable from both `BondstoneBuilder.UseLocalTransport(...)` and `BondstoneOutboxBuilder.UseLocalTransport(...)`.
- **FR-003**: Local transport MUST support explicit target-module command routing through `RouteModule(...).ToQueue(...)` plus queue `AcceptModule(...)` binding.
- **FR-004**: Local transport MUST support convention-based command routing through `UseModuleQueueConvention()`.
- **FR-005**: Local transport MUST support explicit event routing through `RouteEvent(...).ToQueue(...)` plus queue `SubscribeEvent(...)` subscriber bindings.
- **FR-006**: Local transport MUST dispatch durable command envelopes through `IDurableEnvelopeReceiver.ReceiveCommandAsync(...)`.
- **FR-007**: Local transport MUST dispatch durable event envelopes once per matching subscriber through `IDurableEnvelopeReceiver.ReceiveEventAsync(...)`.
- **FR-008**: Local transport MUST preserve durable inbox idempotency for duplicate command and event deliveries.
- **FR-009**: Local transport MUST surface missing dispatcher and missing receive binding setup errors with Bondstone setup codes.
- **FR-010**: Local transport MUST propagate handler exceptions raised during local receive pipeline execution.
- **FR-011**: Local transport MUST normalize required route, queue, module, message type, subscriber module, and subscriber identity values.
- **FR-012**: Local transport MUST remain a local/dev/test adapter and MUST NOT claim production broker durability or topology ownership.

### Key Entities _(include if feature involves data)_

- **BondstoneLocalTransportBuilder**: User-facing local transport topology builder for module routes, event routes, queue bindings, and queue naming conventions.
- **LocalTransportTopology**: Internal immutable topology snapshot used to resolve command queue bindings and event subscribers at dispatch time.
- **LocalDurableEnvelopeDispatchRoute**: Internal dispatch route that bridges durable outbox records to module command and event receive pipelines through `IDurableEnvelopeReceiver`.
- **LocalQueueRegistration**: Internal queue registration containing accepted command modules and event subscriber bindings.
- **LocalEventSubscription**: Internal event subscriber binding containing message type, subscriber module, and subscriber identity.
- **LocalCommandQueueBinding**: Internal resolved command queue binding containing queue name and target module.

## Success Criteria _(mandatory)_

### Measurable Outcomes

- **SC-001**: Unit tests prove explicit command routing, command queue convention routing, event fan-out, missing route diagnostics, and startup validation.
- **SC-002**: Integration tests prove local transport dispatch persists durable inbox rows for command and event delivery and skips duplicate deliveries.
- **SC-003**: Integration tests prove local handler exceptions propagate and do not create a target inbox record.
- **SC-004**: Package README states the adapter is for samples, tests, and local development, not production broker durability.

## Assumptions

- The migrated feature describes behavior already implemented before SpecKit adoption.
- EF Core PostgreSQL persistence behavior used by integration tests is owned by persistence packages; local transport only routes envelopes into receive pipelines.
- Broker-backed transports are separate adapters and are not part of this migrated feature.
- The source of truth for durable transport boundaries is `../../docs/architecture.md`.

## Migration Notes

- Source scope: `src/Bondstone.Transport.Local`.
- Test scope: `tests/Bondstone.Transport.Local.Tests`.
- Known gaps are listed in `tasks.md`; they are candidates for future specs, not behavior claimed by this migrated spec.
