# 0041 Outbox Terminal Failure Boundary

Status: Amended
Application: Applied
Date: 2026-06-10

## Context

Bondstone owns persisted outgoing outbox records. When a module transaction
commits source state and an outgoing durable message, the outbox worker later
claims that record and attempts to hand it to the configured transport. If the
handoff fails because the broker is unavailable, routing is invalid, the native
send fails, or the transport adapter throws, Bondstone needs a retry and
terminal-failure policy for that local outbox record.

RabbitMQ and Azure Service Bus receive behavior has a different ownership
boundary. After a broker has delivered a message to a Bondstone receive worker,
Bondstone dispatches through the module receive pipeline and then performs the
native settlement handoff. RabbitMQ redelivery, delayed retry, dead-letter
exchange behavior, Service Bus max delivery count, and Service Bus dead-letter
subqueue behavior remain broker/app-owned.

The previous `DurableOutboxStatus.DeadLettered` term can be confused with
provider-native dead-letter queues. The current behavior means "Bondstone has
stopped retrying this outgoing local outbox record," not "Bondstone wrote a
message to a broker DLQ."

## Decision

Bondstone will use terminal-failure language for outgoing persisted outbox
records that have exhausted Bondstone-owned retry.

- Keep Bondstone-owned retry policy for claimed outgoing persisted outbox
  records.
- Keep provider-owned receive retry and broker dead-letter policy outside
  Bondstone.
- Rename the current outgoing outbox terminal state from dead-letter language
  to terminal-failure language before stronger compatibility expectations
  harden.
- Use `DurableOutboxStatus.TerminalFailed` as the current persisted/public
  terminal status text for new outbox terminal failures.
- Use `DurableOutboxFailureDecisionKind.TerminalFailure`,
  `DurableOutboxFailureDecision.TerminalFailure(...)`,
  `ShouldTerminalFail`, `TerminalFailedCount`, and
  `MarkTerminalFailedAsync(...)` for the current public API vocabulary.
- Preserve old `DeadLettered`/`DeadLetter` API members as obsolete
  compatibility aliases where practical, and keep old persisted
  `DeadLettered` status text readable.
- Avoid introducing a provider-neutral broker DLQ abstraction.

## Consequences

Renaming the terminal state reduces confusion between outgoing outbox terminal
failure and broker dead-lettering.

New terminal outbox failures write `TerminalFailed` status text. Existing
`DeadLettered` rows remain readable through the compatibility enum alias. A
data migration from `DeadLettered` to `TerminalFailed` is optional for
operators that want uniform status text, not required for Bondstone to read
existing rows.

Source callers should move to the terminal-failure members. Consumers that
construct `DurableOutboxDispatchResult` with named arguments must update the
terminal count argument name from `deadLetteredCount` to
`terminalFailedCount`. Third-party `IDurableOutboxDispatchRecorder`
implementations can continue implementing the obsolete
`MarkDeadLetteredAsync(...)`; the new default `MarkTerminalFailedAsync(...)`
method forwards to it for compatibility.

Provider-native RabbitMQ dead-letter exchanges, Service Bus dead-letter
subqueues, retry schedules, delivery counts, and related broker policy remain
application/provider-owned.

## Amendment 2026-06-10: No Legacy Compatibility Surface

After implementation review, Bondstone does not have old persisted
`DeadLettered` data or an established public compatibility surface that needs
to be supported. The terminal-failure rename should therefore be clean:

- do not keep obsolete `DeadLettered` or `DeadLetter` public aliases;
- do not keep `MarkDeadLetteredAsync(...)`,
  `ShouldDeadLetter`, or `DeadLetteredCount`;
- do not parse old persisted `DeadLettered` status text as a supported
  compatibility path;
- write and read only the current `TerminalFailed` outbox status text for
  terminal outgoing outbox failures.

This amendment narrows the compatibility consequences above. It does not
change the boundary between Bondstone-owned outgoing outbox terminal failure
and provider-owned receive-side broker dead-letter policy.

## Related Decisions

- [0011 Outbox Claim Lease State](0011-outbox-claim-lease-state.md)
- [0013 Outbox Dispatch Lifecycle Contract](0013-outbox-dispatch-lifecycle-contract.md)
- [0017 Outbox Dispatcher Composition](0017-outbox-dispatcher-composition.md)
- [0038 Provider Retry Recovery And Settlement Boundaries](0038-provider-retry-recovery-and-settlement-boundaries.md)

## Application Notes

- Current contract: Bondstone writes and reads `TerminalFailed` for outgoing
  persisted outbox records that exhaust Bondstone-owned retry. Provider-native
  receive retry and DLQ behavior remains outside Bondstone.
- Stable docs: applied in
  [messaging.md](../architecture/messaging.md),
  [persistence-core.md](../architecture/persistence-core.md),
  [persistence-postgresql.md](../architecture/persistence-postgresql.md),
  [persistence-postgres.md](../architecture/persistence-postgres.md), and
  [setup.md](../setup.md).
- Agent guidance: applied in [AGENTS.md](../../AGENTS.md).
- Application evidence: core public names now include
  `DurableOutboxStatus.TerminalFailed`,
  `DurableOutboxFailureDecisionKind.TerminalFailure`,
  `DurableOutboxFailureDecision.TerminalFailure(...)`,
  `ShouldTerminalFail`, `TerminalFailedCount`, and
  `IDurableOutboxDispatchRecorder.MarkTerminalFailedAsync(...)`. Built-in
  PostgreSQL recorders write `TerminalFailed`.
- Pending or deferred: none for this decision. Future retention or archival
  tooling remains outside this ADR.

## Verification

- `pnpm format:check`
- `pnpm backend:build`
- `pnpm backend:test:fast`
- `pnpm backend:test:integration`
