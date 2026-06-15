# 0055 Package README And XML Documentation Artifacts

Status: Accepted
Application: Applied
Date: 2026-06-14

## Context

Bondstone packages are consumed from NuGet and package-manager UI surfaces where
repository-relative links such as `docs/setup.md`, `docs/packaging.md`, and
`src/` do not resolve. The repository already keeps package-specific README
files under each packable project, but shared package metadata packed the
repository root README into every package.

Package consumers also need IntelliSense/API-browser help for the main setup,
module, durable messaging, local transport, and EF Core persistence entrypoints
without requiring a local source checkout. Enabling XML documentation for all
packable projects currently exposes a large historical public surface with many
missing-comment warnings. Issue #24 scopes the first documentation pass to the
main consumer-facing APIs rather than every public type.

## Decision

Packable Bondstone projects must package their project-local `README.md` as the
NuGet readme. Package READMEs should describe the specific package purpose,
when to install it, and should use absolute GitHub links for repository docs,
source paths, and tests so links work outside a cloned repository.

Packable Bondstone projects must generate XML documentation files and include
them in package artifacts. XML comments should cover the main consumer-facing
entrypoints first: `AddBondstone`, module contracts, result command contracts,
durable command sending/result reading, local transport setup, and EF Core
persistence setup.

The initial XML documentation pass intentionally suppresses compiler missing
comment warning `CS1591` for packable projects. That warning is suppressed only
because the current public surface includes advanced composition and exposed
implementation types that are not part of this issue's first consumer
documentation slice. Future documentation cleanup can remove or narrow the
suppression when API documentation coverage becomes comprehensive.

## Consequences

NuGet package pages and package-manager UI should show package-specific
guidance instead of the repository root overview.

Package README links stay useful from NuGet because they point to GitHub URLs
instead of repository-relative files.

Consumers get XML documentation for the primary setup and messaging APIs while
the repository avoids a noisy all-public-types documentation rewrite in this
slice.

The package artifact policy changes, but package IDs, package boundaries,
target frameworks, public signatures, and compatibility shape do not change.

## Related Decisions

- [0003 Package Boundaries And Target Framework](0003-package-boundaries-and-target-framework.md)
- [0046 Public API Surface Policy](0046-public-api-surface-policy.md)
- [0051 Package Boundary Split](0051-package-boundary-split.md)
- [0053 GitHub-Tracked Work And Current Docs](0053-github-tracked-work-and-current-docs.md)

## Application Notes

- Current contract: packable package projects ship project-local package
  READMEs and XML documentation files. README links that reference repository
  docs, source, or tests should be absolute GitHub links.
- Stable docs: package artifact policy is recorded in
  [docs/packaging.md](../packaging.md). Package/package-API discovery remains
  in [docs/package-discovery.md](../package-discovery.md) and
  [docs/public-api.md](../public-api.md).
- Agent guidance: root [AGENTS.md](../../AGENTS.md) and
  [docs/adr/README.md](README.md) already require ADR review for package
  publishing/documentation policy and public API documentation changes.
- Application evidence: shared MSBuild package metadata uses each packable
  project README, packable projects generate XML documentation files, package
  READMEs use absolute GitHub links, and focused XML comments cover the
  consumer-facing entrypoints from issue #24.
- Pending or deferred: comprehensive XML documentation for every public type is
  deferred.

## Verification

Verified this applied decision with:

- `git diff --check`
- `pnpm format:check`
- `pnpm backend:build`
- `pnpm backend:pack`
- direct inspection of generated `1.0.1-local` package artifacts confirmed
  package-specific `README.md` files and `lib/net10.0/*.xml` documentation
  files are included for each current packable package.
