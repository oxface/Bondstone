# Architecture And Code Review

Goal: deeply review the completed MVP surface and make the first tightening
fixes before larger post-MVP expansion.

## Scope

- Start with high-level package, module, persistence, messaging, transport,
  hosting, sample, and test maps.
- Sweep from architecture to implementation multiple times, moving from broad
  dependency direction and public API shape into lower-level behavior.
- Look for stale design pressure from removed dependencies such as Rebus or a
  mediator-style abstraction.
- Review distributed durability behavior against real-world expectations:
  outbox retry and terminal failure, inbox idempotency, settlement ordering,
  message identity, operation state, serialization, topology validation,
  diagnostics, observability, and integration-test realism.
- Apply small, coherent fixes discovered during the review when they are
  clearly safe and reviewable.
- Produce a human review plan with slices, files, review questions, known
  risks, and suggested verification.

## Expected Human Review Slices

- modular persistence and package boundaries;
- outbox writing, claiming, dispatch, retry, and terminal failure;
- inbox registration, idempotency, and recovery behavior;
- provider-neutral receive pipelines and module-scoped execution;
- command sending and integration event publishing;
- transport adapters: Local, RabbitMQ, and Service Bus;
- hosted workers and receive lifecycle helpers;
- topology validation and startup diagnostics;
- diagnostics, tracing, logging, and observability surface;
- serialization and durable message identity;
- operation-state semantics;
- EF Core persistence and PostgreSQL non-EF persistence;
- samples, setup guidance, and developer ergonomics;
- tests, integration infrastructure, and CI verification.

## Exit Criteria

- The first review pass produces either merged fixes or explicit follow-up
  items with owners and verification guidance.
- The human review plan is organized by slice and points to the files and tests
  that matter for each slice.
- Any broad public API, package-boundary, provider-support, compatibility, or
  release-policy change discovered during review is routed through ADR work.

## Handover Prompt

```text
You are taking over Phase 03 of Bondstone's post-MVP stabilization:
Architecture And Code Review.

Read first:

- AGENTS.md
- README.md
- docs/README.md
- docs/backlog/README.md
- docs/backlog/01-adr-application-audit.md
- docs/backlog/02-documentation-tightening.md
- docs/backlog/03-architecture-code-review.md
- the final reports from Phases 01 and 02, if present
- docs/architecture/README.md and the topic docs it links
- docs/testing.md
- docs/samples.md
- docs/setup.md

Mission:

Perform a deep, aggressive architecture and code review of Bondstone's
post-MVP surface. Start broad, sweep lower, make safe reviewable fixes, and
produce a human review plan organized by architecture slice and file area.

Operating rules:

- Start with context gathering. Do not jump into isolated fixes before mapping
  package boundaries, public APIs, persistence, messaging, transport, hosting,
  samples, and tests.
- Route broad public API, package-boundary, provider-support, compatibility,
  release-policy, or durable behavior changes through ADR work.
- Look especially for stale design pressure from removed dependencies such as
  Rebus or mediator-style abstractions.
- Review durability behavior against real distributed-app expectations:
  outbox retry and terminal failure, inbox idempotency, settlement ordering,
  message identity, operation state, serialization, topology validation,
  diagnostics, observability, and integration-test realism.
- Do not touch unrelated user changes. Check git status before edits and work
  with the current tree.

Execution:

1. Build a repository map by package, test project, sample, and stable doc.
2. Do a high-level dependency and public API sweep.
3. Do focused sweeps for:
   - modular persistence and package boundaries;
   - outbox writing, claiming, dispatch, retry, and terminal failure;
   - inbox registration, idempotency, and recovery behavior;
   - provider-neutral receive pipelines and module-scoped execution;
   - command sending and integration event publishing;
   - Local, RabbitMQ, and Service Bus transport adapters;
   - hosted workers and receive lifecycle helpers;
   - topology validation and startup diagnostics;
   - diagnostics, tracing, logging, and observability;
   - serialization and durable message identity;
   - operation-state semantics;
   - EF Core persistence and PostgreSQL non-EF persistence;
   - samples, setup guidance, and developer ergonomics;
   - tests, integration infrastructure, and CI verification.
4. Apply small fixes that are clearly correct and reviewable.
5. For larger concerns, write follow-up items with the owning slice, files,
   risk, recommended change, and verification path.
6. Run appropriate verification for changed code and docs.

Deliverables:

- Reviewable fixes for safe issues found during the sweep.
- A human review plan organized by slice. For each slice include:
  - purpose and current design summary;
  - important files and tests;
  - review questions;
  - risks or smells found;
  - recommended next actions;
  - verification commands.
- A final report listing fixes made, tests run, ADRs needed, and remaining
  review work.

Phase 03 is complete only when the first architecture/code sweep has produced
both concrete fixes and a useful human review plan for the next round.
```
