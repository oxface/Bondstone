---
baseline_commit: ea112318dd49ed47997a594e5f0ffad49da55aca
---

# Story 8.5: Consumer Trial Handoff

Status: review

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a maintainer,
I want a clear first-consumer migration handoff,
so that real feedback can drive the next library iteration.

## Acceptance Criteria

1. Given setup docs are reviewed, when trial guidance is created, then the recommended EF/PostgreSQL path is visible and linked.
2. Given a consumer needs package choices, when trial guidance is created, then it points to current package IDs and package/namespace discovery.
3. Given trial readiness is reviewed, when BMAD epics and sprint status are inspected, then remaining pre-trial work is named or explicitly recorded as complete.
4. Given real-project trial work reveals gaps, when the story closes, then deferred work is tracked in GitHub Issues or Projects instead of being buried in docs or story notes.
5. Given service extraction is part of the product promise, when trial guidance is reviewed, then the service-extraction proof requirements are visible before trial scope is treated as complete.

## Tasks / Subtasks

- [x] Inventory the current consumer-trial readiness surface before editing. (AC: 1, 2, 3, 5)
  - [x] Read `README.md`, `docs/README.md`, `docs/setup.md`, `docs/package-discovery.md`, `docs/packaging.md`, `docs/operations.md`, `docs/testing.md`, `docs/samples.md`, `samples/README.md`, `_bmad-output/planning-artifacts/epics.md`, and `_bmad-output/implementation-artifacts/sprint-status.yaml`.
  - [x] Confirm Epic 8 stories 8.1 through 8.4 are done in sprint status before making any trial-readiness claim.
  - [x] Confirm `docs/setup.md` names EF Core plus PostgreSQL as the supported production durable persistence path and the golden local modular-monolith path.
  - [x] Confirm `docs/package-discovery.md` and `docs/packaging.md` list only current package IDs as install guidance.
- [x] Create or update consumer trial handoff guidance. (AC: 1, 2, 3, 5)
  - [x] Prefer a focused consumer-facing doc under `docs/` only if existing docs do not already provide a single handoff route; otherwise add a narrow section/link from `docs/README.md` or `README.md`.
  - [x] Link setup, package discovery, testing, operations, samples, and BMAD epics rather than duplicating their full content.
  - [x] Make the handoff clear about what to try first: a modular-monolith host using EF/PostgreSQL persistence, current package IDs, app-owned EF migrations, and local transport only for local/test/sample usage.
  - [x] Include service-extraction proof visibility: stable contract assemblies, durable message identities, handler/subscriber identities, inbox/outbox semantics, operation observation, module-owned persistence, and host-owned broker topology.
- [x] Track deferred work through GitHub Issues or Projects. (AC: 4)
  - [x] Read `docs/github-workflow.md` before creating or commenting on any issue.
  - [x] Check current open GitHub Issues and the Bondstone Project for existing trial/deferred items.
  - [x] If gaps are found or trial work remains, create focused issues using the `Trial`, `Feature Or Enhancement`, `Bug`, or `Cleanup` formats from `docs/github-workflow.md`; use existing labels such as `type:trial`, `documentation`, `enhancement`, `bug`, `area:api`, `area:transport`, `area:template`, and `bmad-review-required` where appropriate.
  - [x] Do not use repository docs as a backlog. Docs may link to GitHub tracking, but durable follow-up ownership belongs in issues/projects.
- [x] Preserve package, public API, and verification discipline. (AC: 1, 2, 3)
  - [x] Do not rename packages, change target framework, alter release metadata, change public API baselines, or update package READMEs unless the inventory finds a narrow handoff defect.
  - [x] Do not claim v2 trial readiness if required runtime stories remain incomplete or untracked.
  - [x] Do not imply Bondstone owns broker topology, migrations, cleanup, retention, replay, purge, or dead-letter handling.
- [x] Verify and record evidence. (AC: 1, 2, 3, 4, 5)
  - [x] Run focused link/reference sweeps for the handoff routes and current package IDs.
  - [x] Run `pnpm format:check` for documentation/story formatting.
  - [x] Run `pnpm backend:pack` if package READMEs, package metadata, XML docs, or package artifact behavior changes.
  - [x] Run `pnpm check` if the change broadens beyond narrow docs/trial-handoff text.

## Dev Notes

Story 8.5 closes Epic 8 by producing a practical first-consumer handoff. The expected implementation is a narrow docs and tracking slice. It should make the trial path discoverable, verify remaining work is either complete or tracked, and prepare GitHub Issues/Projects to capture real-project findings. It should not become a runtime feature, package policy rewrite, release PR, broad sample rewrite, or public API cleanup.

### Current State Intelligence

- `docs/setup.md` already identifies the normal adoption path: `Bondstone`, `Bondstone.Hosting`, `Bondstone.Persistence.EntityFrameworkCore`, `Bondstone.Persistence.EntityFrameworkCore.Postgres`, and `Bondstone.Transport.Local` for local/sample/test flows. It also calls EF Core plus PostgreSQL the supported production durable persistence path.
- `docs/setup.md` explains that applications own EF migrations, PostgreSQL schemas, connection strings, topology names, stable identities, broker retry/dead-letter policy, schema deployment, and inbox/outbox maintenance.
- `docs/package-discovery.md` maps capabilities to the eight current package IDs: `Bondstone`, `Bondstone.Hosting`, `Bondstone.Persistence`, `Bondstone.Persistence.EntityFrameworkCore`, `Bondstone.Persistence.EntityFrameworkCore.Postgres`, `Bondstone.Transport.Local`, `Bondstone.Transport.RabbitMq`, and `Bondstone.Transport.ServiceBus`.
- `docs/packaging.md` owns target framework `net10.0`, package IDs, dependency direction, versioning, publishing, package artifacts, and v2 migration/release coordination.
- `docs/operations.md` owns production receive, broker settlement, inbox/outbox inspection, operation finalization/expiration, EF migrations, package upgrades, contract evolution, retention, and app-owned recovery guidance.
- `docs/testing.md` owns verification entrypoints and says the modular monolith sample integration suite is covered by `pnpm backend:test:integration`.
- `docs/samples.md` and `samples/README.md` document the modular monolith adoption proof and service-split path. The service-split proof uses stable contracts, separate service providers, PostgreSQL-backed module persistence, and broker-backed adapter proofs while keeping broker topology app-owned.
- `docs/github-workflow.md` already defines trial issue bodies and labels. At story creation time, the GitHub repository had no open issues and already had `type:trial`, `type:cleanup`, `documentation`, `enhancement`, `bug`, `area:api`, `area:transport`, `area:template`, and `bmad-review-required` labels.
- `_bmad-output/implementation-artifacts/sprint-status.yaml` shows Epic 8 in progress, stories 8.1 through 8.4 done, and story 8.5 as the remaining backlog story at creation time.
- No UX files exist under `_bmad-output/planning-artifacts`; UX is not applicable for this library handoff story.

### Files To Read Or Update

Read these UPDATE files completely before changing them:

- `README.md` - root discovery route for BMAD artifacts, packages, setup, docs, verification, and current direction.
- `docs/README.md` - docs ownership model and index. Update if a new trial handoff doc is added.
- `docs/setup.md` - golden EF/PostgreSQL setup path, package set, host composition, migration ownership, local transport, broker adapter, and service-extraction examples.
- `docs/package-discovery.md` - capability-to-package and namespace matrix.
- `docs/packaging.md` - package policy, active IDs, removed package notes, versioning, publishing, package artifact, and migration/release policy.
- `docs/operations.md` - production ownership, receive settlement, operation evidence, migration, retention, and cleanup boundaries.
- `docs/testing.md` - verification commands and sample integration coverage.
- `docs/samples.md` and `samples/README.md` - adoption proof and service-split readiness.
- `docs/github-workflow.md` - issue/project statuses, trial issue format, labels, working-issue rules, and completion-comment format.
- `_bmad-output/planning-artifacts/epics.md` - Epic 8 story acceptance criteria and what remains before trial.
- `_bmad-output/implementation-artifacts/sprint-status.yaml` - current story/epic statuses.

Only read or update package READMEs, `src/README.md`, package project files, public API baselines, tests, or sample source if the inventory finds a concrete handoff defect in those surfaces.

### Architecture Compliance

- BMAD artifacts own product requirements, runtime architecture, implementation sequence, and lean agent guardrails. `/docs` owns consumer-facing and repository-operation guidance.
- EF Core plus PostgreSQL is the supported production durable persistence path. Consumers own EF migrations and schema rollout.
- `Bondstone.Transport.Local` is explicit local/dev/test/sample infrastructure. It is not production broker durability and must not be described as a hidden fallback.
- RabbitMQ and Azure Service Bus adapters are thin native-driver envelope adapters. Hosts own topology, provisioning, subscriptions, retry, dead-letter policy, prefetch/concurrency, credentials, native monitoring, and deployment topology.
- Service-extraction continuity requires stable message identities, stable handler/subscriber identities, contract assemblies where messages cross module boundaries, inbox/outbox semantics, operation observation, module-owned persistence, and host-owned broker topology.
- Operation observation is not orchestration, saga/process-manager state, request/response durable command execution, or automatic failure inference from broker/outbox/inbox evidence.
- GitHub Issues and Projects track backlog work, real-project findings, cleanup tasks, prioritization, and ownership. Durable requirement or architecture changes must update BMAD artifacts.

### Suggested Trial-Handoff Shape

Use the current docs as source material and keep the handoff short:

- "Start here": `docs/setup.md` for the EF/PostgreSQL modular monolith path.
- "Choose packages": `docs/package-discovery.md` and `docs/packaging.md`.
- "Operate it": `docs/operations.md` and `docs/observability.md`.
- "Verify it": `docs/testing.md`, `pnpm check`, and `pnpm backend:test:integration` for provider-backed sample/trial coverage.
- "Use the proof": `docs/samples.md` and `samples/README.md` for the modular monolith and service-split proof.
- "Track findings": `docs/github-workflow.md` trial issue format and GitHub Project statuses.
- "Know what remains": Epic 8 and sprint status for current readiness, plus created issues for deferred trial findings.

Avoid a long new guide if a concise handoff section with links is enough.

### Previous Story Intelligence

Story 8.4 established:

- `pnpm check` is the default quality gate, `pnpm verify` is only an alias, CI `Quality Gate` runs `pnpm check`, `backend:test:integration` remains explicit provider-backed coverage, and `backend:pack` remains package artifact coverage.
- No runtime, public/protected API, package metadata, package README, XML documentation, baseline, or test classification changes were needed.
- Verification passed with `pnpm format:check`, `pnpm backend:restore`, `pnpm backend:build`, `pnpm backend:test`, `pnpm backend:pack`, and final `pnpm check`.

Story 8.3 established:

- Active docs and agent indexes no longer route to deleted `docs/adr/**`, `docs/architecture/**`, or `docs/plans/**` paths.
- Historical implementation artifacts may mention retired paths as evidence. Do not churn completed story records solely to reduce grep output.

Story 8.2 established:

- `docs/packaging.md` is the central package policy owner.
- `docs/package-discovery.md` is the central package and namespace discovery owner.
- Package READMEs use absolute GitHub URLs for repository docs because they render outside the repo in NuGet/package-manager surfaces.

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

Recent work favors narrow documentation/runtime corrections with targeted verification, then broader gates when the changed surface is wide. Continue that pattern and avoid bundling unrelated runtime, package, release, or public API cleanup into Story 8.5.

### Latest Technical Information

No dependency upgrade is required for this story. Use repository-pinned versions from `global.json`, `package.json`, `Directory.Build.props`, and `Directory.Packages.props`.

- Microsoft documents `dotnet package add` as the CLI command for adding a NuGet package reference after compatibility checks. This can inform trial-package instructions if needed. Source: https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-package-add
- Microsoft documents `dotnet nuget add source` for adding package sources and warns about dependency-confusion risk when multiple sources are configured. If a trial uses local packages from `artifacts/packages`, prefer explicit trusted source guidance. Source: https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-nuget-add-source
- Microsoft NuGet config docs recommend a repository-root `nuget.config` for repeatability when package sources matter. Do not require this unless the trial explicitly needs a local/package-source setup. Source: https://learn.microsoft.com/en-us/nuget/reference/nuget-config-file
- EF Core migrations docs describe CLI tools for creating and applying migrations; Bondstone consumer apps own migration generation and rollout for module `DbContext` models. Source: https://learn.microsoft.com/en-us/ef/core/cli/dotnet
- Microsoft documents `dotnet test --filter` for selected test runs; Bondstone repository scripts use category filters for fast and integration gates. Source: https://learn.microsoft.com/en-us/dotnet/core/testing/selective-unit-tests

### Project Structure Notes

- A new consumer-facing trial handoff doc, if needed, belongs under `docs/` and should be indexed from `docs/README.md`; root `README.md` only needs a link if the handoff becomes a primary entrypoint.
- Do not create long-lived plans under `docs/`. Accepted requirements, architecture, and sequencing belong in BMAD artifacts. Remaining work belongs in GitHub Issues/Projects.
- Sample guidance belongs in `docs/samples.md` or `samples/README.md`; sample source changes belong under `samples/` and must preserve the sample as an adoption proof, not a product app.
- Runtime source changes belong under the owning `src/` package and require nearest `src/**/AGENTS.md` plus package-local tests. This story should avoid runtime source changes unless the handoff inventory exposes a concrete blocker.
- Generated packages under `artifacts/packages`, coverage, temporary sample outputs, and build artifacts should stay out of source.

### Project Context Reference

Project context says Bondstone is a .NET library/framework for durable module boundaries, durable command sending, EF Core backed inbox/outbox persistence, operation observation, and transport adapters. It says EF Core plus PostgreSQL is the supported production durable persistence path, consumers own EF migrations, local transport is explicit local/dev/test infrastructure only, public API changes are compatibility-sensitive, and GitHub Issues/Projects track backlog work and real-project findings.

### Open Questions

None blocking. During implementation, decide whether existing docs already provide a sufficient first-consumer handoff through links, or whether a short new `docs/` handoff page is needed. Default to the smallest discoverable handoff that prevents trial readiness from being overstated.

## Dev Agent Record

### Agent Model Used

{{agent_model_name_version}}

### Debug Log References

- 2026-06-22: Activation resolver failed because local `python3` could not import stdlib `json`; workflow customization was resolved manually from base/team/user TOML fallback. No prepend or append steps configured.
- 2026-06-22: Loaded project context, config, sprint status, create-story workflow assets, checklist, Epic 8, PRD FR3/FR6/FR9/FR10 sections, architecture package/persistence/documentation/verification sections, consumer docs for setup/package discovery/packaging/operations/testing/samples/GitHub workflow, sample indexes, previous Epic 8 story files, recent git history, GitHub open issues and labels, and current external docs for NuGet package/source config, EF migrations, and dotnet test filtering.
- 2026-06-22: Confirmed story key `8-5-consumer-trial-handoff`; `epic-8` is already `in-progress`, and story status should move from `backlog` to `ready-for-dev`.
- 2026-06-22: Confirmed no UX artifact exists for this library handoff story.
- 2026-06-22: Confirmed current GitHub repository has no open issues at story creation time and has labels suitable for trial/deferred tracking.
- 2026-06-22: Dev-story activation resolver failed because local `python3` could not import stdlib `json`; workflow customization was resolved manually from TOML fallback. No prepend or append steps configured.
- 2026-06-22: Read required consumer-trial inventory docs and confirmed sprint status has Epic 8 stories 8.1 through 8.4 done before adding the handoff guidance.
- 2026-06-22: Confirmed no open GitHub issues existed; created trial issue #34 and added it to the Bondstone project with Status `Ready`.
- 2026-06-22: Verification passed: focused handoff/package reference sweeps, `pnpm format:check`, `pnpm check`, and `git diff --check`.

### Completion Notes List

- Ultimate context engine analysis completed - comprehensive developer guide created.
- Story 8.5 is ready for dev with consumer-trial handoff scope, source documents, GitHub tracking guardrails, service-extraction proof requirements, and verification commands.
- Added `docs/consumer-trial-handoff.md` as the focused first-consumer migration handoff route, linking setup, package discovery, packaging, testing, operations, observability, samples, BMAD epics, sprint status, and GitHub tracking.
- Linked the handoff route from `README.md` and `docs/README.md` without changing package IDs, target framework, release metadata, public API baselines, package READMEs, runtime code, or sample source.
- Created GitHub issue #34, `Run first consumer migration trial`, with label `type:trial` and Bondstone project Status `Ready` to track the follow-on real-project migration slice.
- Verified with focused reference sweeps, `pnpm format:check`, full `pnpm check`, and `git diff --check`.

### File List

- `_bmad-output/implementation-artifacts/8-5-consumer-trial-handoff.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `README.md`
- `docs/README.md`
- `docs/consumer-trial-handoff.md`

### Change Log

- 2026-06-22: Created story 8.5 consumer trial handoff context and updated sprint status to `ready-for-dev`.
- 2026-06-22: Implemented consumer trial handoff doc, linked it from README/docs index, created GitHub trial issue #34, and verified with `pnpm check`.

### Story Completion Status

Ready for review.

### References

- `_bmad-output/planning-artifacts/epics.md` - Epic 8 and Story 8.5 acceptance criteria.
- `_bmad-output/planning-artifacts/prds/prd-Bondstone-2026-06-18/prd.md` - FR3.3, FR6.3, FR6.4, FR9.1, FR10.1, FR10.3, and FR10.5.
- `_bmad-output/planning-artifacts/architecture.md` - Product Positioning, Package Architecture, Persistence Architecture, Documentation Ownership, Verification Strategy, and service-extraction review guidance.
- `_bmad-output/project-context.md` - runtime, package, testing, workflow, persistence, and transport guardrails.
- `README.md` - root package, setup, docs, verification, and current direction routes.
- `docs/README.md` - docs ownership map and index.
- `docs/setup.md` - EF/PostgreSQL setup path, package set, host composition, migrations, local transport, broker adapters, and service-extraction setup examples.
- `docs/package-discovery.md` - current package ID and namespace discovery.
- `docs/packaging.md` - package policy, active/removed IDs, versioning, publishing, and migration/release coordination.
- `docs/operations.md` - production operations, EF migrations, receive settlement, operation evidence, retention, and app-owned recovery.
- `docs/testing.md` - verification commands and integration-test routing.
- `docs/samples.md` and `samples/README.md` - modular monolith adoption proof and service-split readiness.
- `docs/github-workflow.md` - GitHub issue/project statuses, trial issue format, labels, and completion comments.
- `_bmad-output/implementation-artifacts/8-1-public-api-and-package-collaboration-validation.md` - public API/package collaboration learnings.
- `_bmad-output/implementation-artifacts/8-2-package-policy-and-discovery-documentation.md` - package policy/discovery learnings.
- `_bmad-output/implementation-artifacts/8-3-documentation-reference-sweep.md` - reference-sweep learnings.
- `_bmad-output/implementation-artifacts/8-4-verification-gate.md` - verification-gate learnings.
