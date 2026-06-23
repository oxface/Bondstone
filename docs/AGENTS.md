# Documentation Agent Index

This folder contains consumer-facing and repository-operation docs.

Start with:

- [README.md](README.md) for the documentation model and ownership map.
- [architecture.md](architecture.md)
  before changing internal runtime architecture or durable behavior rules.
- [../.specify/memory/constitution.md](../.specify/memory/constitution.md)
  before changing governance, compatibility, package-boundary, or verification
  rules.
- [github-workflow.md](github-workflow.md) before changing GitHub issue,
  project, label, or completion-comment conventions.
- [repository.md](repository.md) before changing repository layout, tooling, or
  context-index structure.

Keep `/docs` focused on library-user guidance, repository-operation guidance,
durable architecture, and project profile references. Put governance in the
SpecKit constitution and implementation sequencing in feature specs, then link
to those artifacts when a consumer doc needs context.
