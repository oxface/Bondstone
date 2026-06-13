# 0045 Module Execution Context Semantics

Status: Accepted
Application: Applied
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

Bondstone keeps ambient module execution context as the primary source-module
mechanism for durable command sending and integration event publishing in
normal module handler execution.

The current execution context is intentionally scoped to module command and
event subscriber execution. System pipeline behaviors push the executing
module into the ambient context before application handlers run and restore the
previous context after the handler pipeline completes. `IDurableCommandSender`
and `IDurableEventPublisher` use the current module as the source module and
fail fast when no current module execution context exists.

The ambient context is not a general source-module override API. Durable
send/publish APIs are intended to be called from work that is executing inside
the current module execution flow. Code that queues work beyond the handler
lifetime, suppresses execution-context flow, or calls durable send/publish
after the pipeline scope is disposed has no supported source-module context.

HTTP routes, custom hosts, and other app-owned entrypoints should execute
registered module commands through `IModuleCommandExecutor` when they need
module command semantics and handler-scoped durable send/publish. Bondstone
does not currently provide explicit module-scoped durable sender/publisher
clients or public APIs that let arbitrary code select a source module for
durable send/publish.

Adding explicit lower-level APIs, module-scoped clients, provider-owned
execution context objects, or mediator-like HTTP command routing requires a
later compatibility/API decision.

## Consequences

Keeping ambient context preserves simple handler APIs and aligns source-module
selection with the executing module.

The limit is explicit: durable send/publish is handler-flow scoped. Non-handler
execution scenarios must either route through module command execution or wait
for a later accepted explicit source-module API.

Changing source-module context semantics affects public messaging APIs and
handler authoring style, so it requires compatibility review.

## Related Decisions

- [0025 Module Command Execution Boundary](0025-module-command-execution-boundary.md)
- [0031 Durable Operation State Integration](0031-durable-operation-state-integration.md)
- [0033 First-Class Event Publish Subscribe Topology](0033-first-class-event-publish-subscribe-topology.md)
- [0046 Public API Surface Policy](0046-public-api-surface-policy.md)

## Application Notes

- Current contract: durable send/publish uses the current module execution
  context as the source module. Command and event subscriber execution
  pipelines provide that context for handlers and restore the previous context
  afterward. No explicit public source-module override or module-scoped
  sender/publisher API exists.
- Stable docs: applied to
  [docs/architecture/messaging.md](../architecture/messaging.md),
  [docs/architecture/modules.md](../architecture/modules.md), and
  [docs/setup.md](../setup.md).
- Agent guidance: root [AGENTS.md](../../AGENTS.md) already requires ADR
  review before public API, durable behavior, or module runtime changes. No
  additional agent instruction is needed for this accepted current behavior.
- Application evidence: current code uses `ModuleExecutionContextAccessor` and
  module execution context pipeline behaviors for command and event subscriber
  execution. `DurableCommandSender` and `DurableEventPublisher` throw when no
  current module execution context exists, and existing unit tests cover
  context setup/cleanup plus missing-context failures.
- Pending or deferred: explicit source-module APIs, module-scoped clients, and
  mediator-like HTTP/custom execution APIs remain deferred to the public API
  cleanup and real-project readiness backlog tracks.

## Verification

Read back this ADR and affected stable docs. Existing fast tests cover the
accepted behavior. Verified this decision update with the commands reported in
the backlog resolution.
