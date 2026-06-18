---
baseline_commit: 783b26021a9d21a39ba74446287e3d256c161590
---

# Story 3.2: Rewrite Root AGENTS

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As an agent,
I want root `AGENTS.md` to route me to BMAD artifacts and scoped docs,
so that I load the right source before editing.

## Acceptance Criteria

1. Root AGENTS points to PRD, architecture, epics, and project-context.
2. Root AGENTS keeps operating rules and verification commands.
3. Root AGENTS removes retired decision workflow requirements and non-native planning routing.
4. Root AGENTS links resolve to existing files.

## Tasks / Subtasks

- [x] Review root agent routing. (AC: 1, 2)
  - [x] Keep the starting index ordered around README, PRD, architecture, epics, project-context, and relevant docs.
  - [x] Keep instructions to prefer nearest scoped `AGENTS.md` files.
  - [x] Keep operating rules about narrow edits, no commits/pushes, no overwriting user changes, and compatibility-sensitive public API cleanup.
- [x] Remove stale workflow requirements. (AC: 3)
  - [x] Remove active references to retired decision-file review, `docs/adr/**`, `bondstone-adr-*`, `adr-required`, and non-native planning artifacts.
  - [x] Route product scope to PRD, runtime architecture and package boundaries to BMAD architecture, sequencing to epics, and lean guardrails to project-context.
  - [x] Keep `/docs` framed as consumer-facing and repository-operation docs.
- [x] Verify links and formatting. (AC: 4)
  - [x] Confirm every root AGENTS link resolves.
  - [x] Run targeted stale-reference sweeps.
  - [x] Run formatting checks for touched markdown.

### Review Findings

- [x] [Review][Patch] Align live GitHub labels with `bmad-review-required` guidance [`.agents/skills/bondstone-github-issue-workflow/SKILL.md`:33]
- [x] [Review][Patch] Make `sprint-status.yaml` `last_updated` metadata consistent [`_bmad-output/implementation-artifacts/sprint-status.yaml`:2]

## Dev Notes

Root `AGENTS.md` is the repository-wide agent index. It should tell agents what to read before changing source, tests, docs, workflow, package boundaries, or BMAD artifacts. It must remain short enough to be useful in every session.

### Current State Intelligence

At baseline `783b26021a9d21a39ba74446287e3d256c161590`, root `AGENTS.md` already:

- points to README, BMAD PRD, BMAD architecture, epics, project-context, docs index, testing docs, samples docs, GitHub workflow docs, and local skill docs;
- keeps operating rules and common verification commands;
- says consumer docs under `docs/` should explain usage and operation while BMAD architecture owns internal durable behavior;
- contains no active `docs/architecture/**` or `docs/adr/**` links in the initial sweep.

This story can remain a verification pass if the current file still satisfies the acceptance criteria. Avoid duplicating long architecture rules from BMAD architecture.

### Architecture Compliance

Agents changing runtime architecture, durable messaging, persistence, hosting, transport behavior, package boundaries, public API strategy, documentation ownership, or verification strategy must start with BMAD architecture and then use the nearest scoped `AGENTS.md`.

### File Structure Requirements

Primary file:

- `AGENTS.md`

Do not rewrite scoped `src/**`, `tests/**`, or package-level AGENTS files as part of this story unless a root AGENTS instruction sends agents to a deleted or retired path. Scoped reference cleanup is covered by Epic 4.

### Testing Requirements

Recommended verification:

- `test -f README.md`
- `test -f _bmad-output/planning-artifacts/prds/prd-Bondstone-2026-06-18/prd.md`
- `test -f _bmad-output/planning-artifacts/architecture.md`
- `test -f _bmad-output/planning-artifacts/epics.md`
- `test -f _bmad-output/project-context.md`
- `test -f docs/README.md`
- `rg -n "docs/architecture|docs/adr|adr-required|bondstone-adr|non-native planning|decision-file review" AGENTS.md`
- `pnpm exec prettier --check AGENTS.md _bmad-output/implementation-artifacts/3-2-rewrite-root-agents.md _bmad-output/implementation-artifacts/sprint-status.yaml`

No runtime tests are expected for this documentation-only story.

### Previous Story Intelligence

Story 3.1 handles the human-facing root README. Keep root AGENTS consistent with README routing but agent-focused: what to read before changing each kind of artifact.

Epic 2 removed retired decision routing. Do not reintroduce retired ADR workflow language as an agent precondition.

### Git Intelligence

Recent commits emphasize BMAD document routing and a lean project context. Preserve the index style used in current root AGENTS rather than expanding it into a full handbook.

### Latest Technical Information

No web research is required. This story uses local repository routing and source-of-truth rules.

### Project Context Reference

Project context says AGENTS files orient agents and should reference BMAD artifacts and consumer docs instead of duplicating durable architecture rules.

### References

- `_bmad-output/planning-artifacts/prds/prd-Bondstone-2026-06-18/prd.md` - FR2.4 and documentation routing requirements
- `_bmad-output/planning-artifacts/architecture.md` - Documentation Ownership
- `_bmad-output/planning-artifacts/epics.md` - Epic 3 and Story 3.2
- `_bmad-output/project-context.md` - lean implementation rules
- `AGENTS.md` - implementation target
- `docs/repository.md` - context-index convention

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- `test -f README.md && test -f _bmad-output/planning-artifacts/prds/prd-Bondstone-2026-06-18/prd.md && test -f _bmad-output/planning-artifacts/architecture.md && test -f _bmad-output/planning-artifacts/epics.md && test -f _bmad-output/project-context.md && test -f docs/README.md`
- `rg -n "docs/architecture|docs/adr|adr-required|bondstone-adr|non-native planning|decision-file review" AGENTS.md` (no matches)
- `pnpm exec prettier --check AGENTS.md _bmad-output/implementation-artifacts/3-2-rewrite-root-agents.md _bmad-output/implementation-artifacts/sprint-status.yaml`

### Completion Notes List

- Verified root AGENTS routes agents to README, BMAD PRD, architecture, epics, project context, and relevant docs before edits.
- Confirmed root AGENTS has no active retired decision workflow, deleted architecture path, or non-native planning routing references, so no AGENTS content change was required.

### File List

- `AGENTS.md` (verified; no content change required)
- `_bmad-output/implementation-artifacts/3-2-rewrite-root-agents.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`

## Change Log

- 2026-06-18: Verified root AGENTS routing and moved story to review.
