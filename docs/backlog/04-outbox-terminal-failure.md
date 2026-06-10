# Outbox Terminal Failure

Goal: resolve Bondstone's outgoing outbox retry and terminal-failure
terminology before the persisted/public status language hardens.

## Scope

- Review `DurableOutboxStatus.DeadLettered`,
  `DurableOutboxFailureDecisionKind.DeadLetter`, recorder methods, tests, and
  docs for confusion with provider-native broker DLQs.
- Keep the boundary clear: Bondstone owns outgoing persisted outbox retry and
  terminal failure; RabbitMQ and Service Bus own receive retry and native
  dead-letter policy after nack/abandon.
- Decide whether to keep `DeadLettered` with sharper docs or rename it to a
  term such as terminal failure.
- If renaming, define compatibility and persistence migration expectations.

## ADRs

- [0041 Outbox Terminal Failure Boundary](../adr/0041-outbox-terminal-failure-boundary.md)

## Review Questions

- Is `DeadLettered` acceptable as a Bondstone outbox term if docs repeatedly
  distinguish it from broker DLQs?
- Is a rename worth the migration and compatibility cost after initial package
  publication?
- Should failure reason text be constrained or redacted before production
  guidance hardens?

## Candidate Deliverables

- Accepted or rejected ADR 0041.
- Stable docs updated with the accepted outbox terminal-failure language.
- Follow-up implementation issue for rename/migration if the ADR accepts one.

## Verification

- `pnpm backend:test:fast`
- `pnpm backend:test:integration` if persisted status text or provider SQL
  changes.
