# Outbox Worker Topology

Archived: 2026-06-10

## Outcome

[ADR 0044](../../adr/0044-module-outbox-worker-topology.md) accepted the
current aggregate-only worker topology.

The built-in hosted outbox worker remains an aggregate worker over the
app-facing `IDurableOutboxDispatcher`. For module-owned persistence,
`DurableModuleOutboxDispatchAggregator` invokes module dispatchers
sequentially, shares one batch budget across modules, and preserves
provider-owned claim, lease, send, and outcome recording inside each module
dispatcher.

## Deferred Work

Module-targeted worker registration, selected-module dispatch, per-module
worker options, parallel aggregate dispatch, dispatch timeout policy,
per-module concurrency controls, and stronger noisy-neighbor isolation remain
future work in
[../13-transport-and-hosting-ergonomics.md](../13-transport-and-hosting-ergonomics.md).

## Verification

Stable hosting and persistence docs were updated with the accepted contract.
Fast unit tests cover aggregate dispatcher budgeting, ordering, and failure
propagation.
