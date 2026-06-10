# 0041 Outbox Terminal Failure Boundary

Status: Proposed
Application: Not Applicable
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

The current `DurableOutboxStatus.DeadLettered` term can be confused with
provider-native dead-letter queues. The current behavior means "Bondstone has
stopped retrying this outgoing local outbox record," not "Bondstone wrote a
message to a broker DLQ."

## Decision

Decide whether Bondstone should keep, clarify, or rename the current outgoing
outbox terminal failure terminology and retry boundary.

The candidate direction is:

- Keep Bondstone-owned retry policy for claimed outgoing persisted outbox
  records.
- Keep provider-owned receive retry and broker dead-letter policy outside
  Bondstone.
- Rename or strongly document `DeadLettered` as outgoing outbox terminal
  failure before stronger compatibility expectations harden.
- Avoid introducing a provider-neutral broker DLQ abstraction.

## Consequences

Clarifying or renaming the terminal state would reduce confusion between
outbox terminal failure and broker dead-lettering.

Renaming the public enum value and persisted status text would require careful
compatibility and migration handling because package versions have already
been published.

Keeping the existing name would avoid migration churn but requires strong docs
and diagnostic wording so users do not assume Bondstone manages broker DLQs.

## Related Decisions

- [0011 Outbox Claim Lease State](0011-outbox-claim-lease-state.md)
- [0013 Outbox Dispatch Lifecycle Contract](0013-outbox-dispatch-lifecycle-contract.md)
- [0017 Outbox Dispatcher Composition](0017-outbox-dispatcher-composition.md)
- [0038 Provider Retry Recovery And Settlement Boundaries](0038-provider-retry-recovery-and-settlement-boundaries.md)

## Application Notes

- Current contract: proposed only; no binding change yet.
- Stable docs: if accepted, update architecture messaging, persistence core,
  provider transport docs, and setup wording where terminal failure and broker
  DLQ boundaries are described.
- Agent guidance: if accepted, update root AGENTS architecture direction only
  if terminology or ownership changes.
- Application evidence: current code has outgoing outbox retry and
  `DeadLettered` persisted status; RabbitMQ and Service Bus receive workers use
  native nack/abandon handoff for provider-owned retry/DLQ.
- Pending or deferred: choose keep-versus-rename and, if renaming, define the
  compatibility/migration path.

## Verification

No executable verification yet; this is a proposed decision draft.
