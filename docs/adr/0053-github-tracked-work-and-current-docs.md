# 0053 GitHub-Tracked Work And Current Docs

Status: Accepted
Application: Applied
Date: 2026-06-13

## Context

Bondstone's repository docs had three non-current information paths:

- stable docs for current behavior;
- ADRs for decision history;
- repository backlog and archive folders for planning notes, pressure points,
  and historical campaigns.

ADR 0049 reduced backlog maintenance by making `docs/backlog` ad hoc rather
than roadmap-like. That still left non-current planning material in the
repository. Maintainers now want repository documentation to describe how
Bondstone works today, with ADRs as the only repository documentation that can
preserve replaced decisions, rejected options, or historical rationale.

Actual task tracking belongs in GitHub Issues and GitHub Projects, where
ownership, prioritization, labels, milestones, and discussion can move without
being mistaken for package or architecture guidance.

## Decision

Bondstone will not maintain repository backlog or planning archive folders.

Stable docs describe the current operating contract only. They should not
carry speculative future work, roadmap notes, pressure-point lists, historical
campaign notes, or "maybe later" implementation ideas. When a stable doc needs
to state that Bondstone does not provide a feature, it should state the
current boundary directly rather than pointing to future work.

ADRs are the repository's durable decision trail. They remain the place for
context, considered alternatives, superseded decisions, accepted constraints,
and application history.

GitHub Issues and GitHub Projects are the tracker for actual backlog work,
including real-project findings, cleanup tasks, possible enhancements, and
prioritization. Issue descriptions should link to current stable docs and ADRs
when they need repository context. When issue work creates a durable technical
decision, the decision must move into an ADR. When issue work changes current
behavior, stable docs must be updated.

## Consequences

Repository docs become easier to trust as current operating guidance.

Maintainers lose an in-repository place for loose pressure-point lists.
Planning discipline moves to GitHub labels, projects, milestones, and linked
issues instead.

Historical planning notes that are not ADRs are removed from active source
navigation. Decision traceability remains in ADRs.

## Related Decisions

- Supersedes [0049 Ad Hoc Backlog Planning](0049-ad-hoc-backlog-planning.md)
- [0001 Adopt ADR-Led Maintenance](0001-adopt-adr-led-maintenance.md)
- [0048 Reference-Based Context Structure](0048-reference-based-context-structure.md)

## Application Notes

- Current contract: stable docs are current-state documentation; ADRs are the
  only repository docs that preserve decision history or non-current options;
  GitHub Issues and Projects track backlog work.
- Stable docs: applied to the root README, documentation index, repository
  docs, architecture docs, package docs, public API inventory, sample docs, and
  setup docs.
- Agent guidance: applied to root and documentation AGENTS files.
- Application evidence: `docs/backlog` and `docs/archive` are removed from
  current repository documentation.
- Pending or deferred: none.

## Verification

- `pnpm format:check`
- `git diff --check`
- Searched non-ADR Markdown for stale backlog/archive links and future-work
  wording.
