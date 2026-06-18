---
baseline_commit: 783b26021a9d21a39ba74446287e3d256c161590
---

# Story 3.4: Keep Internal Architecture Out Of Consumer Docs

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a maintainer,
I want internal architecture to stay in BMAD artifacts,
so that there is one source for internal architecture.

## Acceptance Criteria

1. Consumer docs link to BMAD architecture instead of duplicating internal design.
2. Package README, scoped AGENTS, and docs do not link to deleted paths.
3. New internal architecture sections are added to BMAD architecture.
4. Reference sweeps show consumer docs as usage/operation guidance rather than duplicated architecture books.

## Tasks / Subtasks

- [x] Sweep consumer docs for duplicated internal architecture. (AC: 1, 3, 4)
  - [x] Review `docs/*.md` for sections that should be usage, operation, verification, or repository guidance.
  - [x] Replace internal durable architecture duplication with concise references to BMAD architecture where the consumer needs context.
  - [x] Preserve practical usage guidance, setup examples, operations guidance, testing policy, public API classification, and package discovery.
- [x] Sweep indexes and scoped docs for deleted paths. (AC: 2)
  - [x] Search root docs, `/docs`, package READMEs, scoped AGENTS files, samples, and tests for `docs/architecture/**`, `docs/adr/**`, `bondstone-adr-*`, `adr-required`, and retired workflow routing.
  - [x] Fix any active deleted-path link discovered in the sweep.
  - [x] Do not rewrite broad source/test scoped guidance beyond stale deleted-path fixes; Epic 4 owns scoped package and test reference alignment.
- [x] Verify documentation ownership. (AC: 1, 3, 4)
  - [x] Confirm BMAD architecture remains the only internal architecture source of truth.
  - [x] Confirm `/docs` remains useful to consumers and repository maintainers.
  - [x] Run formatting checks for touched markdown.

### Review Findings

- [x] [Review][Patch] Align live GitHub labels with `bmad-review-required` guidance [`.agents/skills/bondstone-github-issue-workflow/SKILL.md`:33]
- [x] [Review][Patch] Make `sprint-status.yaml` `last_updated` metadata consistent [`_bmad-output/implementation-artifacts/sprint-status.yaml`:2]

## Dev Notes

Story 3.4 is the cross-doc guardrail for Epic 3. Its purpose is not to delete every architecture-related sentence from consumer docs. Consumer docs may explain how to use or operate a behavior. They should not become the durable internal architecture book.

### Current State Intelligence

Initial sweeps at baseline found:

- `docs/setup.md` links to BMAD architecture in durable-context sections.
- `docs/package-discovery.md` links to BMAD architecture for package-boundary details.
- `docs/public-api.md` uses the word "decision" in public API classification/history contexts; that is not automatically retired decision-file routing.
- `docs/architecture` and `docs/adr` directories are absent.
- Root README, root AGENTS, docs README, and docs AGENTS already route to BMAD artifacts.

Treat generic words like "decision" carefully. Remove active retired workflow routing, not every English use of the word.

### Architecture Compliance

BMAD architecture owns internal runtime architecture, package-boundary rules, durable behavior, and documentation ownership. Consumer docs own setup, package discovery, packaging, public API review, operations, observability, samples, testing, repository workflow, and GitHub issue guidance.

### File Structure Requirements

Likely update candidates if stale content is found:

- `docs/*.md`
- `README.md`
- `AGENTS.md`
- scoped `README.md` and `AGENTS.md` files under `src/`, `tests/`, and `samples/` only for deleted-path or retired-workflow links

Avoid converting Story 3.4 into the Epic 4 scoped reference rewrite. If a source or test scoped file has broader wording issues but no deleted-path link, leave that to Epic 4.

### Testing Requirements

Recommended verification:

- `test ! -d docs/architecture`
- `test ! -d docs/adr`
- `rg -n "docs/architecture|docs/adr|adr-required|bondstone-adr|decision-file review|Use ADR skills|ADRs preserve durable decisions" README.md AGENTS.md docs src tests samples .agents/skills`
- `rg -n "../_bmad-output/planning-artifacts/architecture.md|_bmad-output/planning-artifacts/architecture.md" docs README.md AGENTS.md`
- `pnpm exec prettier --check README.md AGENTS.md docs/*.md _bmad-output/implementation-artifacts/3-4-keep-internal-architecture-out-of-consumer-docs.md _bmad-output/implementation-artifacts/sprint-status.yaml`

No runtime tests are expected for this documentation-only story.

### Previous Story Intelligence

Stories 3.1 through 3.3 align root and docs indexes. Story 3.4 checks the broader documentation body so those indexes do not point into duplicated or stale architecture material.

Epic 2 already removed retired decision-file and skill routing. Keep that cleanup intact and avoid restoring deleted workflow paths.

### Git Intelligence

Recent documentation commits already moved internal content into BMAD artifacts and updated project context. This story should verify that direction across consumer docs and make only focused fixes.

### Latest Technical Information

No web research is required. This story uses local documentation ownership and source-of-truth rules.

### Project Context Reference

Project context says README files orient humans, AGENTS files orient agents, and both should reference BMAD artifacts and consumer docs instead of duplicating durable architecture rules.

### References

- `_bmad-output/planning-artifacts/prds/prd-Bondstone-2026-06-18/prd.md` - FR2.3 and FR2.6
- `_bmad-output/planning-artifacts/architecture.md` - Documentation Ownership
- `_bmad-output/planning-artifacts/epics.md` - Epic 3 and Story 3.4
- `_bmad-output/project-context.md` - workflow and source-of-truth rules
- `docs/README.md` - docs ownership map
- `docs/setup.md` - current consumer setup guidance with BMAD architecture links
- `docs/package-discovery.md` - current package discovery guidance with BMAD architecture links

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- `test ! -d docs/architecture && test ! -d docs/adr`
- `rg -n "docs/architecture|docs/adr|adr-required|bondstone-adr|decision-file review|Use ADR skills|ADRs preserve durable decisions" README.md AGENTS.md docs src tests samples` (no matches)
- `rg -n "docs/architecture|docs/adr|adr-required|bondstone-adr|decision-file review|Use ADR skills|ADRs preserve durable decisions" README.md AGENTS.md docs src tests samples .agents/skills` (only generic skill-template references to documentation discovery/output locations; no active Bondstone deleted-path routing)
- `rg -n "../_bmad-output/planning-artifacts/architecture.md|_bmad-output/planning-artifacts/architecture.md" docs README.md AGENTS.md`
- `rg -n "Architecture|architecture|durable behavior|internal runtime|source of truth|BMAD architecture" docs/*.md`
- `pnpm exec prettier --check README.md AGENTS.md docs/*.md _bmad-output/implementation-artifacts/3-4-keep-internal-architecture-out-of-consumer-docs.md _bmad-output/implementation-artifacts/sprint-status.yaml`

### Completion Notes List

- Verified `docs/architecture` and `docs/adr` remain absent.
- Confirmed root docs, `/docs`, `src`, `tests`, and `samples` do not actively route to deleted architecture/ADR paths or retired decision workflows.
- Reviewed consumer-doc architecture references and confirmed they point to BMAD architecture for internal runtime behavior while preserving usage and operations guidance.

### File List

- `README.md` (verified; no content change required)
- `AGENTS.md` (verified; no content change required)
- `docs/*.md` (verified; no content change required)
- `_bmad-output/implementation-artifacts/3-4-keep-internal-architecture-out-of-consumer-docs.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`

## Change Log

- 2026-06-18: Verified consumer documentation ownership and moved story to review.
