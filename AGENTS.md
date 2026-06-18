# Agent Index

This repository is the maintenance home for Bondstone, a .NET library for
durable module boundaries, durable command sending, EF Core backed inbox/outbox
persistence, operation observation, and transport adapters.

Start with:

- [README.md](README.md) for package purpose, BMAD artifact routing, and
  verification entrypoints.
- [\_bmad-output/planning-artifacts/prds/prd-Bondstone-2026-06-18/prd.md](_bmad-output/planning-artifacts/prds/prd-Bondstone-2026-06-18/prd.md)
  before changing product scope, requirements, non-goals, or success criteria.
- [\_bmad-output/planning-artifacts/architecture.md](_bmad-output/planning-artifacts/architecture.md)
  before changing runtime architecture, durable messaging, persistence,
  hosting, transport behavior, package boundaries, public API strategy,
  documentation ownership, or verification strategy.
- [\_bmad-output/planning-artifacts/epics.md](_bmad-output/planning-artifacts/epics.md)
  before implementing planned work or creating sprint/story artifacts.
- [\_bmad-output/project-context.md](_bmad-output/project-context.md) for lean
  agent-facing implementation rules.
- [docs/README.md](docs/README.md) for consumer-facing and repository
  operation documentation.
- [docs/testing.md](docs/testing.md) before moving or writing tests.
- [docs/samples.md](docs/samples.md) before adding or changing sample
  applications.
- [docs/github-workflow.md](docs/github-workflow.md) before creating,
  triaging, prioritizing, or completing GitHub Issues or Project items.
- [.agents/skills/AGENTS.md](.agents/skills/AGENTS.md) before adding or
  changing repository agent skills.

For scoped work, prefer the nearest folder `AGENTS.md` as the local index and
follow its references. The repository context-index convention is documented in
[docs/repository.md](docs/repository.md).

## Operating Rules

- Do not vibe-code. Read the relevant docs and implementation first, state the
  intended change, then make the smallest coherent edit.
- Implement gradually. Prefer narrow, reviewable steps over broad rewrites, and
  verify each risky step before expanding scope.
- Do not run git commit and push directly. You can use status and diff commands
  freely.
- Do not overwrite or revert user changes. Work with the current files and ask
  when local edits make the requested change ambiguous.
- Keep generated artifacts, packed packages, temporary sample outputs,
  coverage, and build artifacts out of committed source unless the user
  explicitly asks to preserve them.
- Do not use `InternalsVisibleTo` for production package collaboration.
  Reserve friend assemblies for tests; runtime packages should collaborate
  through explicit contracts or package-local implementation.
- Treat public API cleanup as compatibility-sensitive. Normal setup APIs,
  documented advanced composition APIs, and public implementation types exposed
  for now must be inventoried before broad hiding, renaming, or removal.

## Repository Direction

Repository layout, local tooling, context-index structure, and C# conventions
are documented in [docs/repository.md](docs/repository.md). Package IDs,
target framework, dependency direction, versioning, and publishing are
documented in [docs/packaging.md](docs/packaging.md).

## Architecture Direction

Runtime architecture is owned by
[\_bmad-output/planning-artifacts/architecture.md](_bmad-output/planning-artifacts/architecture.md).
Use that artifact and the nearest scoped `AGENTS.md` before changing
messaging, modules, persistence, hosting, transport behavior, package
boundaries, public API, samples, testing, or repository workflow.

BMAD artifacts are the durable internal source of truth:

- PRD owns requirements and scope.
- Architecture owns runtime and documentation design.
- Epics own sequencing and story acceptance criteria.
- Project context owns lean implementation guardrails.

Consumer-facing docs under `docs/` should explain how to use and operate the
library. They should link to BMAD architecture for internal durable behavior
instead of duplicating architecture rules.

Track backlog work, real-project findings, cleanup tasks, and prioritization
in GitHub Issues or GitHub Projects using the conventions in
[docs/github-workflow.md](docs/github-workflow.md).

## Common Commands

Prefer the repository package scripts when available:

- `pnpm check`
- `pnpm verify`
- `pnpm backend:restore`
- `pnpm backend:build`
- `pnpm backend:test`
- `pnpm backend:test:integration`
- `pnpm backend:pack`

The direct .NET equivalents are:

- `dotnet restore Bondstone.slnx`
- `dotnet build Bondstone.slnx --configuration Release --no-restore`
- `dotnet test Bondstone.slnx --configuration Release --no-build --filter "Category=Unit|Category=Application"`
- `dotnet pack Bondstone.slnx --configuration Release --no-build`

Keep CI, Husky hooks, and docs aligned with the package-script entrypoints.
