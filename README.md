# Bondstone

Bondstone is a .NET library for durable module boundaries, durable command
sending, EF Core backed inbox/outbox persistence, and transport adapters.

Stable docs describe the current package, architecture, setup, repository,
sample, and verification contracts. Backlog docs hold ad hoc planning notes
and active issue notes that are not current operating guidance.

## Packages

Current package IDs, dependency direction, target framework, versioning, and
publishing policy are recorded in [docs/packaging.md](docs/packaging.md).

## Getting Started

Start with [docs/setup.md](docs/setup.md) for the normal host setup path. It
shows how to compose modules, PostgreSQL persistence, a direct transport
adapter, and the hosted outbox worker through `AddBondstone`.

Use the package READMEs under [src/](src/) as quick package-purpose guides.
Use architecture docs when you need the durable behavior contract behind a
package.

## Repository Map

- [docs/README.md](docs/README.md) is the durable documentation index.
- [docs/adr/README.md](docs/adr/README.md) explains the ADR workflow.
- [docs/architecture/README.md](docs/architecture/README.md) records runtime
  positioning.
- [docs/setup.md](docs/setup.md) is the single user-facing setup example.
- [docs/backlog/README.md](docs/backlog/README.md) explains ad hoc planning
  notes and active issue extraction.
- [docs/packaging.md](docs/packaging.md) records package and release policy.
- [docs/repository.md](docs/repository.md) records repository layout and
  tooling.
- [docs/samples.md](docs/samples.md) records sample direction.
- [docs/testing.md](docs/testing.md) records test categories and verification.
- [docs/archive/README.md](docs/archive/README.md) preserves historical
  planning documents that should not steer new work.
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
work should follow the stable docs, check ADR requirements before broad
technical decisions, and keep package boundaries, public API shape, tests,
docs, and service-split pressure visible.
