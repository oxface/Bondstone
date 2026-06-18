---
baseline_commit: 783b26021a9d21a39ba74446287e3d256c161590
---

# Story 2.4: Guard Against Retired Decision Skills

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As an agent maintainer,
I want retired decision workflow skills to stay absent,
so that agents cannot accidentally start the old workflow.

## Acceptance Criteria

1. `.agents/skills/AGENTS.md` describes BMAD artifact ownership.
2. No repository-local skill description routes durable decisions through the retired workflow.
3. Skill index and local skill directories contain no active ADR workflow routing.

## Tasks / Subtasks

- [x] Inventory repository-local skill routing. (AC: 1, 2, 3)
  - [x] Review `.agents/skills/AGENTS.md`, `.agents/skills/README.md`, and repository-local skill `SKILL.md` files.
  - [x] Search for `bondstone-adr-*`, `docs/adr`, `ADR workflow`, `adr-required`, and durable-decision routing.
  - [x] Confirm no repository-local retired decision skill directories exist.
- [x] Update skill indexes and affected skill descriptions. (AC: 1, 2, 3)
  - [x] Keep `.agents/skills/AGENTS.md` grounded in BMAD PRD, architecture, epics, and project-context ownership.
  - [x] Remove active references to retired `bondstone-adr-*` skills.
  - [x] Replace durable-decision routing in local skill bodies with BMAD artifact routing.
- [x] Avoid over-cleaning generic upstream skill knowledge. (AC: 2, 3)
  - [x] Do not rewrite unrelated BMad or WDS skill internals that mention ADR as generic test architecture or research terminology.
  - [x] Focus on Bondstone repository-local skills and active local workflow routing.
- [x] Run verification. (AC: 3)
  - [x] Prove local skill indexes and local skill descriptions no longer route to retired ADR workflows.
  - [x] Run formatting checks for touched skill docs.

### Review Findings

- [x] [Review][Patch] Include project-context in durable guardrail routing [.agents/skills/bondstone-github-issue-workflow/SKILL.md:44]

## Dev Notes

Story 2.4 completes Epic 2 by guarding the agent-skill layer. Story 2.3 removes retired decision-file routing from docs and workflow guidance; this story ensures repository-local skills cannot reintroduce the retired workflow.

### Current State Intelligence

Current evidence:

- `.agents/skills/AGENTS.md` already says durable technical decisions belong in the BMAD PRD, architecture, epics, or project-context according to scope.
- `.agents/skills/README.md` still links to `../../docs/adr/README.md` and lists retired `bondstone-adr-*` skills.
- `.agents/skills/bondstone-github-issue-workflow/SKILL.md` still routes durable decisions through ADRs and ADR skills.
- No `bondstone-adr-*` skill directories were found at max depth 2.
- The only local directory-like ADR/decision match found was `.agents/skills/bmad-create-architecture/architecture-decision-template.md`; that is part of a generic BMAD architecture skill, not a Bondstone-retired ADR workflow by itself.

### Architecture Compliance

Repository-local skills are reusable workflows, not durable architecture or planning sources. Skills may summarize rules, but should link to BMAD artifacts instead of copying long architecture text. Durable technical decisions belong in BMAD artifacts according to scope.

Do not make skill files the place where durable Bondstone architecture rules live. They should route agents to the owning artifacts.

### File Structure Requirements

Likely update candidates:

- `.agents/skills/README.md`
- `.agents/skills/bondstone-github-issue-workflow/SKILL.md`
- Possibly `.agents/skills/AGENTS.md` only if verification shows ownership language is incomplete

Avoid editing generated skill resources or broad third-party/generic skill knowledge unless they actively describe Bondstone's current local workflow.

### Testing Requirements

Recommended verification:

- `find .agents/skills -maxdepth 2 -type d \\( -iname '*adr*' -o -iname '*decision*' \\) -print`
- `rg -n "bondstone-adr|docs/adr|ADR workflow|adr-required|Use ADR skills|ADRs preserve durable decisions" .agents/skills/README.md .agents/skills/AGENTS.md .agents/skills/*/SKILL.md`
- `rg -n "PRD|architecture|epics|project-context|BMAD" .agents/skills/README.md .agents/skills/AGENTS.md .agents/skills/bondstone-github-issue-workflow/SKILL.md`
- `pnpm exec prettier --check .agents/skills/README.md .agents/skills/AGENTS.md .agents/skills/bondstone-github-issue-workflow/SKILL.md _bmad-output/implementation-artifacts/2-4-guard-against-retired-decision-skills.md _bmad-output/implementation-artifacts/sprint-status.yaml`
- Run `pnpm check` if edits extend beyond local skill docs.

No runtime tests are expected.

### Previous Story Intelligence

Story 2.3 should remove retired decision-file routing from current docs and workflow guidance. If Story 2.3 already updates `bondstone-github-issue-workflow`, Story 2.4 should verify and tighten the skill index rather than duplicate the same edits.

### Git Intelligence

Recent commits show a documentation reset and project-context update. Keep skill changes narrow and aligned with `.agents/skills/AGENTS.md`.

### Latest Technical Information

No web research is required. This story is local skill-index and skill-description hygiene.

### Project Context Reference

Project context says README files orient humans, AGENTS files orient agents, and both should reference BMAD artifacts and consumer docs instead of duplicating durable architecture rules. This applies directly to `.agents/skills/README.md` and `.agents/skills/AGENTS.md`.

### References

- `_bmad-output/planning-artifacts/prds/prd-Bondstone-2026-06-18/prd.md` - FR2.1 and retired decision workflow scope
- `_bmad-output/planning-artifacts/architecture.md` - Documentation Ownership
- `_bmad-output/planning-artifacts/epics.md` - Epic 2 and Story 2.4
- `_bmad-output/project-context.md` - workflow and source-of-truth rules
- `.agents/skills/AGENTS.md` - current local skill ownership rules
- `.agents/skills/README.md` - known stale retired skill index
- `.agents/skills/bondstone-github-issue-workflow/SKILL.md` - known stale ADR skill routing

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- `find .agents/skills -maxdepth 2 -type d \\( -iname '*adr*' -o -iname '*decision*' \\) -print`
- `rg -n "bondstone-adr|docs/adr|ADR workflow|adr-required|Use ADR skills|ADRs preserve durable decisions" .agents/skills/README.md .agents/skills/AGENTS.md .agents/skills/*/SKILL.md`
- `rg -n "PRD|architecture|epics|project-context|BMAD" .agents/skills/README.md .agents/skills/AGENTS.md .agents/skills/bondstone-github-issue-workflow/SKILL.md`

### Completion Notes List

- Verified `.agents/skills/AGENTS.md` already describes BMAD artifact ownership for durable technical decisions and skill authoring.
- Confirmed no repository-local retired `bondstone-adr-*` skill directories exist at the local skill depth checked by the story.
- Confirmed local skill index and local Bondstone issue workflow descriptions no longer route durable decisions through retired ADR workflow text.
- Avoided editing generic upstream BMad/WDS skill internals because the focused local skill sweep is clean.

### File List

- `.agents/skills/README.md`
- `.agents/skills/bondstone-github-issue-workflow/SKILL.md`
- `_bmad-output/implementation-artifacts/2-4-guard-against-retired-decision-skills.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
