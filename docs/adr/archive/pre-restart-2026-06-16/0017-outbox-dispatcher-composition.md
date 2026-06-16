# 0017 Outbox Dispatcher Composition

Status: Archived
Application: Not Applicable
Date: 2026-06-05

## Context

Bondstone now has provider-neutral outbox primitives for claiming rows,
renewing claim leases, recording dispatch outcomes, and deciding retry versus
dead-letter after a failed delivery attempt. PostgreSQL implements the
provider-specific SQL for claiming, lease renewal, and dispatch outcome
recording.

The next boundary is dispatch composition. A dispatcher must claim rows, send
them through a transport adapter, renew ownership before work that may happen
after a batch delay, and record the outcome. This affects public runtime
ownership and transport adapter shape, so it needs an ADR before
implementation.

Bondstone should remain a library, not a framework runtime. The first
dispatcher must not introduce leader election, singleton sweeper requirements,
hosted service registration, global orchestration, handler discovery, or
transport-specific behavior.

## Decision

`Bondstone` defines a provider-neutral `IDurableOutboxTransport` contract for
sending a claimed `DurableOutboxRecord`.

`Bondstone` defines `IDurableOutboxDispatcher` as the provider-neutral
dispatcher boundary. `DurableOutboxDispatcher` is the default plain composable
implementation that dispatches one batch when called. It composes:

- `IDurableOutboxClaimer`;
- `IDurableOutboxLeaseRenewer`;
- `IDurableOutboxTransport`;
- `IDurableOutboxFailurePolicy`;
- `IDurableOutboxDispatchRecorder`;
- `TimeProvider`.

The dispatcher accepts a stable worker identity, positive lease duration, and
positive maximum claim count. It claims due rows, renews the lease for each row
before sending, sends through the transport, records success, and records
retry or dead-letter outcomes after transport failure.

If renewal or outcome recording returns `false`, the dispatcher treats the row
as stale or no longer owned and moves on. If the caller's cancellation token is
cancelled, cancellation propagates instead of being converted to retry.

The dispatcher returns `DurableOutboxDispatchResult` with counts for claimed,
dispatched, retry-scheduled, dead-lettered, and stale rows.

The first dispatcher is intentionally not a hosted service. Hosted worker
registration, polling intervals, minimum message age, route circuit breaking,
dead-letter routing, archiving, stale-claim sweeps, and transport-specific
adapters remain separate decisions.

## Consequences

Consumers and future hosted workers can use one small dispatcher unit without
adopting a framework runtime.

Competitive workers remain the default direction. Multiple workers can call
the dispatcher concurrently when the provider claimer supports skip-locked or
equivalent claim semantics.

The dispatcher gives Bondstone a testable runtime composition point while
keeping provider-specific locking and transport-specific sending outside core.

At-least-once delivery remains explicit. If a message is sent but outcome
recording fails because the lease was lost, the message may be sent again.
Consumers still need an inbox or idempotent handlers.

## Related Decisions

- [0011 Outbox Claim Lease State](0011-outbox-claim-lease-state.md)
- [0012 Outbox Claim Contract](0012-outbox-claim-contract.md)
- [0013 Outbox Dispatch Lifecycle Contract](0013-outbox-dispatch-lifecycle-contract.md)

## Application Notes

- Current contract: `IDurableOutboxDispatcher` is the dispatch boundary.
  `DurableOutboxDispatcher` dispatches one claimed batch by composing claim,
  lease renewal, transport send, failure policy, and dispatch outcome
  recording. `IDurableOutboxTransport` is the minimal transport send boundary
  for claimed records.
- Stable docs: Current persistence rules are described in
  [docs/architecture/persistence.md](../architecture/persistence.md),
  [docs/architecture/persistence-core.md](../architecture/persistence-core.md),
  [docs/architecture/hosting.md](../architecture/hosting.md), and
  [docs/archive/mvp-plan.md](../archive/mvp-plan.md).
- Agent guidance: Root [AGENTS.md](../../AGENTS.md) requires ADR review before
  broad durable behavior, provider support, transport support, or migration
  policy changes.
- Application evidence: Core dispatcher contract and implementation, transport
  contract, dispatch result, neutral unit tests, PostgreSQL integration tests
  using real claim, lease renewal, and dispatch recording with a fake
  transport, direct transport adapters, routed transport selection, and neutral
  hosted outbox worker registration are applied.
- Pending or deferred: None for the dispatcher composition decision. Minimum
  message age, route or destination circuit breaking, archiving, stale-claim
  sweeps, and cleanup workers remain separate future decisions.

## Verification

Read back [docs/architecture/persistence.md](../architecture/persistence.md),
[docs/architecture/persistence-core.md](../architecture/persistence-core.md),
[docs/archive/extraction.md](../archive/extraction.md), and
[docs/archive/extraction-plan.md](../archive/extraction-plan.md). Ran no-restore build,
targeted unit tests, targeted PostgreSQL integration tests, fast tests, pack,
format check, diff check, and `pnpm backend:test:integration`. Later
checkpoint verification restored the default `pnpm check` gate.
