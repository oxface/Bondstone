# 0009 Prune Pre-Restart Archive And Planning Notes

Status: Accepted
Application: Applied
Date: 2026-06-16

## Context

ADR 0001 restarted Bondstone's active ADR history after the post-MVP pivot and
kept the old ADR sequence under `docs/adr/archive/pre-restart-2026-06-16/` for
traceability. Closer review showed that most of the archived material now
creates more confusion than value: it describes removed packages, discarded
transport topology ideas, obsolete runtime pipeline shapes, and deferred
experiments that are no longer useful for the template handoff.

The repository also still carried a transitional post-MVP plan under
`docs/todos/`. Its implemented decisions now live in active ADRs and stable
docs, while remaining backlog work belongs in GitHub Issues or Projects.

The maintainer explicitly approved pruning these documents after durable
current decisions were moved into active ADRs and stable docs.

## Decision

Remove the detailed pre-restart ADR archive and the transitional `docs/todos/`
planning folder from the repository.

The durable current decision trail is:

- the active ADR set in `docs/adr/`;
- stable docs under `docs/`;
- GitHub Issues and Projects for backlog items and real-project follow-up.

The old pre-restart sequence is preserved only as summarized context in ADR
0001 and this ADR. Future work should amend or supersede active ADRs instead
of consulting, editing, or reintroducing the old archive.

If a deleted pre-restart detail proves necessary later, recover it from Git
history and move only the still-relevant rule into an active ADR or stable doc.

## Consequences

The documentation surface becomes much easier for another team to consume.
There is less accidental authority from obsolete ADRs and temporary planning
notes.

Fine-grained historical archaeology moves to Git history. That is acceptable
for this repository because the active baseline already captured the applied
architecture, and the deleted material mostly described paths the project
intentionally abandoned.

`docs/todos/` no longer exists as a planning area. Temporary plans should be
short-lived in issues, comments, or local work notes, and durable outcomes must
land in ADRs and stable docs.

## Related Decisions

- Amends [0001 Restart ADR History Around Current Baseline](0001-restart-adr-history-around-current-baseline.md).
- Relates to [0006 Samples Testing Packaging And Docs](0006-samples-testing-packaging-and-docs.md).

## Application Notes

- Current contract: active ADRs and stable docs are the repository's durable
  documentation surface.
- Stable docs: [docs/adr/README.md](README.md),
  [docs/README.md](../README.md), and
  [docs/repository.md](../repository.md) describe documentation ownership and
  ADR navigation.
- Agent guidance: root [AGENTS.md](../../AGENTS.md) and
  [docs/adr/AGENTS.md](AGENTS.md) point agents to active ADRs and stable docs,
  not the removed archive or planning folder.
- Application evidence: `docs/adr/archive/pre-restart-2026-06-16/` and
  `docs/todos/` were removed after current behavior was applied to active ADRs
  and stable docs.
- Pending or deferred: none.

## Verification

Checked references with `rg` and removed stale links to the pruned archive and
planning folder. Formatting and repository verification run with
`pnpm format:check`, `pnpm backend:test:integration`, and `pnpm check`.
