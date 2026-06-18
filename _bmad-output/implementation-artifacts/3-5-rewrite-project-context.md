---
baseline_commit: 783b26021a9d21a39ba74446287e3d256c161590
---

# Story 3.5: Rewrite Project Context

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As an implementation agent,
I want `_bmad-output/project-context.md` to be lean and current,
so that it guides work without duplicating architecture.

## Acceptance Criteria

1. Project context removes retired-source contradictions.
2. Project context links to BMAD PRD, architecture, and epics.
3. Project context keeps critical technology, code, testing, and workflow rules.
4. Project context links resolve and remain concise enough for agent loading.

## Tasks / Subtasks

- [x] Review source-of-truth routing. (AC: 1, 2)
  - [x] Keep front matter and body links to BMAD PRD, architecture, and epics.
  - [x] Keep `docs/README.md` as the consumer/repository docs entrypoint.
  - [x] Remove any retired-source contradiction involving non-native planning, deleted architecture docs, or retired decision files.
- [x] Keep project context lean and implementation-focused. (AC: 3, 4)
  - [x] Preserve critical technology stack facts that agents need immediately.
  - [x] Preserve high-signal runtime guardrails without copying full BMAD architecture sections.
  - [x] Preserve code rules, testing rules, workflow commands, and "Do Not Miss" guardrails.
  - [x] Replace long internal architecture explanations with references to BMAD architecture.
- [x] Verify links, concision, and formatting. (AC: 4)
  - [x] Confirm linked BMAD artifacts and docs entrypoint exist.
  - [x] Confirm the file remains concise enough for routine agent loading.
  - [x] Run formatting checks for touched markdown.

### Review Findings

- [x] [Review][Patch] Align live GitHub labels with `bmad-review-required` guidance [`.agents/skills/bondstone-github-issue-workflow/SKILL.md`:33]
- [x] [Review][Patch] Make `sprint-status.yaml` `last_updated` metadata consistent [`_bmad-output/implementation-artifacts/sprint-status.yaml`:2]

## Dev Notes

Story 3.5 closes Epic 3 by keeping project-context as the lean agent-facing implementation rule set. It should be more direct than architecture and more operational than the PRD, but it must not become a duplicate architecture document.

### Current State Intelligence

At baseline `783b26021a9d21a39ba74446287e3d256c161590`, `_bmad-output/project-context.md` already:

- links to BMAD PRD, architecture, and epics in front matter and the Source Of Truth section;
- identifies `docs/README.md` as the consumer/repository docs entrypoint;
- lists the current .NET, pnpm, EF Core, Npgsql, transport adapter, and testing technology facts;
- keeps critical runtime, code, testing, workflow, and "Do Not Miss" guardrails;
- is about 130 lines, which is appropriately lean for an agent context file.

This story may be a verification and tightening pass if the current file still satisfies the acceptance criteria.

### Architecture Compliance

Project context is not the architecture source of truth. It can repeat critical guardrails that prevent common implementation mistakes, but durable runtime design belongs in BMAD architecture. Product requirements belong in PRD. Implementation sequence and acceptance criteria belong in epics.

### File Structure Requirements

Primary file:

- `_bmad-output/project-context.md`

Only edit planning artifacts if project-context uncovers a contradiction that truly belongs in PRD, architecture, or epics. Do not broaden this story into a runtime architecture rewrite.

### Testing Requirements

Recommended verification:

- `test -f _bmad-output/planning-artifacts/prds/prd-Bondstone-2026-06-18/prd.md`
- `test -f _bmad-output/planning-artifacts/architecture.md`
- `test -f _bmad-output/planning-artifacts/epics.md`
- `test -f docs/README.md`
- `rg -n "docs/architecture|docs/adr|adr-required|bondstone-adr|non-native planning|decision-file review" _bmad-output/project-context.md`
- `wc -l _bmad-output/project-context.md`
- `pnpm exec prettier --check _bmad-output/project-context.md _bmad-output/implementation-artifacts/3-5-rewrite-project-context.md _bmad-output/implementation-artifacts/sprint-status.yaml`

No runtime tests are expected for this documentation-only story.

### Previous Story Intelligence

Stories 3.1 through 3.4 align root README, root AGENTS, docs indexes, and consumer-doc architecture ownership. Project context should match those routes while remaining the shortest agent-facing source.

Epic 2 removed retired decision-file and skill routing. Do not reintroduce retired workflow paths or labels into project-context.

### Git Intelligence

Recent commit `13140c5 docs: bmad project context` specifically created or refined project-context. Use that as a signal to keep edits narrow unless a contradiction is found.

### Latest Technical Information

No web research is required. This story uses local repository technology facts and source-of-truth rules.

### Project Context Reference

This story edits project-context itself. Preserve its role as a lean implementation guide and keep it synchronized with BMAD artifact ownership.

### References

- `_bmad-output/planning-artifacts/prds/prd-Bondstone-2026-06-18/prd.md` - FR1.1 source-of-truth chain
- `_bmad-output/planning-artifacts/architecture.md` - Documentation Ownership and runtime guardrails
- `_bmad-output/planning-artifacts/epics.md` - Epic 3 and Story 3.5
- `_bmad-output/project-context.md` - implementation target
- `docs/README.md` - consumer/repository docs entrypoint

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- `test -f _bmad-output/planning-artifacts/prds/prd-Bondstone-2026-06-18/prd.md && test -f _bmad-output/planning-artifacts/architecture.md && test -f _bmad-output/planning-artifacts/epics.md && test -f docs/README.md`
- `rg -n "docs/architecture|docs/adr|adr-required|bondstone-adr|non-native planning|decision-file review" _bmad-output/project-context.md` (no matches)
- `wc -l _bmad-output/project-context.md` (146 lines)
- `pnpm exec prettier --check _bmad-output/project-context.md _bmad-output/implementation-artifacts/3-5-rewrite-project-context.md _bmad-output/implementation-artifacts/sprint-status.yaml`

### Completion Notes List

- Verified project context keeps BMAD PRD, architecture, epics, and docs index routing with no retired-source contradictions.
- Confirmed the file remains lean enough for routine agent loading while preserving critical technology, runtime, code, testing, workflow, and "Do Not Miss" guardrails.

### File List

- `_bmad-output/project-context.md` (verified; no content change required)
- `_bmad-output/implementation-artifacts/3-5-rewrite-project-context.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`

## Change Log

- 2026-06-18: Verified project context routing and moved story to review.
