---
baseline_commit: 783b26021a9d21a39ba74446287e3d256c161590
---

# Story 2.2: Guard Against Duplicate Architecture Artifacts

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a maintainer,
I want duplicate internal architecture artifacts to stay absent,
so that implementation agents use one architecture source.

## Acceptance Criteria

1. Active docs point to `_bmad-output/planning-artifacts/architecture.md`.
2. New architecture material is added to the BMAD architecture artifact or a future BMAD-native architecture workflow output, not to consumer docs.
3. Reference sweeps find no current source-of-truth routing to deleted `docs/architecture/**` files.

## Tasks / Subtasks

- [x] Inventory duplicate architecture routing. (AC: 1, 3)
  - [x] Search root docs, consumer docs, scoped AGENTS files, package READMEs, and skills for `docs/architecture`, `architecture/*.md`, and deleted architecture paths.
  - [x] Separate user-facing "deeper behavior" links from source-of-truth instructions.
  - [x] Confirm whether each linked target exists before editing.
- [x] Replace active deleted-path links. (AC: 1, 3)
  - [x] Point internal behavior and durable architecture references to `_bmad-output/planning-artifacts/architecture.md`.
  - [x] Keep consumer setup and package-discovery docs concise; link to architecture instead of recreating deleted architecture books.
  - [x] Do not recreate `docs/architecture/**`.
- [x] Preserve consumer-doc ownership. (AC: 2)
  - [x] Leave practical setup, operations, observability, testing, packaging, and package-discovery guidance in `/docs`.
  - [x] Move or summarize only the routing language needed to prevent duplicate architecture authority.
- [x] Run documentation verification. (AC: 3)
  - [x] Run a targeted reference sweep proving no active current source-of-truth route points to deleted `docs/architecture/**`.
  - [x] Run formatting checks for touched markdown.

## Dev Notes

Story 2.2 covers PRD FR2.3: former internal architecture and planning docs must be fully migrated into BMAD artifacts when durable content is absorbed. This is a documentation-routing story, not a runtime architecture rewrite.

### Current State Intelligence

The `docs/architecture` directory is currently absent. Targeted sweeps found active stale links in:

- `docs/setup.md`: references `architecture/modules.md`, `architecture/persistence-ef-core.md`, `architecture/persistence-postgresql.md`, `architecture/transport-local.md`, `architecture/hosting.md`, and `architecture/persistence-core.md`.
- `docs/package-discovery.md`: references `architecture/transport-local.md`, `architecture/messaging.md`, `architecture/persistence-core.md`, `architecture/persistence-ef-core.md`, `architecture/persistence-postgresql.md`, and `architecture/hosting.md`.

Those links should not be treated as proof that the deleted docs need restoring. The PRD and architecture say internal durable architecture now belongs in `_bmad-output/planning-artifacts/architecture.md`.

### Architecture Compliance

Follow the architecture document's "Documentation Ownership" section:

- BMAD architecture owns internal runtime architecture and package-boundary rules.
- `/docs` owns consumer-facing and repository-operation docs.
- `/docs` should reference BMAD architecture for internal design details instead of duplicating durable rules.
- If a docs file is fully absorbed into BMAD, it should be deleted and references fixed.

Do not add a replacement `docs/architecture` folder, architecture index, or consumer-doc architecture book unless a future PRD and architecture update accepts that reversal.

### File Structure Requirements

Likely update candidates:

- `docs/setup.md`
- `docs/package-discovery.md`
- Any scoped README or AGENTS file found by the reference sweep

Avoid touching runtime packages, tests, samples, or BMAD architecture content unless the stale reference exposes a real missing rule that belongs in architecture.

### Testing Requirements

Recommended verification:

- `test ! -d docs/architecture`
- `rg -n "docs/architecture|architecture/[A-Za-z0-9_-]+\\.md|\\]\\(architecture/" README.md AGENTS.md docs src tests samples .agents/skills`
- `rg -n "_bmad-output/planning-artifacts/architecture.md|BMAD architecture" docs/setup.md docs/package-discovery.md`
- `pnpm exec prettier --check docs/setup.md docs/package-discovery.md _bmad-output/implementation-artifacts/2-2-guard-against-duplicate-architecture-artifacts.md _bmad-output/implementation-artifacts/sprint-status.yaml`
- Run `pnpm check` if edits are broad.

No runtime tests are expected for this documentation-routing story.

### Previous Story Intelligence

Story 2.1 should remove any non-native planning routes before this story focuses on duplicate architecture. If Story 2.1 is not implemented yet, avoid mixing broad planning cleanup into this story unless the same line explicitly routes architecture through a deleted or retired source.

### Git Intelligence

Recent commits show the project is mid-reset and documentation-heavy. Keep the change reviewable: fix architecture-routing defects, but do not rewrite setup or package-discovery content wholesale.

### Latest Technical Information

No web research is required. This story depends on local documentation ownership and deleted-path reference checks, not current external technology versions.

### Project Context Reference

Project context says consumer docs should explain how to use and operate the library while BMAD architecture owns internal durable behavior. It also warns against duplicating durable architecture rules into README/AGENTS indexes.

### References

- `_bmad-output/planning-artifacts/prds/prd-Bondstone-2026-06-18/prd.md` - FR2.3, NFR2, success metrics
- `_bmad-output/planning-artifacts/architecture.md` - Documentation Ownership
- `_bmad-output/planning-artifacts/epics.md` - Epic 2 and Story 2.2
- `_bmad-output/project-context.md` - source-of-truth and docs routing rules
- `docs/setup.md` - known stale deleted architecture links
- `docs/package-discovery.md` - known stale deleted architecture links

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- `rg -n "docs/architecture|architecture/[A-Za-z0-9_-]+\\.md|\\]\\(architecture/" README.md AGENTS.md docs src tests samples .agents/skills`
- `rg -n "_bmad-output/planning-artifacts/architecture.md|BMAD architecture" docs/setup.md docs/package-discovery.md`
- `test ! -d docs/architecture`

### Completion Notes List

- Replaced active deleted architecture-doc links in `docs/setup.md` and `docs/package-discovery.md` with links to `_bmad-output/planning-artifacts/architecture.md`.
- Preserved `/docs` ownership for setup, operations, observability, packaging, package discovery, public API, and other consumer/repository guidance.
- Confirmed `docs/architecture` remains absent and no replacement duplicate architecture folder was created.
- Remaining `docs/architecture`-style matches are generic reusable skill examples, not active Bondstone architecture routing.

### File List

- `docs/setup.md`
- `docs/package-discovery.md`
- `_bmad-output/implementation-artifacts/2-2-guard-against-duplicate-architecture-artifacts.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
