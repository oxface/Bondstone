---
baseline_commit: 783b26021a9d21a39ba74446287e3d256c161590
---

# Story 3.3: Rewrite Docs Indexes

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a docs maintainer,
I want `docs/README.md` and `docs/AGENTS.md` to describe consumer-doc ownership,
so that `/docs` no longer competes with BMAD architecture.

## Acceptance Criteria

1. `docs/README.md` lists remaining consumer/repository docs.
2. `docs/AGENTS.md` points architecture work to BMAD architecture.
3. Removed docs are not linked.
4. Docs index links resolve to existing files.

## Tasks / Subtasks

- [x] Review `docs/README.md` as the consumer docs index. (AC: 1, 3, 4)
  - [x] List only existing docs under `docs/`.
  - [x] Keep BMAD artifact links for PRD, architecture, epics, and project-context.
  - [x] Keep document ownership focused on setup, package discovery, packaging, operations, observability, public API, repository, samples, testing, and GitHub workflow guidance.
- [x] Review `docs/AGENTS.md` as the docs-area agent index. (AC: 2, 3, 4)
  - [x] Point internal runtime architecture and durable behavior work to BMAD architecture.
  - [x] Point GitHub workflow changes to `docs/github-workflow.md` and repository layout/tooling changes to `docs/repository.md`.
  - [x] Keep `/docs` focused on library-user and repository-operation guidance.
- [x] Verify docs index links and stale references. (AC: 3, 4)
  - [x] Confirm every indexed docs file exists.
  - [x] Confirm removed paths such as `docs/architecture/**` and `docs/adr/**` are not linked.
  - [x] Run formatting checks for touched markdown.

### Review Findings

- [x] [Review][Patch] Align live GitHub labels with `bmad-review-required` guidance [`.agents/skills/bondstone-github-issue-workflow/SKILL.md`:33]
- [x] [Review][Patch] Make `sprint-status.yaml` `last_updated` metadata consistent [`_bmad-output/implementation-artifacts/sprint-status.yaml`:2]

## Dev Notes

Story 3.3 makes `/docs` a consumer and repository-operations documentation area. It should help readers find current docs without treating `/docs` as the internal architecture source.

### Current State Intelligence

At baseline `783b26021a9d21a39ba74446287e3d256c161590`, `docs/README.md` already lists the current docs files:

- `github-workflow.md`
- `observability.md`
- `operations.md`
- `packaging.md`
- `package-discovery.md`
- `public-api.md`
- `repository.md`
- `samples.md`
- `setup.md`
- `testing.md`

At the same baseline, `docs/AGENTS.md` already points internal runtime architecture and durable behavior work to BMAD architecture.

The `docs/architecture` and `docs/adr` directories are absent. Do not recreate them to satisfy stale links.

### Architecture Compliance

BMAD architecture explicitly says `/docs` owns setup and package discovery, packaging and public API review, operations and observability, samples and testing, repository workflow, and GitHub issue guidance. `/docs` should reference BMAD architecture for internal design details instead of duplicating durable rules.

### File Structure Requirements

Primary files:

- `docs/README.md`
- `docs/AGENTS.md`

Only edit other docs if they are necessary to fix a link introduced or exposed by the index changes. Story 3.4 handles broader consumer-doc architecture deduplication.

### Testing Requirements

Recommended verification:

- `find docs -maxdepth 1 -type f -name '*.md' -print | sort`
- `test -f docs/github-workflow.md`
- `test -f docs/observability.md`
- `test -f docs/operations.md`
- `test -f docs/packaging.md`
- `test -f docs/package-discovery.md`
- `test -f docs/public-api.md`
- `test -f docs/repository.md`
- `test -f docs/samples.md`
- `test -f docs/setup.md`
- `test -f docs/testing.md`
- `rg -n "docs/architecture|docs/adr|adr-required|bondstone-adr|decision-file review" docs/README.md docs/AGENTS.md`
- `pnpm exec prettier --check docs/README.md docs/AGENTS.md _bmad-output/implementation-artifacts/3-3-rewrite-docs-indexes.md _bmad-output/implementation-artifacts/sprint-status.yaml`

No runtime tests are expected for this documentation-only story.

### Previous Story Intelligence

Stories 3.1 and 3.2 handle root README and root AGENTS routing. Keep docs indexes consistent with those root entrypoints, but make the docs-area ownership map more specific.

Epic 2 removed retired decision-file routing. Do not reintroduce deleted decision or architecture docs as index entries.

### Git Intelligence

Recent documentation commits moved internal source-of-truth content into BMAD artifacts. Continue using `/docs` as an index and consumer guidance area.

### Latest Technical Information

No web research is required. This story uses local documentation routing and source-of-truth rules.

### Project Context Reference

Project context says consumer/repository docs live under `docs/README.md`, while BMAD architecture owns internal durable behavior.

### References

- `_bmad-output/planning-artifacts/prds/prd-Bondstone-2026-06-18/prd.md` - FR2.5 and FR2.6
- `_bmad-output/planning-artifacts/architecture.md` - Documentation Ownership
- `_bmad-output/planning-artifacts/epics.md` - Epic 3 and Story 3.3
- `_bmad-output/project-context.md` - source-of-truth and workflow rules
- `docs/README.md` - implementation target
- `docs/AGENTS.md` - implementation target
- `docs/repository.md` - context-index convention

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- `find docs -maxdepth 1 -type f -name '*.md' -print | sort`
- `test -f docs/github-workflow.md && test -f docs/observability.md && test -f docs/operations.md && test -f docs/packaging.md && test -f docs/package-discovery.md && test -f docs/public-api.md && test -f docs/repository.md && test -f docs/samples.md && test -f docs/setup.md && test -f docs/testing.md`
- `rg -n "docs/architecture|docs/adr|adr-required|bondstone-adr|decision-file review" docs/README.md docs/AGENTS.md` (no matches)
- `pnpm exec prettier --check docs/README.md docs/AGENTS.md _bmad-output/implementation-artifacts/3-3-rewrite-docs-indexes.md _bmad-output/implementation-artifacts/sprint-status.yaml`

### Completion Notes List

- Verified `docs/README.md` lists only existing consumer/repository documentation and keeps BMAD source-of-truth links.
- Verified `docs/AGENTS.md` routes internal runtime architecture and durable behavior changes to BMAD architecture while keeping `/docs` focused on usage and repository operation guidance.

### File List

- `docs/README.md` (verified; no content change required)
- `docs/AGENTS.md` (verified; no content change required)
- `_bmad-output/implementation-artifacts/3-3-rewrite-docs-indexes.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`

## Change Log

- 2026-06-18: Verified docs indexes and moved story to review.
