# 0012 Outbox Claim Contract

Status: Accepted
Application: Applied
Date: 2026-06-05

## Context

Bondstone now has provider-neutral outbox records, dispatch state with claim
lease fields, EF Core mappings for those fields, and PostgreSQL integration
tests proving `FOR UPDATE SKIP LOCKED` can support concurrent outbox row
selection.

The next durable boundary is claiming outbox messages for dispatch. Claiming is
public durable behavior because it decides which rows are eligible, when a row
is considered owned by a worker, how lease expiration is represented, and what
state a transport adapter receives before attempting delivery.

The claim contract must stay smaller than a dispatcher. It should not decide
transport acknowledgement, retry delays, lease renewal, stale claim recovery,
or dead-letter ownership yet.

## Decision

`Bondstone` defines a provider-neutral `IDurableOutboxClaimer` contract that
claims due outbox messages and returns the claimed `DurableOutboxRecord`
instances.

Claiming:

- accepts a stable `claimedBy` worker identity;
- accepts a positive lease duration;
- accepts a positive maximum row count;
- marks claimed rows as `Processing`;
- writes `ClaimedBy` and `ClaimedUntilUtc`;
- increments `AttemptCount` when a row is claimed;
- includes pending due rows and expired processing rows;
- does not dispatch, acknowledge, retry, renew, dead-letter, or clean up rows.

Provider-specific packages implement the contract using provider-specific
locking or update primitives. PostgreSQL uses `FOR UPDATE SKIP LOCKED`.

## Consequences

Transport adapters can depend on a small claim boundary without owning database
locking details.

Claimed rows carry enough state for future recovery and retry decisions, but
those decisions remain deferred.

Attempt count increments at claim time. This treats a claim as the start of a
delivery attempt, even if transport delivery later fails or the process dies.

The first implementation can remain PostgreSQL-specific while other providers
choose equivalent SQL or locking strategies later.

## Application Notes

- Current contract: `IDurableOutboxClaimer` claims eligible outbox records and
  returns records with `Processing` dispatch state and populated claim lease
  fields.
- Stable docs: Current persistence rules are described in
  [docs/architecture/persistence.md](../architecture/persistence.md), with
  extraction state in [docs/extraction.md](../extraction.md) and
  [docs/extraction-plan.md](../extraction-plan.md).
- Agent guidance: Root [AGENTS.md](../../AGENTS.md) requires ADR review before
  broad durable behavior, provider support, or migration policy changes.
- Application evidence: Core claim contract, PostgreSQL implementation,
  PostgreSQL service registration, and Testcontainers-backed claim behavior
  tests are applied. Tests cover validation, pending due rows, scheduled
  pending rows, locked-row skipping, expired lease reclaim, active lease
  exclusion, and schema-aware registration.
- Pending or deferred: Dispatch loops, transport send implementation,
  stale claim recovery, dead-letter routing, dispatcher configuration,
  provider implementations beyond PostgreSQL, and migration helpers remain
  future work. Lease renewal is now covered by ADR 0011, and basic retry versus
  dead-letter failure decisions are now covered by ADR 0013.

## Verification

Read back [docs/architecture/persistence.md](../architecture/persistence.md),
[docs/extraction.md](../extraction.md), and
[docs/extraction-plan.md](../extraction-plan.md). Ran targeted PostgreSQL
tests for service registration and outbox claim behavior. Ran formatting,
no-restore build, fast tests, pack, and `pnpm backend:test:integration`.
Fresh restore timed out while checking the PostgreSQL test project and should
be investigated separately before relying on a clean-machine verification.
