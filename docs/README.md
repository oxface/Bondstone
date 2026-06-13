# Bondstone Documentation

This folder is the shared source of durable repository knowledge for humans and
agents. README files should help developers navigate; AGENTS files should tell
agents what to read and how to act; both should reference these docs instead of
duplicating durable rules.

## Index

- [adr/README.md](adr/README.md) defines the ADR workflow, statuses, and how
  ADR decisions are applied into stable docs and agent instructions.
- [architecture/README.md](architecture/README.md) records current runtime
  positioning and durable module-boundary architecture.
- [github-workflow.md](github-workflow.md) records GitHub issue, project
  status, label, and issue-body conventions.
- [packaging.md](packaging.md) records current package IDs, target framework,
  and package dependency direction.
- [public-api.md](public-api.md) records the current public API surface
  classification used by ADR 0046 cleanup work.
- [repository.md](repository.md) records current repository layout and tooling
  direction.
- [samples.md](samples.md) records current sample application direction.
- [setup.md](setup.md) is the single user-facing library setup example.
- [testing.md](testing.md) records current testing direction.

## Documentation Model

- Stable docs describe the current operating contract.
- ADRs preserve the decision trail and explain why a durable technical decision
  exists.
- GitHub Issues and GitHub Projects track backlog work, real-project findings,
  cleanup tasks, and prioritization.
- README files orient human maintainers to a folder or workflow.
- AGENTS files orient agents to the relevant docs, local constraints, and
  verification expectations for a folder or workflow.
- Root and scoped AGENTS files should make ADR work discoverable before broad
  changes to public API, package boundaries, supported platforms, provider or
  transport strategy, compatibility, release policy, samples, repository
  workflow, or agent harness behavior.

Prefer references over duplication. If a rule matters to both humans and
agents, record it in stable docs, then reference it from README and AGENTS
files with the local context each audience needs.

## Document Ownership

- `setup.md` is the only stable doc for library-user code examples.
- `packaging.md` owns package IDs, dependency direction, target framework,
  versioning, and publishing policy.
- `github-workflow.md` owns GitHub issue, project, label, and completion
  comment conventions.
- `public-api.md` owns current package public API classification notes.
- `repository.md` owns repository layout, local tooling, CI, and code
  conventions, but should reference `packaging.md` for package inventory.
- `testing.md` owns test policy, categories, and command entrypoints.
- `architecture/` owns runtime contracts and durable boundary principles.

## Expanding Docs

Create additional stable docs as soon as corresponding ADRs are accepted, and
link them from this index.
