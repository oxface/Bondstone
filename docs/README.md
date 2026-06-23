# Bondstone Documentation

This folder contains consumer-facing documentation, repository-operation
documentation, and durable architecture references. SpecKit owns the
constitution, brownfield project profile, and feature artifacts:

- [SpecKit constitution](../.specify/memory/constitution.md)
- [Bondstone architecture](architecture.md)
- [Bondstone project profile](../.specify/memory/project-profile.md)
- Feature-local specs under `../specs/` when present

## Index

- [github-workflow.md](github-workflow.md) records GitHub issue, project,
  status, label, and issue-body conventions.
- [consumer-trial-handoff.md](consumer-trial-handoff.md) records the first
  consumer migration trial route and tracking handoff.
- [observability.md](observability.md) records current diagnostic surfaces.
- [operations.md](operations.md) records production operations, lifecycle,
  retention, receive, migration, and ownership guidance.
- [packaging.md](packaging.md) records package IDs, target framework, package
  artifact policy, dependency direction, versioning, and publishing policy.
- [package-discovery.md](package-discovery.md) maps common consumer-facing
  capabilities to package IDs and namespaces.
- [architecture.md](architecture.md) records durable runtime architecture and
  package-boundary direction.
- [public-api.md](public-api.md) records public API surface classification used
  during cleanup work.
- [repository.md](repository.md) records repository layout, tooling, and code
  conventions.
- [samples.md](samples.md) records sample application direction.
- [setup.md](setup.md) is the primary user-facing library setup example.
- [testing.md](testing.md) records testing direction, categories, and command
  entrypoints.

## Documentation Model

- SpecKit constitution owns governance and non-negotiable implementation
  principles.
- Bondstone architecture owns internal runtime architecture and durable
  boundary rules.
- Bondstone project profile owns brownfield repository facts used by agents and
  migration specs under `../.specify/memory/project-profile.md`.
- SpecKit feature specs, plans, and tasks own change-scoped intent and
  implementation deltas.
- `/docs` owns library-user and repository-operation guidance.
- GitHub Issues and GitHub Projects track backlog work, real-project findings,
  cleanup tasks, and prioritization.

Prefer references over duplication. If a rule belongs to governance, put it in
the SpecKit constitution. If a rule belongs to architecture, library usage, or
operations, keep it in this folder.

## Document Ownership

- `setup.md` owns library-user setup examples.
- `packaging.md` owns package IDs, dependency direction, target framework,
  package artifact policy, versioning, and publishing policy.
- `package-discovery.md` owns package and namespace discovery guidance.
- `operations.md` owns production operations guidance for receive semantics,
  broker settlement, outbox and inbox inspection, operation finalization and
  expiration, EF migrations, package upgrades, contract evolution, retention,
  and app-owned recovery.
- `observability.md` owns current diagnostic surfaces.
- `github-workflow.md` owns GitHub issue, project, label, and completion
  comment conventions.
- `consumer-trial-handoff.md` owns the first-consumer trial route and links to
  the stable setup, package, operations, testing, sample, SpecKit, and GitHub
  tracking authorities.
- `architecture.md` owns durable runtime architecture and package-boundary
  direction.
- `public-api.md` owns current package public API classification notes.
- `repository.md` owns repository layout, local tooling, CI, and C#
  conventions.
- `testing.md` owns test policy, categories, and command entrypoints.
- `samples.md` owns sample application direction.

## Expanding Docs

Add new `/docs` files for consumer-facing, repository-operation, or architecture
guidance. Add governance changes to the SpecKit constitution, brownfield profile
facts to `.specify/memory/project-profile.md`, and change-scoped intent to
feature specs.
