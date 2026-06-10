# Real Project Readiness

Priority: Medium

Goal: prepare Bondstone for validation in a real project after the immediate
domain-event and API cleanup work is queued.

## Scope

- Improve setup and package README guidance where real users need the first
  successful path.
- Add service-split, broker, or multi-process samples only as bounded
  scenarios.
- Consider Aspire as preferred local orchestration when samples need multiple
  processes or local infrastructure.
- Consider external event handoff formats such as unwrapped payloads,
  CloudEvents, schema-specific envelopes, or non-JSON payload negotiation only
  after core module behavior is stable.
- Track package compatibility and target framework expansion only when user
  demand justifies it.

## Candidate Work Items

- Add prominent `Bondstone.Transport.Local` warning when package READMEs are
  introduced.
- Add one service-split sample after domain event behavior and provider
  transport docs are stable enough to avoid churn.
- Add real-project adoption notes for module boundaries, persistence,
  transport setup, and operation-state expectations.
- Add HTTP/custom entrypoint guidance that routes app-owned requests through
  registered module commands when handler-scoped durable send/publish is
  needed, instead of calling durable send/publish outside a module execution
  context.

## Proposed Slices

1. Adoption checklist slice: write the first real-project checklist for
   modules, persistence, command sending, integration events, workers, and
   operational expectations.
2. Package README slice: add package-specific quick paths only after the API
   cleanup track identifies the intended normal APIs.
3. Sample slice: add or expand one bounded sample after domain events are
   decided, so sample aggregate patterns do not immediately churn.
4. Validation slice: record findings from the first real-project trial as new
   backlog items or ADR amendments instead of hiding them in sample code.

## Implementation Backlog

### RPR-01: Real-Project Adoption Checklist

Priority: P0 after DE-01 and API-02.

Write a checklist for using Bondstone in a real application: module boundary
rules, persistence choice, command sending, integration event publishing,
workers, provider topology, local transport limits, and recovery expectations.

Important files:

- `docs/setup.md`
- `docs/samples.md`
- `docs/architecture/README.md`

Verification:

- `pnpm format:check`

### RPR-02: HTTP And Custom Entrypoint Guidance

Priority: P0.

Document how app-owned HTTP endpoints and custom schedulers should execute
registered module commands when handlers need durable send/publish. Make the
ambient execution context limit from ADR 0045 visible in normal setup docs.

Important files:

- `docs/setup.md`
- `docs/architecture/modules.md`
- `docs/architecture/messaging.md`

Verification:

- `pnpm format:check`

### RPR-03: Package README Quick Paths

Priority: P1 after API-01.

Add package-specific quick paths only after the public API inventory confirms
which APIs are normal setup and which are advanced composition. Include a
prominent `Bondstone.Transport.Local` warning.

Candidate files:

- package `README.md` files under `src`
- `docs/packaging.md`

Verification:

- `pnpm format:check`
- `pnpm backend:pack`

### RPR-04: Service-Split Sample

Priority: P1 after DE-03.

Add one bounded sample that proves the real deployment shape that is most
likely to be used next: multiple processes, broker-backed transport, module
persistence, explicit integration events, and the accepted domain-event
pattern.

Candidate files:

- `samples`
- `tests/Bondstone.Samples.Tests`
- `docs/samples.md`

Verification:

- `pnpm check`
- `pnpm backend:test:integration`

### RPR-05: First Real-Project Feedback Intake

Priority: P0 during real-project trial.

Record concrete adoption pain as backlog items or ADR amendments. Do not hide
library changes inside sample-specific workarounds.

Candidate outputs:

- new backlog items under `docs/backlog`
- ADR amendments under `docs/adr`
- updates to stable architecture docs when behavior is accepted

Verification:

- depends on the accepted changes.

## Verification

- `pnpm check`
- `pnpm backend:test:integration` for sample or provider-backed changes.
