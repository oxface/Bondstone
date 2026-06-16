# 0013 Outbox Dispatch Lifecycle Contract

Status: Archived
Application: Not Applicable
Date: 2026-06-05

## Context

Bondstone now has provider-neutral outbox records, provider-neutral claim lease
state, a narrow `IDurableOutboxClaimer` contract, and a PostgreSQL claimer that
uses `FOR UPDATE SKIP LOCKED`.

Claiming alone is not enough for a transport adapter. After a message is
claimed and delivery is attempted, the adapter needs a durable way to record
success, schedule a retry after a transient failure, or move the message to a
terminal dead-letter state. These operations affect public persistence
semantics, retry behavior, and stale-worker safety, so they need an ADR before
implementation.

The lifecycle contract must still stay smaller than a dispatcher. It should
record delivery outcomes, but it should not own a background loop, transport
send implementation, retry-delay calculation, max-attempt policy, lease
renewal, or dead-letter routing.

## Decision

`Bondstone` defines a provider-neutral `IDurableOutboxDispatchRecorder` contract
for recording the result of a claimed outbox delivery attempt.

The contract has explicit methods for:

- marking a claimed message as dispatched;
- scheduling a claimed message for retry after a failure;
- marking a claimed message as dead-lettered.

Each method accepts the message id and the stable `claimedBy` worker identity.
Provider implementations must update a row only when the row is still
`Processing`, is still owned by that claimant, and the relevant outcome
timestamp is within the active claim lease. Methods return `true` when the row
was updated and `false` when the row was missing, not processing, claimed by a
different worker, or already outside the active lease.

Scheduling a retry moves the row back to `Pending`, records failure timestamp
and reason, sets `NextAttemptAtUtc`, and clears claim lease fields. It does not
increment attempt count; attempts are counted when rows are claimed.

Marking dispatched or dead-lettered moves the row to a terminal state and
clears claim lease fields. Dispatch clears retry/failure fields. Dead-letter
keeps failure timestamp and reason.

Provider-specific packages implement these updates using provider-specific SQL
or concurrency primitives. PostgreSQL uses claim-owner and lease-aware
`UPDATE` statements.

## Consequences

Transport adapters can record delivery outcomes without owning database
locking details.

Late or stale workers cannot acknowledge a row after the claim lease has
expired. They must renew the lease in a later API or accept that the message
may be retried.

Retry delay calculation, max-attempt policy, lease renewal, dispatcher loops,
and dead-letter routing remain deliberately deferred. A future dispatcher can
compose those policies around this recorder.

## Amendment 2026-06-05: Failure Decision Policy

Bondstone now defines a small provider-neutral failure decision policy around
the dispatch recorder. The policy decides whether a failed claimed outbox
record should be retried or dead-lettered from the current attempt count,
maximum attempts, retry delay sequence, failure reason, and failure timestamp.

This is still not a dispatcher. The policy does not claim rows, send transport
messages, renew leases, record database updates, route dead letters, discover
handlers, or register background workers. Future dispatchers and transport
adapters can compose the policy with `IDurableOutboxDispatchRecorder`.

The default policy uses five maximum attempts and the initial retry delay
sequence `0s`, `10s`, `1m`, `5m`, and `15m`. Consumers that need different
behavior can construct the policy with explicit `maxAttempts` and
`retryDelays` parameters. A later ADR is still required before introducing
configuration binding, hosted dispatchers, transport-specific retry behavior,
or dead-letter routing.

## Amendment 2026-06-10: Terminal Failure Terminology

[ADR 0041](0041-outbox-terminal-failure-boundary.md) renames the current
outgoing outbox terminal status and public vocabulary from dead-letter wording
to terminal-failure wording. `IDurableOutboxDispatchRecorder` now exposes
`MarkTerminalFailedAsync(...)` for the current API vocabulary. The old
`MarkDeadLetteredAsync(...)` compatibility method is not kept because there is
no old public or persisted compatibility surface to support.

## Application Notes

- Current contract: `IDurableOutboxDispatchRecorder` records dispatch success,
  retry scheduling, and terminal-failure outcomes for currently claimed outbox
  rows. `IDurableOutboxFailurePolicy` and `DurableOutboxFailurePolicy` decide
  retry versus terminal failure after a failed claimed delivery attempt.
- Stable docs: Current persistence rules are described in
  [docs/architecture/persistence.md](../architecture/persistence.md),
  [docs/architecture/persistence-core.md](../architecture/persistence-core.md),
  [docs/architecture/persistence-postgresql.md](../architecture/persistence-postgresql.md),
  and [docs/archive/mvp-plan.md](../archive/mvp-plan.md).
- Agent guidance: Root [AGENTS.md](../../AGENTS.md) requires ADR review before
  broad durable behavior, provider support, or migration policy changes.
- Application evidence: Core dispatch lifecycle contract, PostgreSQL
  implementation, PostgreSQL service registration, and Testcontainers-backed
  lifecycle tests are applied. Core failure decision policy and neutral unit
  tests are applied.
- Pending or deferred: None for the dispatch lifecycle decision. Stale claim
  recovery orchestration, advanced configuration, provider implementations
  beyond PostgreSQL, and retention or archival tooling remain separate future
  decisions.

## Verification

Read back [docs/architecture/persistence.md](../architecture/persistence.md),
[docs/archive/extraction.md](../archive/extraction.md), and
[docs/archive/extraction-plan.md](../archive/extraction-plan.md). Ran targeted PostgreSQL
tests for dispatch success, retry scheduling, dead-lettering, stale owner
rejection, expired lease rejection, validation, and schema-aware registration.
Ran neutral failure-policy unit tests, formatting, no-restore build, fast
tests, pack, and `pnpm backend:test:integration`. Later checkpoint
verification restored the default `pnpm check` gate.
