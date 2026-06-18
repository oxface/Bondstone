---
baseline_commit: 783b26021a9d21a39ba74446287e3d256c161590
---

# Story 2.3: Guard Against Retired Decision Files

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a maintainer,
I want retired decision files to stay absent,
so that durable decisions route through BMAD artifacts.

## Acceptance Criteria

1. Root and scoped docs do not link to retired decision files.
2. No active instruction says broad changes require retired decision-file review.
3. Durable requirements, architecture, and sequence changes update BMAD artifacts.
4. Reference sweeps find no active links to `docs/adr/**` as current workflow.

## Tasks / Subtasks

- [x] Inventory retired decision-file references. (AC: 1, 2, 4)
  - [x] Search root docs, docs, scoped AGENTS/README files, and repository-local skill docs for `docs/adr`, ADR workflow routing, decision-file review, and `adr-required`.
  - [x] Distinguish historical/example mentions from active Bondstone workflow instructions.
  - [x] Confirm whether `docs/adr` exists before choosing replacement wording.
- [x] Replace active decision-file routing. (AC: 1, 2, 3, 4)
  - [x] Route requirements changes to the PRD.
  - [x] Route runtime architecture, package boundary, public API strategy, persistence, transport, hosting, and verification strategy changes to BMAD architecture.
  - [x] Route sequencing and story acceptance changes to epics.
  - [x] Route lean implementation guardrails to project-context.
- [x] Preserve useful GitHub issue guidance without retired decision labels. (AC: 2, 3)
  - [x] Do not require `adr-required` or retired decision-file review as a current gate.
  - [x] Use BMAD review semantics or existing GitHub labels only where current docs support them.
- [x] Run verification. (AC: 4)
  - [x] Prove no active current workflow link points to `docs/adr/**`.
  - [x] Run formatting checks for touched files.

## Dev Notes

Story 2.3 covers PRD FR2.1: retired decision files and decision workflow skills must be removed. It also supports FR2.4 and FR2.5 by keeping root/scoped agent and docs routing aligned with BMAD artifacts.

### Current State Intelligence

The `docs/adr` directory is currently absent. Targeted sweeps found active stale ADR routing in:

- `.agents/skills/README.md`: links to `../../docs/adr/README.md` and lists `bondstone-adr-create`, `bondstone-adr-update`, `bondstone-adr-supersede`, and `bondstone-adr-archive`.
- `.agents/skills/bondstone-github-issue-workflow/SKILL.md`: says ADRs preserve durable decisions, links to `docs/adr/README.md`, uses `adr-required`, says to read ADRs before changes, and says to use ADR skills for durable technical decisions.

These are implementation targets for this story. Do not restore `docs/adr` to satisfy these links. The PRD says the decision-file workflow is retired.

### Architecture Compliance

Durable decisions now route through BMAD artifacts according to scope:

- Product requirements, scope, non-goals, and success criteria: PRD.
- Runtime architecture, package boundaries, persistence, hosting, transport, public API strategy, docs ownership, and verification strategy: architecture.
- Implementation sequence and acceptance criteria: epics.
- Lean agent guardrails: project-context.

GitHub Issues and Projects remain backlog/work tracking. They do not replace BMAD source-of-truth artifacts for durable requirements or architecture.

### File Structure Requirements

Likely update candidates:

- `.agents/skills/README.md`
- `.agents/skills/bondstone-github-issue-workflow/SKILL.md`
- Any root/scoped README or AGENTS file found by the retired decision-file sweep
- Potentially `docs/github-workflow.md` if it contains active retired ADR-label semantics

Do not delete unrelated BMad or WDS skill references to ADR as a generic testing/research concept unless they actively route Bondstone decisions through retired `docs/adr/**` files. This story is about repository workflow correctness, not cleansing every generic acronym in vendored or reusable skill knowledge.

### Testing Requirements

Recommended verification:

- `test ! -d docs/adr`
- `find .agents/skills -maxdepth 2 -type d \\( -iname '*adr*' -o -iname '*decision*' \\) -print`
- `rg -n "docs/adr|adr-required|ADR skills|bondstone-adr|decision-file review|retired decision" README.md AGENTS.md docs .agents/skills`
- `rg -n "_bmad-output/planning-artifacts/prds|_bmad-output/planning-artifacts/architecture.md|_bmad-output/planning-artifacts/epics.md|_bmad-output/project-context.md" .agents/skills/README.md .agents/skills/bondstone-github-issue-workflow/SKILL.md`
- `pnpm exec prettier --check .agents/skills/README.md .agents/skills/bondstone-github-issue-workflow/SKILL.md _bmad-output/implementation-artifacts/2-3-guard-against-retired-decision-files.md _bmad-output/implementation-artifacts/sprint-status.yaml`
- Run `pnpm check` if edits are broad.

No runtime tests are expected.

### Previous Story Intelligence

Story 2.2 focuses on deleted duplicate architecture docs. If stale `docs/architecture` and `docs/adr` links occur near each other, keep the final wording consistent: internal durable rules belong in BMAD architecture, while consumer docs stay practical.

### Git Intelligence

Recent commits are documentation reset commits. Preserve that direction: delete or reroute stale decision-file instructions instead of creating transitional decision docs.

### Latest Technical Information

No web research is required. This story uses local repository routing and source-of-truth rules.

### Project Context Reference

Project context says GitHub Issues and Projects track backlog work, real-project findings, cleanup tasks, prioritization, and ownership. It does not preserve ADR review as the durable decision mechanism.

### References

- `_bmad-output/planning-artifacts/prds/prd-Bondstone-2026-06-18/prd.md` - FR2.1, FR2.4, FR2.5, NFR2
- `_bmad-output/planning-artifacts/architecture.md` - Documentation Ownership
- `_bmad-output/planning-artifacts/epics.md` - Epic 2 and Story 2.3
- `_bmad-output/project-context.md` - workflow and source-of-truth rules
- `.agents/skills/README.md` - known stale ADR routing
- `.agents/skills/bondstone-github-issue-workflow/SKILL.md` - known stale ADR routing
- `docs/github-workflow.md` - GitHub workflow guidance to verify for retired ADR-label semantics

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- `find .agents/skills -maxdepth 2 -type d \\( -iname '*adr*' -o -iname '*decision*' \\) -print`
- `rg -n "docs/adr|adr-required|ADR skills|bondstone-adr|decision-file review|retired decision" README.md AGENTS.md docs .agents/skills`
- `rg -n "_bmad-output/planning-artifacts/prds|_bmad-output/planning-artifacts/architecture.md|_bmad-output/planning-artifacts/epics.md|_bmad-output/project-context.md|bmad-review-required" .agents/skills/README.md .agents/skills/bondstone-github-issue-workflow/SKILL.md docs/github-workflow.md`
- `test ! -d docs/adr`

### Completion Notes List

- Removed active local skill index links to retired `docs/adr/**` and removed the retired `bondstone-adr-*` skill list from `.agents/skills/README.md`.
- Updated `bondstone-github-issue-workflow` to route durable requirements, architecture, and sequencing changes through BMAD PRD, architecture, and epics.
- Preserved current GitHub issue guidance by using the existing `bmad-review-required` label convention from `docs/github-workflow.md`.
- Verified `docs/adr` remains absent and the retired decision-file sweep returns no active matches.

### File List

- `.agents/skills/README.md`
- `.agents/skills/bondstone-github-issue-workflow/SKILL.md`
- `_bmad-output/implementation-artifacts/2-3-guard-against-retired-decision-files.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
