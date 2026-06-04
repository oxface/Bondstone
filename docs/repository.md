# Repository

This document records the current repository structure direction for
Bondstone.

## Layout

Bondstone uses a library-maintenance repository layout:

```text
.agents/
  skills/
.config/
.devcontainer/
.github/
  workflows/
docs/
  adr/
samples/
src/
tests/
AGENTS.md
Bondstone.slnx
Directory.Build.props
Directory.Packages.props
global.json
package.json
pnpm-workspace.yaml
README.md
```

The initial source projects are:

- `src/Bondstone`
- `src/Bondstone.EntityFrameworkCore`
- `src/Bondstone.EntityFrameworkCore.Postgres`
- `src/Bondstone.Transport.Rebus`

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

## Application State

This structure is accepted and partially scaffolded. Root tooling, initial
source projects, initial test projects, solution structure, devcontainer, and
GitHub automation exist. Samples, real package implementation, and richer
integration-test infrastructure remain future application work.
