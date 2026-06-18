---
baseline_commit: 9bc4667caf7f22e214583384c9dac92fbdfe7d1b
---

# Story 4.3: Update GitHub Workflow Guidance

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a maintainer,
I want GitHub work tracking to fit BMAD-native decisions,
so that issues remain useful without retired decision labels driving architecture.

## Acceptance Criteria

1. `docs/github-workflow.md` uses BMAD review semantics.
2. Issue guidance points durable decisions to BMAD PRD/architecture/epics.
3. Backlog tracking remains in GitHub Issues/Projects.

## Tasks / Subtasks

- [x] Review GitHub workflow semantics for retired decision remnants. (AC: 1, 2)
  - [x] Sweep `docs/github-workflow.md` for deleted ADR paths, retired decision labels, decision-file review language, and non-native planning workflow references.
  - [x] Preserve or tighten `bmad-review-required` as the BMAD-native signal for changes that may require PRD, architecture, or epics updates.
  - [x] Ensure the label is not described as an implementation blocker for ordinary issues that do not affect durable requirements, architecture, sequencing, package boundaries, public API, or repository workflow.
- [x] Keep issue templates and working-issue guidance aligned with BMAD ownership. (AC: 1, 2)
  - [x] Confirm bug, feature/enhancement, cleanup, and trial formats point durable requirements, architecture, sequencing, compatibility, and package/public API policy to BMAD artifacts when needed.
  - [x] Confirm consumer-facing behavior changes still update `/docs`.
  - [x] Confirm follow-up findings belong in GitHub Issues or Projects, not long-lived transitional docs.
- [x] Preserve GitHub Issues/Projects as the backlog and ownership system. (AC: 3)
  - [x] Keep project statuses, label families, issue body conventions, working-issue steps, and completion-comment guidance intact unless they contradict BMAD-native semantics.
  - [x] Keep project movement and closure rules clear: work moves to `In Progress` when started and `Done` only after implementation/trial evidence, verification, and closure when appropriate.
  - [x] Do not move backlog tracking into BMAD artifacts; BMAD artifacts own durable product/architecture/sequence, while GitHub owns tracked work.
- [x] Verify workflow guidance and formatting. (AC: 1, 2, 3)
  - [x] Run a targeted stale-reference sweep over `docs/github-workflow.md`.
  - [x] Confirm the file still documents `bmad-review-required` or an intentional BMAD-native replacement.
  - [x] Run Prettier on touched markdown and this story file.

## Dev Notes

Story 4.3 is a documentation cleanup for GitHub work tracking guidance. It should not create, triage, label, close, or move actual GitHub issues or project items unless the user separately asks for GitHub workflow operations.

### Current State Intelligence

Current `docs/github-workflow.md` already describes BMAD-native ownership at the top:

- BMAD artifacts describe requirements, architecture, and implementation sequencing.
- GitHub Issues and GitHub Projects track backlog work, real-project findings, cleanup tasks, prioritization, and ownership.

Current label guidance uses `bmad-review-required` as the decision signal. It applies when an issue affects public API, package boundaries, target frameworks, provider or transport support, migration policy, compatibility, release/publishing, sample architecture, repository workflow, or agent harness behavior. The file says the label means BMAD PRD, architecture, or epics may need updates before implementation.

Initial stale-reference sweep over `docs/github-workflow.md` found no active `docs/architecture`, `docs/adr`, `adr-required`, `bondstone-adr`, decision-file review, retired decision, or non-native planning references. Implementation may therefore be a verification or small wording-tightening pass. Do not invent edits if the document already satisfies the acceptance criteria.

### Architecture Compliance

BMAD architecture owns internal runtime architecture, durable behavior, package-boundary rules, public API strategy, documentation ownership, and verification strategy. The PRD owns requirements and scope. Epics own implementation sequence and story acceptance criteria. GitHub Issues and Projects own backlog work, real-project findings, cleanup tasks, prioritization, and ownership.

For GitHub workflow guidance:

- Keep `bmad-review-required` semantics tied to BMAD artifact review/update needs.
- Do not restore old ADR labels, decision-file review requirements, or deleted architecture-doc paths.
- Keep issue bodies operational and actionable; do not ask issue authors to duplicate architecture books in issues.
- Keep completion comments focused on summary, verification, follow-up issues, and residual notes.
- Durable requirement, architecture, or sequencing changes should update the appropriate BMAD artifact; consumer-facing behavior changes should update `/docs`.

### File Structure Requirements

Primary implementation target:

- `docs/github-workflow.md`

Do not edit `tests/**`; Story 4.2 owns test scoped references. Do not edit `src/**`; Story 4.1 owns source package references. Do not create or modify actual GitHub Issues or Projects as part of this documentation story.

### Testing Requirements

Recommended verification:

- `rg -n "docs/architecture|docs/adr|adr-required|bondstone-adr|decision-file review|decision file|retired decision|non-native planning" docs/github-workflow.md`
- `rg -n "bmad-review-required|BMAD PRD|architecture|epics|GitHub Issues|GitHub Projects" docs/github-workflow.md`
- `pnpm exec prettier --check docs/github-workflow.md _bmad-output/implementation-artifacts/4-3-update-github-workflow-guidance.md _bmad-output/implementation-artifacts/sprint-status.yaml`

No runtime tests are expected for this documentation-only story. Use `pnpm check` only if broader repository changes happen unexpectedly.

### Previous Story Intelligence

Story 4.2 is the immediately preceding Epic 4 story and is scoped to test AGENTS/README reference hygiene. Its guidance carries forward the same pattern:

- Verification-only completion is acceptable when scoped docs already satisfy acceptance criteria.
- Keep durable architecture in BMAD artifacts and keep consumer/repository docs focused on usage or operations.
- Avoid runtime code, package metadata, public API, tests, and unrelated docs.

Story 4.1 completed the same pattern for source package references. It explicitly left `docs/github-workflow.md` to Story 4.3 and established that scoped reference cleanup should be narrow and rollback-aware.

### Git Intelligence

Recent commits:

- `9bc4667 docs: metadata fix`
- `bb2c102 docs: bmad native docs`
- `783b260 docs: more bmad documents refactoring`
- `13140c5 docs: bmad project context`
- `f9dc177 chore: bmad`

The recent direction is BMAD-native source-of-truth routing with small documentation verification passes. Preserve that approach here.

### Latest Technical Information

No web research is required. This story is governed by local BMAD artifacts and repository workflow docs. Do not update GitHub product guidance, labels, or project behavior based on external lookup as part of this story.

### Project Context Reference

Project context says GitHub Issues and GitHub Projects track backlog work, real-project findings, cleanup tasks, prioritization, and ownership. It also says BMAD PRD, architecture, epics, and project-context are the durable source-of-truth chain for requirements, runtime architecture, sequencing, and lean agent guardrails.

### Story Completion Status

Ultimate context engine analysis completed - comprehensive developer guide created.

### References

- `_bmad-output/planning-artifacts/prds/prd-Bondstone-2026-06-18/prd.md` - FR2.5 and Open Items
- `_bmad-output/planning-artifacts/architecture.md` - Documentation Ownership and Verification Strategy
- `_bmad-output/planning-artifacts/epics.md` - Epic 4 and Story 4.3
- `_bmad-output/project-context.md` - workflow and source-of-truth guardrails
- `docs/github-workflow.md` - target workflow guidance
- `docs/repository.md` - work tracking and context-index conventions
- `_bmad-output/implementation-artifacts/4-1-update-source-package-references.md` - source-reference cleanup pattern
- `_bmad-output/implementation-artifacts/4-2-update-test-references.md` - previous story context

## Dev Agent Record

### Agent Model Used

Codex (GPT-5)

### Debug Log References

- Resolved `bmad-dev-story` customization manually after `_bmad/scripts/resolve_customization.py` failed because Python could not import `json`.
- Loaded sprint status, project context, Story 4.3, Epic 4, BMAD architecture documentation ownership, and `docs/github-workflow.md`.
- Ran stale-reference sweep over `docs/github-workflow.md`; no deleted architecture, ADR, retired decision, decision-file review, or non-native planning references were found.
- Tightened `bmad-review-required` label semantics so ordinary issues outside durable/change-sensitive surfaces proceed without the label.
- Confirmed the workflow guidance still routes durable requirement, architecture, and sequencing changes to BMAD PRD, architecture, or epics.
- Confirmed GitHub Issues and GitHub Projects remain the backlog, prioritization, and ownership system.
- Ran Prettier check over `docs/github-workflow.md`, this story file, and sprint status.
- Ran `pnpm check`; formatting, restore, Release build, fast tests, pack, and package tests passed.
- Ran final `workflow.on_complete` resolver; it failed with the same Python `json` import error, and manual fallback found no configured completion hook.

### Completion Notes List

- GitHub workflow guidance now explicitly describes `bmad-review-required` as a BMAD review signal, not a blanket implementation blocker.
- Durable requirements, architecture, sequencing, public API, package-boundary, and repository-workflow changes remain routed to BMAD artifacts.
- Bug, feature/enhancement, cleanup, trial, working-issue, and completion-comment guidance still keep tracked work in GitHub Issues/Projects.
- Documentation-only validation passed; no GitHub issues/projects, runtime code, package metadata, or unrelated docs were changed.
- `pnpm check` passed after story bookkeeping updates.

### File List

- `_bmad-output/implementation-artifacts/4-3-update-github-workflow-guidance.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `docs/github-workflow.md`

### Change Log

- 2026-06-18: Tightened BMAD review label semantics in GitHub workflow guidance and completed Story 4.3 verification.
