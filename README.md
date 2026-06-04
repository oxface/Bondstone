# Bondstone

Bondstone is a .NET library for durable module boundaries, in-process command
handling, EF Core backed inbox/outbox persistence, and transport adapters.

The repository is currently in early extraction shape: documentation, ADRs,
repo tooling, package projects, test projects, CI, and NuGet release plumbing
exist; runtime implementation is still being moved slowly from the historical
template repository.

## Packages

The initial package set is:

- `Bondstone`
- `Bondstone.EntityFrameworkCore`
- `Bondstone.EntityFrameworkCore.Postgres`
- `Bondstone.Transport.Rebus`

All packages initially target `net10.0` and share one coordinated version.

## Repository Map

- [docs/README.md](docs/README.md) is the durable documentation index.
- [docs/adr/README.md](docs/adr/README.md) explains the ADR workflow.
- [docs/architecture.md](docs/architecture.md) records runtime positioning.
- [docs/extraction.md](docs/extraction.md) records the slow extraction plan.
- [docs/packaging.md](docs/packaging.md) records package and release policy.
- [docs/testing.md](docs/testing.md) records test categories and verification.
- [src/](src/) contains package projects.
- [tests/](tests/) contains package and integration-boundary test projects.
- [samples/](samples/) is reserved for sample applications.

## Verification

```sh
pnpm install
pnpm check
```

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

Bondstone is extracted gradually. Do not bulk-copy implementation code from the
template repository. Each extraction slice should review package boundaries,
public API shape, tests, docs, and service-extraction pressure.
