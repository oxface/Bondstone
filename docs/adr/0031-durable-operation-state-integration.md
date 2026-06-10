# 0031 Durable Operation State Integration

Status: Accepted
Application: Applied
Date: 2026-06-08

## Context

Bondstone already exposes durable operation contracts:
`DurableOperationState`, `DurableOperationStatus`,
`IDurableOperationReader`, and `IDurableOperationStateStore`. EF Core and
PostgreSQL adapters can persist operation state, and durable envelopes carry an
optional `DurableOperationId`.

Those pieces are not yet connected to the durable command loop. A caller can
provide an operation id when sending a command, and that id flows through the
outbox and Rebus wire envelope, but `IDurableOperationReader` is only useful if
application code writes operation state manually.

Operation state is distinct from delivery persistence. Outbox records describe
messages staged for transport and their dispatch lifecycle; inbox records
describe receive idempotency and processed markers. Operation state is the
caller-visible logical tracking state for the workflow represented by a
durable operation id.

The missing integration should make operation lookup useful without adding a
`send and wait` API, polling model, retry-state machine, stale receive
recovery, or cross-service orchestration framework. It also must respect the
current transaction model: successful module command execution commits handler
state, outbox writes, inbox markers, and any operation-state update together;
failed receive attempts throw so Rebus retry/dead-letter policy remains in
control.

## Decision

Bondstone will integrate durable operation state into the command loop for
caller-supplied operation ids.

The default command sender will not generate operation ids. If a caller
provides a `durableOperationId`, the sender will stage a `Pending` operation
state together with the outgoing command envelope. Sending with an operation id
requires `IDurableOperationStateStore`; missing operation-state persistence is
a configuration error. Repeated sends with the same operation id must not
downgrade an existing state such as `Completed`.

The module command receive path will carry the envelope's durable operation id
into module command execution metadata. A Bondstone system pipeline behavior
will record `Completed` after a durable command with an operation id finishes
successfully. This update runs inside the module command transaction when EF
module persistence is configured, so handler state, outgoing outbox messages,
receive inbox markers, and the operation completion update commit together.

The first integration does not record durable failure state for handler
exceptions. With the current receive contract, a thrown handler exception rolls
back the module transaction and lets Rebus retry/dead-letter policy decide what
happens next. Persisting `Failed`, retry counts, stale receive recovery,
timeouts, cancellation, or operation result payloads requires a later decision
because those behaviors need explicit retry and acknowledgement semantics.

`IDurableOperationReader` remains a read contract. It does not define polling,
wait handles, result deserialization, timeout policy, or client-facing
operation endpoints.

## Consequences

Operation ids become useful for the current MVP command loop: callers that
provide a stable id can query whether the command is still pending or has been
handled successfully.

Bondstone avoids inventing operation ids for callers that do not need durable
operation tracking, preserving the existing nullable operation id result
surface.

Operation-state persistence becomes required only when operation tracking is
used. Durable messaging without operation ids can continue to use the current
outbox and inbox path without an operation-state store.

Receive failures remain operationally loud and are still governed by Rebus
retry/dead-letter policy. A later ADR can add richer operation-state
transitions once retry state, stale receive recovery, failure classification,
or result payload semantics are accepted.

## Related Decisions

- [0015 Inbox Handle-Once Orchestration](0015-inbox-handle-once-orchestration.md)
- [0016 EF Core Persistence Scope](0016-ef-core-persistence-scope.md)
- [0023 Rebus Receive-Side Inbox Integration](0023-rebus-receive-side-inbox-integration.md)
- [0024 Rebus Typed Command Receive Pipeline](0024-rebus-typed-command-receive-pipeline.md)
- [0025 Module Command Execution Boundary](0025-module-command-execution-boundary.md)
- [0027 Optional EF Core Persistence Mapping](0027-optional-ef-core-persistence-mapping.md)

## Application Notes

- Current contract: Durable operation tracking is command-loop integrated for
  caller-supplied operation ids. Sends with an operation id stage `Pending`
  operation state when the operation is unknown. Successful module command
  receive with an operation id stages `Completed` inside module command
  execution. Missing operation-state persistence is a clear configuration
  error when tracking is used.
- Stable docs: Current operation-state behavior is described in
  [docs/architecture/messaging.md](../architecture/messaging.md),
  [docs/architecture/modules.md](../architecture/modules.md),
  [docs/architecture/persistence-core.md](../architecture/persistence-core.md),
  [docs/architecture/persistence-ef-core.md](../architecture/persistence-ef-core.md),
  and [docs/mvp-plan.md](../mvp-plan.md).
- Agent guidance: Root [AGENTS.md](../../AGENTS.md) requires ADR review before
  public API, durable behavior, provider, transport, or module runtime changes.
- Application evidence: Default command sending stages `Pending` operation
  state for new caller-supplied operation ids, module command execution carries
  operation ids from neutral receive envelopes, a system pipeline behavior
  stages `Completed` after successful durable command execution, EF and
  PostgreSQL operation-state persistence have clear mapping/registration
  behavior, module-owned operation reads use explicit status precedence, and
  focused unit/application tests cover send, receive, reader precedence, and
  transaction behavior.
- Pending or deferred: None for the current operation-state integration.
  Failure states, running states, cancellation states, result payloads,
  polling/waiting APIs, retry state, stale receive recovery, and
  provider-specific concurrency policy remain separate future decisions.

## Verification

Read back affected architecture docs and ran focused core, direct transport,
PostgreSQL, and EF Core tests, followed by `pnpm check`.
