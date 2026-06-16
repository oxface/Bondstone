# 0007 Keep Orchestration App Owned For Now

Status: Accepted
Application: Applied
Date: 2026-06-16

## Context

Durable operation polling is enough for the current result-returning command
model, but it is not a full workflow engine. If an HTTP request starts
cross-module work and the origin process dies, the command can still complete
because it is persisted in the durable outbox/inbox loop, but the caller may
lose the in-memory wait.

Larger workflow needs can point toward sagas, process managers, state machines,
replayable orchestration, or external systems such as Temporal, Dapr Workflow,
Elsa, Rebus sagas, Wolverine sagas, or another app-selected engine.

Bondstone should not prematurely build a workflow platform inside the module
boundary library.

## Decision

Keep orchestration app-owned for now.

Bondstone provides durable command sending, durable events, receive inbox
idempotency, operation handles, operation result polling, explicit operation
finalization, and app-owned expiry processing. That is the current workflow
support boundary.

Bondstone will not add a saga engine, replay engine, durable timer system,
workflow state machine framework, HTTP request durability layer, or
provider-neutral workflow scheduler in the current baseline.

Simple orchestration can be written by the app using module handlers,
integration event subscribers, explicit process tables, and durable command
sends. More serious orchestration should be delegated to an app-selected
workflow or bus library until Bondstone has repeated real use cases that prove
a smaller native abstraction is worth owning.

A future tiny Bondstone process-manager helper is possible, but only after the
core durable module boundary model stays stable and real projects show the
same small missing shape repeatedly.

## Consequences

Bondstone avoids overextending immediately after simplifying transport and
pipeline machinery.

HTTP request durability remains an application architecture concern. Apps that
need durable waits across process death should store request/workflow state or
use an orchestration platform, rather than relying on an in-memory
`WaitForResultAsync` call.

Operation handles and polling remain useful for simple accepted-work flows,
tests, UI refresh loops, and bounded synchronous waits with timeouts.

## Related Decisions

- Builds on
  [0004 Persistence Operation State And Results](0004-persistence-operation-state-and-results.md)
  and
  [0005 Transport Adapters And Receive Helpers](0005-transport-adapters-and-receive-helpers.md).
- See archived ADR
  [0057](archive/pre-restart-2026-06-16/0057-operation-state-operational-semantics.md)
  for prior operation-state context.

## Application Notes

- Current contract: operation result polling, finalization, and expiration
  processing are documented in
  [docs/architecture/messaging.md](../architecture/messaging.md) and
  [docs/package-discovery.md](../package-discovery.md).
- Stable docs: no standalone orchestration doc exists because orchestration is
  not an active Bondstone subsystem.
- Agent guidance: root [AGENTS.md](../../AGENTS.md) requires ADR review before
  adding durable behavior, runtime behavior, public API, package boundaries, or
  provider/transport behavior.
- Application evidence: operation handle and polling APIs exist; no saga,
  workflow, replay, or durable HTTP request subsystem is present.
- Pending or deferred: revisit only after real project usage produces repeated
  small orchestration needs.

## Verification

Read current messaging and operation-state docs plus the post-MVP plan. No
executable verification is required because this ADR records a scope boundary.
