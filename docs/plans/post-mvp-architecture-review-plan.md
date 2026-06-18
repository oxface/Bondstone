# Post-MVP Architecture Review Plan

Date: 2026-06-16
Review update: 2026-06-18

## Purpose

This plan prioritizes the architecture work identified after Bondstone's
public MVP publication and initial consumer feedback.

The plan is intentionally a planning handoff, not a durable architecture
contract. Use the accepted ADRs below as decision boundaries while moving
implementation tasks into GitHub Issues or GitHub Projects.

The 2026-06-18 design reset reframes the plan as the v2 MVP replacement for
the over-ambitious v1 line. Compatibility with the current public packages is
not a constraint because the only current consumer can migrate from zero. The
goal is to ship a clean library for module execution, durable messaging, and a
single durable inbox receive model, then enter feedback-focused maintenance.

## Accepted Or Amended ADRs

- [0011 OTel Native Diagnostics And Misconfiguration Reporting](../adr/0011-otel-native-diagnostics-and-misconfiguration-reporting.md)
- [0013 Worker Boundaries And Transport Adapter Ownership](../adr/0013-worker-boundaries-and-transport-adapter-ownership.md)
- [0014 Production Operations And Lifecycle Guidance](../adr/0014-production-operations-and-lifecycle-guidance.md)
- [0015 Service Extraction Proof Before Broad Bus Features](../adr/0015-service-extraction-proof-before-broad-bus-features.md)
- [0016 Non-Throwing Operation Wait Ergonomics](../adr/0016-non-throwing-operation-wait-ergonomics.md)
- [0017 Single Durable Inbox Incoming Ledger](../adr/0017-single-durable-inbox-incoming-ledger.md)

Accepted v2 design reset ADRs, with application pending:

- [0018 V2 Module Execution And Durable Inbox Reset](../adr/0018-v2-module-execution-and-durable-inbox-reset.md)
- [0019 Operation Observation Not Orchestration](../adr/0019-operation-observation-not-orchestration.md)
- [0020 Host-Owned Transport With Module-Aware Bindings](../adr/0020-host-owned-transport-with-module-aware-bindings.md)

Superseded receive-side trail:

- [0012 Direct Receive Inbox And Durable Receive Buffer](../adr/0012-direct-receive-inbox-and-durable-receive-buffer.md)

## Review Findings

Bondstone's current architecture is directionally strong. The core value is
not being a smaller general-purpose bus; it is stable, durable module
boundaries with a credible path from modular monolith to service extraction.

The highest-risk gap before handing Bondstone to the first consumer project is
receive-side coherence. The incremental design now has too much transitional
language around direct receive, durable incoming inbox, old inbox identity, and
future buffered receive. V2 should present one durable receive ledger: durable
inbox.

The second high-risk gap is module ingress ergonomics. Consumers should not
have to choose between raw handler calls and durable broker-like receive for
every HTTP endpoint or `.Contracts` call. V2 should expose clear command and
query execution pipelines, with durable inbox as the durable ingress rather
than the only execution path.

The third high-risk gap is operation-result positioning. Operation state and
results remain valuable, but result waiting must not be presented as
orchestration. Durable inter-module continuations remain app-owned until a
future saga/process-manager feature is accepted.

Diagnostics are not currently too scattered in code. The bigger issue is that
metrics and machine-readable misconfiguration codes are not yet stable.

## Worker Topology

The v2 target topology has three durable worker/listener roles:

- Source outbox worker: current. It claims source-module outbox rows, dispatches
  envelopes to the configured dispatcher, and records dispatched, retry, stale,
  or terminal-failed outbox state.
- Transport ingestion worker or app-owned native listener: transport to
  Bondstone durable inbox record, then native settlement after durable
  ingestion.
- Durable inbox processing worker: Bondstone-owned delivery from durable inbox
  to the target module command or event subscriber pipeline, recording
  processed, retry, stale, and terminal outcomes.

Direct command execution and query execution are not workers. They are
same-process module pipeline calls for HTTP, app-owned entrypoints, tests, and
explicit direct `.Contracts` usage.

A generic cleanup worker is not current behavior and should not be added as a
default background service without ADR review. Cleanup mutates durable evidence
such as processed inbox rows, dispatched outbox rows, operation states, domain
event records, terminal outbox failures, and future durable inbox records.
Retention may be useful, but defaults can destroy operational evidence. Prefer
first adding inspection, runbook guidance, filters, and explicit app-owned
cleanup samples.

## Before First Consumer Project

These items are the proposed v2 MVP scope before the first consumer project
starts migration.

1. Design reset review.
   ADRs 0018-0020 are accepted and pending implementation. Use the design reset
   report as the implementation handoff for command execution, query
   execution, durable command send, durable event fanout, HTTP durable ingress,
   operation observation, and the durable inbox worker topology.

2. Durable inbox completion sweep.
   Make durable inbox the only durable receive ledger. Remove, collapse, or
   hide old direct-receive inbox semantics. Complete the end-to-end proof:
   transport delivery, durable inbox ingestion, hosted worker claim/lease,
   module handler execution, outgoing outbox, operation finalization where
   applicable, and processed/failure outcomes.

3. Module execution and query ergonomics.
   Clean up direct module command execution as the immediate command pipeline.
   Add module query contracts, handlers, executor, diagnostics hooks, tests,
   and docs. Add small HTTP/minimal API helper ideas only if they remain
   library-shaped and do not require code generation.

4. Operation observation cleanup.
   Reposition operation APIs around status/result observation. Keep optional
   short wait helpers only as edge-facing convenience. Remove documentation
   that implies operation waiting is saga/orchestration. Add operation-linked
   inspection filters if they are needed to explain pending durable work.

5. Transport and fanout ergonomics.
   Keep transport infrastructure host-owned while durable runtime semantics
   stay module-owned. Improve receive-worker language and helpers around
   module command queues and event subscriber bindings. Decide Service Bus
   durable inbox ingestion parity. Keep event fanout on native
   exchanges/topics/subscriptions.

6. Worker operations and retention.
   Finish durable inbox lease-renewal policy or document processing limits.
   Add durable inbox inspection ergonomics, health/readiness recipes, and
   explicit app-owned cleanup/retention guidance. Do not add a default cleanup
   worker without a separate accepted ADR.

7. Stable misconfiguration error codes.
   Keep exception messages human-readable, but add a small stable code
   vocabulary for common setup failures: missing module persistence, missing EF
   mappings, missing dispatcher, duplicate module durable registrations,
   invalid durable identities, missing receive binding, and ambiguous dispatch
   routes.

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
    outbox failures, durable inbox stale processing rows, worker dispatch
    failures, and durable inbox terminal failures. Keep broker health
    provider-native or app-owned.

11. Final v2 cleanup sweep.
    Remove stale future-tense docs outside ADRs and `docs/todos` if that folder
    is introduced. Reset sample migrations immediately before v2 publication.
    Re-run public API review, package validation, full tests, docs review, and
    prepare the template-project migration handoff prompt.

## Easy Wins

- Draft the design reset diagram/report before code execution so subsequent
  agents implement the same model.
- Add a "why is my operation still pending?" troubleshooting section that
  distinguishes source outbox pending/terminal failure, durable inbox pending,
  retry, stale processing, terminal failure, missing finalizer policy, target
  operation state, and result deserialization failure.
- Add examples for `IDurableOutboxInspector`, `IDurableInboxInspector`,
  `IDurableOperationFinalizer`, and `IDurableOperationExpirationProcessor` in
  one operations-focused sample or doc section.
- Rename or reposition wait-oriented operation docs as operation observation.
- Add the module query pipeline in one coherent slice, with command/query docs
  updated together.
- Add tests for outbox aggregate dispatch fairness or at least document the
  current registration-order behavior as intentional pending review.
- Add a minimal diagnostic-code enum or constants draft behind an ADR before
  changing exception types broadly.

## Progress Notes

2026-06-17 operation visibility quick wins started:

- Added operations troubleshooting for pending durable operations, including
  source outbox terminal failures, direct inbox ambiguity, broker-native
  surfaces, explicit finalization, and app-owned expiration.
- Added a direct receive versus future durable inbox sequence in operations
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

2026-06-17 minimal observability metrics applied:

- Added OpenTelemetry-native .NET counters for Bondstone-owned outbox
  transitions: claimed, dispatched, retry scheduled, terminal failed, and
  stale.
- Added direct receive counters for handled, already processed, and already
  received ambiguous outcomes.
- Added operation counters for explicit finalization plus expiration
  candidates and expiration finalizations.
- Kept metric dimensions deliberately small: module, message kind,
  source/target module, and operation status only where applicable. Message
  ids, operation ids, exception messages, payload data, broker delivery counts,
  topology, dead-letter state, and provider-native monitoring remain outside
  the metric contract.
- Left stable misconfiguration error codes deferred because implementing them
  cleanly would require a separate exception/error-code design pass.

2026-06-17 worker ergonomics documentation hardening applied:

- Documented the current aggregate outbox dispatch contract as registration
  order over module dispatchers, one shared `BatchSize`/`maxCount` budget,
  failure propagation to the hosted worker, and no current fairness or
  selected-module scheduling guarantee.
- Added app-owned health/readiness recipes for terminal outbox failures, stale
  direct inbox rows, repeated outbox worker batch failures, and operation
  expiration backlog.
- Deferred selected-module outbox worker options. The current hosted worker
  resolves the app-facing `IDurableOutboxDispatcher`, while selected-module
  targeting must apply only to Bondstone-owned module outbox dispatcher
  registrations and not to custom/root dispatchers. That needs a small public
  option and composition design rather than a quiet worker tweak.
- Deferred a hosted operation expiration worker. The existing
  `IDurableOperationExpirationProcessor` is intentionally app-scheduled, and a
  default worker would make cadence, cutoff, terminal status, and user-visible
  reason look library-owned too early.
- Did not implement cleanup worker behavior. Cleanup mutates durable evidence
  and still requires ADR review before any default worker.

2026-06-17 durable inbox design slice applied, later superseded by ADR 0017:

- Amended ADR 0012 with the optional separate-buffer identity, structured
  persistence record, ingestion boundary, processing worker responsibilities,
  failure semantics, package ownership, worker split, observability vocabulary,
  migration ownership, and compatibility constraints.
- Kept direct receive as the default and confirmed buffered receive remains
  opt-in.
- Updated stable docs to describe the future buffered receive path as
  non-current behavior while preserving the current direct receive, inbox,
  outbox, operation-state, worker, and broker ownership contracts.
- Deferred runtime contracts, EF mappings, PostgreSQL concurrency behavior,
  worker options, inspection contracts, adapter handoff hooks, migrations,
  public API review, and tests to implementation work items.

2026-06-18 design reset discussion captured and accepted:

- Agreed that v2 can break compatibility and should optimize for a clean
  library model.
- Agreed that durable inbox should become the only durable receive ledger.
- Agreed that module command execution and module query execution should be
  first-class direct pipelines, while durable inbox remains the durable receive
  ingress.
- Agreed that `.Contracts` projects share module APIs; they should not imply
  raw handler calls.
- Agreed that operation tracking is observation and edge-facing result
  retrieval, not orchestration. Sagas/process managers are deferred.
- Agreed that transport topology and event fanout remain host-owned, with
  module-aware receive bindings and durable inbox ingestion boundaries.
- Captured the guiding split: host-owned transport infrastructure,
  module-owned durable runtime semantics. This supports later module extraction
  without making module implementation code own broker topology.
- Accepted ADRs 0018-0020 as the design-reset package before large
  implementation chunks begin. Application is pending until implementation and
  stable docs are updated.

2026-06-17 durable inbox implementation slice 1 applied:

- Added provider-neutral durable inbox contracts and records in
  `Bondstone.Persistence`: receive identity key, incoming ledger record,
  status/state, ingestion result, retry/terminal failure decision, ingestion
  store, claimer, lease renewer, outcome recorder, and read-only inspection
  store.
- Kept the slice below runtime behavior. No EF mapping, PostgreSQL SQL,
  hosted worker, adapter handoff, direct receive behavior change, operation
  failure inference, or cleanup/retention mutation API was added.
- Kept broker delivery counts, dead-letter state, settlement state, and
  topology out of the provider-neutral record and contracts.
- Added focused unit coverage for durable inbox key, record, state,
  ingestion result, and failure decision validation.

2026-06-17 durable inbox EF Core mapping slice applied:

- Added the provider-neutral EF Core durable inbox entity and configuration
  with a separate `incoming_inbox_messages` table, composite receive identity
  primary key, structural durable envelope columns, incoming inbox state columns, and
  indexes for future due-record claiming and inspection.
- Added the granular `ApplyBondstoneIncomingInbox(...)` mapping helper and
  intentionally kept it out of `ApplyBondstonePersistence(...)` while receive
  buffering remains optional and non-runtime.
- Did not add EF stores, PostgreSQL SQL, hosted workers, adapter handoff,
  direct receive behavior changes, operation failure inference, or cleanup
  mutation APIs.
- Added focused EF entity round-trip and model metadata tests.

2026-06-17 durable inbox pivot applied:

- Added ADR 0017 and superseded the separate receive-buffer direction from ADR 0012.
- Accepted a single richer durable inbox incoming ledger as the target
  buffered receive model: durable envelope, receive identity, claim/lease,
  retry, terminal failure, and processed state in one incoming ledger.
- Confirmed `DurableIncomingInbox*` and `IDurableIncomingInbox*` as the
  accepted provider-neutral names for the single richer incoming ledger.
- Kept direct receive as the current default/simple path until a later ADR
  changes the default.

## Execution Model

This planning thread should stay the high-level orchestrator. Large
implementation chunks should get handoff prompts that include the accepted or
proposed ADR boundaries, the current plan item, expected tests, and the cleanup
rules for stable docs.

Use bigger coherent chunks rather than repeated two-file design edits. The
preferred rhythm is:

1. design reset diagram/report for review;
2. apply accepted ADRs 0018-0020;
3. run subagents for durable inbox completion, module command/query pipelines,
   operation observation, transport/fanout ergonomics, worker operations, and
   docs/API/sample cleanup;
4. merge and review each chunk as code, not as design discovery;
5. run one final deep sweep for design tightening, stale docs, tests, public
   API, sample migrations, and template-project handoff.

## Explicitly Deferred

Do not start these until consumer evidence or a new ADR justifies them:

- Bondstone-owned sagas, process managers, workflow state machines, replayable
  orchestration, durable timers, or generic command-result continuation.
- Polling another module's operation store from a caller module as a supported
  inter-module orchestration mechanism.
- Default cleanup workers that mutate durable evidence.
- Automatic operation failure inference from broker/dead-letter/outbox/durable
  inbox state.
- Broad broker runtime ownership, topology provisioning, subscription storage,
  broker retry, delivery-count abstraction, or provider-neutral DLQ handling.
- Code generation for HTTP endpoint binding or module contract plumbing.
- Two-transport samples as showcases before real consumer need.

## ADR Gates

ADRs 0018-0020 are accepted and pending. Apply them before implementation
changes that remove direct receive semantics, introduce query pipelines,
reposition operation waiting, or rename receive-worker topology concepts. If
implementation needs to change those boundaries, amend or supersede the ADRs
before broad code changes.

Create or amend ADRs before implementation if the work chooses any of these
directions:

- adding a saga/process-manager capability;
- adding a default cleanup worker that mutates durable evidence;
- inferring operation failure automatically from outbox, broker, or durable
  inbox state;
- making Bondstone own broker retry, dead-letter, delivery-count, topology, or
  subscription policy;
- adding a public application middleware/runtime pipeline beyond command/query
  execution helpers;
- resetting sample migrations as a release policy rather than a one-time v2
  cleanup.

## After V2 MVP

- Saga/process-manager design if real projects repeatedly need durable
  continuations.
- Projection/query helpers if direct module queries become a migration pain.
- Broader service extraction proofs beyond the first template-project
  migration.
- Optional hosted operation expiration worker if application feedback shows it
  belongs in Bondstone rather than app-owned scheduling.
- Optional cleanup worker, only after ADR review proves a safe default
  ownership model for retention and durable-evidence mutation.

## Verification

Executed v2 work that already landed before this review was moved into stable
docs and the application notes of the accepted ADRs linked above. This plan now
carries the v2 MVP orchestration shape plus longer-term deferred items.

The 2026-06-17 update reviewed architecture, operations, observability,
packaging, setup, public API, testing, ADR 0010, ADR 0012, ADR 0013, ADR 0016,
and ADR 0004. Later on 2026-06-17, ADR 0017 superseded ADR 0012's separate
durable incoming inbox direction. No executable verification was run for the planning
handoff updates.

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

On 2026-06-18, proposed ADRs 0018-0020 were created from the design reset
discussion and this plan was updated to describe the current v2 MVP execution
model. Later on 2026-06-18, ADRs 0018-0020 were tightened and accepted with
application pending. Verification: `pnpm format:check`.
