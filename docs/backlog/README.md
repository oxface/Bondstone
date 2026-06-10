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

Use the active items as an implementation backlog, not as another acceptance
campaign. The current recommended order is:

1. [09-module-pipeline-and-capability-runtime.md](09-module-pipeline-and-capability-runtime.md),
   because Domain Events and future capabilities need explicit runtime
   pipeline planning before implementation.
2. [10-domain-events.md](10-domain-events.md), because real-project aggregate
   and handler patterns should not form around an unstated domain-event
   convention.
3. The inventory and documentation slices of
   [11-public-api-and-composition-cleanup.md](11-public-api-and-composition-cleanup.md),
   because public API cleanup needs a baseline before compatibility hardens.
4. The operator docs and maintenance API slices of
   [12-persistence-recovery-and-maintenance.md](12-persistence-recovery-and-maintenance.md),
   because production validation needs a recovery story even if operators own
   recovery at first.
5. The receive-helper and worker-isolation slices of
   [13-transport-and-hosting-ergonomics.md](13-transport-and-hosting-ergonomics.md),
   when custom receive loops or deployment isolation become active needs.
6. [14-real-project-readiness.md](14-real-project-readiness.md), after domain
   events and the first API cleanup pass are stable enough to avoid immediate
   sample churn.

- [09-module-pipeline-and-capability-runtime.md](09-module-pipeline-and-capability-runtime.md)
  defines explicit module pipeline planning and capability contribution before
  Domain Events add runtime behavior.
- [10-domain-events.md](10-domain-events.md) defines explicit module-local
  domain event behavior before real-project validation.
- [11-public-api-and-composition-cleanup.md](11-public-api-and-composition-cleanup.md)
  turns the decision work into focused API and composition cleanup.
- [12-persistence-recovery-and-maintenance.md](12-persistence-recovery-and-maintenance.md)
  tracks operational persistence recovery, retention, and operation-state work.
- [13-transport-and-hosting-ergonomics.md](13-transport-and-hosting-ergonomics.md)
  tracks worker, receive-helper, topology, and provider ergonomics.
- [14-real-project-readiness.md](14-real-project-readiness.md) tracks sample,
  setup, and adoption work needed before broader real-project validation.
- [15-future-work.md](15-future-work.md) collects lower-priority ideas that
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
