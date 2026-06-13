# 0008 NuGet Release And Publishing

Status: Amended
Application: Applied
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
- publish `.nupkg` files to NuGet using a short-lived key from trusted
  publishing;
- publish every `.nupkg` produced by the solution pack step;
- not publish `.snupkg` files separately unless NuGet tooling requires it.

Use NuGet trusted publishing for package publication. The workflow requests a
GitHub OIDC token, exchanges it through `NuGet/login@v1` for a short-lived
NuGet API key, and uses that temporary key for `dotnet nuget push`.

Release Please must use `RELEASE_PLEASE_TOKEN`. Use a GitHub App token or
personal access token for that secret because Release Please-created releases
must trigger the separate publish workflow. Do not fall back to GitHub's
default workflow token; it can create the release without triggering downstream
workflows from that release.

Use centralized package and build metadata in:

- [Directory.Build.props](../../Directory.Build.props)
- [Directory.Packages.props](../../Directory.Packages.props)

Package metadata should be defined centrally when it is shared by every package
and locally only when a package needs a specific description, dependency, or
asset rule.

The root `package.json` is repository tooling only. It is not an npm package
and must stay private.

Publish to nuget.org only. Do not publish to GitHub Packages unless a later ADR
accepts a separate need for GitHub Packages visibility or private/internal
package flows.

## Consequences

All initial packages will share one version number. This keeps release
coordination simple while package boundaries are still settling.

Release Please does not decide package contents. The `.csproj` files and
central MSBuild props decide what is packed.

Publishing to nuget.org has been verified through the trusted-publishing
workflow. CI continues to verify restore, build, test, and pack before release
publication.

If consumers later need independent package versions, package-specific release
configuration must be accepted in a later ADR.

## Application Notes

- Current contract: Release Please owns coordinated package versioning through
  central MSBuild metadata, and NuGet publishing targets nuget.org through
  trusted publishing from GitHub release events.
- Stable docs: Current versioning, package metadata, and publishing rules are
  described in [docs/packaging.md](../packaging.md), with workflow context in
  [docs/repository.md](../repository.md).
- Agent guidance: Root [AGENTS.md](../../AGENTS.md) points agents to packaging
  docs before release or publishing changes.
- Application evidence: CI, Release Please, and NuGet publish workflow are
  configured, and real nuget.org publication has succeeded.
- Pending or deferred: Independent package versioning and GitHub Packages
  publication remain deferred unless a later ADR accepts them.

## Verification

Read back the changed docs and workflow files. Ran `pnpm verify`, which covered
formatting, restore, build, test, and pack. Verified NuGet publication through
the trusted-publishing workflow.
