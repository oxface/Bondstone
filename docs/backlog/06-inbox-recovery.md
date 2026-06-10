# Inbox Recovery

Goal: decide what Bondstone should do with already-received but unprocessed
inbox rows.

## Scope

- Review `DurableInboxHandlerExecutor`, `DurableInboxRegistrationStatus`,
  `DurableInboxHandleStatus`, `DurableInboxAlreadyReceivedException`, provider
  inbox registrars, and module receive pipeline behavior.
- Decide whether current loud behavior remains the only contract.
- Consider inbox leases, stale receive recovery, app-owned recovery hooks, or
  maintenance workers.
- Review whether the low-level inbox executor commit delegate should remain,
  move, or be renamed to make transaction ownership clearer.
- Revisit `Registrar` and `Store` vocabulary only if contract names are
  already changing.

## ADRs

- [0043 Inbox Stale Receive Recovery](../adr/0043-inbox-stale-receive-recovery.md)

## Review Questions

- Is operator-owned recovery acceptable for stale already-received inbox rows?
- What proof would let Bondstone safely re-run a handler?
- Should recovery be provider-owned, app-hooked, or a core worker capability?
- Does commit ownership belong in the low-level executor or provider/module
  transaction behavior?

## Candidate Deliverables

- Accepted, rejected, or narrowed ADR 0043.
- Stable messaging and persistence docs updated with the accepted recovery
  story.
- Follow-up implementation tasks for leases, hooks, maintenance workers, or
  naming cleanup if accepted.

## Verification

- `pnpm backend:test:fast`
- `pnpm backend:test:integration` for provider-backed recovery behavior.
