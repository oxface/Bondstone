---
baseline_commit: 783b26021a9d21a39ba74446287e3d256c161590
---

# Story 3.1: Rewrite Root README

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a repository visitor,
I want README to explain package purpose and BMAD-native source routing,
so that I can find setup, verification, and planning artifacts quickly.

## Acceptance Criteria

1. README links to PRD, architecture, epics, and project-context.
2. README links to consumer docs that remain under `/docs`.
3. README does not link to retired decision or deleted architecture docs.
4. README links resolve to existing files.

## Tasks / Subtasks

- [x] Review the root README for current source routing. (AC: 1, 2)
  - [x] Keep the package-purpose introduction concise and library-focused.
  - [x] Keep BMAD artifact links to PRD, architecture, epics, and project-context.
  - [x] Keep consumer-facing docs discoverable through `docs/README.md` and key docs links.
- [x] Remove stale or competing source-of-truth language. (AC: 2, 3)
  - [x] Remove any active link to `docs/architecture/**`, `docs/adr/**`, retired decision workflows, or non-native planning artifacts.
  - [x] Do not duplicate internal durable architecture text in README; link to BMAD architecture when needed.
  - [x] Preserve useful repository guidance such as verification, package map, publishing, and current direction.
- [x] Verify links and formatting. (AC: 4)
  - [x] Confirm every README link resolves to an existing file or folder.
  - [x] Run targeted reference sweeps for deleted architecture and retired decision paths.
  - [x] Run formatting checks for touched markdown.

### Review Findings

- [x] [Review][Patch] Align live GitHub labels with `bmad-review-required` guidance [`.agents/skills/bondstone-github-issue-workflow/SKILL.md`:33]
- [x] [Review][Patch] Make `sprint-status.yaml` `last_updated` metadata consistent [`_bmad-output/implementation-artifacts/sprint-status.yaml`:2]

## Dev Notes

Story 3.1 starts Epic 3 by making the root README the human-facing repository entrypoint. It should orient visitors to Bondstone's package purpose and then route durable requirements, architecture, sequencing, and lean agent rules to BMAD artifacts.

### Current State Intelligence

At baseline `783b26021a9d21a39ba74446287e3d256c161590`, `README.md` already:

- describes Bondstone as a .NET library for durable module boundaries, durable command sending, EF Core backed inbox/outbox persistence, operation observation, and transport adapters;
- links to the BMAD PRD, architecture, epics, and project-context;
- links to current docs such as setup, package discovery, packaging, operations, observability, public API, repository, samples, and testing;
- describes `pnpm check` and `pnpm verify`;
- does not show active `docs/architecture/**` or `docs/adr/**` links in the initial sweep.

This story may be a verification and tightening pass if the file still satisfies the acceptance criteria when implemented. Do not churn wording just to make a diff.

### Architecture Compliance

BMAD architecture owns internal runtime architecture and documentation ownership. `/docs` owns consumer-facing and repository-operation docs. README is an index and quick orientation page, not a second architecture document.

### File Structure Requirements

Primary file:

- `README.md`

Do not update scoped package or test indexes for this story unless a README link points to a deleted path discovered while verifying this file. Broader scoped reference cleanup is covered by later Epic 3 and Epic 4 stories.

### Testing Requirements

Recommended verification:

- `test -f _bmad-output/planning-artifacts/prds/prd-Bondstone-2026-06-18/prd.md`
- `test -f _bmad-output/planning-artifacts/architecture.md`
- `test -f _bmad-output/planning-artifacts/epics.md`
- `test -f _bmad-output/project-context.md`
- `rg -n "docs/architecture|docs/adr|adr-required|bondstone-adr|non-native planning|decision-file review" README.md`
- `pnpm exec prettier --check README.md _bmad-output/implementation-artifacts/3-1-rewrite-root-readme.md _bmad-output/implementation-artifacts/sprint-status.yaml`

No runtime tests are expected for this documentation-only story.

### Previous Story Intelligence

Epic 2 removed retired decision-file and retired decision-skill routing. Do not restore `docs/adr/**`, `bondstone-adr-*`, or ADR label requirements to make README links work.

### Git Intelligence

Recent commits are documentation reset commits:

- `783b260 docs: more bmad documents refactoring`
- `13140c5 docs: bmad project context`
- `f9dc177 chore: bmad`

Continue the same direction: concise indexes that point to current source-of-truth files.

### Latest Technical Information

No web research is required. This story uses local documentation routing and repository source-of-truth rules.

### Project Context Reference

Project context says README files orient humans and should reference BMAD artifacts and consumer docs instead of duplicating durable architecture rules.

### References

- `_bmad-output/planning-artifacts/prds/prd-Bondstone-2026-06-18/prd.md` - FR2.5 and documentation deduplication scope
- `_bmad-output/planning-artifacts/architecture.md` - Documentation Ownership and Verification Strategy
- `_bmad-output/planning-artifacts/epics.md` - Epic 3 and Story 3.1
- `_bmad-output/project-context.md` - workflow and source-of-truth rules
- `README.md` - implementation target
- `docs/README.md` - consumer docs index to link from root README

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- `test -f _bmad-output/planning-artifacts/prds/prd-Bondstone-2026-06-18/prd.md && test -f _bmad-output/planning-artifacts/architecture.md && test -f _bmad-output/planning-artifacts/epics.md && test -f _bmad-output/project-context.md && test -f docs/README.md`
- `rg -n "docs/architecture|docs/adr|adr-required|bondstone-adr|non-native planning|decision-file review" README.md` (no matches)
- `pnpm exec prettier --check README.md _bmad-output/implementation-artifacts/3-1-rewrite-root-readme.md _bmad-output/implementation-artifacts/sprint-status.yaml`

### Completion Notes List

- Verified the root README already routes readers to the BMAD PRD, architecture, epics, project context, and current consumer docs.
- Confirmed README has no active links to retired architecture or decision workflow paths, so no README content change was required.

### File List

- `README.md` (verified; no content change required)
- `_bmad-output/implementation-artifacts/3-1-rewrite-root-readme.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`

## Change Log

- 2026-06-18: Verified README source routing and moved story to review.
