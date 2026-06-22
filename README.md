# Bondstone

Bondstone is a .NET library for durable module boundaries, durable command
sending, EF Core backed inbox/outbox persistence, operation observation, and
transport adapters.

Bondstone now uses native BMAD planning artifacts as the source of truth for
internal product requirements, runtime architecture, and implementation
sequencing. Consumer-facing package and operations docs remain under `docs/`.

## BMAD Planning

- [PRD](_bmad-output/planning-artifacts/prds/prd-Bondstone-2026-06-18/prd.md)
  owns product requirements, scope, non-goals, and success criteria.
- [Architecture](_bmad-output/planning-artifacts/architecture.md) owns
  internal runtime architecture, package-boundary rules, durable behavior, and
  documentation ownership.
- [Epics](_bmad-output/planning-artifacts/epics.md) owns implementation
  sequencing and story acceptance criteria.
- [Project context](_bmad-output/project-context.md) is the lean
  agent-facing implementation rule set.

## Packages

Current package IDs, dependency direction, target framework, versioning, and
publishing policy are recorded in [docs/packaging.md](docs/packaging.md).

## Getting Started

Start with [docs/setup.md](docs/setup.md) for the normal host setup path. It
shows how to compose modules, PostgreSQL persistence, a direct transport
adapter, and the hosted outbox worker through `AddBondstone`.

Use [docs/consumer-trial-handoff.md](docs/consumer-trial-handoff.md) for the
first consumer migration trial route and follow-up tracking.

Use the package READMEs under [src/](src/) as quick package-purpose guides.
Use the BMAD architecture artifact when you need the durable behavior contract
behind a package.

## Repository Map

- [docs/README.md](docs/README.md) indexes consumer-facing and repository
  operation docs.
- [docs/setup.md](docs/setup.md) is the primary user-facing setup example.
- [docs/package-discovery.md](docs/package-discovery.md) maps capabilities to
  packages and namespaces.
- [docs/consumer-trial-handoff.md](docs/consumer-trial-handoff.md) links the
  first trial path, readiness evidence, and follow-up tracking.
- [docs/packaging.md](docs/packaging.md) records package and release policy.
- [docs/operations.md](docs/operations.md) records production operations
  guidance.
- [docs/observability.md](docs/observability.md) records diagnostic surfaces.
- [docs/public-api.md](docs/public-api.md) records public API classification.
- [docs/repository.md](docs/repository.md) records repository layout and
  tooling.
- [docs/samples.md](docs/samples.md) records sample direction.
- [docs/testing.md](docs/testing.md) records test categories and verification.
- [src/](src/) contains package projects.
- [tests/](tests/) contains package and integration-boundary test projects.
- [samples/](samples/) is reserved for sample applications.

## Verification

Run `pnpm install`, then `pnpm check`.

`pnpm verify` is kept as an alias for `pnpm check`.

The default quality gate runs formatting, restore, build, fast test categories,
and pack. Infrastructure-backed integration tests are intentionally separate.

Pull request titles must follow Conventional Commits because squash merges use
the PR title as the release-relevant commit message.

## Publishing

Release Please manages the central package version in `Directory.Build.props`,
the changelog, release pull request, tag, and GitHub release. NuGet publication
runs from the `Publish NuGet` workflow when a release is published, or manually
through workflow dispatch for the selected ref.

Required repository setup:

- `RELEASE_PLEASE_TOKEN` so Release Please-created releases can trigger the
  separate publish workflow.
- `NUGET_USER` repository variable with the nuget.org username or organization
  profile name used by trusted publishing.
- NuGet trusted publishing policy for `.github/workflows/publish-nuget.yml`.

## Current Direction

Bondstone is built gradually as a durable module-boundary library. Do not
bulk-copy implementation code from the historical template repository or
preserve compatibility with it as a design constraint. Current implementation
work should follow the BMAD PRD, BMAD architecture, BMAD epics, and
project-context while keeping package boundaries, public API shape, tests, and
consumer docs aligned.
