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
- [backlog/README.md](backlog/README.md) tracks campaign-sized future work and
  ideas that are not current operating guidance.
- [mvp-plan.md](mvp-plan.md) tracks implemented surface, MVP priority groups,
  current slice, verification surface, and deferred work.
- [packaging.md](packaging.md) records current package IDs, target framework,
  and package dependency direction.
- [repository.md](repository.md) records current repository layout and tooling
  direction.
- [samples.md](samples.md) records current sample application direction.
- [setup.md](setup.md) is the single user-facing library setup example.
- [testing.md](testing.md) records current testing direction.

## Documentation Model

- Stable docs describe the current operating contract.
- ADRs preserve the decision trail and explain why a durable technical decision
  exists.
- Backlog docs track future work, review campaigns, and speculative ideas that
  should not be presented as current behavior.
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
- `repository.md` owns repository layout, local tooling, CI, and code
  conventions, but should reference `packaging.md` for package inventory.
- `testing.md` owns test policy, categories, and command entrypoints.
- `mvp-plan.md` owns the current implementation, verification surface,
  priority groups, current slice, and deferred-work snapshot.
- `architecture/` owns runtime contracts and durable boundary principles.
- `backlog/` owns future work and review campaigns that are not active
  operating guidance.
- `archive/` preserves historical planning documents that should not steer new
  implementation work.

## Expanding Docs

Create additional stable docs as soon as corresponding ADRs are accepted, and
link them from this index.
