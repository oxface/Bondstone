# 0002 Initial Repository And Package Structure

Status: Accepted
Application: Partially Applied
Date: 2026-06-03

## Context

Bondstone currently has an empty repository shell and source code still lives
outside this repository. Before moving code, the repository needs an agreed
layout for source, tests, samples, docs, automation, local tooling, and package
metadata.

The repository should feel ordinary for .NET library maintenance. It should not
inherit validation flows that only make sense for application bootstrapping or
browser applications. At the same time, Bondstone still needs root automation,
commit discipline, centralized .NET build configuration, and room for samples
that exercise realistic modular monolith and service-split scenarios.

## Decision

Use this initial repository shape:

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

The first source projects are expected to live under `src/`:

- `Bondstone`
- `Bondstone.EntityFrameworkCore`
- `Bondstone.EntityFrameworkCore.Postgres`
- `Bondstone.Transport.Rebus`

Tests should live under `tests/`, grouped by package or integration boundary.
Tests extracted from the previous root framework test project should be moved
or rewritten into neutral Bondstone test fixtures instead of depending on a
separate product module.

Samples should live under `samples/` and should be used to validate realistic
library usage, especially modular monolith operation and service extraction
paths. Samples are not core package code and must not introduce domain behavior
into the library packages.

Use root `package.json`, pnpm, Husky, commitlint, Prettier, and lightweight
script orchestration for repository hygiene. Do not add frontend/browser tooling
such as Playwright unless an accepted sample or documentation ADR creates a
real need for it. Exclude release-generated changelog output from Prettier.

Require semantic pull request titles using Conventional Commits. Squash merges
should use the PR title as the release-relevant commit message, so the PR title
is the durable metadata to validate.

Use centralized .NET configuration through `Directory.Build.props` and
`Directory.Packages.props`.

Use GitHub Actions for normal library verification and release automation. The
repository should not include application bootstrap validation.

Use a devcontainer for .NET library maintenance with Node/pnpm and useful shell
tools. Avoid frontend-only devcontainer features unless samples later require
them.

## Consequences

The repository can be scaffolded before source is moved, giving code extraction
a stable destination.

Root pnpm remains useful for hooks and scripted workflows even without a
frontend.

Semantic PR title validation makes squash-merge history and Release Please
behavior more predictable, but branch protection or a ruleset must require the
check for it to block merging.

Samples are available as a deliberate validation surface, but they do not force
browser tooling into the baseline repo.

This ADR intentionally does not decide NuGet release details or exact sample
project names. CI command names are now established through the initial
package scripts.

## Applied To

- Code:
  - [Bondstone.slnx](../../Bondstone.slnx)
  - [Directory.Build.props](../../Directory.Build.props)
  - [Directory.Packages.props](../../Directory.Packages.props)
  - [global.json](../../global.json)
  - [package.json](../../package.json)
  - [pnpm-workspace.yaml](../../pnpm-workspace.yaml)
  - [.prettierignore](../../.prettierignore)
  - [.devcontainer/devcontainer.json](../../.devcontainer/devcontainer.json)
  - [.github/workflows/verify.yml](../../.github/workflows/verify.yml)
  - [.github/workflows/semantic-pr-title.yml](../../.github/workflows/semantic-pr-title.yml)
  - initial `src/` and `tests/` project files
  - [samples/README.md](../../samples/README.md)
- Stable docs:
  - [docs/repository.md](../repository.md)
  - [docs/README.md](../README.md)
- Agent instructions:
  - [AGENTS.md](../../AGENTS.md)
- Skills: Not applicable.

## Verification

Read back [docs/repository.md](../repository.md),
[docs/README.md](../README.md), and [AGENTS.md](../../AGENTS.md). Ran
`pnpm verify`, which covered formatting, restore, build, test, and pack for the
scaffold. Real source extraction, sample projects, and infrastructure-backed
integration checks remain pending.
