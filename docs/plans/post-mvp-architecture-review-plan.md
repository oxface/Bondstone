# Post-MVP Architecture Review Plan

Date: 2026-06-16
Review update: 2026-06-17

## Purpose

This plan prioritizes the architecture work identified after Bondstone's
public MVP publication and initial consumer feedback.

The plan is intentionally a planning handoff, not a durable architecture
contract. Use the accepted ADRs below as decision boundaries while moving
implementation tasks into GitHub Issues or GitHub Projects.

The 2026-06-17 architecture review reframes the plan as a
first-consumer-project hardening plan. The goal is to keep Bondstone's scope
narrowly focused on durable module boundaries while reducing the operational
gaps that would make a real consumer project painful or misleading.

## Accepted ADRs

- [0011 OTel Native Diagnostics And Misconfiguration Reporting](../adr/0011-otel-native-diagnostics-and-misconfiguration-reporting.md)
- [0012 Direct Receive Inbox And Durable Receive Buffer](../adr/0012-direct-receive-inbox-and-durable-receive-buffer.md)
- [0013 Worker Boundaries And Transport Adapter Ownership](../adr/0013-worker-boundaries-and-transport-adapter-ownership.md)
- [0014 Production Operations And Lifecycle Guidance](../adr/0014-production-operations-and-lifecycle-guidance.md)
- [0015 Service Extraction Proof Before Broad Bus Features](../adr/0015-service-extraction-proof-before-broad-bus-features.md)
- [0016 Non-Throwing Operation Wait Ergonomics](../adr/0016-non-throwing-operation-wait-ergonomics.md)

## Review Findings

Bondstone's current architecture is directionally strong. The core value is
not being a smaller general-purpose bus; it is stable, durable module
boundaries with a credible path from modular monolith to service extraction.

The highest-risk gap before handing Bondstone to the first consumer project is
receive-side operations. Current direct receive is safe by default but
operationally sharp: already-received/unprocessed inbox rows are ambiguous and
fail loudly. That behavior is correct for the current model, but consumers
will need better productized recovery semantics.

The second high-risk gap is operation-result observation. The operation store
is the right abstraction, but result reads are still too happy-path oriented:
they report completed results well, but a pending operation may actually be
blocked behind source outbox terminal failure, receive ambiguity, broker
dead-letter policy, handler failure/retry, or a missing finalization policy.
Bondstone should not infer all of those as operation failure automatically, but
it should make the distinction observable and ergonomic.

Diagnostics are not currently too scattered in code. The bigger issue is that
metrics and machine-readable misconfiguration codes are not yet stable.

## Worker Topology

Current behavior has two worker/listener roles:

- Source outbox worker: current. It claims source-module outbox rows, dispatches
  envelopes to the configured dispatcher, and records dispatched, retry, stale,
  or terminal-failed outbox state.
- Transport receive worker or app-owned native listener: current and opt-in.
  It reads broker-native deliveries, parses durable envelopes, calls
  `IDurableEnvelopeReceiver`, and settles the native message after Bondstone
  receive succeeds.

Current direct receive does not have a separate inbox processing worker. The
transport receive worker calls the module receive pipeline, and the handler
runs inside that receive attempt. The inbox table is an idempotency ledger, not
a queued work table.

The future durable receive-buffer path should introduce the three-stage
production topology when the application opts into it:

- outbox worker: source outbox to transport;
- transport ingestion worker/listener: transport to Bondstone-owned durable
  receive-buffer record, then native settlement after durable ingestion;
- receive-buffer processing worker: Bondstone-owned buffered delivery to inbox,
  operation state, outgoing outbox, and handler execution inside the target
  module persistence boundary.

A generic cleanup worker is not current behavior and should not be added as a
default background service without ADR review. Cleanup mutates durable evidence
such as processed inbox rows, dispatched outbox rows, operation states, domain
event records, terminal outbox failures, and future receive-buffer records.
Retention may be useful, but defaults can destroy operational evidence. Prefer
first adding inspection, runbook guidance, filters, and explicit app-owned
cleanup samples.

## Before First Consumer Project

These items are candidates to complete before treating the current line as
consumer-ready v2 scope.

1. Durable receive-buffer design slice.
   Define the persistence record, claim/lease model, retry attempts, terminal
   receive failure state, operation interaction, worker options, and retention
   guidance. Keep the direct receive path as the simple default unless a later
   ADR changes it.

2. Operation-result hardening.
   Add a non-throwing wait/read API per ADR 0016, then review whether Bondstone
   needs an operation diagnostic snapshot that can explain "still pending"
   using source outbox state, terminal source dispatch failure, future receive
   buffer state, finalization state, and result deserialization failure. Avoid
   automatically converting transport or inbox failures into operation failure
   unless application policy explicitly finalizes the operation.

3. Operation-linked inspection.
   Review whether inspectors need filters by operation id and message id, not
   only module and timestamp. This is a fast way to make "why is my operation
   pending?" debuggable without turning operation state into a full workflow
   engine.

4. Outbox worker fairness and targeting.
   The aggregate dispatcher currently dispatches module outboxes in registration
   order with one shared batch budget. Review selected-module worker options,
   fairness, and operator guidance before real multi-module load.

5. Minimal metrics.
   Stabilize metric names and dimensions for the states Bondstone owns:
   outbox claimed/dispatched/retry/terminal/stale, direct receive
   handled/already-processed/already-received, future receive-buffer outcomes,
   operation finalization, and operation expiration.

6. Stable misconfiguration error codes.
   Keep exception messages human-readable, but add a small stable code
   vocabulary for common setup failures: missing module persistence, missing EF
   mappings, missing dispatcher, duplicate module durable registrations,
   invalid durable identities, missing receive binding, and ambiguous dispatch
   routes.

7. First-consumer ingress ergonomics.
   Keep the current rule that durable send/publish requires a module execution
   context. Improve docs or add a small module-entrypoint helper so HTTP
   endpoints, schedulers, and custom app-owned entrypoints naturally execute a
   registered module command before durable sends happen inside that handler.

8. Route-aware multi-transport ergonomics.
   ADR 0010 is accepted and partially applied. Add route-builder helpers and
   startup conflict validation only below the topology line: one durable
   envelope must map to exactly one outbound route, and Bondstone must not
   provision or validate provider-native broker topology.

9. ApiCompat/package validation.
   Keep the PublicApiGenerator baseline for human review, and add package
   validation or ApiCompat against the latest stable published package before a
   stronger compatibility promise.

10. Health/readiness guidance.
    Consider small health-check helpers or documented recipes for terminal
    outbox failures, stale direct inbox rows, worker dispatch failures, and
    future receive-buffer terminal failures. Keep broker health provider-native
    or app-owned.

## Easy Wins

- Add a "why is my operation still pending?" troubleshooting section that
  distinguishes source outbox pending/terminal failure, broker delivery,
  direct receive ambiguity, missing finalizer policy, target operation state,
  and result deserialization failure.
- Add examples for `IDurableOutboxInspector`, `IDurableInboxInspector`,
  `IDurableOperationFinalizer`, and `IDurableOperationExpirationProcessor` in
  one operations-focused sample or doc section.
- Add XML doc remarks to operation-result APIs making timeout, pending,
  failure, cancellation, and deserialization failure behavior hard to
  misunderstand.
- Add an explicit "direct receive vs buffered receive" diagram or sequence in
  docs before implementing the buffer.
- Add tests for outbox aggregate dispatch fairness or at least document the
  current registration-order behavior as intentional pending review.
- Add a minimal diagnostic-code enum or constants draft behind an ADR before
  changing exception types broadly.

## Progress Notes

2026-06-17 operation visibility quick wins started:

- Added operations troubleshooting for pending durable operations, including
  source outbox terminal failures, direct inbox ambiguity, broker-native
  surfaces, explicit finalization, and app-owned expiration.
- Added a direct receive versus future receive-buffer sequence in operations
  guidance.
- Added XML documentation remarks for current operation result reader,
  operation reader, result-state, and result-deserialization failure behavior.

Remaining after the operation visibility quick wins:

- Add or refine operations examples if the first consumer project needs more
  copy-pasteable runbook code.
- Decide whether operation-linked inspection filters should be implemented as
  narrow inspector overloads before a broader diagnostic snapshot API.

2026-06-17 operation-result API hardening applied:

- Added `DurableOperationWaitResult<TResult>` and
  `IDurableOperationResultReader.TryWaitForResultAsync<TResult>(...)` overloads
  for operation-id-only, module-hinted, and handle-based waits.
- Kept existing `WaitForResultAsync<TResult>()` timeout behavior unchanged:
  caller timeout still throws `TimeoutException` and does not write operation
  state.
- Deferred operation-linked inspection filters and a broader diagnostic
  snapshot API until consumer evidence shows the simple wait API is
  insufficient.

## Implementation Order

Start with small visibility and ergonomics slices before large receive-buffer
runtime work. This should make the first consumer project easier to support
even if the durable receive buffer takes more than one implementation pass.

### 0. Convert Plan To Work Items

Create GitHub Issues or Project items for the implementation slices below
before starting broad code changes. Keep ADR-gated items explicit so a task
does not quietly change durable ownership while looking like a small cleanup.

### 1. Operation Visibility Quick Wins

Do first because these are low-risk and immediately useful during the consumer
trial:

- add a "why is my operation still pending?" troubleshooting section;
- add operations examples for outbox inspection, inbox inspection, operation
  finalization, and operation expiration;
- add XML doc remarks for operation-result timeout, pending, terminal failure,
  cancellation, and result-deserialization behavior;
- document direct receive versus buffered receive as a sequence before
  implementing buffered receive.

This slice should not add new durable state or infer operation failure from
transport, inbox, broker, or outbox state.

### 2. Operation-Result API Hardening

Implement the additive non-throwing wait/read API described by ADR 0016. Keep
the existing throwing wait API for compatibility and tests.

Then add operation-linked inspection support if the design stays narrow:

- terminal outbox lookup by operation id and message id;
- unprocessed inbox lookup by message id, module, and handler/subscriber
  identity;
- documentation showing how these inspectors help explain a pending operation.

Defer a full "operation diagnostic snapshot" API until the narrow inspection
filters prove insufficient in the consumer trial.

### 3. Minimal Observability Contracts

Add the smallest stable metric vocabulary for Bondstone-owned state
transitions:

- outbox claimed, dispatched, retry scheduled, terminal failed, stale;
- direct receive handled, already processed, already received;
- operation finalized and operation expiration candidates/finalizations.

Add stable misconfiguration codes for the most common setup failures only.
Avoid broad exception-type churn unless an ADR approves it.

### 4. Worker Ergonomics Before New Workers

Harden the current outbox worker before adding receive-buffer workers:

- document the current aggregate dispatch registration-order behavior;
- review selected-module worker options;
- decide whether fairness needs code now or only operator guidance;
- add health/readiness recipes for terminal outbox rows, stale direct inbox
  rows, and repeated worker dispatch failures.

This is also the right point to decide whether hosted operation expiration is
still app-owned or deserves an opt-in worker.

### 5. Durable Receive-Buffer ADR Design Slice

Before implementation, amend ADR 0012 or create a follow-up ADR that locks the
receive-buffer shape:

- receive-buffer record identity and persisted fields;
- ingestion boundary and idempotency behavior;
- claim ownership, lease expiry, retry attempts, and terminal receive failure;
- interaction with inbox rows, operation state, and outgoing outbox rows;
- worker options, retention, and inspection surface;
- adapter handoff rules for RabbitMQ, Service Bus, and app-owned native loops.

Keep the direct receive path as the default unless a later ADR intentionally
changes that rule.

### 6. Durable Receive-Buffer Implementation Slice

Implement the receive buffer in thin vertical cuts:

1. provider-neutral contracts and records;
2. EF Core mapping and PostgreSQL behavior;
3. ingestion service and inspection API;
4. receive-buffer processing dispatcher/worker;
5. adapter opt-in integration;
6. integration tests for crash/retry/terminal failure behavior;
7. operations and setup docs.

Do not add cleanup mutation as part of this slice. Retention should start as
documentation, inspection, and app-owned cleanup.

### 7. Multi-Transport Ergonomics

After the receive and operation visibility work is stable, add route-builder
helpers and startup conflict validation for ADR 0010. Keep this below the
topology line: Bondstone chooses exactly one outbound dispatcher for a durable
envelope, but does not own provider-native topology.

### 8. Compatibility And Consumer Trial Gate

Before handing the line to the first consumer project:

- add ApiCompat or package validation against the latest stable published
  package;
- run `pnpm check`, `pnpm backend:test:integration`, and `pnpm backend:pack`;
- run the modular monolith sample and service-split broker proofs;
- record consumer-trial findings as GitHub Issues instead of expanding this
  plan indefinitely.

### 9. Explicitly Deferred

Do not start these until consumer evidence or a new ADR justifies them:

- default cleanup worker for durable tables;
- automatic operation failure inference from broker/dead-letter/outbox/inbox
  state;
- broad broker runtime ownership;
- public middleware/runtime pipeline;
- two-transport sample as a showcase rather than a real consumer need.

## ADR Gates

No new ADR was added by the 2026-06-17 review because ADRs 0012, 0013, 0016,
and 0010 already cover receive buffering, worker ownership, operation wait
ergonomics, and multi-transport routing direction.

Create or amend ADRs before implementation if the work chooses any of these
directions:

- making durable receive buffer the default receive model instead of optional;
- adding a default cleanup worker that mutates durable evidence;
- inferring operation failure automatically from outbox, inbox, broker, or
  receive-buffer state;
- making Bondstone own broker retry, dead-letter, delivery-count, topology, or
  subscription policy;
- adding a public application middleware/runtime pipeline beyond the current
  fixed module runtime sequence.

## After First Consumer Hardening

- Durable receive buffer design and implementation, including persistence
  records, leases, retry attempts, terminal receive failure state, worker
  options, retention, and tests.
- Finalized metrics and a stable metric instrument vocabulary for outbox,
  receive, inbox, operation finalization, and operation expiration outcomes.
- Stable misconfiguration error codes. Current exception messages remain
  diagnostic surfaces, but they are not a machine-readable error-code
  vocabulary.
- .NET package validation or ApiCompat against the latest stable v2 package
  after v2 ships, while keeping the current `PublicApiGenerator` baseline test
  as the human-readable public surface review tool.
- Service extraction proof expansion beyond the current local modular
  monolith path and thin adapter proofs, including broader extraction
  scenarios, route-aware multi-transport ergonomics, and a two-transport
  sample only when real demand appears.
- optional hosted operation expiration worker unless application feedback
  shows it belongs in Bondstone rather than app-owned scheduling.
- Optional cleanup worker, only after ADR review proves a safe default
  ownership model for retention and durable-evidence mutation.

## Verification

Executed v2 work that already landed before this review was moved into stable
docs and the application notes of the accepted ADRs linked above. This plan now
carries first-consumer hardening candidates plus longer-term handoff items.

The 2026-06-17 update reviewed architecture, operations, observability,
packaging, setup, public API, testing, ADR 0010, ADR 0012, ADR 0013, ADR 0016,
and ADR 0004. No executable verification was run because this change updates a
planning handoff only.

Later on 2026-06-17, operation visibility quick wins were applied to
operations guidance and XML documentation. Verification:
`pnpm exec prettier --check docs/operations.md docs/plans/post-mvp-architecture-review-plan.md`,
`dotnet build Bondstone.slnx --configuration Release`, and
`pnpm backend:test`.

Later on 2026-06-17, operation-result API hardening was applied with the
additive non-throwing wait API from ADR 0016. Verification:
`dotnet test tests/Bondstone.Tests/Bondstone.Tests.csproj --configuration Release --filter FullyQualifiedName~DurableOperationResultReaderTests`,
`BONDSTONE_UPDATE_PUBLIC_API_BASELINE=1 dotnet test tests/Bondstone.PublicApi.Tests/Bondstone.PublicApi.Tests.csproj --configuration Release`,
`dotnet build Bondstone.slnx --configuration Release`, and
`pnpm backend:test`.
