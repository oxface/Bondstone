# 0008 NuGet Release And Publishing

Status: Amended
Application: Partially Applied
Date: 2026-06-04

## Context

Bondstone is a reusable .NET library that will ship multiple NuGet packages
from one repository. The packages should be released together until a later ADR
accepts independent package versioning.

The repository also needs normal GitHub verification and release automation,
but it should not inherit template-factory publishing behavior or application
bootstrap validation.

The first release setup should be boring and reviewable: conventional commits
drive changelog and tags, CI proves the package set can restore, build, test,
and pack, and publishing happens from an explicit GitHub release event.

## Decision

Use coordinated versioning for all Bondstone NuGet packages at first.

Use Release Please in manifest mode to manage the root changelog, release pull
request, GitHub release, and version tag. Use a single root package entry and a
`simple` release type.

Use the central `VersionPrefix` in `Directory.Build.props` as the
source-controlled package version. Release Please's `simple` release type
updates that MSBuild property through its XML extra-file updater. The publish
workflow does not derive a NuGet version from the GitHub release tag.

Publish packages to NuGet from GitHub Actions when a GitHub release is
published. The publish workflow may also be started manually with an explicit
ref for first-publication or recovery use. The workflow should:

- restore the repository through `Bondstone.slnx`;
- build in Release configuration;
- run the default test suite;
- pack the package projects with symbols;
- publish `.nupkg` files to NuGet using a repository secret;
- publish every `.nupkg` produced by the solution pack step;
- not publish `.snupkg` files separately unless NuGet tooling requires it.

Use NuGet trusted publishing for package publication. The workflow requests a
GitHub OIDC token, exchanges it through `NuGet/login@v1` for a short-lived
NuGet API key, and uses that temporary key for `dotnet nuget push`.

Release Please should use `RELEASE_PLEASE_TOKEN` when configured. Use a GitHub
App token or personal access token for that secret when Release Please-created
releases must trigger the separate publish workflow. The workflow may fall back
to the default workflow token, but that fallback is not sufficient for
triggering downstream workflows from the release it creates.

Use centralized package and build metadata in:

- [Directory.Build.props](../../Directory.Build.props)
- [Directory.Packages.props](../../Directory.Packages.props)

Package metadata should be defined centrally when it is shared by every package
and locally only when a package needs a specific description, dependency, or
asset rule.

The root `package.json` is repository tooling only. It is not an npm package
and must stay private.

## Consequences

All initial packages will share one version number. This keeps release
coordination simple while package boundaries are still settling.

Release Please does not decide package contents. The `.csproj` files and
central MSBuild props decide what is packed.

Publishing cannot be fully verified until the NuGet trusted publishing policy,
`NUGET_USER` repository variable, Release Please token, and a real release
exist. CI can still verify restore, build, test, and pack before that.

If consumers later need independent package versions, package-specific release
configuration must be accepted in a later ADR.

## Applied To

- Code:
  - [Directory.Build.props](../../Directory.Build.props)
  - [Directory.Packages.props](../../Directory.Packages.props)
  - [package.json](../../package.json)
  - [pnpm-workspace.yaml](../../pnpm-workspace.yaml)
  - [.github/workflows/verify.yml](../../.github/workflows/verify.yml)
  - [.github/workflows/release-please.yml](../../.github/workflows/release-please.yml)
  - [.github/workflows/publish-nuget.yml](../../.github/workflows/publish-nuget.yml)
  - [.github/release-please-config.json](../../.github/release-please-config.json)
  - [.github/.release-please-manifest.json](../../.github/.release-please-manifest.json)
  - [AGENTS.md](../../AGENTS.md)
- Stable docs:
  - [docs/packaging.md](../packaging.md)
  - [docs/repository.md](../repository.md)
- Agent instructions:
  - [AGENTS.md](../../AGENTS.md)
- Skills: Not applicable.

## Verification

Read back the changed docs and workflow files. Ran `pnpm verify`, which covered
formatting, restore, build, test, and pack. Real NuGet publish verification
remains pending until a release, trusted publishing policy, and required
repository variables exist.
