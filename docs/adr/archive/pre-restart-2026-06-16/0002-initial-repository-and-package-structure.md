# 0002 Initial Repository And Package Structure

Status: Archived
Application: Not Applicable
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

## Application Notes

- Current contract: Bondstone uses a library-maintenance repository layout
  with source under `src/`, tests under `tests/`, docs under `docs/`, samples
  under `samples/`, and lightweight root tooling.
- Stable docs: Current repository layout and tooling rules are described in
  [docs/repository.md](../repository.md) and indexed from
  [docs/README.md](../README.md).
- Agent guidance: Root [AGENTS.md](../../AGENTS.md) points agents to the
  repository and extraction docs before code or automation changes.
- Application evidence: The repository shell, solution, package projects, test
  projects, sample projects, devcontainer, package scripts, and GitHub
  automation are scaffolded and reflected in stable docs.
- Pending or deferred: None for this repository-structure decision. Current
  post-MVP review campaigns are tracked under
  [docs/backlog](../backlog/README.md).

## Verification

Read back [docs/repository.md](../repository.md),
[docs/README.md](../README.md), and [AGENTS.md](../../AGENTS.md). Ran
`pnpm verify`, which covered formatting, restore, build, test, and pack for the
scaffold. Phase 01 audit verification rechecked repository layout, package
projects, sample projects, docs, and agent instructions against the current
tree.
