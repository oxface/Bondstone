# 0043 Inbox Stale Receive Recovery

Status: Proposed
Application: Not Applicable
Date: 2026-06-10

## Context

Bondstone receive-side idempotency records an inbox row before handler
execution and marks it processed after successful handler execution. If a
message-handler pair is already processed, receive can skip handling. If a row
exists without a processed timestamp, current module receive pipelines treat
the state as operationally loud through `DurableInboxAlreadyReceivedException`.

This is safe because Bondstone has no accepted proof that re-running the
handler is idempotent after a partial or ambiguous receive attempt. However,
the current behavior can strand a message until an operator or future recovery
tool handles the stale row.

The low-level `DurableInboxHandlerExecutor` also accepts a commit delegate.
That keeps the primitive flexible for provider-specific transaction boundaries,
but module transaction behaviors often pass a no-op because the outer module
transaction owns the final commit.

## Decision

Decide whether Bondstone should add stale receive recovery semantics for
already-received inbox rows and whether inbox commit ownership should be
reshaped.

The candidate options are:

- Keep current loud behavior and document operator-owned recovery.
- Add inbox leases so a stale receive can be reclaimed after a bounded timeout.
- Add app-owned recovery hooks or maintenance workers that can inspect,
  delete, requeue, mark failed, or otherwise handle stale inbox rows.
- Reshape the low-level inbox executor API so commit ownership is clearer or
  owned by provider/module transaction behavior.

## Consequences

Keeping the current behavior is simple and safe but leaves stale receives as an
operator concern.

Adding recovery behavior improves operability but requires a durable proof of
when re-execution is safe, how leases interact with provider transactions, and
how recovery differs between EF Core and non-EF PostgreSQL persistence.

Changing public contract names such as registrar/store/executor or changing
commit-delegate shape is a compatibility-sensitive API change.

## Related Decisions

- [0014 Inbox Registration Contract](0014-inbox-registration-contract.md)
- [0015 Inbox Handle-Once Orchestration](0015-inbox-handle-once-orchestration.md)
- [0038 Provider Retry Recovery And Settlement Boundaries](0038-provider-retry-recovery-and-settlement-boundaries.md)

## Application Notes

- Current contract: proposed only; no binding change yet.
- Stable docs: if accepted, update messaging, persistence core, EF Core, and
  PostgreSQL persistence docs.
- Agent guidance: if accepted, update architecture direction only if stale
  receive recovery becomes a default durable behavior.
- Application evidence: current code distinguishes already processed from
  already received and throws for already-received/unprocessed module receive
  outcomes.
- Pending or deferred: decide whether recovery is operator-owned, app-hooked,
  or Bondstone-owned through leases/workers.

## Verification

No executable verification yet; this is a proposed decision draft.
