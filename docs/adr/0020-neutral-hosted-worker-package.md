# 0020 Neutral Hosted Worker Package

Status: Amended
Application: Applied
Date: 2026-06-05

## Context

[0019 Hosted Outbox Worker Composition](0019-hosted-outbox-worker-composition.md)
accepted a hosted outbox worker but placed the first worker in
`Bondstone.Transport.Rebus` as a pragmatic first implementation.

That package boundary is too transport-specific for the current dispatcher
shape. `IDurableOutboxDispatcher` already abstracts the durable workflow:
claiming outbox rows, renewing leases, sending through
`IDurableOutboxTransport`, applying failure policy, and recording outcomes.
The hosted loop only needs to call the dispatcher with a worker identity,
lease duration, batch size, polling interval, and failure delay.

Keeping that loop in a Rebus package would make a reusable worker look
transport-specific and would encourage future adapters to duplicate the same
hosted-service mechanics. Putting hosted-service dependencies into core would
also weaken the low-dependency `Bondstone` boundary.

## Decision

Create a neutral `Bondstone.Hosting` package for reusable hosted workers that
compose Bondstone core abstractions.

`Bondstone.Hosting` owns:

- `DurableOutboxWorker`;
- `DurableOutboxWorkerOptions`;
- `DurableOutboxWorkerOptionsValidator`;
- hosted outbox worker DI registration;
- default dispatcher and failure-policy DI registration as implementation
  details of hosted outbox composition.

`Bondstone.Hosting` depends on `Bondstone` and
`Microsoft.Extensions.Hosting`/options/logging abstractions. It does not
depend on EF Core, PostgreSQL, Rebus, provider SQL, transport-specific
envelopes, handler discovery, or broker configuration.

`Bondstone.Transport.Rebus` remains transport-focused. It registers the Rebus
implementation of `IDurableOutboxTransport`, Rebus destination resolution, and
Rebus wire mapping. It does not own hosted worker registration or default
dispatcher registration.

Future inbox and maintenance workers should also live in `Bondstone.Hosting`
when their underlying core abstractions are stable. Provider-specific SQL and
transport-specific behavior remain in provider and transport adapter packages.

## Consequences

Applications can combine provider, transport, and hosting packages explicitly:

```text
Bondstone.Hosting -> Bondstone
Bondstone.Transport.Rebus -> Bondstone
Bondstone.EntityFrameworkCore.Postgres -> Bondstone.EntityFrameworkCore
```

The worker can be reused by Rebus, direct Service Bus, or other future
transports through `IDurableOutboxTransport`.

Core remains free of hosted-service dependencies. Rebus remains free of
generic worker-loop ownership.

The first hosting package only includes the outbox worker. Inbox workers,
cleanup workers, stale-claim recovery, dead-letter routing, minimum message
age, route circuit breaking, worker metrics, and multi-worker/module-specific
registration remain future decisions or later application work.

## Amendment 2026-06-09: Module-Targeted Worker Direction

The first hosted outbox worker remains an aggregate worker over the local
module outbox dispatcher. That is enough for the initial durable command loop
and the Phase 4 adoption-proof sample.

Future worker registration should support targeting all local modules by
default or a selected module/outbox set when a host needs operational
isolation. In a modular monolith, module outboxes are persistence and backlog
ownership boundaries. A noisy module should not have to monopolize dispatch
capacity for unrelated module outboxes when the host wants independent
throughput, retry, or extraction headroom.

This amendment does not add a worker API yet. It records the direction so the
next worker slice can be designed around module-targeted dispatch instead of a
single shared queue assumption.

## Related Decisions

- Supersedes
  [0019 Hosted Outbox Worker Composition](0019-hosted-outbox-worker-composition.md)
- [0003 Package Boundaries And Target Framework](0003-package-boundaries-and-target-framework.md)
- [0017 Outbox Dispatcher Composition](0017-outbox-dispatcher-composition.md)
- [0018 Rebus Outbox Transport Adapter](0018-rebus-outbox-transport-adapter.md)

## Application Notes

- Current contract: hosted worker composition lives in `Bondstone.Hosting`.
  Transport adapters provide `IDurableOutboxTransport`; provider adapters
  provide persistence, claiming, leasing, and outcome recording. The public
  outbox hosting entrypoint registers one aggregate worker plus default
  dispatcher composition, resolves the dispatcher inside a service scope per
  batch, and fails fast on startup when the dispatcher graph is incomplete.
  Future worker registration should support module-targeted workers for
  hosts that need outbox isolation.
- Stable docs: Current package boundaries are described in
  [docs/packaging.md](../packaging.md), hosting rules in
  [docs/architecture/hosting.md](../architecture/hosting.md), transport rules
  in [docs/architecture/messaging.md](../architecture/messaging.md),
  persistence rules in
  [docs/architecture/persistence-core.md](../architecture/persistence-core.md),
  and current implementation state in [docs/archive/mvp-plan.md](../archive/mvp-plan.md).
- Agent guidance: Root [AGENTS.md](../../AGENTS.md) requires ADR review before
  package-boundary or durable runtime behavior changes.
- Application evidence: `Bondstone.Hosting` package, hosted outbox worker,
  options, validator, DI registration, neutral hosting tests, package docs, and
  direct transport composition are applied.
- Pending or deferred: None for the neutral hosted worker package decision.
  Module-targeted outbox workers, inbox hosted workers, cleanup/maintenance
  workers, stale-claim recovery, worker metrics, circuit breaking, and minimum
  message age remain separate future decisions.

## Verification

Read back this ADR and affected stable docs. Ran `dotnet restore
Bondstone.slnx --disable-build-servers -p:NuGetAudit=false`, `dotnet build
Bondstone.slnx --configuration Release --no-restore --disable-build-servers`,
fast `Unit|Application` tests, the sample `Integration` smoke test,
`pnpm format:check`, and `git diff --check`.
