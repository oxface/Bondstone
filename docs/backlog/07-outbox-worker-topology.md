# Outbox Worker Topology

Goal: decide whether the current aggregate outbox worker remains enough or
Bondstone should add module-targeted worker topology.

## Scope

- Review `DurableModuleOutboxDispatchAggregator`, `DurableOutboxWorker`,
  worker options, module-owned dispatchers, and provider claim/lease behavior.
- Keep the current aggregate worker simple unless there is an accepted
  isolation or fairness requirement.
- Consider module-targeted workers, per-module batch sizes, per-module
  concurrency, dispatch timeouts, and noisy-neighbor isolation.

## ADRs

- [0044 Module Outbox Worker Topology](../adr/0044-module-outbox-worker-topology.md)

## Review Questions

- Is sequential aggregate dispatch acceptable as the default worker behavior?
- Should selected-module worker registration become a supported API?
- Should one slow module be isolated by separate workers or by provider/client
  timeout guidance?
- How should global batch size interact with per-module dispatch limits?

## Candidate Deliverables

- Accepted, rejected, or narrowed ADR 0044.
- Stable hosting docs updated with accepted default and opt-in worker
  topology.
- Follow-up implementation tasks for worker APIs and tests if accepted.

## Verification

- `pnpm backend:test:fast`
- Provider integration tests if worker scheduling or claim behavior changes.
