# 0044 Module Outbox Worker Topology

Status: Archived
Application: Not Applicable
Date: 2026-06-10

## Context

Bondstone module-owned persistence can register one module outbox dispatcher
per module. The app-facing `IDurableOutboxDispatcher` can aggregate across
those module dispatchers with `DurableModuleOutboxDispatchAggregator`. The
aggregate dispatcher currently dispatches module outboxes sequentially and
shares one batch budget across module dispatchers.

This keeps the first default worker simple, but it is not a fairness or
isolation model. A slow provider call, noisy module, or module with a large
ready backlog can delay later modules in the aggregate loop. Per-module worker
registration, per-module concurrency, dispatch timeouts, and noisy-neighbor
isolation were deferred during the first implementation.

## Decision

Bondstone keeps the aggregate hosted outbox worker as the only current
supported worker topology.

The public worker registration continues to run `DurableOutboxWorker` over the
app-facing `IDurableOutboxDispatcher`. For module-owned persistence, provider
registrations can contribute one module outbox dispatcher registration per
module, and the app-facing dispatcher can be
`DurableModuleOutboxDispatchAggregator`.

The aggregate dispatcher contract is intentionally simple:

- it invokes module dispatchers sequentially in registration order;
- it shares the caller's `maxCount` across all module dispatchers;
- it passes each module dispatcher only the remaining batch budget;
- it stops the batch when the shared budget is exhausted;
- it propagates module dispatcher failures to the caller rather than skipping
  the failed module; and
- each provider-backed module dispatcher owns its module's claim, lease,
  transport send, failure policy, and dispatch outcome recording behavior.

Bondstone does not currently add selected-module worker registration,
per-module worker options, parallel aggregate dispatch, per-module dispatch
timeouts, or per-module concurrency controls.

Module-targeted workers may be accepted later only through a separate API
decision that defines the option shape, DI registration, validation behavior,
failure semantics, global-versus-module batch budgeting, and test scope. Worker
topology must not imply broker provisioning or transport ownership.

## Consequences

Keeping only the aggregate worker minimizes API surface and preserves the
current hosting package boundary.

The aggregate worker is not a fairness or noisy-neighbor isolation model. A
slow module dispatcher can delay later modules, a failing module dispatcher can
stop the current aggregate batch, and an earlier module can consume the shared
batch budget before later modules are reached. Hosts that need stronger
isolation must use provider/client timeout configuration, run custom advanced
schedulers over the dispatcher contracts, or wait for a future accepted
module-targeted worker API.

Default worker semantics, failure propagation, module iteration order, and
batch budgeting are durable behavior and should not change accidentally.

## Related Decisions

- [0017 Outbox Dispatcher Composition](0017-outbox-dispatcher-composition.md)
- [0019 Hosted Outbox Worker Composition](0019-hosted-outbox-worker-composition.md)
- [0020 Neutral Hosted Worker Package](0020-neutral-hosted-worker-package.md)
- [0032 Module-Owned Durable EF Persistence](0032-module-owned-durable-ef-persistence.md)

## Application Notes

- Current contract: the only built-in hosted worker topology is the aggregate
  worker over the configured app-facing `IDurableOutboxDispatcher`. Module
  dispatch aggregation is sequential, uses one shared batch budget, and
  preserves provider-owned per-module claim/lease/send/outcome behavior.
- Stable docs: current worker topology and limits are described in
  [docs/architecture/hosting.md](../architecture/hosting.md),
  [docs/architecture/persistence-core.md](../architecture/persistence-core.md),
  [docs/architecture/persistence-postgresql.md](../architecture/persistence-postgresql.md),
  and [docs/architecture/persistence-postgres.md](../architecture/persistence-postgres.md).
- Agent guidance: if accepted, update root AGENTS hosting direction only if
  default worker behavior changes. This decision keeps the default behavior,
  so no AGENTS update is required.
- Application evidence: `DurableOutboxWorker` owns polling, scope creation,
  option validation, failure delay, and retry of later batches.
  `DurableModuleOutboxDispatchAggregator` loops sequentially across module
  dispatchers and shares `maxCount` as one aggregate batch budget. EF
  PostgreSQL and non-EF PostgreSQL module dispatchers construct provider-owned
  claim, lease, and recorder components for the current module dispatch call.
- Pending or deferred: selected-module worker registration, per-module worker
  options, parallel dispatch, dispatch timeout policy, per-module concurrency,
  and stronger noisy-neighbor isolation remain future work requiring a separate
  accepted decision before implementation.

## Verification

Read back this ADR and affected stable docs. Verified the accepted aggregate
dispatcher contract with fast unit tests and:

- `pnpm format:check`
- `pnpm backend:build`
- `pnpm backend:test:fast`
