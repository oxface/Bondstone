# 0026 Event Shape Guardrail

Status: Proposed
Application: Not Applicable
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

## Decision

Bondstone should introduce an event-shape guardrail before completing broad
receive listener binding and topology diagnostics.

The guardrail should define the conceptual split between:

- commands as directed work requests to one target module;
- integration events as durable cross-module facts that may have multiple
  subscribers;
- domain events as module-local facts that are not automatically public
  integration contracts.

The first event slice should focus on design and minimal core API shape. It
may introduce or formalize:

- explicit durable event publisher contract shape;
- module event subscriber registration metadata;
- stable subscriber identity rules;
- event envelope rules using `MessageKind.Event` without a target module;
- per-subscriber inbox identity based on message id, subscriber module, and
  stable subscriber identity;
- diagnostics vocabulary that can describe command routes and event
  subscriptions without assuming all durable messages are commands;
- Rebus topic/subscription vocabulary for later implementation.

The guardrail should not implement full event fan-out, Rebus
publish/subscribe, event subscription workers, event choreography samples, or
automatic domain-event-to-integration-event publication.

Event publication should remain explicit. Later mapping helpers may reduce
ceremony when converting a module-local domain event into an integration
event, but they must preserve the visible step where private module state
becomes a durable public contract.

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

## Related Decisions

- [0004 Positioning And Service Extraction Path](0004-positioning-and-service-extraction-path.md)
- [0018 Rebus Outbox Transport Adapter](0018-rebus-outbox-transport-adapter.md)
- [0023 Rebus Receive-Side Inbox Integration](0023-rebus-receive-side-inbox-integration.md)
- [0024 Rebus Typed Command Receive Pipeline](0024-rebus-typed-command-receive-pipeline.md)
- [0025 Module Command Execution Boundary](0025-module-command-execution-boundary.md)

## Application Notes

- Current contract: Proposed only. Current implementation remains
  command-first. `IIntegrationEvent` and `MessageKind.Event` exist in core
  messaging, but first-class event publishing/subscription behavior remains
  deferred.
- Stable docs: Current command-first behavior and deferred event work are
  described in [docs/architecture/messaging.md](../architecture/messaging.md),
  [docs/architecture/modules.md](../architecture/modules.md), and
  [docs/architecture/transport-rebus.md](../architecture/transport-rebus.md).
  Sequencing is tracked in [docs/mvp-plan.md](../mvp-plan.md).
- Agent guidance: Root [AGENTS.md](../../AGENTS.md) requires ADR review before
  broad public API, durable behavior, provider, transport, module runtime, or
  topology changes.
- Application evidence: None yet beyond existing command/event markers and
  documentation planning.
- Pending or deferred: Accept or revise this ADR, then apply any accepted
  guardrail into stable docs and minimal core APIs. Full event runtime remains
  later MVP work.

## Verification

Read back the proposed ADR and related stable docs. No executable verification
is relevant for this proposal-only decision.
