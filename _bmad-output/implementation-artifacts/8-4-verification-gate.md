---
baseline_commit: ea112318dd49ed47997a594e5f0ffad49da55aca
---

# Story 8.4: Verification Gate

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a maintainer,
I want the repo verification surface green,
so that the reset is reviewable.

## Acceptance Criteria

1. Given documentation-only changes are made, when verification runs, then formatting and reference checks run where available.
2. Given runtime code or package changes occur, when verification runs, then targeted tests run first and the relevant package scripts run afterward.
3. Given the default quality gate is named, when repo instructions are reviewed, then `pnpm check` is the default gate.
4. Given tests are added or reclassified, when test traits are reviewed, then xUnit categories are consistently one of `Unit`, `Application`, `Integration`, or `Package`.
5. Given packable packages change, when verification runs, then public API baseline and package checks are included.

## Tasks / Subtasks

- [x] Inventory the current verification routing before editing. (AC: 1, 2, 3, 5)
  - [x] Read `package.json`, root `README.md`, root `AGENTS.md`, `docs/testing.md`, `docs/repository.md`, `.github/workflows/verify.yml`, and `_bmad-output/project-context.md`.
  - [x] Confirm `pnpm check` is the default gate in active repo instructions and CI, and `pnpm verify` remains an alias rather than a competing primary gate.
  - [x] Confirm `pnpm backend:test:integration` is still the explicit provider-backed gate and `pnpm backend:pack` is still the package artifact gate.
- [x] Tighten or repair verification documentation only where the inventory finds drift. (AC: 1, 2, 3, 5)
  - [x] Keep verification guidance centralized in `docs/testing.md`, `docs/repository.md`, root `README.md`, root `AGENTS.md`, and package scripts.
  - [x] Do not duplicate broad runtime architecture, package policy, or public API classification text into verification docs.
  - [x] Do not change package IDs, target framework, dependency direction, release versioning, public API baselines, or runtime code solely for wording cleanup.
- [x] Verify test category consistency. (AC: 4)
  - [x] Search test source for `[Trait("Category", "...")]` values and confirm every value is exactly `Unit`, `Application`, `Integration`, or `Package`.
  - [x] If adding or reclassifying tests, read the nearest `tests/**/AGENTS.md` and keep category choice aligned with `docs/testing.md`.
  - [x] Keep `Package` tests limited to freshly packed artifact inspection from `pnpm backend:pack`.
- [x] Preserve public API and package verification discipline. (AC: 5)
  - [x] If public/protected API shape changes, run the targeted public API baseline test before any baseline refresh and classify the API change in `docs/public-api.md`.
  - [x] Use `BONDSTONE_UPDATE_PUBLIC_API_BASELINE=1` only after compatibility review; review the diff and rerun without the variable.
  - [x] Run `pnpm backend:pack` when packable projects, public API baselines, package metadata, package READMEs, XML docs, or package artifact behavior change.
- [x] Run and record verification evidence. (AC: 1, 2, 3, 4, 5)
  - [x] For documentation-only changes, run `pnpm format:check` and the relevant reference/routing sweeps from Dev Notes.
  - [x] For runtime source or test changes, run targeted tests for the touched surface first, then `pnpm backend:build`, `pnpm backend:test`, and the relevant broader gate.
  - [x] For provider-backed PostgreSQL, RabbitMQ, Azure Service Bus, or sample smoke behavior, run `pnpm backend:test:integration` after build.
  - [x] For package/public API/artifact changes, run `pnpm backend:pack`.
  - [x] Run `pnpm check` as the final default quality gate unless the story is intentionally limited to a documented narrower verification and records why.

### Review Findings

- [x] [Review][Patch] Remove stale completion notes copied from another workflow [_bmad-output/implementation-artifacts/8-4-verification-gate.md:216]

## Dev Notes

Story 8.4 is the verification closeout for Epic 8. It should prove the repo's active verification instructions and scripts agree, then run the appropriate gates. The expected implementation is either a verified no-op plus recorded evidence or a narrow documentation/script correction if active guidance has drifted.

### Current State Intelligence

- `package.json` defines `pnpm check` as the default quality gate: `format:check`, restore, Release build, fast tests, and package verification. `pnpm verify` is an alias for `pnpm check`.
- `docs/testing.md` already says the default verification commands are `pnpm check`, `pnpm backend:restore`, `pnpm backend:build`, `pnpm backend:test`, and `pnpm backend:pack`; it names `pnpm backend:test:integration` for `Integration` tests and `pnpm backend:test:all` for every discovered test.
- `docs/testing.md` defines the allowed xUnit categories: `Unit`, `Application`, `Integration`, and `Package`.
- `docs/repository.md` names the default CI workflow as the repository quality gate and the required merge status check as `Quality Gate`.
- `.github/workflows/verify.yml` runs `pnpm check` in the `Quality Gate` job.
- Root `AGENTS.md` and `_bmad-output/project-context.md` both tell agents to prefer the repository scripts and identify `pnpm check` as the default quality gate.
- Story 8.3 already ran the stale deleted-path reference sweeps and full `pnpm check`; do not redo Story 8.3's cleanup unless new drift appears.
- Story 8.2 already aligned package policy and discovery docs; do not rewrite package policy while validating verification routing.
- Story 8.1 already aligned public API baseline package coverage and package collaboration rules; do not refresh baselines unless this story makes a deliberate public/protected API change.
- No UX files exist under `_bmad-output/planning-artifacts`; UX is not applicable for this library verification story.

### Files To Read Or Update

Read these UPDATE files completely before changing them:

- `package.json` - script source of truth for `pnpm check`, `pnpm verify`, backend restore/build/test/integration/pack gates, and package artifact test orchestration.
- `README.md` - human-facing quickstart and verification routing.
- `AGENTS.md` - agent-facing verification entrypoints and operating rules.
- `docs/README.md` - docs ownership map if verification docs links need adjustment.
- `docs/testing.md` - test categories and verification surface.
- `docs/repository.md` - repository tooling, CI, and quality-gate guidance.
- `.github/workflows/verify.yml` - CI quality gate command and job name.
- `.github/workflows/publish-nuget.yml` - package/release verification only if publish or package gate wording changes.
- `tests/AGENTS.md` and nearest `tests/**/AGENTS.md` - required before adding, moving, or reclassifying tests.
- `tests/Bondstone.PublicApi.Tests/README.md` and `tests/Bondstone.PublicApi.Tests/PublicApiBaselineTests.cs` - only if public API baseline verification changes.
- `tests/Bondstone.Package.Tests/**` - only if package artifact checks change.
- `docs/public-api.md`, `docs/packaging.md`, and `docs/package-discovery.md` - only if verification inventory reveals drift involving public API, package artifact, or package policy guidance.

### Architecture Compliance

- BMAD architecture owns internal runtime and package-boundary rules; `/docs` owns consumer-facing and repository-operation guidance.
- `pnpm check` remains the default quality gate. Do not create a second default gate or weaken the default gate in active docs.
- Documentation-only cleanup should at least run formatting/reference checks where available.
- Runtime changes should run targeted tests first, then the broader package script appropriate to the changed surface.
- Public API baselines guard all packable packages. Baseline diffs are review evidence, not approval to change compatibility.
- `Integration` tests require real infrastructure/provider behavior. EF Core InMemory is not proof for PostgreSQL durability, uniqueness, transactions, locking, claiming, or retries.
- `Package` tests inspect freshly packed artifacts and require `pnpm backend:pack` to produce a clean artifact directory.

### Suggested Verification Commands

Use these as starting points; adjust only when the changed surface justifies a narrower or broader command.

```bash
rg -n "pnpm check|pnpm verify|backend:restore|backend:build|backend:test|backend:test:integration|backend:pack|Quality Gate" README.md AGENTS.md docs .github package.json _bmad-output/project-context.md -g "*.md" -g "AGENTS.md" -g "*.yml" -g "package.json"
```

```bash
rg -n --pcre2 '\[Trait\("Category", "(?!(Unit|Application|Integration|Package)")' tests -g "*.cs"
```

```bash
pnpm format:check
pnpm backend:restore
pnpm backend:build
pnpm backend:test
pnpm backend:pack
pnpm check
```

If public API changes occur:

```bash
dotnet test tests/Bondstone.PublicApi.Tests/Bondstone.PublicApi.Tests.csproj --configuration Release
```

If provider-backed behavior changes:

```bash
pnpm backend:test:integration
```

### Previous Story Intelligence

Story 8.3 established:

- Active references to deleted `docs/adr/**`, `docs/architecture/**`, and `docs/plans/**` paths were clean.
- Remaining broad-sweep matches were historical implementation records, BMAD criteria/verification text, or generic reusable skill examples.
- `pnpm format:check` and full `pnpm check` passed after the reference sweep.
- Use fixed-string sweeps for literal deleted paths and `--pcre2` only when the pattern needs PCRE2 behavior.

Story 8.2 established:

- `docs/packaging.md` is the central package policy owner.
- `docs/package-discovery.md` is the central capability-to-package and namespace owner.
- Package READMEs use absolute GitHub URLs for repository docs because they render outside the repo in NuGet/package-manager surfaces.
- Docs-only package policy changes should not touch public API baselines.

Story 8.1 established:

- The eight packable packages, public API baseline package list, and checked-in baselines are aligned.
- Production package collaboration uses explicit contracts or package-local implementation; `InternalsVisibleTo` remains test-only.
- Public API baseline diffs require compatibility review and migration notes where applicable.

### Git Intelligence

Recent commits at story creation time:

- `ea11231 docs: more docs updates`
- `2365cfc docs: missed docs`
- `f0fd433 docs: public api`
- `4b07a5c fix: diagnostics improvement`
- `fb42e57 docs: more tightening`

Recent work favors narrow documentation/runtime corrections with targeted verification, followed by the broad gate when the surface is wide. Continue that pattern and avoid bundling unrelated package policy, public API, or runtime cleanup into Story 8.4.

### Latest Technical Information

No dependency upgrade is required for this story. Use repository-pinned tool versions from `global.json`, `package.json`, `Directory.Build.props`, and `Directory.Packages.props`.

- Microsoft documents `dotnet test --filter` as the standard way to run selected tests with filter expressions; Bondstone's scripts use category filters for the fast and integration gates. Source: https://learn.microsoft.com/en-us/dotnet/core/testing/selective-unit-tests
- Microsoft documents `dotnet pack` as the command that builds projects and creates `.nupkg` packages. Bondstone's `pnpm backend:pack` wraps `dotnet pack` and then runs package artifact tests. Source: https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-pack
- NuGet MSBuild pack docs cover pack properties and note `SymbolPackageFormat=snupkg`, matching Bondstone's central package artifact expectations. Source: https://learn.microsoft.com/en-us/nuget/reference/msbuild-targets
- pnpm's script docs describe package scripts as commands in `package.json`; Bondstone intentionally centralizes repo gates there. Source: https://pnpm.io/scripts

### Project Structure Notes

- Verification script changes belong in root `package.json` and should keep CI/docs aligned.
- CI quality-gate changes belong under `.github/workflows/` and should keep the required check name `Quality Gate` unless repository branch rules are deliberately updated.
- Test category changes belong in test source and docs together; do not invent new category names.
- Public API baseline verification belongs under `tests/Bondstone.PublicApi.Tests`.
- Package artifact verification belongs under `tests/Bondstone.Package.Tests` and the `pnpm backend:pack` flow.
- Generated packages under `artifacts/packages`, coverage, temporary sample outputs, and build artifacts should stay out of source.

### Project Context Reference

Project context says Bondstone is a .NET library/framework for durable module boundaries, durable command sending, EF Core backed inbox/outbox persistence, operation observation, and transport adapters. It says `pnpm check` is the default quality gate, `pnpm backend:test:integration` covers PostgreSQL/RabbitMQ/Service Bus/sample smoke behavior that requires real infrastructure, public API baselines guard packable packages, and generated artifacts should stay out of source.

### Open Questions

None blocking. During implementation, decide whether Story 8.4 is evidence-only or whether active docs/scripts need a narrow correction. Default to evidence-only if `pnpm check`, routing sweeps, and category review are already clean.

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- 2026-06-22: Activation resolver failed because local `python3` could not import stdlib `json`; workflow customization was resolved manually from base/team/user TOML fallback. No prepend or append steps configured.
- 2026-06-22: Loaded project context, config, sprint status, create-story workflow assets, checklist, Epic 8, PRD FR10 sections, architecture verification/documentation/public API sections, docs testing/repository guidance, package scripts, previous Epic 8 story files, recent git history, local repo status, and current external docs for dotnet test filtering, dotnet pack, NuGet pack properties, and pnpm scripts.
- 2026-06-22: Confirmed story key `8-4-verification-gate`; `epic-8` is already `in-progress`, and story status should move from `backlog` to `ready-for-dev`.
- 2026-06-22: Confirmed no UX artifact exists for this library verification story.
- 2026-06-22: Dev-story activation resolver failed again because local `python3` could not import stdlib `json`; workflow customization was resolved manually from `/workspaces/Bondstone/.agents/skills/bmad-dev-story/customize.toml`; no team/user overrides, prepend steps, append steps, or on-complete hook were present.
- 2026-06-22: Verified `package.json`, root `README.md`, root `AGENTS.md`, `docs/testing.md`, `docs/repository.md`, `.github/workflows/verify.yml`, and `_bmad-output/project-context.md`; `pnpm check` remains the default gate, `pnpm verify` remains an alias, CI `Quality Gate` runs `pnpm check`, `backend:test:integration` remains explicit provider-backed coverage, and `backend:pack` remains the package artifact gate.
- 2026-06-22: Ran verification routing sweep for `pnpm check`, `pnpm verify`, backend gates, and `Quality Gate`; no drift found requiring docs or script edits.
- 2026-06-22: Ran retired path reference sweep for `docs/adr/`, `docs/architecture/`, and `docs/plans/` across active docs/source/tests/workflows; no active references found.
- 2026-06-22: Ran invalid xUnit category sweep; no invalid `[Trait("Category", "...")]` values found. Category counts: `Application` 53, `Integration` 82, `Package` 1, `Unit` 465.
- 2026-06-22: Confirmed no runtime, public/protected API, package metadata, package README, XML documentation, baseline, or test classification changes were needed. `tests/AGENTS.md` and `tests/Bondstone.Package.Tests/PackageArtifactTests.cs` confirmed `Package` tests are artifact-only and tied to freshly packed packages.
- 2026-06-22: Verification passed: `pnpm format:check`, `pnpm backend:restore`, `pnpm backend:build`, `pnpm backend:test`, `pnpm backend:pack`, and final `pnpm check`. `backend:pack` emitted the existing non-packable sample warning for `samples/ModularMonolith`, then package tests passed.
- 2026-06-22: Re-ran final `pnpm check` after story-record bookkeeping; formatting, restore, Release build, fast tests, package creation, and package artifact tests passed again.

### Implementation Plan

- Complete Story 8.4 as a verified no-op unless inventory reveals active verification routing drift.
- Preserve `pnpm check` as the default quality gate and avoid creating competing verification guidance.
- Record evidence for routing, trait categories, public API/package discipline, and package artifact verification without changing runtime source, tests, package metadata, or baselines.

### Completion Notes List

- Story 8.4 completed with verification routing inventory, category consistency checks, public API/package guardrails, previous Epic 8 learnings, and command guidance.
- Confirmed active repo instructions and CI consistently route the default quality gate through `pnpm check`; `pnpm verify` is only an alias.
- Confirmed provider-backed integration testing remains explicit through `pnpm backend:test:integration`, while package artifact verification remains explicit through `pnpm backend:pack` and final `pnpm check`.
- Confirmed xUnit categories are limited to `Unit`, `Application`, `Integration`, and `Package`; no tests were added or reclassified.
- No verification documentation, package policy, public API baseline, runtime source, or test source edits were required; Story 8.4 completed as evidence-only.
- Verification evidence: `pnpm format:check`, routing/reference sweeps, category sweeps, `pnpm backend:restore`, `pnpm backend:build`, `pnpm backend:test`, `pnpm backend:pack`, and final `pnpm check` all passed.

### File List

- `_bmad-output/implementation-artifacts/8-4-verification-gate.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`

### Story Completion Status

Done.

### Change Log

- 2026-06-22: Completed verification routing inventory, confirmed no drift requiring docs/scripts/source changes, recorded passing verification evidence, and moved story toward review.
- 2026-06-22: Marked story ready for review after final `pnpm check` passed.

### References

- `_bmad-output/planning-artifacts/epics.md` - Epic 8 and Story 8.4 acceptance criteria.
- `_bmad-output/planning-artifacts/prds/prd-Bondstone-2026-06-18/prd.md` - FR10 testing and verification requirements.
- `_bmad-output/planning-artifacts/architecture.md` - Verification Strategy, Documentation Ownership, and Public API And Compatibility.
- `_bmad-output/project-context.md` - workflow, testing, public API, package collaboration, and artifact guardrails.
- `package.json` - repository verification script definitions.
- `README.md` and `AGENTS.md` - root human and agent verification routing.
- `docs/testing.md` - test category and verification surface owner.
- `docs/repository.md` - tooling and CI quality-gate guidance.
- `.github/workflows/verify.yml` - CI quality gate command and job name.
- `_bmad-output/implementation-artifacts/8-1-public-api-and-package-collaboration-validation.md` - previous public API/package collaboration learnings.
- `_bmad-output/implementation-artifacts/8-2-package-policy-and-discovery-documentation.md` - previous package policy/discovery learnings.
- `_bmad-output/implementation-artifacts/8-3-documentation-reference-sweep.md` - previous reference sweep and verification learnings.
