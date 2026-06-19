# Repository

This document records the current repository structure direction for
Bondstone.

## Layout

Bondstone uses a library-maintenance repository layout with repository
automation at the root, BMAD planning artifacts under `_bmad-output/`,
consumer docs under `docs/`, samples under `samples/`, package projects under
`src/`, tests under `tests/`, and repository agent skills under
`.agents/skills/`. Current package projects and dependency direction are
recorded in [packaging.md](packaging.md).

Tests live under `tests/`, grouped by package or integration boundary. Tests
extracted from the previous root framework test project should be moved or
rewritten into neutral Bondstone fixtures instead of depending on a separate
product module.

Samples live under `samples/` and validate realistic library usage. Samples
are not core package code and must not introduce domain behavior into library
packages.

## Reference-Based Context Structure

Bondstone uses local context indexes so humans and agents can start from the
smallest useful folder context and follow references only when needed.

Significant folders should have:

- `AGENTS.md` for agents working in that folder;
- `README.md` for humans navigating that folder.

These files are indexes, not alternate architecture docs. Keep them short,
name the folder's scope, and reference BMAD artifacts, consumer docs, sibling
packages, tests, and verification entrypoints that matter for that area. Avoid
copying durable rules from architecture, packaging, testing, or sample docs
unless the local rule is unusually important and repeated agent misses justify
duplication.

When adding a package, test boundary, sample, docs area, or agent workflow
folder, add the local indexes in the same change or explain why an existing
parent index is enough.

## Work Tracking

BMAD artifacts describe durable requirements, architecture, and implementation
sequence. Repository docs describe consumer-facing behavior and repository
operations. Use GitHub Issues and GitHub Projects for backlog work,
real-project findings, cleanup tasks, prioritization, and ownership. When an
issue changes durable requirements, architecture, or sequencing, update the
appropriate BMAD artifact. When an issue changes consumer-facing behavior,
update docs in the same change.
Issue formats, project statuses, label conventions, and completion comments
are recorded in [github-workflow.md](github-workflow.md).

Do not keep long-lived transitional plans under `docs/`. Once a plan is
accepted or implemented, move durable requirements, architecture, and sequence
into BMAD artifacts and remaining work into GitHub Issues or Projects.

Runtime implementation stories that change messaging, persistence, hosting,
transport, or package APIs must record review evidence for the product
boundary they preserve. The evidence should explain how the change keeps
durable module boundaries, service-extraction contracts, and handler patterns
intact, keeps module durability module-owned, and leaves broker topology,
provisioning, credentials, native retry, dead-letter policy,
prefetch/concurrency, and monitoring host-owned. Do not describe runtime work
as making Bondstone a generic bus, workflow engine, code generator, SaaS
framework, application platform, or broker runtime owner.

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
sample direction or BMAD architecture update creates a real need for it.

Use Release Please for release pull requests, changelog updates, GitHub
releases, and tags. Use GitHub Actions for package verification and NuGet
publishing from release events or explicit manual dispatch.

## C# Conventions

Use `ct` as the standard parameter and local variable name for
`CancellationToken` values in Bondstone C# source and tests. This convention
applies to public APIs as well as implementations because C# parameter names
are visible to named-argument callers.

Keep this document focused on repository layout, local tooling, CI, and
release automation.
