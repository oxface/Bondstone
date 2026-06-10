# Inbox Recovery

Status: Resolved by
[ADR 0043](../../adr/0043-inbox-stale-receive-recovery.md).

Goal: decide what Bondstone should do with already-received but unprocessed
inbox rows.

## Scope

- Reviewed `DurableInboxHandlerExecutor`, `DurableInboxRegistrationStatus`,
  `DurableInboxHandleStatus`, `DurableInboxAlreadyReceivedException`,
  `DurableInboxMessageKey`, provider inbox registrars/stores, and module
  receive pipeline behavior.
- Verified that newly registered rows run handlers, already-processed rows skip
  handlers, already-received unprocessed rows remain loud in module receive,
  and provider settlement happens only after successful dispatch.
- Reviewed whether the low-level inbox executor commit delegate should remain,
  move, or be removed.

## ADRs

- [0043 Inbox Stale Receive Recovery](../../adr/0043-inbox-stale-receive-recovery.md)

## Resolution

- Current loud behavior is accepted. Bondstone does not silently re-run
  handlers for already-received unprocessed inbox rows because it cannot prove
  safe re-execution.
- Stale receive recovery is operator-owned or application-owned for now.
  Applications that inspect or mutate stale inbox rows own the safety proof,
  provider/broker coordination, and audit trail.
- Bondstone does not currently provide inbox leases, stale receive sweepers,
  recovery hooks, failed receive states, maintenance workers, or
  provider-neutral row mutation helpers.
- Direct provider receive workers continue to settle only after successful
  Bondstone dispatch. `DurableInboxAlreadyReceivedException` follows the same
  failure handoff as other receive dispatch failures.
- The low-level `DurableInboxHandlerExecutor` no longer accepts a commit
  delegate. It stages receive-side work; normal module transaction behaviors
  own the real commit boundary.

## Follow-Up

- Any future Bondstone-owned stale receive recovery requires a focused ADR that
  defines owner, timeout or lease model, transaction boundary, provider SQL
  semantics, allowed mutations, and transport settlement interaction.
- Any future public API tightening around inbox executor naming, commit
  ownership, or row mutation helpers should go through compatibility/API ADR
  review.

## Verification

- Existing coverage already protects the accepted behavior: executor duplicate
  handling, module receive `AlreadyReceived` exceptions, PostgreSQL
  registration outcomes, provider transaction commit ownership, and provider
  settlement-after-success behavior.
- This resolution removed the low-level inbox executor commit delegate and
  updated call sites/tests to commit outside the executor.
- Verified this resolution with:
  - `git diff --check`
  - `pnpm format:check`
  - `pnpm backend:build`
  - `pnpm backend:test:fast`
  - `pnpm backend:test:integration`
  - `dotnet test tests/Bondstone.EntityFrameworkCore.Postgres.Tests/Bondstone.EntityFrameworkCore.Postgres.Tests.csproj --configuration Release --no-build --filter "Category=Integration"`
