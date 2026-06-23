# Feature Specification: Durable Command And Event Runtime

**Feature Branch**: `N/A - migrated existing functionality`

**Created**: 2026-06-23

**Status**: migrated

**Input**: Existing durable command sender, event publisher, envelope receiver, and result models in `src/Bondstone/Messaging`, with focused tests in `tests/Bondstone.Tests/Messaging`

## Scope And Boundaries _(mandatory)_

**Bondstone Capability**: Durable command send API, integration event publish API, source-module envelope staging, operation handle creation, outbound route dispatch contracts, and durable envelope receive handoff into module pipelines.

**Affected Packages/Areas**:

- `src/Bondstone/Messaging/Contracts/IDurableCommandSender.cs`
- `src/Bondstone/Messaging/Contracts/IDurableEventPublisher.cs`
- `src/Bondstone/Messaging/Contracts/IDurableEnvelopeReceiver.cs`
- `src/Bondstone/Messaging/Sending`
- `src/Bondstone/Messaging/Publishing`
- `src/Bondstone/Messaging/Receiving`
- `tests/Bondstone.Tests/Messaging/DurableCommand*.cs`
- `tests/Bondstone.Tests/Messaging/DurableEvent*.cs`
- `tests/Bondstone.Tests/Messaging/DurableEnvelopeReceiverTests.cs`

**Out Of Scope**:

- Concrete outbox persistence, outbox dispatch workers, and broker transports.
- Core module handler registration and receive execution internals.
- Envelope serialization and message identity primitives except as dependencies.

## User Scenarios & Testing _(mandatory)_

### User Story 1 - Send Durable Commands (Priority: P1)

As source module code, I want to send durable commands to target modules so accepted work is staged through the source module outbox and can carry an operation id.

### User Story 2 - Publish Durable Integration Events (Priority: P2)

As module code, I want to publish integration events so subscriber envelopes are staged for each registered subscriber.

### User Story 3 - Receive Durable Envelopes (Priority: P3)

As transport or durable inbox code, I want a provider-neutral envelope receiver that routes command and event envelopes into module receive pipelines.

## Requirements _(mandatory)_

- **FR-001**: Durable command sender MUST resolve message identity and target module.
- **FR-002**: Durable command sender MUST stage envelopes through the source module outbox writer.
- **FR-003**: Durable command send results MUST expose accepted/duplicate/failure status and optional operation handle.
- **FR-004**: Operation id sends MUST create pending operation state only when unknown.
- **FR-005**: Durable event publisher MUST fan out envelopes to registered subscribers.
- **FR-006**: Durable event publish results MUST report per-subscriber staging outcome.
- **FR-007**: Envelope receiver MUST route command envelopes to command receive pipeline and event envelopes to event receive pipeline.
- **FR-008**: Missing routes, invalid message kind, and invalid module context MUST produce clear diagnostics.

## Success Criteria _(mandatory)_

- **SC-001**: Unit tests prove command staging and operation handle behavior.
- **SC-002**: Unit tests prove event fan-out and publish results.
- **SC-003**: Unit tests prove envelope receiver routing and error behavior.

## Assumptions

- Actual delivery is owned by outbox dispatchers and transport adapters.
- Operation result observation is migrated separately.
