# Repository

This document records the current repository structure direction for
Bondstone.

## Layout

Bondstone uses a library-maintenance repository layout with repository
automation at the root, stable docs under `docs/`, deferred samples under
`samples/`, package projects under `src/`, tests under `tests/`, and
repository agent skills under `.agents/skills/`. Current package projects and
dependency direction are recorded in [packaging.md](packaging.md).

Tests live under `tests/`, grouped by package or integration boundary. Tests
extracted from the previous root framework test project should be moved or
rewritten into neutral Bondstone fixtures instead of depending on a separate
product module.

Samples live under `samples/` and validate realistic library usage. Samples
are not core package code and must not introduce domain behavior into library
packages.

## Tooling

Use root `package.json`, pnpm, Husky, commitlint, Prettier, and lightweight
script orchestration for repository hygiene. The pre-commit hook runs Prettier
and re-stages formatted files. It does not run tests. Release-generated files
such as `CHANGELOG.md` are excluded from Prettier.

Pull request titles must follow Conventional Commits. This supports squash
merging, Release Please, and readable release history even when individual
branch commits are noisy.

Use centralized .NET configuration through:

- `Directory.Build.props`
- `Directory.Packages.props`

Use GitHub Actions for normal library CI and release automation. The default CI
workflow is the repository quality gate. Do not add application bootstrap
validation.

Configure the GitHub branch ruleset to require these status checks before
merge:

- `Quality Gate`
- `Semantic PR Title`

Use a devcontainer for .NET library maintenance with Node/pnpm and useful shell
tools. Avoid frontend-only devcontainer features unless samples later require
them.

Do not add frontend/browser tooling such as Playwright unless an accepted
sample or documentation ADR creates a real need for it.

Use Release Please for release pull requests, changelog updates, GitHub
releases, and tags. Use GitHub Actions for package verification and NuGet
publishing from release events or explicit manual dispatch.

## C# Conventions

Use `ct` as the standard parameter and local variable name for
`CancellationToken` values in Bondstone C# source and tests. This convention
applies to public APIs as well as implementations because C# parameter names
are visible to named-argument callers.

## Current Status

This structure is accepted. Current package implementation and verification
state is summarized in [status.md](status.md). Keep this document focused on
repository layout, local tooling, CI, and release automation.
