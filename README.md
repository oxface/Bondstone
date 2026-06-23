# Bondstone

Bondstone is a .NET library for durable module boundaries, durable command
sending, EF Core backed inbox/outbox persistence, operation observation, and
transport adapters.

Bondstone uses the SpecKit constitution and feature artifacts for governance
and change-scoped implementation intent. Durable architecture, project profile,
consumer-facing package docs, and operations docs live under `docs/`.

## SpecKit Planning

- [Constitution](.specify/memory/constitution.md) owns governance and
  non-negotiable implementation principles.
- [Bondstone architecture](docs/architecture.md) owns internal runtime
  architecture, package-boundary rules, durable behavior, and documentation
  ownership.
- [Bondstone project profile](.specify/memory/project-profile.md) records the
  brownfield scan used to seed SpecKit.
- Feature-local specs under `specs/` own change-scoped implementation intent
  when present.

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
Use the Bondstone architecture when you need the durable behavior contract
behind a package.

## Repository Map

- [docs/README.md](docs/README.md) indexes consumer-facing and repository
  operation docs.
- [.specify/memory/project-profile.md](.specify/memory/project-profile.md)
  records brownfield project facts used by SpecKit workflows.
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
- [.specify/](.specify/) contains the source-controlled SpecKit constitution,
  extension choices, and local routing indexes. Installed SpecKit templates,
  scripts, and extension payloads are local tooling.

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
work should follow the SpecKit constitution, Bondstone architecture, feature
artifacts, and stable docs while keeping package boundaries, public API shape,
tests, and consumer docs aligned.
