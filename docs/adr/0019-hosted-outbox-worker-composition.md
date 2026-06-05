# 0019 Hosted Outbox Worker Composition

Status: Superseded
Application: Not Applicable
Date: 2026-06-05

Superseded By:
[0020 Neutral Hosted Worker Package](0020-neutral-hosted-worker-package.md)

## Context

Bondstone now has a plain `IDurableOutboxDispatcher` boundary and an outgoing
Rebus command transport adapter. Consumers can already call the dispatcher
manually, but most applications need a hosted loop that repeatedly dispatches
claimed outbox rows.

The worker must stay aligned with Bondstone's library positioning. It should
compose existing primitives and integrate with `Microsoft.Extensions.Hosting`
without introducing leader election, singleton sweeper ownership, hidden
transport configuration, handler discovery, or a framework runtime.

The core `Bondstone` package is intentionally low dependency and independent
from hosting. Adding hosted-service dependencies to core would weaken that
boundary and make non-hosted consumers take dependencies they do not need.

A neutral `Bondstone.Hosting` package is a plausible later split, but the first
hosted worker can be applied inside the existing Rebus package because the
current concrete transport adapter is Rebus and that package already owns
hosting-related dependencies.

## Decision

`Bondstone.Transport.Rebus` provides the first durable outbox hosted worker for
Rebus transport scenarios. The worker composes `IDurableOutboxDispatcher`; it
does not send directly through Rebus and does not own provider-specific SQL.

The worker:

- uses a configured worker id as the claim owner;
- dispatches one batch per dispatcher call;
- uses configured lease duration, batch size, polling interval, and failure
  delay;
- immediately continues when a batch claims rows so it can drain backlog;
- waits for the polling interval when no rows are claimed;
- logs unexpected dispatch failures, waits for the failure delay, and
  continues;
- propagates host cancellation and stops without converting cancellation into
  retry.

The worker uses the same competitive-consumer model as the PostgreSQL claimer:
multiple workers may run concurrently when the provider supports skip-locked
or equivalent claim semantics. Bondstone does not require leader election for
this worker.

Minimum message age, route or destination circuit breaking, stale-claim
recovery sweeps, dead-letter routing, worker health endpoints, metrics, and
transport-neutral hosting package extraction remain separate decisions.

## Consequences

Applications using the Rebus adapter can run Bondstone outbox dispatch through
standard .NET hosting without adopting a larger framework runtime.

Core remains free of hosting dependencies. Consumers that do not need hosted
workers can reference only `Bondstone`.

The first worker is intentionally small. Operational policy such as circuit
breaking, minimum message age, and stale-claim recovery will be added only
after real transport or sample behavior proves the shape.

A future direct Service Bus adapter can implement the same dispatcher-based
worker registration in its own package, or a later ADR can extract common
hosting code into a neutral package when duplication or package ergonomics
justify it.

## Related Decisions

- [0003 Package Boundaries And Target Framework](0003-package-boundaries-and-target-framework.md)
- [0017 Outbox Dispatcher Composition](0017-outbox-dispatcher-composition.md)
- [0018 Rebus Outbox Transport Adapter](0018-rebus-outbox-transport-adapter.md)
- Superseded by
  [0020 Neutral Hosted Worker Package](0020-neutral-hosted-worker-package.md)

## Application Notes

- Current contract: Superseded by
  [0020 Neutral Hosted Worker Package](0020-neutral-hosted-worker-package.md).
  Rebus no longer owns hosted worker registration.
- Stable docs: Current hosting rules are described in
  [docs/architecture/hosting.md](../architecture/hosting.md),
  transport rules in
  [docs/architecture/transport-rebus.md](../architecture/transport-rebus.md),
  package-boundary rules in [docs/packaging.md](../packaging.md), and
  extraction state in [docs/extraction-plan.md](../extraction-plan.md).
- Agent guidance: Root [AGENTS.md](../../AGENTS.md) requires ADR review before
  package-boundary or durable runtime behavior changes.
- Application evidence: This ADR is retained for traceability only. The
  applied worker package is now `Bondstone.Hosting`.
- Pending or deferred: See
  [0020 Neutral Hosted Worker Package](0020-neutral-hosted-worker-package.md)
  and
  [0021 Fluent Service Composition Guardrails](0021-fluent-service-composition-guardrails.md)
  for current deferred hosting and composition work.

## Verification

Read back this superseded ADR, ADR 0020, ADR 0021, and affected stable docs.
