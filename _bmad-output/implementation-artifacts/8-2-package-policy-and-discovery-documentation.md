---
baseline_commit: 2365cfcd47b8143d8108467093544c5f5ab894ee
---

# Story 8.2: Package Policy And Discovery Documentation

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a library consumer,
I want package IDs, dependency direction, target framework, versioning, and publishing policy documented centrally,
so that setup and upgrade choices are discoverable.

## Acceptance Criteria

1. Given package guidance is reviewed, when package IDs or namespaces are needed, then `docs/packaging.md` and `docs/package-discovery.md` are the central consumer-facing references.
2. Given dependency direction or target framework changes, when docs are updated, then package policy stays centralized instead of scattered through scoped READMEs.
3. Given publishing or versioning policy changes, when release work proceeds, then migration and package docs are updated together.

## Tasks / Subtasks

- [x] Audit current package policy and discovery routing. (AC: 1, 2)
  - [x] Compare active package IDs in `docs/packaging.md`, `docs/package-discovery.md`, `README.md`, `docs/README.md`, `src/README.md`, and `src/*/README.md`.
  - [x] Confirm package IDs match the packable project names under `src/*/*.csproj` and the solution package set.
  - [x] Sweep scoped READMEs for policy drift: target framework, dependency direction, versioning, publishing, registry, and migration rules should link to `docs/packaging.md` instead of being restated.
- [x] Tighten `docs/packaging.md` as the central package policy reference. (AC: 1, 2, 3)
  - [x] Ensure it owns package IDs, target framework, dependency direction, removed package IDs, replacement guidance, versioning, Release Please ownership, NuGet publishing, package artifacts, symbols, README/XML doc policy, and migration/release-note coordination.
  - [x] Keep package-boundary and release-policy guidance concise and consumer-facing; route internal runtime architecture details to `_bmad-output/planning-artifacts/architecture.md`.
  - [x] Do not invent a v2 NuGet version, release date, or registry action that is not already present in source-controlled release metadata or approved release workflow state.
- [x] Tighten `docs/package-discovery.md` as the central capability-to-package and namespace reference. (AC: 1)
  - [x] Confirm each capability maps to the current package ID, common namespaces, and normal setup APIs.
  - [x] Keep the page focused on "which package/namespace do I use?" and link deeper behavior to setup, operations, observability, packaging, public API, and BMAD architecture docs.
  - [x] Verify removed/non-current package IDs appear only as migration notes, not as current install guidance.
- [x] Keep indexes and scoped READMEs discoverable without duplicating policy. (AC: 1, 2)
  - [x] Confirm `README.md`, `docs/README.md`, and `src/README.md` link to both `docs/packaging.md` and `docs/package-discovery.md`.
  - [x] Confirm each package README links to package discovery and packaging policy with absolute GitHub URLs suitable for NuGet README rendering.
  - [x] If a package README contains stale package policy text, replace it with a short package-purpose statement plus links to the central docs.
- [x] Coordinate migration/versioning language. (AC: 3)
  - [x] If release or publishing policy text changes, update migration guidance in `docs/packaging.md` in the same edit.
  - [x] If package setup examples change, update `docs/setup.md` or package READMEs only where the normal setup path is affected.
  - [x] Do not update public API baselines unless public/protected API shape changes; docs-only package policy changes should not touch baselines.
- [x] Verify documentation consistency. (AC: 1, 2, 3)
  - [x] Run stale package reference sweeps for removed IDs: `Bondstone.Persistence.Postgres`, `Bondstone.Transport`, `Bondstone.Capabilities.DomainEvents`, and `Bondstone.Capabilities.DomainEvents.EntityFrameworkCore`.
  - [x] Run `pnpm format:check` for documentation formatting.
  - [x] Run `pnpm backend:pack` if package README content, package metadata, package docs included in artifacts, or package artifact policy changes.
  - [x] Run `pnpm check` if changes broaden beyond narrow docs cleanup or package artifact behavior.

## Dev Notes

Story 8.2 is a documentation authority and discoverability slice. The expected outcome is a focused package-policy and package-discovery cleanup, not a package rename, dependency graph change, target framework change, release-version bump, NuGet publish, or public API cleanup.

### Current State Intelligence

- `docs/packaging.md` already records the current target framework `net10.0`, active package IDs, removed package IDs, v2 replacement and migration guidance, release checklist, dependency direction, public API policy, central package management, Release Please versioning, package artifact expectations, NuGet trusted publishing, and nuget.org-only publishing.
- `docs/package-discovery.md` already maps the active capability set to current package IDs and common namespaces, and links to `setup.md`, `packaging.md`, `operations.md`, `observability.md`, `public-api.md`, and BMAD architecture.
- Root `README.md` points package policy to `docs/packaging.md` and package/namespace discovery to `docs/package-discovery.md`.
- `docs/README.md` names `packaging.md` as owner of package IDs, target framework, package artifact policy, dependency direction, versioning, and publishing policy. It names `package-discovery.md` as owner of package and namespace discovery guidance.
- `src/README.md` lists the eight current package projects and points package IDs, dependency direction, target framework, and release policy to `docs/packaging.md`.
- Package READMEs under `src/*/README.md` already link to package discovery and packaging/release/migration policy using absolute GitHub URLs, which is appropriate for NuGet package README rendering.
- Active packable projects are the eight current package IDs: `Bondstone`, `Bondstone.Hosting`, `Bondstone.Persistence`, `Bondstone.Persistence.EntityFrameworkCore`, `Bondstone.Persistence.EntityFrameworkCore.Postgres`, `Bondstone.Transport.Local`, `Bondstone.Transport.RabbitMq`, and `Bondstone.Transport.ServiceBus`.
- Removed/non-current package IDs are already called out as migration notes in `docs/packaging.md`: `Bondstone.Persistence.Postgres`, `Bondstone.Transport`, `Bondstone.Capabilities.DomainEvents`, and `Bondstone.Capabilities.DomainEvents.EntityFrameworkCore`.
- No UX files exist under `_bmad-output/planning-artifacts`; UX is not applicable for this library documentation story.

### Files To Read Or Update

Read these UPDATE files completely before changing them:

- `docs/packaging.md` - central package IDs, target framework, dependency direction, versioning, publishing, artifacts, and migration policy.
- `docs/package-discovery.md` - central capability, package ID, namespace, and setup API discovery.
- `README.md` - root discovery route for package policy and package discovery.
- `docs/README.md` - docs ownership model and docs index.
- `src/README.md` - package project index and central policy link.
- `src/*/README.md` - package-purpose pages that are rendered as package READMEs and should link to central package docs without duplicating broad policy.
- `docs/setup.md` - package install list and setup examples if implementation finds package setup drift.
- `docs/public-api.md` - public API classification context only if package policy edits touch public API compatibility language.
- `Directory.Build.props` - source-controlled `TargetFramework`, `VersionPrefix`, package metadata, symbol settings, and package README inclusion.
- `Directory.Packages.props` - central dependency versions.
- `src/*/*.csproj` - active packable package opt-ins, package descriptions, package references, and project dependency direction.

If implementation touches tests, read the nearest `tests/**/AGENTS.md` and `docs/testing.md` first. If implementation touches runtime source packages, read the nearest `src/**/AGENTS.md` first.

### Architecture Compliance

- Bondstone remains a .NET library for durable module boundaries, not a generic bus, workflow engine, SaaS framework, broker topology manager, or code generator.
- Package dependency direction must stay layered from core contracts to provider/runtime implementations. Do not use production `InternalsVisibleTo` for package collaboration.
- `/docs` owns consumer-facing and repository-operation guidance. BMAD architecture owns internal runtime behavior and package-boundary rules.
- `docs/packaging.md` is the consumer-facing package policy source for package IDs, dependency direction, target framework, package artifacts, versioning, and publishing.
- `docs/package-discovery.md` is the consumer-facing capability-to-package and namespace map.
- Scoped package READMEs should explain package purpose and quick path, then link to `docs/packaging.md` and `docs/package-discovery.md`; they should not become separate policy authorities.
- Release and migration language must stay source-controlled and conservative: do not direct new consumers to v1 package IDs, old setup examples, GitHub Packages, or removed package IDs as current install paths.

### Package Policy Guardrails

- The target framework comes from `Directory.Build.props` and is currently `net10.0`.
- The version comes from `Directory.Build.props` `VersionPrefix`, currently `1.1.0`, with Release Please owning normal release PR updates. Do not change `VersionPrefix` unless the work is intentionally a release PR or approved recovery release.
- Package versions are centrally managed through `Directory.Packages.props`. Do not add package versions to individual project files.
- Package IDs match packable project names. New package IDs, removed package IDs, target framework changes, or dependency-direction changes require docs, BMAD architecture/PRD review where applicable, and release/migration notes.
- The NuGet publish workflow publishes to nuget.org only. GitHub Packages is not a current target.
- Package READMEs that link to repository docs, source paths, or tests should use absolute GitHub URLs because they render outside the repository in NuGet and package-manager UI surfaces.
- `pnpm backend:pack` verifies package artifacts after packing and includes package artifact tests. Run it when package README content, package metadata, XML documentation expectations, or pack behavior changes.

### Previous Story Intelligence

Story 8.1 established:

- The eight packable package projects, public API baseline package list, and checked-in public API baselines are aligned.
- Production package references follow `docs/packaging.md` dependency direction.
- `InternalsVisibleTo` remains test-only and must not be used for production package collaboration.
- Public API baseline diffs are review evidence, not approval. Docs-only package policy edits should not refresh baselines unless public/protected API shape changes.
- `docs/public-api.md` now records the Story 8.1 validation result and should remain the classification home for public API posture, while `docs/packaging.md` remains the package policy home.

Story 7.4 established:

- Stable setup-code diagnostics live in `Bondstone.Persistence` because both core composition and provider-neutral persistence surfaces use them, and `Bondstone` may depend on that neutral contract package. Do not casually relocate package ownership for docs tidiness.

Story 7.1 established:

- RabbitMQ and Azure Service Bus packages are thin native-driver adapter packages. Broker topology, provisioning, retries, dead-letter policy, credentials, monitoring, and native lifecycle stay host-owned.

### Git Intelligence

Recent commits at story creation time:

- `2365cfc docs: missed docs`
- `f0fd433 docs: public api`
- `4b07a5c fix: diagnostics improvement`
- `fb42e57 docs: more tightening`
- `8a7090d fix: more observation`

Recent work favors narrow docs/runtime corrections with targeted verification. Continue that pattern: keep story 8.2 focused on package policy/discovery docs and do not combine unrelated API cleanup or runtime behavior changes.

### Latest Technical Information

No dependency upgrade is required for this story. Use repository-pinned versions from `Directory.Build.props`, `Directory.Packages.props`, `global.json`, and `package.json`.

- Microsoft documents target framework monikers as standardized tokens for specifying a .NET app or library target framework; Bondstone currently targets `net10.0` centrally. Source: https://learn.microsoft.com/en-us/dotnet/standard/frameworks
- NuGet uses TFMs to identify and isolate framework-dependent package assets; package artifact checks should continue to expect `lib/net10.0/` while `Directory.Build.props` targets `net10.0`. Source: https://learn.microsoft.com/en-us/nuget/reference/target-frameworks
- NuGet trusted publishing uses short-lived credentials from CI/CD systems such as GitHub Actions instead of long-lived API keys. This matches the current docs' nuget.org trusted publishing posture. Source: https://learn.microsoft.com/en-us/nuget/nuget-org/trusted-publishing
- NuGet MSBuild pack properties support `SymbolPackageFormat=snupkg`; Bondstone currently sets `IncludeSymbols=true` and `SymbolPackageFormat=snupkg` centrally. Source: https://learn.microsoft.com/en-us/nuget/reference/msbuild-targets
- Release Please creates release PRs, changelogs, tags, and GitHub releases from conventional commits; it does not publish packages to package managers. Keep publish behavior owned by the NuGet workflow. Source: https://github.com/googleapis/release-please

### Project Structure Notes

- Consumer package policy belongs in `docs/packaging.md`.
- Consumer package and namespace discovery belongs in `docs/package-discovery.md`.
- Normal setup examples belong in `docs/setup.md`; package docs should link there instead of duplicating the golden path.
- Package-purpose summaries belong in `src/*/README.md`; broad release, dependency, target framework, publishing, and migration policy should link back to central docs.
- Internal durable runtime and package-boundary architecture belongs in `_bmad-output/planning-artifacts/architecture.md`.
- Generated packages, coverage, temporary sample outputs, and build artifacts should stay out of source.

### Project Context Reference

Project context says Bondstone is a .NET library/framework for durable module boundaries, durable command sending, EF Core backed inbox/outbox persistence, operation observation, and transport adapters. Public/protected API changes are compatibility-sensitive. Production package collaboration must use explicit contracts or package-local implementation; do not add `InternalsVisibleTo` for runtime package collaboration. Use repository scripts for verification, and keep generated artifacts out of source.

### Open Questions

None blocking. During implementation, decide whether the existing package docs only need verification notes or whether any stale package-policy duplication in scoped READMEs should be replaced with central links.

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- 2026-06-22: Activation resolver failed because local `python3` could not import stdlib `json`; workflow customization was resolved manually from base/team/user TOML fallback. No prepend or append steps configured.
- 2026-06-22: Loaded project context, config, sprint status, create-story workflow assets, checklist, Epic 8, PRD FR9/FR10 sections, architecture documentation ownership and verification sections, package/public API docs, setup docs, root/docs/src indexes, package READMEs, package project files, previous story 8.1, recent git history, and current external docs for .NET TFMs, NuGet trusted publishing, NuGet pack MSBuild properties, and Release Please.
- 2026-06-22: Confirmed story key `8-2-package-policy-and-discovery-documentation`; `epic-8` is already `in-progress`, and story status should move from `backlog` to `ready-for-dev`.
- 2026-06-22: Confirmed no UX artifact exists for this library story.
- 2026-06-22: Dev-story activation resolved manually after local Python stdlib import failure; no activation prepend or append steps were configured. Loaded project context, story file, sprint status, package policy/discovery docs, setup/public API docs, source package index, package READMEs, solution, central build props, central package props, and packable package project files.
- 2026-06-22: Captured baseline commit `2365cfcd47b8143d8108467093544c5f5ab894ee` and moved story 8.2 from `ready-for-dev` to `in-progress`.
- 2026-06-22: Initial documentation audit found active package IDs match the eight packable `src/*/*.csproj` projects in `Bondstone.slnx`; removed package IDs appear only as migration/public-history references; package READMEs use absolute GitHub links to packaging and package discovery. Drift found: `src/README.md` linked packaging policy but not package discovery.
- 2026-06-22: Updated `docs/packaging.md`, `docs/package-discovery.md`, and `src/README.md`; verified central package policy/discovery routing, active package project names, removed-ID migration references, package README absolute links, `pnpm format:check`, `pnpm backend:pack`, and `pnpm check`.

### Implementation Plan

- Keep changes docs-only and centered on package policy/discovery ownership.
- Tighten `docs/packaging.md` as the package-policy authority for IDs, target framework, dependency direction, versioning, publishing, artifact, and migration coordination.
- Tighten `docs/package-discovery.md` as the capability/package/namespace authority and keep removed IDs framed as migration notes only.
- Add the missing central package-discovery route to `src/README.md`.
- Verify with stale package reference sweeps, formatting, package artifact verification, and the repository default quality gate.

### Completion Notes List

- Ultimate context engine analysis completed - comprehensive developer guide created.
- Story 8.2 is ready for dev with concrete package-policy files, current state intelligence, centralization guardrails, stale package reference sweeps, and verification commands.
- Tightened `docs/packaging.md` as the central consumer-facing package policy owner for active IDs, target framework, dependency direction, artifacts, versioning, publishing, and migration/release coordination.
- Tightened `docs/package-discovery.md` as the capability-to-package and namespace owner, with removed/non-current IDs framed only as migration notes.
- Added the missing `docs/package-discovery.md` route to `src/README.md` so root, docs, and source indexes all point to both central references.
- Confirmed package READMEs already link to package discovery and packaging policy with absolute GitHub URLs, so no package README content change was needed.
- Verification passed: stale package reference sweeps, `pnpm format:check`, `pnpm backend:pack`, and `pnpm check`.

### File List

- `_bmad-output/implementation-artifacts/8-2-package-policy-and-discovery-documentation.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `docs/packaging.md`
- `docs/package-discovery.md`
- `src/README.md`

### Change Log

- 2026-06-22: Created story 8.2 package policy and discovery documentation context; updated sprint status to `ready-for-dev`.
- 2026-06-22: Implemented story 8.2 package policy/discovery centralization docs and moved story to review.

### Story Completion Status

Review.

### References

- `_bmad-output/planning-artifacts/epics.md` - Epic 8 and Story 8.2 acceptance criteria.
- `_bmad-output/planning-artifacts/prds/prd-Bondstone-2026-06-18/prd.md` - FR9.1 package and public API surface requirement plus FR10 verification context.
- `_bmad-output/planning-artifacts/architecture.md` - Package Architecture, Documentation Ownership, Public API And Compatibility, and Verification Strategy.
- `_bmad-output/project-context.md` - package collaboration, public API, testing, and workflow guardrails.
- `docs/packaging.md` - central package IDs, target framework, dependency direction, versioning, publishing, and migration policy.
- `docs/package-discovery.md` - central package and namespace discovery matrix.
- `docs/setup.md` - normal setup package list and golden path.
- `docs/README.md` - docs ownership map.
- `README.md` - root package policy and discovery routes.
- `src/README.md` and `src/*/README.md` - package indexes and package README link surfaces.
- `Directory.Build.props` - target framework, version prefix, package metadata, symbol package settings, and README packing.
- `Directory.Packages.props` - central package dependency versions.
- `_bmad-output/implementation-artifacts/8-1-public-api-and-package-collaboration-validation.md` - previous Epic 8 story learnings.
