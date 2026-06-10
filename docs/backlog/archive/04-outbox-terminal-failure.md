# Outbox Terminal Failure

Status: Archived after resolution by
[ADR 0041](../../adr/0041-outbox-terminal-failure-boundary.md).

Goal: resolve Bondstone's outgoing outbox retry and terminal-failure
terminology before the persisted/public status language hardens.

## Scope

- Review the legacy `DurableOutboxStatus.DeadLettered`,
  `DurableOutboxFailureDecisionKind.DeadLetter`, recorder methods, tests, and
  docs for confusion with provider-native broker DLQs.
- Keep the boundary clear: Bondstone owns outgoing persisted outbox retry and
  terminal failure; RabbitMQ and Service Bus own receive retry and native
  dead-letter policy after nack/abandon.
- Decision: rename the current write-side vocabulary to terminal-failure
  language. New terminal rows use `DurableOutboxStatus.TerminalFailed`;
  no old persisted outbox status text or obsolete public dead-letter API names
  are kept.

## ADRs

- [0041 Outbox Terminal Failure Boundary](../../adr/0041-outbox-terminal-failure-boundary.md)

## Review Questions

- Resolved: `DeadLettered` is no longer the current Bondstone outbox
  vocabulary because it is too easy to confuse with provider-native broker
  dead-letter queues.
- Resolved: the rename is worth doing before compatibility expectations
  harden, without compatibility aliases or legacy persisted-text reads.
- Deferred: failure reason redaction or production operator guidance is not
  part of ADR 0041.

## Candidate Deliverables

- Accepted ADR 0041.
- Stable docs updated with the accepted outbox terminal-failure language.
- Focused implementation completed for core API names, provider persisted
  status writes, and tests.

## Follow-Up

- Consider future failure-reason redaction or retention guidance before
  production operations guidance hardens.

## Verification

- `pnpm backend:test:fast`
- `pnpm backend:test:integration` if persisted status text or provider SQL
  changes.
