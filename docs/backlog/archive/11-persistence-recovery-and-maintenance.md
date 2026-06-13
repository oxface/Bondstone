# Persistence Recovery And Maintenance

Archived: 2026-06-12
Extracted: 2026-06-12

This issue clarifies the current recovery and maintenance story around durable
messaging persistence. It is not a design for new recovery helper APIs.

## Outcome

The persistence recovery and maintenance slice resolved as a documentation and
focused test-hardening pass.

- Stable docs now describe the operator-owned recovery boundary.
- Tests were added for dispatcher stale outcomes.
- Tests were added for command already-processed receive behavior.
- Tests were added for non-EF PostgreSQL outbox claim/reclaim/terminal
  failure behavior.
- Tests were added for non-EF PostgreSQL inbox duplicate outcomes.
- No recovery helper APIs were added.
- Helper APIs still require ADR review.

Future recovery work has returned to [../00-plans.md](../00-plans.md) as a
narrow pressure point for terminal outbox maintenance helpers, inbox stale
receive recovery helpers, and structured diagnostics/reporting.

## Current Behavior

- Terminal outbox rows use `DurableOutboxStatus.TerminalFailed` after
  Bondstone-owned outgoing dispatch attempts are exhausted. The row remains in
  persistence with `FailedAtUtc` and `FailureReason`; Bondstone does not move
  it to a broker dead-letter queue.
- Outbox failure text is diagnostic text from the failing dispatch exception
  and recorder path. It is useful for operators to inspect, but it is not a
  provider-neutral remediation command or a normalized machine contract.
- Outbox claim leases are active-dispatch coordination. PostgreSQL claimers
  can reclaim `Processing` rows after `ClaimedUntilUtc` expires. Outcome
  recorders and lease renewers update rows only when status, claimant, and
  lease checks still match.
- Already-received but unprocessed inbox rows remain operationally loud.
  Module receive pipelines throw `DurableInboxAlreadyReceivedException`
  instead of re-running handlers or acknowledging the provider message as
  handled.
- Already-processed inbox rows are idempotent success for the receive copy and
  skip handler execution.
- Operation state currently records `Pending` on durable send and `Completed`
  after successful durable command receive when a caller supplied an operation
  id. `Running`, `Failed`, and `Cancelled` remain storage/read-model values
  for application-owned policy.
- RabbitMQ and Service Bus receive workers settle only after Bondstone receive
  dispatch succeeds. On dispatch failure, RabbitMQ negative-acknowledges with
  the configured requeue value and Service Bus abandons the message, leaving
  retry/dead-letter policy provider-owned.

## Known Gaps

- Bondstone has no terminal-outbox retention, archival, purge, reset, replay,
  or operator mutation helper.
- Bondstone has no inbox lease, stale-row sweeper, failed receive state,
  stale-row reset, forced processed marker, replay helper, or
  provider-neutral row mutation helper.
- Failure text is captured, but there is no public cross-provider diagnostic
  report shape or structured remediation model.
- Claim leases recover expired outbox `Processing` claims by normal claiming;
  there is no separate claim maintenance API.
- Provider receive retry is intentionally broker/provider-owned, so Bondstone
  does not expose a provider-neutral receive retry or dead-letter abstraction.

## Accepted Decisions

- ADR 0038 keeps Bondstone retry ownership on persisted outgoing outbox
  dispatch and keeps provider receive retry/dead-letter policy broker-owned.
- ADR 0041 accepts terminal-failure terminology and `TerminalFailed` for
  outgoing persisted outbox records that exhaust Bondstone-owned retry.
- ADR 0043 keeps already-received but unprocessed inbox rows loud and defers
  stale receive recovery helpers until a later ADR proves a safe model.

## Decisions Requiring ADR

Any Bondstone-owned helper API or default runtime behavior for these areas
requires ADR review before implementation:

- terminal outbox row retention, purge, replay, reset, or archival;
- structured failure diagnostics or cross-provider operational reports;
- inbox leases, stale receive sweepers, recovery hooks, failed receive states,
  or row mutation helpers;
- default operation `Running`, `Failed`, `Cancelled`, retry, timeout, or
  cancellation transitions;
- provider-neutral receive retry/dead-letter abstractions;
- broker topology declaration helpers or policy ownership changes.

The ADR needs to define the owner, transaction boundary, timeout or lease
model, provider SQL semantics, allowed mutations, audit expectations, and
transport settlement interaction.

## Non-Goals

- Do not add recovery helper APIs in this slice.
- Do not change public API, provider receive behavior, durable messaging
  semantics, operation-state transitions, or persistence schemas.
- Do not sweep archived backlog notes.
- Do not treat broker dead-letter queues as Bondstone outbox terminal rows.

## Implementation Review Notes

- Core outbox dispatcher composes claim, lease renewal, transport send,
  failure policy, and lease-aware outcome recording. Stale lease or claimant
  mismatches are counted as stale outcomes, not force-updated.
- EF-backed PostgreSQL and non-EF PostgreSQL providers both claim due pending
  rows and expired processing rows, use claim-owner and lease-aware outcome
  updates, and record `TerminalFailed` for terminal outgoing dispatch failure.
- PostgreSQL inbox registrars return registered, already-received, and
  already-processed outcomes with `INSERT ... ON CONFLICT DO NOTHING`
  semantics.
- Module receive pipelines throw for already-received unprocessed rows; direct
  provider receive workers hand that failure back to native broker policy.

## Exit Criteria

- Stable docs name the current operator-owned recovery boundary.
- The next implementation slice does not add recovery helpers without a new or
  amended ADR.
