# Packaging

This document records the current Bondstone package boundary and target
framework direction.

## Target Framework

Bondstone targets `net10.0` for the initial extraction.

Wider target framework support is deferred until a later compatibility ADR
evaluates demand and maintenance cost.

## Package Set

Ship one NuGet package per package project. Package IDs match project names:

- `Bondstone`
- `Bondstone.Hosting`
- `Bondstone.EntityFrameworkCore`
- `Bondstone.EntityFrameworkCore.Postgres`
- `Bondstone.Transport.Rebus`

## Package Dependencies

Use this dependency direction:

- `Bondstone` stays at the core with no dependency on provider, transport, or
  hosting packages.
- `Bondstone.Hosting` depends on `Bondstone`.
- `Bondstone.EntityFrameworkCore` depends on `Bondstone`.
- `Bondstone.EntityFrameworkCore.Postgres` depends on
  `Bondstone.EntityFrameworkCore` and may reference `Bondstone` directly for
  shared builder extension methods.
- `Bondstone.Transport.Rebus` depends on `Bondstone`.

Provider-specific packages contain provider-specific behavior only. A provider
package may also reference `Bondstone` directly for shared builder extension
methods when its normal provider dependency already reaches core transitively.
Transport-specific packages contain transport adapter behavior only.
Reusable hosted worker composition belongs in `Bondstone.Hosting`.
Core domain, command, messaging, and module abstractions stay in `Bondstone`
unless a later ADR defines a narrower package split.

`Bondstone` owns the lightweight `AddBondstone` service-composition builder.
Provider, transport, and hosting packages add extension methods to that
builder while preserving the dependency direction above. The builder is a
guardrail for common host setup, not a replacement for lower-level
registration methods needed by advanced consumers and tests.

## Version And Dependency Management

Use central package management through `Directory.Packages.props`.

Use coordinated versioning for the current package set. Release Please manages
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

## Current Status

The package boundary, target framework, coordinated versioning, and release
automation direction are accepted and scaffolded. Initial packages have been
published to nuget.org. Current package implementation state is summarized in
[status.md](status.md). Keep this document focused on package boundaries,
dependency direction, versioning, and publishing rules.
