# 0044 Module Outbox Worker Topology

Status: Proposed
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

Decide whether Bondstone should keep only the aggregate outbox worker or add
module-targeted worker topology.

The candidate direction is:

- Keep the aggregate worker as the simple default.
- Add opt-in module-targeted workers or per-module worker options when hosts
  need selected-module dispatch or stronger isolation.
- Avoid making worker topology imply broker provisioning or transport
  ownership.
- Keep claim/lease/dispatch outcome recording inside each provider-backed
  module dispatcher.

## Consequences

Keeping only the aggregate worker minimizes API surface but leaves fairness to
provider timeouts and worker scheduling.

Adding module-targeted workers improves operational isolation but expands
hosting API, options validation, diagnostics, and test coverage.

Default worker semantics, concurrency, and fairness are durable behavior and
should not change accidentally.

## Related Decisions

- [0017 Outbox Dispatcher Composition](0017-outbox-dispatcher-composition.md)
- [0019 Hosted Outbox Worker Composition](0019-hosted-outbox-worker-composition.md)
- [0020 Neutral Hosted Worker Package](0020-neutral-hosted-worker-package.md)
- [0032 Module-Owned Durable EF Persistence](0032-module-owned-durable-ef-persistence.md)

## Application Notes

- Current contract: proposed only; no binding change yet.
- Stable docs: if accepted, update hosting and persistence architecture docs
  plus setup examples for worker registration.
- Agent guidance: if accepted, update root AGENTS hosting direction only if
  default worker behavior changes.
- Application evidence: current aggregate dispatcher loops sequentially across
  module dispatchers and uses provider claim/lease behavior per module.
- Pending or deferred: decide default behavior, opt-in API shape, and
  verification expectations for noisy-neighbor isolation.

## Verification

No executable verification yet; this is a proposed decision draft.
