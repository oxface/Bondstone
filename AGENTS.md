# Agent Index

This repository is the maintenance home for Bondstone, a .NET library for
durable module boundaries, durable command sending, EF Core backed inbox/outbox
persistence, and transport adapters.

Start with:

- [README.md](README.md) when it exists for package purpose, install examples,
  and verification entrypoints.
- [docs/README.md](docs/README.md) when it exists for durable developer and
  architecture documentation.
- [docs/adr/README.md](docs/adr/README.md) when it exists before creating,
  updating, superseding, or archiving ADRs.
- [docs/architecture/README.md](docs/architecture/README.md) before changing
  runtime architecture, durable messaging, persistence, hosting, or transport
  behavior.
- [docs/backlog/README.md](docs/backlog/README.md) before choosing or
  continuing a backlog campaign or future-work item.
- [docs/testing.md](docs/testing.md) before moving or writing tests.
- [docs/samples.md](docs/samples.md) before adding or changing sample
  applications.
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
- Keep generated artifacts, packed packages, temporary sample outputs, coverage,
  and build artifacts out of committed source unless the user explicitly asks to
  preserve them.
- Do not use `InternalsVisibleTo` for production package collaboration. Reserve
  friend assemblies for tests; runtime packages should collaborate through
  explicit contracts or package-local implementation.

## Repository Direction

Repository layout, local tooling, context-index structure, and C# conventions
are documented in [docs/repository.md](docs/repository.md). Package IDs,
target framework, dependency direction, versioning, and publishing are
documented in [docs/packaging.md](docs/packaging.md).

## Architecture Direction

Runtime architecture is indexed from
[docs/architecture/README.md](docs/architecture/README.md). Use that index and
the nearest scoped `AGENTS.md` before changing messaging, modules,
persistence, hosting, or transport behavior.

Durable behavior, public API shape, package boundaries, provider or transport
support, migration strategy, compatibility policy, release/publishing, sample
architecture, repository workflow, and agent harness behavior require ADR
review before broad implementation.

## ADR And Planning Rules

- Do not use SDD/spec-driven product workflow for Bondstone library work unless
  an accepted ADR introduces it for a bounded purpose.
- Before editing code or automation, check whether ADR work is required when a
  change affects public API, package boundaries, target frameworks, provider or
  transport support, migration policy, compatibility, release/publishing,
  sample architecture, repository workflow, or agent harness behavior.
- Use ADRs for durable technical decisions. Prefer small ADRs with a clear
  status, application state, context, decision, consequences, and links to
  applied docs.
- Treat accepted ADR decision content as append-only. Do not rewrite accepted
  `Context`, `Decision`, or `Consequences` except for mechanical fixes; add a
  dated amendment for compatible clarification, or supersede when the decision
  changes.
- Updating, superseding, archiving, or removing an ADR must preserve the
  decision trail and update the stable docs and agent instructions that depend
  on it.
- Reusable ADR workflows must become repository skills under `.agents/skills`.
  Start with skills for creating ADRs, updating ADRs, superseding ADRs, and
  archiving/removing ADRs.

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
