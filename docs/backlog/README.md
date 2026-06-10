# Backlog

This folder contains documents for potential new work. Backlog documents are
not the current operating contract; they are places to shape review campaigns,
future ideas, and possible implementation slices before they become active
work.

Stable docs describe the current repository state. ADRs preserve durable
decision history. Archived docs preserve historical planning material that
should not steer new work.

When a backlog item becomes active, split it into reviewable slices, create or
update ADRs for durable technical decisions, apply completed behavior into
stable docs, and remove stale backlog language.

## Active Items

- [07-outbox-worker-topology.md](07-outbox-worker-topology.md) decides whether
  aggregate outbox workers need module-targeted isolation options.
- [08-execution-context-and-api-surface.md](08-execution-context-and-api-surface.md)
  decides execution-context alternatives, receive helper shape, and public API
  surface policy.
- [09-future-work.md](09-future-work.md) collects non-current follow-up ideas
  that are not yet active decision work.

## Archived Campaigns

- [archive/01-adr-application-audit.md](archive/01-adr-application-audit.md)
  audited active ADRs and resolved unapplied, deferred, obsolete, or
  superseded decisions.
- [archive/02-documentation-tightening.md](archive/02-documentation-tightening.md)
  cleaned MVP-era documentation into durable current-state documentation.
- [archive/03-architecture-code-review.md](archive/03-architecture-code-review.md)
  performed the post-MVP architecture and code review sweep.
- [archive/03-architecture-code-review-report.md](archive/03-architecture-code-review-report.md)
  records the Phase 03 review map, decision intake, and first resolution pass.
- [archive/04-outbox-terminal-failure.md](archive/04-outbox-terminal-failure.md)
  accepted the outgoing outbox `TerminalFailed` terminology and its boundary
  from broker DLQs.
- [archive/05-module-persistence-metadata.md](archive/05-module-persistence-metadata.md)
  accepted the current module persistence metadata shape and fallback service
  stance.
- [archive/06-inbox-recovery.md](archive/06-inbox-recovery.md) accepted the
  current loud already-received inbox behavior and commit-delegate stance.
