# Bondstone Documentation

This folder contains consumer-facing and repository-operation documentation.
Internal product requirements, runtime architecture, implementation sequencing,
and lean agent guardrails are owned by BMAD artifacts:

- [PRD](../_bmad-output/planning-artifacts/prds/prd-Bondstone-2026-06-18/prd.md)
- [Architecture](../_bmad-output/planning-artifacts/architecture.md)
- [Epics](../_bmad-output/planning-artifacts/epics.md)
- [Project context](../_bmad-output/project-context.md)

## Index

- [github-workflow.md](github-workflow.md) records GitHub issue, project,
  status, label, and issue-body conventions.
- [observability.md](observability.md) records current diagnostic surfaces.
- [operations.md](operations.md) records production operations, lifecycle,
  retention, receive, migration, and ownership guidance.
- [packaging.md](packaging.md) records package IDs, target framework, package
  artifact policy, dependency direction, versioning, and publishing policy.
- [package-discovery.md](package-discovery.md) maps common consumer-facing
  capabilities to package IDs and namespaces.
- [public-api.md](public-api.md) records public API surface classification used
  during cleanup work.
- [repository.md](repository.md) records repository layout, tooling, and code
  conventions.
- [samples.md](samples.md) records sample application direction.
- [setup.md](setup.md) is the primary user-facing library setup example.
- [testing.md](testing.md) records testing direction, categories, and command
  entrypoints.

## Documentation Model

- BMAD PRD owns requirements and scope.
- BMAD architecture owns internal runtime architecture and durable boundary
  rules.
- BMAD epics own implementation sequence and story acceptance criteria.
- `project-context.md` owns lean agent-facing rules.
- `/docs` owns library-user and repository-operation guidance.
- GitHub Issues and GitHub Projects track backlog work, real-project findings,
  cleanup tasks, and prioritization.

Prefer references over duplication. If a rule belongs to internal architecture,
put it in the BMAD architecture artifact and link to it from consumer docs only
when useful. If a rule belongs to library usage or operations, keep it in this
folder.

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
- `public-api.md` owns current package public API classification notes.
- `repository.md` owns repository layout, local tooling, CI, and C#
  conventions.
- `testing.md` owns test policy, categories, and command entrypoints.
- `samples.md` owns sample application direction.

## Expanding Docs

Add new `/docs` files for consumer-facing or repository-operation guidance.
Add internal architecture or planning material to the BMAD artifacts instead.
