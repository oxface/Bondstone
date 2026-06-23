# SpecKit Agent Index

This folder contains the source-controlled SpecKit constitution, extension
choices, and repo-local context indexes. Installed SpecKit templates, scripts,
and extension payloads may exist locally under ignored paths.

Start with:

- [README.md](README.md) for the folder map.
- [memory/constitution.md](memory/constitution.md) before changing governance,
  compatibility, package-boundary, or verification rules.
- [../docs/architecture.md](../docs/architecture.md) before changing runtime
  architecture, durable messaging, persistence, hosting, transport behavior,
  package boundaries, public API strategy, documentation ownership, or
  verification strategy.
- [../docs/project-profile.md](../docs/project-profile.md) before changing
  brownfield assumptions.
- [../docs/README.md](../docs/README.md) for consumer-facing and
  repository-operation documentation ownership.
- [../docs/repository.md](../docs/repository.md) for the reference-based
  context-index convention.

Keep durable project context in the SpecKit constitution, `/docs`, package
READMEs, and scoped `AGENTS.md` files. Treat ignored
templates/scripts/extensions as local SpecKit tooling, not as durable
repository documentation.
