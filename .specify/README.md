# SpecKit

This folder contains the source-controlled SpecKit constitution, extension
choices, and repo-local SpecKit indexes for Bondstone. Installed SpecKit
templates, scripts, and extension payloads may exist locally under ignored
paths.

## Index

- [memory/constitution.md](memory/constitution.md) owns project governance and
  non-negotiable implementation principles.
- [../docs/architecture.md](../docs/architecture.md) owns durable internal
  runtime architecture and package-boundary direction.
- [memory/project-profile.md](memory/project-profile.md) records the
  brownfield scan used to seed SpecKit.
- [extensions.yml](extensions.yml) records enabled extensions and hook policy.
- [templates/](templates/) contains source-controlled Bondstone overrides for
  SpecKit feature specs, plans, tasks, and checklists.
- Local ignored SpecKit tooling may include extension caches or generated
  integration state.

Consumer-facing and repository-operation guidance remains in [docs/](../docs/).
GitHub Issues and Projects remain the backlog and prioritization surface.
