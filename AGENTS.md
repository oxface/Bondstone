# Agent Index

This repository is the maintenance home for Bondstone, a .NET library for
durable module boundaries, durable command sending, EF Core backed inbox/outbox
persistence, operation observation, and transport adapters.

Start with:

- [README.md](README.md) for package purpose, SpecKit artifact routing, and
  verification entrypoints.
- [.specify/memory/constitution.md](.specify/memory/constitution.md) before
  changing governance, compatibility, package-boundary, or verification rules.
- [docs/architecture.md](docs/architecture.md)
  before changing runtime architecture, durable messaging, persistence,
  hosting, transport behavior, package boundaries, public API strategy,
  documentation ownership, or verification strategy.
- [docs/project-profile.md](docs/project-profile.md)
  before changing brownfield assumptions.
- [.specify/README.md](.specify/README.md) before changing the SpecKit
  constitution, extension choices, or local SpecKit routing.
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
[docs/architecture.md](docs/architecture.md).
Use that artifact and the nearest scoped `AGENTS.md` before changing
messaging, modules, persistence, hosting, transport behavior, package
boundaries, public API, samples, testing, or repository workflow.

SpecKit constitution, `/docs`, and feature artifacts are the durable internal
source of truth:

- Constitution owns governance and non-negotiable implementation principles.
- Bondstone architecture owns runtime and documentation design.
- Feature specs, plans, and tasks own change-scoped implementation deltas.
- Bondstone project profile records brownfield assumptions used by agents.

Consumer-facing docs under `docs/` should explain how to use and operate the
library. They should link to Bondstone architecture for internal durable
behavior instead of duplicating architecture rules.

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

<!-- SPECKIT START -->

For additional context about technologies to be used, project structure,
shell commands, and other important information, read the current plan

<!-- SPECKIT END -->
