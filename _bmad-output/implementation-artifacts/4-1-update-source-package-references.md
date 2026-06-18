---
baseline_commit: bb2c1029a7507d3d1a4f86d82c57fa441b0366b5
---

# Story 4.1: Update Source Package References

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a package maintainer,
I want source package README and AGENTS references to point to current docs,
so that agents and humans are not sent to deleted paths.

## Acceptance Criteria

1. `src/**/AGENTS.md` links to BMAD architecture or consumer docs.
2. `src/**/README.md` links do not point to removed architecture files.
3. Package-specific consumer docs remain linked.

## Tasks / Subtasks

- [x] Sweep source package indexes for stale internal-documentation routing. (AC: 1, 2)
  - [x] Review `src/AGENTS.md` and every package-level `src/**/AGENTS.md`.
  - [x] Replace any active link to deleted `docs/architecture/**`, `docs/adr/**`, retired decision-file review, or non-native planning artifacts with the current BMAD architecture, project-context, or consumer docs owner.
  - [x] Keep scoped AGENTS files as short indexes; do not copy durable architecture rules into them.
- [x] Sweep source package READMEs for stale or misleading package links. (AC: 2, 3)
  - [x] Review `src/README.md` and every package-level `src/**/README.md`.
  - [x] Keep package-specific consumer links such as setup, package discovery, operations, observability, packaging, BMAD architecture, and relevant test folders when they still resolve.
  - [x] Remove or update any stale references to removed package IDs, deleted architecture docs, retired ADR paths, or obsolete v1 setup guidance.
- [x] Preserve package-boundary meaning while fixing references. (AC: 1, 3)
  - [x] Keep active package names aligned with `docs/packaging.md`.
  - [x] Keep transport README language clear that RabbitMQ and Service Bus adapters are thin native-driver envelope adapters, not topology or retry owners.
  - [x] Keep local transport README language clear that local transport is explicit local/dev/test infrastructure, not a production fallback.
- [x] Verify reference hygiene and formatting. (AC: 1, 2, 3)
  - [x] Run targeted deleted-path and retired-workflow sweeps over `src/**`.
  - [x] Confirm every edited relative link resolves from its source file.
  - [x] Run Prettier on touched markdown and this story file.

### Review Findings

- [x] [Review][Patch] `baseline_commit` does not resolve to a commit in this repository [`_bmad-output/implementation-artifacts/4-1-update-source-package-references.md`:2]
- [x] [Review][Patch] `sprint-status.yaml` truncates `last_updated` from timestamp format to date-only format [`_bmad-output/implementation-artifacts/sprint-status.yaml`:2]

## Dev Notes

Story 4.1 is a scoped source-package documentation cleanup. It should not change runtime code, package projects, package metadata, or public API unless a stale reference directly requires a documentation-only correction.

### Current State Intelligence

Initial source sweep before story creation found these source documentation files:

- `src/AGENTS.md`
- `src/README.md`
- `src/Bondstone/AGENTS.md`
- `src/Bondstone/README.md`
- `src/Bondstone.Hosting/AGENTS.md`
- `src/Bondstone.Hosting/README.md`
- `src/Bondstone.Persistence/README.md`
- `src/Bondstone.Persistence.EntityFrameworkCore/AGENTS.md`
- `src/Bondstone.Persistence.EntityFrameworkCore/README.md`
- `src/Bondstone.Persistence.EntityFrameworkCore.Postgres/AGENTS.md`
- `src/Bondstone.Persistence.EntityFrameworkCore.Postgres/README.md`
- `src/Bondstone.Transport.Local/AGENTS.md`
- `src/Bondstone.Transport.Local/README.md`
- `src/Bondstone.Transport.RabbitMq/AGENTS.md`
- `src/Bondstone.Transport.RabbitMq/README.md`
- `src/Bondstone.Transport.ServiceBus/AGENTS.md`
- `src/Bondstone.Transport.ServiceBus/README.md`

The initial stale-reference sweep did not find active `docs/architecture`, `docs/adr`, `adr-required`, `bondstone-adr`, or retired decision-file review links in `src/**/AGENTS.md` or `src/**/README.md`. The implementation may therefore be a verification pass if the source files still satisfy the acceptance criteria. Do not invent edits just to make the story look busy.

### Architecture Compliance

BMAD architecture owns internal runtime architecture and package-boundary rules. `/docs` owns setup, package discovery, packaging, public API review, operations, observability, samples, testing, repository workflow, and GitHub issue guidance. Source AGENTS and README files should route readers to those owners instead of duplicating internal durable behavior.

Package responsibilities must stay aligned with BMAD architecture and `docs/packaging.md`:

- `Bondstone`: core module model, command/event contracts, durable message identities, module execution, durable send/publish APIs, domain-event contracts, and runtime composition.
- `Bondstone.Persistence`: provider-neutral durable persistence contracts, operation state, outbox/inbox abstractions, and inspection surfaces.
- `Bondstone.Persistence.EntityFrameworkCore`: EF Core mappings, stores, transaction behavior, outbox/inbox persistence, operation state, and optional EF-backed domain-event persistence.
- `Bondstone.Persistence.EntityFrameworkCore.Postgres`: PostgreSQL-specific EF behavior and provider integration.
- `Bondstone.Hosting`: hosted outbox and durable inbox worker composition.
- `Bondstone.Transport.Local`: explicit local/dev/test transport adapter.
- `Bondstone.Transport.RabbitMq`: thin RabbitMQ native-driver envelope adapter.
- `Bondstone.Transport.ServiceBus`: thin Azure Service Bus native-driver envelope adapter.

### File Structure Requirements

Primary implementation targets are scoped source documentation only:

- `src/AGENTS.md`
- `src/README.md`
- `src/**/AGENTS.md`
- `src/**/README.md`

Do not edit `tests/**` in this story; Story 4.2 owns test scoped references. Do not edit `docs/github-workflow.md`; Story 4.3 owns GitHub workflow guidance. If a source README links to tests, only update that source README link when the link is stale.

Package README files are package artifacts. Repository-relative links in source package READMEs should stay as absolute GitHub URLs when they point to repository docs, source paths, or tests, because those READMEs are rendered from NuGet and package-manager surfaces. Scoped AGENTS files should use local relative links for repository agents.

### Testing Requirements

Recommended verification:

- `test ! -d docs/architecture`
- `test ! -d docs/adr`
- `rg -n "docs/architecture|docs/adr|adr-required|bondstone-adr|decision-file review|Use ADR skills|ADRs preserve durable decisions|non-native planning" src -g 'AGENTS.md' -g 'README.md'`
- `rg -n "Bondstone\\.Persistence\\.Postgres|Bondstone\\.Capabilities\\.DomainEvents|Bondstone\\.Capabilities\\.DomainEvents\\.EntityFrameworkCore" src -g 'AGENTS.md' -g 'README.md'`
- `find src -name AGENTS.md -o -name README.md | sort`
- `pnpm exec prettier --check src/**/*.md _bmad-output/implementation-artifacts/4-1-update-source-package-references.md _bmad-output/implementation-artifacts/sprint-status.yaml`

No runtime tests are expected for this documentation-only story. If markdown-only files change, `pnpm exec prettier --check ...` is the expected lightweight gate. Use `pnpm check` only if broader source changes happen unexpectedly.

### Previous Story Intelligence

There is no previous story inside Epic 4. Epic 3 completed root and docs routing:

- Story 3.2 aligned root `AGENTS.md` with BMAD PRD, architecture, epics, project-context, and scoped docs.
- Story 3.3 aligned `/docs` indexes around consumer/repository ownership.
- Story 3.4 verified consumer docs should link to BMAD architecture for internal runtime behavior and explicitly left broader source/test scoped reference alignment to Epic 4.

Carry those patterns forward: indexes stay concise, deleted paths stay deleted, and source-package files should reference current owners instead of becoming architecture documents.

### Git Intelligence

Recent commits:

- `bb2c102 docs: bmad native docs`
- `783b260 docs: more bmad documents refactoring`
- `13140c5 docs: bmad project context`
- `f9dc177 chore: bmad`
- `998a2d5 fix: durable inbox polish`

The recent direction is BMAD-native source-of-truth routing with narrow documentation verification passes. Avoid unrelated package copy rewrites or runtime cleanup while completing this story.

### Latest Technical Information

No web research is required. This story is governed by local BMAD artifacts, source package README/AGENTS files, and repository docs. Do not update package versions, dependency versions, or NuGet release guidance based on external lookup as part of this story.

### Project Context Reference

Project context says README files orient humans, AGENTS files orient agents, and both should reference BMAD artifacts and consumer docs instead of duplicating durable architecture rules. It also says public API and package-boundary changes are compatibility-sensitive and should be kept out of documentation-only cleanup unless explicitly required.

### Story Completion Status

Ultimate context engine analysis completed - comprehensive developer guide created.

### References

- `_bmad-output/planning-artifacts/prds/prd-Bondstone-2026-06-18/prd.md` - FR2.4 and FR2.5
- `_bmad-output/planning-artifacts/architecture.md` - Architecture Role, Package Architecture, Documentation Ownership, Verification Strategy
- `_bmad-output/planning-artifacts/epics.md` - Epic 4 and Story 4.1
- `_bmad-output/project-context.md` - source-of-truth, code, testing, and workflow guardrails
- `docs/packaging.md` - active package IDs, removed package IDs, dependency direction, package README link policy
- `docs/package-discovery.md` - consumer package and namespace discovery
- `docs/repository.md` - context-index convention
- `docs/testing.md` - docs-only verification and test category guidance
- `src/AGENTS.md` - source package agent index
- `src/README.md` - source package human index

## Dev Agent Record

### Agent Model Used

Codex (GPT-5)

### Debug Log References

- Resolved `bmad-dev-story` customization manually after `_bmad/scripts/resolve_customization.py` failed because Python could not import `json`.
- Loaded sprint status, project context, Story 4.1, Epic 4, `docs/testing.md`, `docs/packaging.md`, `docs/package-discovery.md`, and source package indexes.
- Verified `docs/architecture` and `docs/adr` directories are absent.
- Ran stale-route sweep over `src/**/AGENTS.md` and `src/**/README.md`; no deleted architecture, ADR, retired decision, or non-native planning references were found.
- Ran removed-package sweep over `src/**/AGENTS.md` and `src/**/README.md`; no removed package IDs were found.
- Confirmed 17 source package markdown files have resolving local or repository links.
- Ran Prettier check over source markdown, this story file, and sprint status.
- Ran `pnpm check`; formatting, restore, Release build, fast tests, pack, and package tests passed.
- Ran final `workflow.on_complete` resolver; it failed with the same Python `json` import error, and manual fallback found no configured completion hook.

### Completion Notes List

- Source package AGENTS files already route to BMAD architecture and current consumer docs, so no source AGENTS edits were required.
- Source package README files already avoid deleted architecture/ADR paths, preserve package-specific consumer links, and keep active package IDs aligned with `docs/packaging.md`.
- RabbitMQ and Service Bus README language remains thin-adapter scoped; local transport README language remains explicit local/dev/test only and not a production fallback.
- Documentation verification passed without runtime code, package metadata, or public API changes.
- `pnpm check` passed after story bookkeeping updates, with a final targeted Prettier check after status moved to review.

### File List

- `_bmad-output/implementation-artifacts/4-1-update-source-package-references.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`

### Change Log

- 2026-06-18: Verified source package README/AGENTS reference hygiene and completed Story 4.1 bookkeeping; no source package documentation edits required.
