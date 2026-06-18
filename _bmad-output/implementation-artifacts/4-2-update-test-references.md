---
baseline_commit: 9bc4667caf7f22e214583384c9dac92fbdfe7d1b
---

# Story 4.2: Update Test References

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a test maintainer,
I want test AGENTS/README files to reference current verification and architecture sources,
so that test changes use the right constraints.

## Acceptance Criteria

1. `tests/**/AGENTS.md` avoids retired decision links.
2. Persistence/transport tests point to BMAD architecture plus relevant consumer docs.
3. Public API test docs no longer say retired decision review is required.

## Tasks / Subtasks

- [x] Sweep test AGENTS and README files for stale planning and decision references. (AC: 1, 3)
  - [x] Review `tests/AGENTS.md`, `tests/README.md`, and every `tests/**/AGENTS.md` and `tests/**/README.md`.
  - [x] Remove or replace active references to deleted `docs/architecture/**`, deleted `docs/adr/**`, retired ADR labels, retired decision-file review, or non-native planning workflows.
  - [x] Preserve current BMAD review language where it points to BMAD PRD, architecture, epics, project-context, or consumer docs.
- [x] Align persistence and transport test routing with current owners. (AC: 2)
  - [x] For persistence test folders, route behavior-changing work to BMAD architecture and test category/provider guidance to `docs/testing.md`.
  - [x] For transport test folders, route transport-boundary behavior to BMAD architecture and local/dev/test policy to `docs/testing.md`, `docs/packaging.md`, or package docs as appropriate.
  - [x] Keep scoped AGENTS files concise indexes; do not copy durable runtime rules from BMAD architecture into test folders.
- [x] Keep public API baseline guidance compatibility-sensitive but BMAD-native. (AC: 3)
  - [x] Preserve `docs/public-api.md` and `docs/testing.md` as normal test-owner references.
  - [x] Ensure baseline refresh guidance says BMAD architecture review and release-note treatment may be required for compatibility-sensitive API changes.
  - [x] Do not reintroduce retired decision labels or ADR-required wording.
- [x] Verify reference hygiene and formatting. (AC: 1, 2, 3)
  - [x] Run targeted stale-reference sweeps over `tests/**/AGENTS.md` and `tests/**/README.md`.
  - [x] Confirm edited links resolve from their source files.
  - [x] Run Prettier on touched markdown and this story file.

## Dev Notes

Story 4.2 is a documentation/reference cleanup for the test tree only. It should not change test code, public API baselines, package metadata, runtime code, or test categorization unless a stale documentation reference directly requires a documentation-only correction.

### Current State Intelligence

Initial discovery found these test documentation files:

- `tests/AGENTS.md`
- `tests/README.md`
- `tests/Bondstone.Composition.Tests/AGENTS.md`
- `tests/Bondstone.Composition.Tests/README.md`
- `tests/Bondstone.Hosting.Tests/AGENTS.md`
- `tests/Bondstone.Hosting.Tests/README.md`
- `tests/Bondstone.Persistence.EntityFrameworkCore.Tests/AGENTS.md`
- `tests/Bondstone.Persistence.EntityFrameworkCore.Tests/README.md`
- `tests/Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests/AGENTS.md`
- `tests/Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests/README.md`
- `tests/Bondstone.PublicApi.Tests/AGENTS.md`
- `tests/Bondstone.PublicApi.Tests/README.md`
- `tests/Bondstone.Samples.Tests/AGENTS.md`
- `tests/Bondstone.Samples.Tests/README.md`
- `tests/Bondstone.Tests/AGENTS.md`
- `tests/Bondstone.Tests/README.md`
- `tests/Bondstone.Transport.Local.Tests/AGENTS.md`
- `tests/Bondstone.Transport.Local.Tests/README.md`

The initial stale-reference sweep over test AGENTS and README files found no active `docs/architecture`, `docs/adr`, `adr-required`, `bondstone-adr`, retired decision, or non-native planning references. Implementation may therefore be partly a verification pass. Do not invent edits just to create churn.

Known current routing:

- Root test docs route category and command policy to `docs/testing.md`.
- Composition tests already point to BMAD architecture, `docs/packaging.md`, and `docs/testing.md`.
- PostgreSQL EF tests already point to BMAD architecture for provider behavior and `docs/testing.md` for integration-test rules.
- Public API tests point to `docs/public-api.md` and `docs/testing.md`; README language says baseline diffs are not a substitute for BMAD architecture review or release-note treatment. That is current BMAD-native language and should be preserved unless reworded for clarity.
- Sample tests point to `docs/samples.md` and `docs/testing.md`.
- Core, hosting, EF Core, and local transport test docs currently point primarily to their package scope plus `docs/testing.md`; update them only where Story 4.2 acceptance criteria require clearer architecture/source-of-truth routing.

### Architecture Compliance

BMAD architecture owns internal runtime architecture, durable behavior, package-boundary rules, verification strategy, and documentation ownership. `/docs` owns consumer-facing or repository-operation guidance, including testing, packaging, public API review, samples, repository workflow, and GitHub issue guidance.

For tests, keep these ownership boundaries clear:

- Test category policy and command entrypoints belong in `docs/testing.md`.
- Public API baseline classification belongs in `docs/public-api.md`.
- Package composition, active package IDs, removed package IDs, and package-boundary policy belong in `docs/packaging.md`.
- Internal runtime behavior, persistence semantics, durable inbox/outbox semantics, transport boundaries, and package-boundary architecture belong in BMAD architecture.
- Sample test direction belongs in `docs/samples.md`.

Do not replace BMAD architecture review with old ADR or decision-file terminology. Do not move durable rules into test AGENTS/README files.

### File Structure Requirements

Primary implementation targets are scoped test documentation only:

- `tests/AGENTS.md`
- `tests/README.md`
- `tests/**/AGENTS.md`
- `tests/**/README.md`

Do not edit `src/**`; Story 4.1 already owns source package references. Do not edit `docs/github-workflow.md`; Story 4.3 owns GitHub workflow guidance. Do not refresh `tests/Bondstone.PublicApi.Tests/Baselines/**` as part of this story.

### Testing Requirements

Recommended verification:

- `test ! -d docs/architecture`
- `test ! -d docs/adr`
- `rg -n "docs/architecture|docs/adr|adr-required|bondstone-adr|decision-file review|decision file|retired decision|non-native planning" tests -g 'AGENTS.md' -g 'README.md'`
- `rg -n "BMAD architecture|_bmad-output/planning-artifacts/architecture.md|docs/testing.md|docs/public-api.md|docs/packaging.md|docs/samples.md" tests -g 'AGENTS.md' -g 'README.md'`
- `find tests -name AGENTS.md -o -name README.md | sort`
- `pnpm exec prettier --check tests/**/*.md _bmad-output/implementation-artifacts/4-2-update-test-references.md _bmad-output/implementation-artifacts/sprint-status.yaml`

No runtime tests are expected for this documentation-only story. If markdown-only files change, targeted reference sweeps and Prettier are the expected lightweight gate. Use `pnpm check` only if broader source or build-affecting changes happen unexpectedly.

### Previous Story Intelligence

Story 4.1 completed the same cleanup pattern for `src/**/AGENTS.md` and `src/**/README.md`:

- A verification-only result is acceptable when scoped docs already satisfy acceptance criteria.
- Keep AGENTS and README files as indexes, not duplicate architecture documents.
- Preserve current package-specific consumer links when they still resolve.
- Keep deleted paths deleted and route durable internals to BMAD architecture.
- Avoid runtime code, package metadata, public API, or unrelated documentation changes.

Carry those patterns into `tests/**`.

### Git Intelligence

Recent commits:

- `9bc4667 docs: metadata fix`
- `bb2c102 docs: bmad native docs`
- `783b260 docs: more bmad documents refactoring`
- `13140c5 docs: bmad project context`
- `f9dc177 chore: bmad`

The recent direction is BMAD-native source-of-truth routing with narrow documentation verification passes. Avoid broad test rewrites or baseline updates while completing this story.

### Latest Technical Information

No web research is required. This story is governed by local BMAD artifacts, test AGENTS/README files, and repository docs. Do not update package versions, test framework versions, or dependency guidance based on external lookup as part of this story.

### Project Context Reference

Project context says README files orient humans, AGENTS files orient agents, and both should reference BMAD artifacts and consumer docs instead of duplicating durable architecture rules. It also says tests must follow `docs/testing.md`, public API changes are compatibility-sensitive, and EF Core InMemory is not proof of relational durability.

### Story Completion Status

Ultimate context engine analysis completed - comprehensive developer guide created.

### References

- `_bmad-output/planning-artifacts/prds/prd-Bondstone-2026-06-18/prd.md` - FR2.4 and FR2.5
- `_bmad-output/planning-artifacts/architecture.md` - Documentation Ownership and Verification Strategy
- `_bmad-output/planning-artifacts/epics.md` - Epic 4 and Story 4.2
- `_bmad-output/project-context.md` - source-of-truth, testing, workflow, and documentation guardrails
- `docs/testing.md` - test categories and verification entrypoints
- `docs/public-api.md` - public API baseline guidance and BMAD review language
- `docs/packaging.md` - package-boundary and public API compatibility notes
- `docs/repository.md` - context-index convention
- `_bmad-output/implementation-artifacts/4-1-update-source-package-references.md` - previous story pattern

## Dev Agent Record

### Agent Model Used

Codex (GPT-5)

### Debug Log References

- Resolved `bmad-dev-story` customization manually after `_bmad/scripts/resolve_customization.py` failed because Python could not import `json`.
- Loaded sprint status, project context, Story 4.2, Epic 4, BMAD architecture documentation ownership, `docs/testing.md`, `docs/packaging.md`, and scoped test indexes.
- Verified `docs/architecture` and `docs/adr` directories are absent.
- Ran stale-reference sweep over `tests/**/AGENTS.md` and `tests/**/README.md`; no deleted architecture, ADR, retired decision, or non-native planning references were found.
- Added BMAD architecture routing to EF Core persistence and local transport test documentation where scoped indexes previously only pointed to consumer test docs.
- Confirmed edited reference targets resolve.
- Ran Prettier check over test markdown, this story file, and sprint status.
- Ran `pnpm check`; formatting, restore, Release build, fast tests, pack, and package tests passed.
- Ran final `workflow.on_complete` resolver; it failed with the same Python `json` import error, and manual fallback found no configured completion hook.

### Completion Notes List

- Test scoped documentation now avoids retired decision and deleted architecture/ADR references.
- EF Core persistence test docs route persistence behavior changes to BMAD architecture and test category guidance to `docs/testing.md`.
- Local transport test docs route transport-boundary behavior to BMAD architecture, local/dev/test policy to `docs/packaging.md`, and verification policy to `docs/testing.md`.
- Public API baseline docs already used BMAD-native compatibility language, so no public API test doc edits were required.
- Documentation-only validation passed; no runtime code, test code, package metadata, or public API baselines were changed.
- `pnpm check` passed after story bookkeeping updates.

### File List

- `_bmad-output/implementation-artifacts/4-2-update-test-references.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `tests/Bondstone.Persistence.EntityFrameworkCore.Tests/AGENTS.md`
- `tests/Bondstone.Persistence.EntityFrameworkCore.Tests/README.md`
- `tests/Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests/README.md`
- `tests/Bondstone.Transport.Local.Tests/AGENTS.md`
- `tests/Bondstone.Transport.Local.Tests/README.md`

### Change Log

- 2026-06-18: Updated persistence and local transport test documentation routing and completed Story 4.2 verification.
