# Bondstone

Bondstone is a .NET library for durable module boundaries, durable command
sending, EF Core backed inbox/outbox persistence, and transport adapters.

The repository is in active MVP productization: documentation, ADRs, repo
tooling, package projects, test projects, CI, NuGet release plumbing, and
several runtime slices exist; remaining runtime implementation is tracked in
the MVP plan.

## Packages

Current package IDs, dependency direction, target framework, versioning, and
publishing policy are recorded in [docs/packaging.md](docs/packaging.md).

## Repository Map

- [docs/README.md](docs/README.md) is the durable documentation index.
- [docs/adr/README.md](docs/adr/README.md) explains the ADR workflow.
- [docs/architecture/README.md](docs/architecture/README.md) records runtime
  positioning.
- [docs/setup.md](docs/setup.md) is the single user-facing setup example.
- [docs/mvp-plan.md](docs/mvp-plan.md) tracks implemented surface, priority
  groups, current slice, and deferred MVP work.
- [docs/packaging.md](docs/packaging.md) records package and release policy.
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

Bondstone is built gradually toward a useful library MVP. Do not bulk-copy
implementation code from the historical template repository or preserve
compatibility with it as a design constraint. Each slice should review package
boundaries, public API shape, tests, docs, and service-split pressure.
