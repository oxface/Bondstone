# Consumer Trial Handoff

This page is the first-consumer trial route for Bondstone. It links to the
owning docs and BMAD artifacts instead of restating the full setup, package,
operation, and verification guidance.

## Start Here

Use [setup.md](setup.md) as the primary implementation path. The recommended
first slice is a modular-monolith host with module-owned EF Core `DbContext`
types, PostgreSQL durable persistence, explicit local transport for local
trial execution, and the hosted outbox worker.

Production durable persistence is EF Core plus PostgreSQL through:

- `Bondstone.Persistence.EntityFrameworkCore`
- `Bondstone.Persistence.EntityFrameworkCore.Postgres`

Applications own EF migrations, PostgreSQL schemas, connection strings,
schema rollout, and migration review. Bondstone supplies EF mappings and
provider helpers; it does not ship automatic schema rollout.

Use `Bondstone.Transport.Local` only for samples, tests, and local
development. It is not production broker durability or a hidden fallback.
RabbitMQ and Azure Service Bus adapters are thin native-driver envelope
adapters; the host owns topology, provisioning, retry, dead-letter policy,
prefetch, credentials, deployment, and native monitoring.

## Choose Packages

Use [package-discovery.md](package-discovery.md) for the capability-to-package
and namespace matrix. Use [packaging.md](packaging.md) for package policy,
target framework, dependency direction, versioning, publishing, and migration
guidance.

Current package IDs are:

- `Bondstone`
- `Bondstone.Hosting`
- `Bondstone.Persistence`
- `Bondstone.Persistence.EntityFrameworkCore`
- `Bondstone.Persistence.EntityFrameworkCore.Postgres`
- `Bondstone.Transport.Local`
- `Bondstone.Transport.RabbitMq`
- `Bondstone.Transport.ServiceBus`

Do not start new trial work from removed package IDs or old v1 setup examples.

## Verify And Operate

Use [testing.md](testing.md) for repository verification commands. The default
quality gate is `pnpm check`. Run `pnpm backend:test:integration` when the
trial needs PostgreSQL, transport, or sample smoke behavior.

Use [operations.md](operations.md) and [observability.md](observability.md) for
production receive, outbox, inbox, operation-state, migration, retention,
recovery, and diagnostic guidance. Cleanup, retention, replay, purge,
dead-letter movement, and topology management remain application-owned unless
a future BMAD PRD and architecture add a native Bondstone feature.

## Use The Sample Proof

Use [samples.md](samples.md) and [../samples/README.md](../samples/README.md)
for the modular monolith adoption proof and service-split path. The sample is
the current proof for module-owned persistence, durable command delivery,
integration event return path, operation result observation, and explicit
local transport behavior.

Before treating a trial as service-extraction ready, verify that the slice
preserves:

- stable contract assemblies where messages cross module boundaries;
- stable durable message identities;
- stable command handler and event subscriber identities;
- inbox/outbox semantics across the boundary;
- operation observation for accepted durable work;
- module-owned persistence and schemas;
- host-owned broker topology, retry, dead-letter policy, and monitoring.

## Know What Remains

Use the BMAD [epics](../_bmad-output/planning-artifacts/epics.md) and
[sprint status](../_bmad-output/implementation-artifacts/sprint-status.yaml)
to confirm implementation readiness. Epic 8 stories 8.1 through 8.4 are done
in sprint status; story 8.5 creates this handoff.

The first real-project migration trial is tracked in GitHub issue
[#34](https://github.com/oxface/Bondstone/issues/34). Create separate GitHub
Issues for distinct trial findings using the formats in
[github-workflow.md](github-workflow.md); do not use repository docs as the
trial backlog.
