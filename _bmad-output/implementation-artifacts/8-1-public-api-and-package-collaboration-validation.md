---
baseline_commit: 4b07a5c
---

# Story 8.1: Public API And Package Collaboration Validation

Status: review

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a maintainer,
I want v2 public API reviewed before release,
so that package consumers get deliberate compatibility posture.

## Acceptance Criteria

1. Given public or protected APIs change, when baselines are reviewed, then affected APIs are classified as normal setup APIs, documented advanced composition APIs, temporarily exposed public implementation types, or accidental/obsolete surface.
2. Given runtime packages need to collaborate, when implementation is reviewed, then production package collaboration uses explicit contracts or package-local implementation, not `InternalsVisibleTo`.
3. Given temporary public implementation types are hidden or renamed, when the change is reviewed, then replacement contracts and migration notes exist.
4. Given packable package baselines change, when tests run, then public API baselines guard all packable packages.

## Tasks / Subtasks

- [x] Inventory current packable package public API coverage before editing. (AC: 1, 4)
  - [x] Compare `tests/Bondstone.PublicApi.Tests/PublicApiBaselineTests.cs` package assembly list with every `src/*/*.csproj` where `<IsPackable>true</IsPackable>`.
  - [x] Confirm every active package in `docs/packaging.md` has one baseline under `tests/Bondstone.PublicApi.Tests/Baselines/`.
  - [x] Run or prepare the public API baseline test before accepting any baseline refresh; do not update baselines until the API change is classified.
- [x] Review production package collaboration edges. (AC: 2)
  - [x] Search `src/**` for `InternalsVisibleTo`.
  - [x] Verify every `InternalsVisibleTo` points only to test assemblies or composition test assemblies, never to another production package.
  - [x] Verify production package references match the dependency direction in `docs/packaging.md` and do not introduce provider, transport, or hosting dependencies back into `Bondstone` or `Bondstone.Persistence`.
- [x] Classify any public/protected API diffs. (AC: 1, 3, 4)
  - [x] For each changed public/protected member, classify it in `docs/public-api.md` as normal setup API, user application contract, advanced composition API, provider/runtime contract, provider/runtime concrete API, public implementation detail exposed for now, or cleanup candidate.
  - [x] If a type is hidden, renamed, removed, or has parameter names changed, record why source compatibility is acceptable and what replacement contract or migration path exists.
  - [x] Treat baseline diffs as review evidence, not automatic approval.
- [x] Preserve deliberate public implementation surfaces. (AC: 1, 3)
  - [x] Do not hide broad builders, options, EF mapping types, serializers, registries, or provider/runtime registration records merely because they are concrete.
  - [x] Only reduce public surface when the replacement is already explicit, documented, and package-boundary safe.
  - [x] Keep provider/runtime collaboration through named public contracts, narrow public registration records, or package-local implementation.
- [x] Update docs and migration notes where compatibility posture changes. (AC: 1, 3)
  - [x] Update `docs/public-api.md` for classification, decision notes, and any replacement/migration guidance.
  - [x] Update `docs/packaging.md` only if package dependency direction, package set, or v2 migration guidance changes.
  - [x] Update package READMEs only if normal setup or package discovery guidance changes; avoid duplicating architecture rules.
- [x] Verify. (AC: 2, 4)
  - [x] Run targeted public API tests: `dotnet test tests/Bondstone.PublicApi.Tests/Bondstone.PublicApi.Tests.csproj --configuration Release`.
  - [x] If a baseline must be intentionally refreshed, run `BONDSTONE_UPDATE_PUBLIC_API_BASELINE=1 dotnet test tests/Bondstone.PublicApi.Tests/Bondstone.PublicApi.Tests.csproj --configuration Release`, review the baseline diff, and rerun without the environment variable.
  - [x] Run `pnpm backend:build` and `pnpm backend:test` for runtime or test changes.
  - [x] Run `pnpm backend:pack` when packable projects, public API baselines, package metadata, package READMEs, or XML docs change.
  - [x] Run `pnpm check` as the final broad gate for code, public API, package, or broad docs changes.

## Dev Notes

Story 8.1 is a validation and curation slice. It should not become broad public API cleanup unless the inventory proves a specific surface is accidental or obsolete and the replacement/migration path is clear. The default outcome may be a documented review with tests and docs aligned rather than lots of code churn.

### Current State Intelligence

- Public API baseline tests already cover the eight current packable package assemblies: `Bondstone`, `Bondstone.Hosting`, `Bondstone.Persistence`, `Bondstone.Persistence.EntityFrameworkCore`, `Bondstone.Persistence.EntityFrameworkCore.Postgres`, `Bondstone.Transport.Local`, `Bondstone.Transport.RabbitMq`, and `Bondstone.Transport.ServiceBus`.
- Each active package project under `src/` sets `<IsPackable>true</IsPackable>` and the public API test project references all eight projects directly.
- `Directory.Build.props` defaults `<IsPackable>false</IsPackable>` and packable packages opt in project-by-project. This is the source of truth for test coverage checks.
- Existing production `InternalsVisibleTo` declarations are in package `Properties/AssemblyInfo.cs` files and point to test assemblies: `Bondstone.Hosting.Tests`, `Bondstone.Composition.Tests`, `Bondstone.Transport.Local.Tests`, `Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests`, `Bondstone.Transport.RabbitMq.Tests`, and `Bondstone.Transport.ServiceBus.Tests`. Do not add production package friend assemblies.
- `docs/public-api.md` already contains a package-by-package classification inventory and recent decision notes for stable setup codes, incoming inbox, transport receive settlement, v2 cleanup, and remaining concrete public helpers.
- `docs/packaging.md` owns package IDs, dependency direction, target framework, package artifacts, versioning, publishing, and migration policy. Package collaboration changes must stay aligned there.

### Files To Read Or Update

Read these UPDATE files completely before changing them:

- `tests/Bondstone.PublicApi.Tests/PublicApiBaselineTests.cs` - current generated-baseline assertion, package assembly list, update environment variable, and normalization behavior.
- `tests/Bondstone.PublicApi.Tests/Bondstone.PublicApi.Tests.csproj` - package project references and baseline content copying.
- `tests/Bondstone.PublicApi.Tests/Baselines/*.txt` - checked-in public/protected API surfaces for all packable packages.
- `docs/public-api.md` - classification labels, decision notes, current package-by-package classification, and migration guidance.
- `docs/packaging.md` - active package set, dependency direction, public API surface policy, and package artifact/versioning rules.
- `docs/package-discovery.md` - consumer-facing package and namespace map, especially if normal setup APIs or package IDs change.
- `Directory.Build.props` and `Directory.Packages.props` - target framework, packable default, package metadata, central package versions, and PublicApiGenerator version.
- `src/*/*.csproj` - packable project opt-ins and package-to-package references.
- `src/*/Properties/AssemblyInfo.cs` - current `InternalsVisibleTo` declarations.

If the implementation touches a specific package API, also read that package's nearest `src/**/AGENTS.md`, package README, changed source file, and matching test project `AGENTS.md` before editing.

### Architecture Compliance

- Bondstone remains a durable module-boundary library, not a generic bus, workflow engine, broker runtime owner, topology manager, code generator, SaaS framework, or application platform.
- Runtime package collaboration must use explicit contracts or package-local implementation. Production `InternalsVisibleTo` is not allowed.
- Public/protected API hiding, renaming, removal, and parameter-name churn are compatibility-sensitive because C# parameter names are visible to named-argument callers.
- Normal setup APIs and documented advanced composition APIs need migration notes when changed.
- Public implementation types should be reduced gradually only when explicit contracts or package-local implementation replace them.
- Baseline diffs are evidence that an API changed; they do not by themselves approve compatibility risk, replace BMAD architecture review, or replace release-note treatment.
- Provider/runtime contracts may remain public when package collaboration needs explicit contracts. Prefer small named contracts over friend assembly access.

### Package Collaboration Guardrails

- `Bondstone` may depend on neutral `Bondstone.Persistence`; it must not depend on EF, PostgreSQL, hosting, local transport, RabbitMQ, or Service Bus packages.
- `Bondstone.Persistence` is provider-neutral and should not depend on provider or transport adapter packages.
- `Bondstone.Persistence.EntityFrameworkCore` depends on `Bondstone` and `Bondstone.Persistence`.
- `Bondstone.Persistence.EntityFrameworkCore.Postgres` depends on `Bondstone.Persistence.EntityFrameworkCore`, `Bondstone.Persistence`, and may reference `Bondstone` for shared builder extension methods.
- `Bondstone.Hosting`, `Bondstone.Transport.Local`, `Bondstone.Transport.RabbitMq`, and `Bondstone.Transport.ServiceBus` depend on `Bondstone` and `Bondstone.Persistence`; broker packages may depend on their native drivers.
- Keep topology, provisioning, retries, dead-letter policy, credentials, monitoring, and native broker lifecycle host-owned.

### Testing Requirements

- Public API baseline tests are `Unit` tests and live under `tests/Bondstone.PublicApi.Tests`.
- Use `BONDSTONE_UPDATE_PUBLIC_API_BASELINE=1` only after compatibility review; then review the resulting baseline diff and rerun tests without the variable.
- Use `Package` tests only for tests that inspect freshly packed artifacts from `pnpm backend:pack`.
- If API cleanup touches runtime behavior, add or update package-local `Unit` or `Application` tests for the behavior first; use `Integration` only for provider-backed database, broker, sample, or infrastructure behavior.
- Run `pnpm backend:pack` when public API baselines, packable projects, package metadata, package READMEs, or XML docs change.

### Previous Story Intelligence

Story 7.4 established:

- Public diagnostic setup-code contracts were added in `Bondstone.Persistence` under `Bondstone.Diagnostics` because both core composition and provider-neutral persistence surfaces emit the same coded setup exception contract, and `Bondstone` may depend on that neutral contract package.
- `Bondstone.Persistence` public API baseline was intentionally updated after review; future baseline changes must repeat that review discipline.
- Source-compatible exception inheritance was preserved for broad catch behavior.
- Review patches corrected missing setup-code coverage and documented provider-neutral ownership rather than moving the contract casually.

Story 7.2 established:

- Public API changes require compatibility review and baseline consideration.
- Operation waits and reads remain observation only; avoid public API wording that implies orchestration or durable continuations.

Story 7.1 established:

- Transport adapters remain thin native-driver envelope adapters.
- Host code owns topology, retry, dead-letter policy, credentials, monitoring, worker placement, and broker behavior.

### Git Intelligence

Recent commits at story creation time:

- `4b07a5c fix: diagnostics improvement`
- `fb42e57 docs: more tightening`
- `8a7090d fix: more observation`
- `e85574a docs: postgres durability`
- `dd17279 fix: sb durable worker`

The recent pattern is narrow runtime or docs correction backed by targeted tests, public API baseline updates when public contracts change, and final broad verification. Continue that pattern; do not combine unrelated API cleanup with story 8.1.

### Latest Technical Information

No dependency upgrade is required for this story. Use repository-pinned versions from `Directory.Build.props`, `Directory.Packages.props`, `global.json`, and `package.json`.

- PublicApiGenerator is pinned at `11.5.4`, matching the current NuGet package page checked during story creation. Source: https://www.nuget.org/packages/PublicApiGenerator
- PublicApiGenerator is suitable for approval-style tests that compare generated public API text against version-controlled baselines; Bondstone already uses that pattern in `tests/Bondstone.PublicApi.Tests`. Source: https://www.nuget.org/packages/PublicApiGenerator
- Microsoft documents `InternalsVisibleToAttribute` as friend assembly access to internal members. That mechanism is allowed here for tests only; production package collaboration must use explicit contracts or package-local implementation. Source: https://learn.microsoft.com/en-us/dotnet/standard/assembly/friend
- Microsoft documents `TargetFramework` values such as `net10.0` as SDK target framework aliases. Bondstone currently targets `net10.0` centrally in `Directory.Build.props`. Source: https://learn.microsoft.com/en-us/dotnet/standard/frameworks

### Project Structure Notes

- No UX files were found under `_bmad-output/planning-artifacts`; UX is not applicable for this library story.
- Public API test changes belong under `tests/Bondstone.PublicApi.Tests`.
- Runtime API changes belong in the owning package under `src/`; add or update tests in the corresponding package test project.
- Consumer-facing compatibility notes belong in `docs/public-api.md`; package policy belongs in `docs/packaging.md`; package/namespace discovery belongs in `docs/package-discovery.md`.
- Internal architecture changes belong in `_bmad-output/planning-artifacts/architecture.md`, not duplicated in consumer docs.
- Generated packages, coverage, temporary sample outputs, and build artifacts should stay out of source.

### Project Context Reference

Project context says Bondstone is a .NET library/framework for durable module boundaries, durable command sending, EF Core backed inbox/outbox persistence, operation observation, and transport adapters. Public/protected API changes are compatibility-sensitive. Production package collaboration must use explicit contracts or package-local implementation; do not add `InternalsVisibleTo` for runtime package collaboration. Use repository scripts for verification, and keep generated artifacts out of source.

### Open Questions

None blocking. During implementation, decide whether story 8.1 is a pure validation/docs update or needs focused code/test changes after the inventory. Do not refresh public API baselines unless a concrete API change is deliberately accepted.

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- 2026-06-21: Activation resolver failed because local `python3` could not import stdlib `json`; workflow customization was resolved manually from base/team/user TOML fallback. No prepend or append steps configured.
- 2026-06-21: Loaded project context, config, sprint status, full story workflow assets, full epics, PRD FR9/FR10 sections, architecture package/public API/verification sections, repository/testing/package/public API docs, package discovery, public API test docs, package project files, scoped AGENTS files, story 7.4, recent git history, and latest PublicApiGenerator/.NET friend assembly/target framework sources before creating this story.
- 2026-06-21: Confirmed story key `8-1-public-api-and-package-collaboration-validation`; `epic-8` was backlog and this is the first epic 8 story, so sprint status should move `epic-8` to `in-progress`.
- 2026-06-21: Confirmed eight packable source projects, eight public API test assemblies, and eight baseline files align with active package IDs in `docs/packaging.md`.
- 2026-06-21: Confirmed source `InternalsVisibleTo` declarations target only package test assemblies or composition tests; no production package friend assemblies found.
- 2026-06-21: Reconciled `docs/public-api.md` package classification inventory with current baselines for query APIs, setup-code diagnostics, durable incoming inbox contracts, incoming inbox hosting APIs, and EF incoming inbox surfaces.
- 2026-06-21: Verification passed: targeted public API test, `pnpm backend:build`, `pnpm backend:test`, `pnpm backend:pack`, and final `pnpm check`.

### Completion Notes List

- Ultimate context engine analysis completed - comprehensive developer guide created.
- Story 8.1 is ready for dev with concrete file inventory, package collaboration guardrails, public API baseline workflow, and verification commands.
- Implemented story 8.1 as a validation/docs curation slice with no runtime code changes and no public API baseline refresh.
- Documented the story 8.1 validation result in `docs/public-api.md`, including package/baseline coverage, production collaboration review, classification reconciliation, and the no-migration-note outcome.
- Verified package collaboration remains through explicit public contracts or package-local implementation; `InternalsVisibleTo` remains test-only.
- Verified all required gates pass: targeted public API baseline test, `pnpm backend:build`, `pnpm backend:test`, `pnpm backend:pack`, and final `pnpm check`.

### File List

- `_bmad-output/implementation-artifacts/8-1-public-api-and-package-collaboration-validation.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `docs/public-api.md`

### Change Log

- 2026-06-21: Completed story 8.1 public API and package collaboration validation; updated `docs/public-api.md` classification inventory and decision notes without refreshing baselines.

### Story Completion Status

Ready for review.

### References

- `_bmad-output/planning-artifacts/epics.md` - Epic 8 and Story 8.1 acceptance criteria.
- `_bmad-output/planning-artifacts/prds/prd-Bondstone-2026-06-18/prd.md` - FR9.2, FR9.3, FR9.4, and FR10.4.
- `_bmad-output/planning-artifacts/architecture.md` - Package Architecture, Public API And Compatibility, and Verification Strategy.
- `_bmad-output/project-context.md` - runtime, code, testing, public API, package collaboration, and workflow guardrails.
- `docs/public-api.md` - classification labels, current package API inventory, and compatibility decision notes.
- `docs/packaging.md` - active package set, dependency direction, public API policy, package artifacts, and versioning.
- `docs/package-discovery.md` - consumer-facing package and namespace map.
- `docs/testing.md` - test categories and verification surface.
- `tests/Bondstone.PublicApi.Tests/README.md` - public API baseline workflow.
- `tests/Bondstone.PublicApi.Tests/PublicApiBaselineTests.cs` - baseline test implementation and covered package assemblies.
- `tests/Bondstone.PublicApi.Tests/Bondstone.PublicApi.Tests.csproj` - package references and baseline copy behavior.
- `Directory.Build.props` - target framework, packable default, version, and package metadata.
- `Directory.Packages.props` - pinned package versions including PublicApiGenerator.
