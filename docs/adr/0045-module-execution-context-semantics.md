# 0045 Module Execution Context Semantics

Status: Proposed
Application: Not Applicable
Date: 2026-06-10

## Context

Bondstone durable command sending and event publishing require a current source
module. The current implementation uses a module execution context accessor
backed by `AsyncLocal`. Module command and event subscriber execution pipeline
behaviors push the current module into that ambient context so handlers can
inject `IDurableCommandSender` and `IDurableEventPublisher` without manually
passing the source module through every call.

This is ergonomic for handler code, but ambient context has tradeoffs. It can
surprise code that starts parallel work, queues work outside the handler flow,
suppresses execution-context flow, or calls durable send/publish after the
pipeline scope is disposed.

## Decision

Decide whether Bondstone should keep ambient module execution context as the
primary send/publish source-module mechanism or add explicit alternatives.

The candidate direction is:

- Keep the current ambient context for normal module handler ergonomics.
- Document that durable send/publish APIs require the current module execution
  flow and are not general background-work APIs.
- Consider explicit lower-level APIs, module-scoped clients, or provider-owned
  execution context objects before broadening usage to HTTP command routing or
  custom execution hosts.

## Consequences

Keeping ambient context preserves simple handler APIs.

Adding explicit alternatives can reduce surprises and make non-handler
execution scenarios clearer.

Changing source-module context semantics affects public messaging APIs and
handler authoring style, so it requires compatibility review.

## Related Decisions

- [0025 Module Command Execution Boundary](0025-module-command-execution-boundary.md)
- [0031 Durable Operation State Integration](0031-durable-operation-state-integration.md)
- [0033 First-Class Event Publish Subscribe Topology](0033-first-class-event-publish-subscribe-topology.md)

## Application Notes

- Current contract: proposed only; no binding change yet.
- Stable docs: if accepted, update messaging, modules, and setup docs.
- Agent guidance: if accepted, update architecture direction if ambient context
  constraints or alternatives become current guidance.
- Application evidence: current code uses `ModuleExecutionContextAccessor` and
  module execution context pipeline behaviors for command and event subscriber
  execution.
- Pending or deferred: decide whether explicit APIs are needed before HTTP
  command execution or custom receive hosts become first-class.

## Verification

No executable verification yet; this is a proposed decision draft.
