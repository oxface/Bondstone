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
- [observability.md](observability.md) records current diagnostic surfaces and
  the OpenTelemetry-native direction.
- [operations.md](operations.md) records production operations, lifecycle,
  retention, receive, migration, and ownership guidance.
- [packaging.md](packaging.md) records current package IDs, target framework,
  package artifact policy, and package dependency direction.
- [package-discovery.md](package-discovery.md) maps common consumer-facing
  capabilities to package IDs and namespaces.
- [plans/README.md](plans/README.md) records short-lived planning handoffs
  that must be converted into ADRs, stable docs, or GitHub Issues/Projects.
- [public-api.md](public-api.md) records the current public API surface
  classification used during cleanup work.
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
- Long-lived planning notes should not live under `docs/`. Short-lived
  planning handoffs may live under `docs/plans/` when useful, but durable
  decisions must move into ADRs/stable docs and backlog work must move into
  GitHub Issues or Projects.
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
  package artifact policy, versioning, publishing policy, and v2
  replacement/migration guidance.
- `package-discovery.md` owns package and namespace discovery guidance for
  common consumer-facing APIs.
- `operations.md` owns production operations guidance for receive semantics,
  broker settlement, outbox and inbox inspection, operation finalization and
  expiration, EF migrations, package upgrades, contract evolution, retention,
  and app-owned recovery.
- `observability.md` owns current diagnostic surfaces and the
  OpenTelemetry-native diagnostics direction.
- `plans/` owns short-lived planning handoffs only. It must not become a
  parallel architecture or backlog system.
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
