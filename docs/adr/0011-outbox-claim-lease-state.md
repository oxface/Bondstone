# 0011 Outbox Claim Lease State

Status: Amended
Application: Applied
Date: 2026-06-05

## Context

Bondstone has provider-neutral outbox records and dispatch state, EF Core
outbox mappings, and PostgreSQL integration tests proving `FOR UPDATE SKIP
LOCKED` can support concurrent outbox row selection.

A real outbox dispatcher needs more than a `Processing` status. It needs to
know who currently owns a claimed row and when that claim expires so crashed
dispatchers do not leave messages permanently stuck. Without lease state, a
public claim API would either overfit to one provider's transaction lifetime or
make recovery ambiguous.

Lease state affects public persistence contracts, EF schema shape, provider
implementations, retry behavior, and future cleanup/recovery policy, so it
requires an ADR before implementation.

## Decision

`DurableOutboxDispatchState` includes provider-neutral claim lease fields:

- `ClaimedBy`: stable dispatcher/worker identity for the current claim;
- `ClaimedUntilUtc`: UTC lease expiration timestamp.

The generic EF Core outbox entity and mapping persist those fields. Claim lease
state is part of stored outbox dispatch state, not a PostgreSQL-only concern.

Provider-specific claim implementations may use provider-specific SQL and
locking primitives, such as PostgreSQL `FOR UPDATE SKIP LOCKED`, but they must
write claim ownership and expiry through the shared outbox state shape.

This ADR does not introduce a public outbox claim service, dispatcher loop,
retry policy, lease renewal API, dead-letter policy, or stale-message recovery
policy. Those require a later API decision.

## Consequences

Outbox rows can represent an active but expiring claim in a provider-neutral
way.

PostgreSQL claiming updates rows to `Processing` with a claimant and lease
expiration, while other providers can implement equivalent behavior with their
own locking or update primitives.

The schema grew before the public claim API existed. This was intentional
because the claim API was designed around explicit lease state rather than
retrofitting lease fields later.

Recovery policy remains deferred. The claim API decides how expired claims
become eligible and how attempt counts are incremented, but later dispatcher
APIs must still decide how failures schedule retries and when messages become
dead-lettered.

## Amendment 2026-06-05: Lease Renewal Boundary

Bondstone now defines a provider-neutral `IDurableOutboxLeaseRenewer` contract.
The contract renews the lease for one claimed outbox message when the row is
still `Processing`, still owned by the supplied claimant, and still inside the
active lease at the provider's current time.

Renewal accepts a message id, stable `claimedBy` worker identity, and positive
lease duration. Implementations extend `ClaimedUntilUtc` to current provider
time plus the requested duration and return `true` only when the row was
updated. Missing rows, rows owned by another worker, non-processing rows, and
expired leases return `false`.

This boundary exists so long-running delivery attempts can keep ownership
without holding a database transaction open. It does not dispatch messages,
renew batches, recover stale claims, schedule retries, dead-letter messages,
route dead letters, or start hosted workers.

## Application Notes

- Current contract: Outbox dispatch state carries optional claim ownership and
  lease expiration fields. EF Core maps those fields provider-neutrally, and
  provider claim implementations write them when a row is claimed.
  `IDurableOutboxLeaseRenewer` renews active claim leases for a single claimed
  row.
- Stable docs: Current persistence rules are described in
  [docs/architecture/persistence.md](../architecture/persistence.md), with
  extraction state in [docs/extraction.md](../extraction.md) and
  [docs/extraction-plan.md](../extraction-plan.md).
- Agent guidance: Root [AGENTS.md](../../AGENTS.md) requires ADR review before
  broad durable behavior, provider support, or migration policy changes.
- Application evidence: Core dispatch state, EF mappings, EF metadata tests,
  PostgreSQL schema tests, PostgreSQL outbox claimer tests, and PostgreSQL
  lease-renewal tests include claim lease fields and renewal behavior.
- Pending or deferred: Dispatch loops, transport send implementation, lease
  batch renewal, stale claim recovery, dead-letter routing, dispatcher
  configuration, additional provider implementations, and migration helpers
  remain future work. Basic retry versus dead-letter failure decisions are now
  covered by ADR 0013.

## Verification

Read back [docs/architecture/persistence.md](../architecture/persistence.md),
[docs/extraction.md](../extraction.md), and
[docs/extraction-plan.md](../extraction-plan.md). Ran `pnpm check` and
`pnpm backend:test:integration`; default and integration checks pass after
applying the claim lease state. Later verification for the lease-renewal
amendment ran no-restore build, targeted PostgreSQL unit/integration tests,
fast tests, pack, format check, diff check, and `pnpm backend:test:integration`.
