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
- [extraction.md](extraction.md) records the slow layered extraction strategy.
- [extraction-plan.md](extraction-plan.md) tracks the current tactical
  extraction backlog.
- [handover.md](handover.md) contains a continuation prompt for future
  Bondstone workspace sessions.
- [packaging.md](packaging.md) records current package IDs, target framework,
  and package dependency direction.
- [repository.md](repository.md) records current repository layout and tooling
  direction.
- [samples.md](samples.md) records current sample application direction.
- [setup.md](setup.md) is the single user-facing library setup example.
- [status.md](status.md) summarizes current extraction, verification, and
  deferred implementation state.
- [testing.md](testing.md) records current testing direction.

## Documentation Model

- Stable docs describe the current operating contract.
- ADRs preserve the decision trail and explain why a durable technical decision
  exists.
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
- `status.md` owns the current implementation, verification, accepted
  direction, and deferred-work snapshot.
- `extraction-plan.md` owns tactical backlog notes and may be pruned when
  completed detail stops helping active extraction.
- `architecture/` owns runtime contracts and durable boundary principles.

## Expanding Docs

Create additional stable docs as soon as corresponding ADRs are accepted, and
link them from this index.
