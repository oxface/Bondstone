# Packaging

This document records the current Bondstone package boundary and target
framework direction.

## Target Framework

Bondstone targets `net10.0` for the initial extraction.

Wider target framework support is deferred until a later compatibility ADR
evaluates demand and maintenance cost.

## Initial Packages

Ship one NuGet package per initial project. Package IDs match project names:

- `Bondstone`
- `Bondstone.EntityFrameworkCore`
- `Bondstone.EntityFrameworkCore.Postgres`
- `Bondstone.Transport.Rebus`

## Package Dependencies

Use this dependency direction:

```text
Bondstone
Bondstone.EntityFrameworkCore -> Bondstone
Bondstone.EntityFrameworkCore.Postgres -> Bondstone.EntityFrameworkCore
Bondstone.Transport.Rebus -> Bondstone
```

Provider-specific packages contain provider-specific behavior only.
Transport-specific packages contain transport adapter behavior only.
Core domain, command, messaging, and module abstractions stay in `Bondstone`
unless a later ADR defines a narrower package split.

## Version And Dependency Management

Use central package management through `Directory.Packages.props`.

Use coordinated versioning for the initial package set. Release Please manages
the root changelog, release pull request, GitHub release, and version tag.

Release Please uses the `simple` release type and updates the central
`VersionPrefix` in `Directory.Build.props`. `dotnet pack` uses that
source-controlled MSBuild version without translating the GitHub tag in the
publish workflow.

The NuGet publish workflow runs when a GitHub release is published. It also
supports manual dispatch for recovery or first-publication use; manual dispatch
publishes the version recorded in `Directory.Build.props` at the selected ref.

The publish workflow packs `Bondstone.slnx` and publishes every `.nupkg`
created under `artifacts/packages`. Any future packable package project that is
included in the solution will be covered by the same publish loop.

Shared package metadata belongs in `Directory.Build.props`. Project-specific
descriptions, dependencies, and package assets belong in the package project
file.

Package artifacts should include symbols and the root package README.
Publishing uses GitHub Actions with NuGet trusted publishing. The workflow uses
GitHub OIDC through `NuGet/login@v1` to obtain a short-lived NuGet API key
instead of storing a long-lived `NUGET_API_KEY` secret.

The repository publishes to nuget.org only. GitHub Packages is not a current
target because nuget.org is the normal public .NET package registry and avoids
GitHub Packages authentication/source setup for consumers.

The repository configures a NuGet trusted publishing policy for
`.github/workflows/publish-nuget.yml`. The workflow uses `NUGET_USER` as a
GitHub repository variable containing the nuget.org username or organization
profile name used for the trusted publishing policy.

Configure `RELEASE_PLEASE_TOKEN` with a GitHub App token or personal access
token. This token is required because Release Please-created releases must
trigger the separate publish workflow. GitHub's default workflow token can
create the release without triggering downstream workflows.

Real publish verification has succeeded. Published packages appear on
nuget.org, not GitHub Packages.

Independent package versioning is deferred until a later ADR accepts the need
and release-management cost.

## Application State

The package boundary, target framework, coordinated versioning, and release
automation direction are accepted and scaffolded. Initial packages have been
published to nuget.org. `Bondstone` contains initial core messaging and
persistence contracts without Microsoft.Extensions package dependencies, and
`Bondstone.EntityFrameworkCore` contains initial provider-neutral persistence
entity mappings, outbox writer, inbox store, and operation state store.
`Bondstone.EntityFrameworkCore.Postgres` has started with PostgreSQL
dependencies, provider-specific registration and constraint/unique-violation
classification helpers, PostgreSQL outbox claiming, and Testcontainers-backed
integration tests. Broader PostgreSQL provider behavior, Rebus transport
behavior, additional integration tests, and samples remain future package
implementation work.
