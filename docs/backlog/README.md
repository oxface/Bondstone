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

- [09-domain-events.md](09-domain-events.md) defines explicit module-local
  domain event behavior before real-project validation.
- [10-public-api-and-composition-cleanup.md](10-public-api-and-composition-cleanup.md)
  turns the decision work into focused API and composition cleanup.
- [11-persistence-recovery-and-maintenance.md](11-persistence-recovery-and-maintenance.md)
  tracks operational persistence recovery, retention, and operation-state work.
- [12-transport-and-hosting-ergonomics.md](12-transport-and-hosting-ergonomics.md)
  tracks worker, receive-helper, topology, and provider ergonomics.
- [13-real-project-readiness.md](13-real-project-readiness.md) tracks sample,
  setup, and adoption work needed before broader real-project validation.
- [14-future-work.md](14-future-work.md) collects lower-priority ideas that
  are not yet active decision or implementation work.

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
- [archive/07-outbox-worker-topology.md](archive/07-outbox-worker-topology.md)
  accepted the current aggregate-only worker topology and deferred
  module-targeted worker APIs.
- [archive/08-execution-context-and-api-surface.md](archive/08-execution-context-and-api-surface.md)
  accepted ambient module execution context semantics, accepted the public API
  surface policy, and deferred implementation cleanup to the active tracks.
