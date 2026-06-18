---
baseline_commit: 783b26021a9d21a39ba74446287e3d256c161590
---

# Story 2.1: Guard Against Non-Native Planning Artifacts

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a maintainer,
I want retired non-native planning artifacts to stay absent,
so that agents treat the PRD as the requirements source.

## Acceptance Criteria

1. Active docs do not name non-native planning artifacts as current requirements.
2. New planning work starts from native BMAD PRD, architecture, epics, or the appropriate BMAD workflow.
3. Generated artifacts are clearly marked as generated if they are not source of truth.
4. Reference sweeps find no current routing to retired non-native planning artifacts.

## Tasks / Subtasks

- [x] Inventory current non-native planning references. (AC: 1, 2, 4)
  - [x] Search root docs, scoped docs, BMAD output, and repository-local skills for non-native planning workflow names and stale planning routes.
  - [x] Distinguish active routing from historical/generated text.
  - [x] Record any intentionally retained historical mention in completion notes.
- [x] Remove or reroute active non-native planning guidance. (AC: 1, 2)
  - [x] Point current requirements work to the BMAD PRD workspace.
  - [x] Point current architecture work to `_bmad-output/planning-artifacts/architecture.md`.
  - [x] Point implementation sequencing to `_bmad-output/planning-artifacts/epics.md`.
  - [x] Point lean agent implementation rules to `_bmad-output/project-context.md`.
- [x] Preserve generated-artifact boundaries. (AC: 3)
  - [x] Do not turn generated reports or implementation-story files into new source-of-truth planning artifacts.
  - [x] If a generated artifact is referenced, make the source-of-truth owner explicit.
- [x] Run documentation verification. (AC: 4)
  - [x] Run targeted `rg` sweeps for retired planning route names.
  - [x] Run formatting/reference checks that are proportionate for documentation-only edits.

## Dev Notes

Story 2.1 starts Epic 2, "Keep Retired Planning Workflows Removed." It covers PRD FR2.2 and supports NFR2: stale planning and decision references must not compete with the BMAD-native chain.

The current source-of-truth chain is PRD -> architecture -> epics -> project-context. Do not introduce a new planning directory, long-lived transition plan, or alternate source-of-truth document while fixing references.

### Current State Intelligence

- The PRD says the old non-native planning workflow, decision-file workflow, and duplicated architecture docs are retired.
- The architecture says BMAD planning artifacts own internal source-of-truth content, while `/docs` owns consumer-facing and repository-operation docs.
- The root README and root AGENTS already route agents to BMAD PRD, architecture, epics, and project-context.
- The readiness report from Story 1.4 validated that the BMAD PRD, architecture, and epics are discoverable and aligned.

### Architecture Compliance

This story should only change documentation or agent-routing artifacts. It must not change runtime code, package APIs, tests, samples, or planning artifact scope unless a concrete stale planning route requires a narrow correction.

Use the ownership split from architecture:

- PRD: product requirements, scope, goals, non-goals, success criteria.
- Architecture: internal runtime architecture and package-boundary rules.
- Epics: implementation sequencing and story acceptance criteria.
- Project context: lean agent-facing rules and verification entrypoints.
- `/docs`: consumer-facing and repository-operation docs.

### File Structure Requirements

Likely update candidates are root or scoped documentation files that still describe retired planning as current. Expected search scope:

- `README.md`
- `AGENTS.md`
- `docs/**/*.md`
- `.agents/skills/**/*.md`
- `_bmad-output/**/*.md`

Treat `_bmad-output/implementation-artifacts/*.md` and generated reports carefully: many are historical records of workflow output. Do not rewrite historical generated content unless it actively routes future work incorrectly.

### Testing Requirements

Recommended verification:

- `rg -n "non-native|planning workflow|retired planning|source of truth|source-of-truth" README.md AGENTS.md docs .agents/skills _bmad-output`
- `rg -n "docs/architecture|docs/adr|decision-file|ADR workflow|adr-required" README.md AGENTS.md docs .agents/skills`
- `pnpm exec prettier --check README.md AGENTS.md docs .agents/skills _bmad-output/implementation-artifacts/2-1-guard-against-non-native-planning-artifacts.md _bmad-output/implementation-artifacts/sprint-status.yaml`
- Run `pnpm check` if changes touch broad docs, package docs, or scripts.

No runtime tests are expected unless implementation unexpectedly changes source or test code.

### Previous Story Intelligence

Story 1.4 verified the readiness report and confirmed PRD, architecture, and epics are aligned. It also warned that generated file-size and timestamp snapshots are historical metadata, so do not "fix" generated snapshots unless they create active routing defects.

### Git Intelligence

Recent commits are documentation-reset work: `docs: more bmad documents refactoring`, `docs: bmad project context`, and `chore: bmad`. Keep this story narrow and avoid broad cleanup outside retired planning routes.

### Latest Technical Information

No web research is required. This story validates local repository documentation and BMAD artifact routing, not external library versions or APIs.

### Project Context Reference

Project context says the BMAD PRD, architecture, epics, and project-context are the internal source-of-truth chain. It also says README files orient humans, AGENTS files orient agents, and both should reference BMAD artifacts rather than duplicate durable architecture rules.

### References

- `_bmad-output/planning-artifacts/prds/prd-Bondstone-2026-06-18/prd.md` - FR2.2, NFR2, success metrics
- `_bmad-output/planning-artifacts/architecture.md` - Documentation Ownership
- `_bmad-output/planning-artifacts/epics.md` - Epic 2 and Story 2.1
- `_bmad-output/project-context.md` - source-of-truth routing and workflow rules
- `_bmad-output/implementation-artifacts/1-4-create-readiness-report.md` - previous-story readiness learnings

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- `rg -n "non-native|planning workflow|retired planning|source of truth|source-of-truth" README.md AGENTS.md docs .agents/skills _bmad-output`
- `rg -n "docs/architecture|docs/adr|decision-file|ADR workflow|adr-required" README.md AGENTS.md docs .agents/skills`
- `test ! -d docs/architecture`
- `test ! -d docs/adr`

### Completion Notes List

- Verified active root README, root AGENTS, docs index, repository docs, and skill AGENTS routing point current requirements, architecture, sequencing, and lean implementation guidance to the BMAD-native artifact chain.
- Retained generated and historical mentions inside BMAD planning/implementation artifacts because they describe the reset history or current story acceptance criteria rather than active alternate planning routes.
- Identified active retired ADR skill routing during the broad sweep; left it for Stories 2.3 and 2.4 because it is decision-workflow cleanup, not non-native planning-artifact routing.
- No runtime code, package APIs, tests, samples, or new planning artifacts were changed for this documentation guard story.

### File List

- `_bmad-output/implementation-artifacts/2-1-guard-against-non-native-planning-artifacts.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
