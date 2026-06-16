# Post-MVP Architecture And Consumer Feedback Plan

Date: 2026-06-16

This planning note captures the post-MVP architecture review, Bondstone 1.2.1
consumer feedback, and the next direction for module communication,
persistence, transport simplification, operational state, and result-returning
durable commands.

This is not a replacement for GitHub Issues or ADRs. Convert executable work
below into GitHub Issues using `docs/github-workflow.md`. Use ADR review before
changing public API, package boundaries, migration policy, compatibility,
transport/provider behavior, or durable operation-state semantics.

## Current Product Position

Bondstone should keep its narrow product lane:

- durable module boundaries first;
- explicit module ownership of handlers, persistence, and durable contracts;
- stable message identities that survive service extraction;
- local transactional outbox and receive-side inbox semantics;
- envelope dispatch and receive helpers that let apps own transport runtime;
- EF Core/PostgreSQL as the first supported durable persistence path;
- result-returning commands as a first-class part of module execution and
  durable operation observation.

Bondstone should not try to become a general-purpose bus runtime with every
feature provided by older ecosystems. Wolverine and Brighter are much broader
and have years of feature depth. Bondstone's useful difference is continuity
from modular monolith boundaries to service extraction.

## Simplified Communication Model

Bondstone should follow modular-monolith boundaries strictly:

- Modules should not reference other module implementation assemblies.
- App-owned entrypoints such as HTTP endpoints, schedulers, setup flows, and
  administrative jobs may execute one module command locally.
- A module handler must not synchronously execute another module's command.
- Cross-module writes use durable commands or integration events.
- Domain events stay private to the owning module unless module code
  explicitly maps them to public integration events.
- Cross-module reads use projections or explicit public read APIs, not command
  execution as a query shortcut.

Result-returning commands stay in the model. `ICommand<TResult>` means the
command has a semantic result. Local module execution can return that result
directly to an app entrypoint. Durable command sending accepts work and returns
operation metadata; callers observe the committed result later through
operation state.

## Simplified Transport Model

Bondstone should stop being a transport runtime. The core abstraction should be
durable envelope dispatch and receive, not broker topology.

Bondstone owns:

- durable envelope creation and serialization;
- persisted outbox claiming, leases, retry scheduling, and terminal dispatch
  failure state;
- outbound envelope dispatch through a small dispatcher contract;
- durable envelope parsing helpers for supported adapters;
- receive pipeline execution over `DurableMessageEnvelope`;
- inbox idempotency and module transaction participation.

Applications own:

- RabbitMQ/Rebus/Service Bus/native transport setup;
- queues, exchanges, topics, subscriptions, rules, and bindings;
- native consumers/processors and their lifecycle;
- broker retry, dead-letter, prefetch, concurrency, and monitoring policy;
- native acknowledgement or completion after Bondstone receive succeeds.

Local transport can keep local-specific routing configuration because there is
no broker subscription system to provide dispatch intent. That configuration
must not define a general transport topology abstraction. For real transports,
the app should pass explicit dispatch intent to Bondstone receive helpers, such
as target module for commands or subscriber module plus subscriber identity for
events.

This direction lets consumer apps use Rebus, RabbitMQ.Client, MassTransit,
native Service Bus processors, or a hand-written transport adapter while
Bondstone remains the durable module-boundary layer.

## Persistence Scope

Keep provider-specific concurrency where it matters:

- outbox claiming;
- claim ownership;
- claim lease renewal;
- retry and terminal-failure outcome recording;
- inbox unique-key idempotency;
- transaction boundaries that commit handler state, inbox markers, outbox
  rows, and operation outcomes together.

For the next MVP, support EF Core with PostgreSQL durability semantics. The
direct Dapper-backed `Bondstone.Persistence.Postgres` package has been removed
from the public product surface until a real non-EF consumer need appears.
Provider-neutral persistence contracts may stay only where they keep the
EF/PostgreSQL implementation honest without pretending multiple providers are
imminent.

## Pipeline Scope

The module runtime now uses fixed internal command and event subscriber
sequences, accepted in ADR 0059.

Command execution runs:

1. provider transaction runners;
2. durable operation completion behavior;
3. receive inbox behavior;
4. module execution context;
5. command validation;
6. handler;
7. provider post-handler actions.

Event subscriber execution runs:

1. provider transaction runners;
2. receive inbox behavior;
3. module execution context;
4. handler;
5. provider post-handler actions.

Bondstone no longer provides application pipeline behavior contracts.
Validation remains `ICommandValidator<TCommand>`. Authorization, logging,
auditing, and metrics belong in handlers, DI decorators, endpoint filters,
host middleware, or application frameworks selected by the consumer app.
Generalized public capability pipeline contributions, named runtime slots, and
contribution ordering were removed.
`ModuleRuntimeFeatureCollection`, `IModuleRuntimeExecutionContext`, and
`IModuleTransactionFeature` remain as hidden provider/runtime coordination
contracts until EF transaction and EF domain event behavior no longer need
cross-package observed-commit coordination.

## Consumer Feedback From 1.2.1

### P1: XML API Docs Missing From NuGet Packages

Consumer validation of Bondstone 1.2.1 found package README files, but no XML
API documentation files beside the package assemblies.

Impact:

- IDE IntelliSense remains weak even when source comments exist.
- Public setup and command/result APIs are harder to discover.
- Package publication may not be using the same effective configuration as
  local pack verification.

Acceptance criteria:

- Every public Bondstone package includes XML docs beside the DLL under
  `lib/net10.0/`.
- Example package layout includes both `Bondstone.dll` and `Bondstone.xml`.
- Consumer-facing APIs show IntelliSense after installing from NuGet.
- Pack verification proves the XML files exist in produced `.nupkg` files.

Likely work:

- Verify `GenerateDocumentationFile` and pack output for all packable projects.
  Applied: packable projects generate XML documentation through
  `Directory.Build.targets`.
- Add a package artifact test that opens each `.nupkg` and asserts matching
  `.xml` files under `lib/net10.0/`. Applied:
  `tests/Bondstone.Package.Tests` runs from `pnpm backend:pack`.
- Confirm CI/release pack uses the same configuration as local pack.

### P1: Local Transport Worker Delivery Must Preserve Inbox Semantics

Consumer validation of Bondstone 1.2.1 found local transport delivery ran the
target handler successfully, but did not persist the consumer inbox row after
worker delivery. Direct `IModuleCommandReceivePipeline.HandleOnceAsync(...)`
did persist inbox state, so local transport behavior differed from direct
receive pipeline behavior.

Impact:

- Local transport tests can miss receive idempotency problems.
- Consumers expect local transport to behave like a transport adapter for
  development and tests, even though it is not broker-durable.
- A local transport path that bypasses configured module persistence weakens
  the modular monolith adoption path.

Acceptance criteria:

- First local transport delivery creates a processed consumer inbox row.
- Duplicate local transport delivery skips handler execution through inbox
  idempotency.
- Handler state and inbox processing are transactionally consistent with the
  configured module persistence provider.
- Tests compare local transport delivery with direct receive pipeline
  execution for command delivery and duplicate delivery.

Likely work:

- Audit local transport service lifetimes and receive pipeline resolution.
- Add or strengthen consumer-style integration coverage that dispatches
  through the outbox worker, not only direct route calls.
- Verify local transport against EF/PostgreSQL module persistence.

### P2: Document Operation-State Diagnostic Column Migration

Bondstone 1.2.1 added optional operation-state diagnostic fields:

- `ModuleName`;
- `MessageTypeName`;
- `HandlerIdentity`.

Impact:

- EF consumers upgrading from 1.2.0 need an application migration.
- The columns are optional/backward-compatible, but that must be explicit.

Acceptance criteria:

- Release notes mention the operation-state schema change.
- Setup or migration docs tell EF consumers to add/apply a migration when
  upgrading.
- Docs state that the columns are nullable and old rows may omit diagnostic
  context.

## Architecture Review Findings

### Operational State Is The Highest Priority Gap

Current operation state is useful for happy-path durable command result
observation, but incomplete for production incidents:

- send writes `Pending` for caller-supplied operation ids;
- successful durable command receive writes `Completed`;
- default command receive does not write `Failed`;
- transport/broker dead-letter outcomes do not update operation state;
- terminal outbox rows now have a first-class read-only inspection API, but no
  reset/replay mutation API;
- already-received but unprocessed inbox rows are intentionally loud, but
  operator recovery is fully application-owned.

This makes result polling vulnerable to "pending forever" when delivery or
handler execution fails permanently.

### Result-Returning Commands Are Product-Critical

Result-returning commands should stay. Without them, Bondstone is much less
useful for real modular-monolith flows where HTTP endpoints, schedulers, or
orchestrators need module behavior plus a typed result.

The issue is not the existence of result-returning commands. The issue is that
durable result observation needs a stronger terminal-state story.

### Operation Reads Need A Scalability Plan

The current global operation reader can query every module-owned
operation-state store for one operation id. This is simple and works for small
module counts, but it becomes inefficient when:

- many modules are configured;
- result endpoints poll often;
- services are split;
- modules use independent databases or service providers.

The operation id likely needs routing metadata or a locator path.

### Diagnostics Are Valuable But Scattered

Diagnostics are valuable, but the pre-simplification shape was spread across:

- provider-specific topology diagnostic records;
- provider-specific validators;
- a neutral aggregate outbox route validator;
- worker log messages;
- operation result diagnostic context;
- persistence error messages.

This makes failures easier to debug than before, but harder to explain through
one consumer-facing report.

### Public API Surface Is Too Broad To Ignore

Many concrete runtime and provider types remain public for advanced
composition or because early extraction made them visible. The public API
baseline protects accidental changes, but broad public implementation surface
creates compatibility pressure.

Before stronger compatibility promises, classify and reduce exposure where
possible:

- normal setup APIs;
- user application contracts;
- advanced composition APIs;
- provider/runtime contracts;
- public implementation details exposed for now.

### Samples Prove The Happy Path, Not Operations

The modular monolith sample proves a strong happy path:

- module-owned registration;
- EF/PostgreSQL persistence;
- local transport;
- RabbitMQ transport;
- domain event persistence;
- inbox idempotency;
- operation result polling.

Missing sample coverage:

- failed handler and poison-message behavior;
- terminal outbox inspection and replay/reset guidance;
- stuck inbox row inspection;
- operation failure state;
- extracted-service host shape.

## Operational State Direction

### Recommended Simplification

Keep operation state as a caller-visible workflow/result read model. Do not
turn it into the delivery ledger.

Delivery ledgers remain separate:

- outbox rows own outgoing dispatch attempts, retry scheduling, and terminal
  dispatch failure;
- inbox rows own receive idempotency and processed markers;
- broker or app transport runtime owns provider-native delivery exhaustion;
- operation state summarizes the caller-visible operation outcome.

The simplification is to make operation state a small state machine with clear
ownership rather than an inferred mirror of outbox, inbox, and broker state.

Recommended core statuses:

- `Pending`: accepted for durable work, not yet started by the target handler.
- `Running`: target handler execution started, when Bondstone can safely write
  that transition inside the target module boundary.
- `Completed`: target handler committed and optional result payload stored.
- `Failed`: Bondstone or application policy has determined the operation will
  not complete successfully.
- `Cancelled`: application-owned cancellation outcome.

Rules:

- Bondstone should continue to write `Pending` on durable send when the caller
  supplies an operation id.
- Bondstone should write `Completed` only after the target module transaction
  commits.
- Bondstone should not pretend every transient handler exception is terminal.
- A terminal failure should require a clear policy boundary.

### Failure Policy Options

Option A: Application-owned failure only.

- Bondstone keeps default behavior and documents that `Failed` and `Cancelled`
  are application-owned.
- Lowest implementation risk.
- Weak consumer experience because result polling can remain pending forever.

Option B: App/transport handoff writes failure after delivery exhaustion.

- App-owned transport code or a thin adapter exposes a hook for
  dead-letter/exhausted delivery.
- Operation state can be marked `Failed` when the app or adapter can prove
  final delivery exhaustion.
- Hard to make provider-neutral because Rebus, RabbitMQ, Service Bus, and
  other transports expose different failure semantics.

Option C: Bondstone receive failure policy writes failure after configured
attempts.

- Bondstone records receive attempts and marks operation `Failed` after a
  configured threshold.
- This requires receive failure persistence, not just inbox rows.
- Risk: duplicating broker retry policy and creating confusing double retry
  semantics.

Option D: Explicit operation timeout/expiry policy.

- Applications configure operation expiry by operation kind or module.
- A maintenance job marks old `Pending` or `Running` operations as `Failed` or
  `Cancelled` with a reason.
- This is scalable and provider-neutral, but time-based rather than causally
  tied to delivery exhaustion.

Recommended direction:

- Start with Option D plus explicit application failure APIs.
- Add provider-specific dead-letter handoff helpers later only where a provider
  can make the outcome clear.
- Avoid building a provider-neutral receive retry/DLQ abstraction now.

### Operation Locator

Introduce a way to avoid querying every module store for every operation read.

Candidate approaches:

- Return a `DurableOperationHandle` or equivalent source/target module
  metadata in `DurableCommandSendResult`. Applied with
  `DurableCommandSendResult.Operation`.
- Add optional module hints to `IDurableOperationReader` and
  `IDurableOperationResultReader`.
- Persist a lightweight operation locator row in the source module store when
  sending with an operation id.
- Store enough operation owner metadata in the operation id response for
  callers to query a module-scoped reader.

Recommended first step:

- Add overloads that accept an optional module hint before introducing a new
  locator table.
- Keep the global reader as compatibility behavior for small hosts and tests.
- Document that high-module-count consumers should prefer module-scoped or
  hinted reads.
- Treat the target module as the owner of completed/failed result state. Source
  module `Pending` state is an acceptance receipt.
  Applied: handle-based reads use the target module hint from
  `DurableOperationHandle`.

### Result-Returning Commands

Keep both paths:

- `IModuleCommandExecutor.ExecuteResultAsync` for local in-process module
  execution with typed result.
- Durable send plus `IDurableOperationResultReader` for accepted delivery and
  explicit result observation.

Do not make durable send look like RPC. The API should continue to return
acceptance metadata, not `TResult`.

Needed improvements:

- Better terminal failure semantics.
- Better result polling guidance for HTTP APIs.
- Operation result docs that show `Pending`, `Failed`, `Cancelled`,
  `CompletedWithoutResult`, and deserialization failure handling.
- A sample endpoint that demonstrates timeout-bounded wait and single-read
  status polling.

## Work Plan

### Completed: Public Package Surface Cut

Applied on 2026-06-16:

- Removed `Bondstone.Persistence.Postgres` from the active product surface by
  deleting the package and matching tests, removing it from `Bondstone.slnx`,
  removing it from the default public API baseline matrix, and converting the
  billing sample to EF Core/PostgreSQL persistence.
- Removed `Bondstone.Transport.ServiceBus` from the active product surface by
  deleting the package and matching tests, removing it from `Bondstone.slnx`,
  removing it from the default public API baseline matrix, and removing
  Service Bus from active composition tests.
- Kept `Bondstone.Persistence.EntityFrameworkCore` and
  `Bondstone.Persistence.EntityFrameworkCore.Postgres` as the supported
  PostgreSQL persistence path.
- Kept `Bondstone.Transport.Local` as the explicit dev/test/sample transport
  path and `Bondstone.Transport.RabbitMq` as the remaining direct broker
  adapter/sample path without expanding its runtime scope.
- Updated stable packaging, package discovery, setup, architecture, testing,
  sample, public API, and package README docs to describe the current
  supported surface.

### Completed: Envelope Dispatch Vocabulary Cut

Applied on 2026-06-16:

- Renamed the neutral outbox handoff from `IDurableOutboxTransport` and
  `IDurableOutboxTransportRoute` to `IDurableEnvelopeDispatcher` and
  `IDurableEnvelopeDispatchRoute`.
- Renamed `RoutedDurableOutboxTransport` to
  `RoutedDurableEnvelopeDispatcher` and
  `RabbitMqDurableOutboxTransport` to `RabbitMqDurableEnvelopeDispatcher`.
- Changed the neutral envelope handoff method from `SendAsync(...)` to
  `DispatchAsync(...)`.
- Kept package-level `UseLocalTransport(...)` and
  `UseRabbitMqTransport(...)` naming for now because those still describe the
  selected adapter package.
- Updated public API baselines, composition tests, local transport tests,
  RabbitMQ tests, and stable docs for the new vocabulary.

### Now: Patch Consumer-Visible 1.2.x Gaps

1. Create ADR coverage for the post-MVP communication and transport
   simplification.
2. Add a guardrail that prevents module handlers from synchronously executing
   another module's command through `IModuleCommandExecutor`. Applied.
3. Verify XML docs are produced and included in `.nupkg` files. Applied.
4. Add package artifact tests for XML docs beside DLLs. Applied.
5. Fix local transport delivery so worker dispatch preserves consumer inbox
   persistence. Applied.
6. Add local transport integration tests for first delivery and duplicate
   delivery. Applied.
7. Document the operation-state diagnostic column migration. Applied.

### Applied: Consumer-Visible 1.2.x Gaps

- Cross-module synchronous command execution through
  `IModuleCommandExecutor` is blocked while a module handler is already
  running. Same-module nested local execution remains allowed.
- Package verification now opens produced `.nupkg` files and asserts XML
  documentation files sit beside package assemblies under `lib/net10.0/`.
- Local transport delivery preserves target-module receive inbox persistence
  and duplicate delivery idempotency through the same receive pipeline.
- Operation-state diagnostic columns are documented as nullable EF migration
  additions with no required backfill.

### Applied: RabbitMQ Transport DSL Cut

- Removed RabbitMQ command exchange, module route, event route, and outbound
  convention DSLs.
- Replaced outbound RabbitMQ routing with `DispatchCommandsTo(...)` and
  `DispatchEventsTo(...)` destination functions over the durable envelope.
- Kept RabbitMQ receive queue bindings as adapter-local routing metadata for
  receive helpers and the opt-in worker.
- Kept local transport as the correctness baseline over outbox dispatch and
  receive pipelines.

### Applied: Transport Diagnostics Package Removal

- Removed the separate `Bondstone.Transport` package and its public API
  baseline from the active product surface.
- Removed core aggregate startup topology diagnostics and the
  `BondstoneBuilder.AddTransportTopologyDiagnosticSource(...)` extension hook.
- Removed RabbitMQ topology diagnostics from the public/service surface.
- Kept route failures loud at dispatch/receive time through the concrete Local
  and RabbitMQ adapter paths.

### Next: Operational MVP

1. Create an ADR for operation-state semantics after publication. Accepted and
   applied as ADR 0057.
2. Add an application-facing operation-state writer or failure marker API.
   Applied with `IDurableOperationFinalizer`.
3. Add operation expiry/timeout policy and a maintenance worker or documented
   application-owned job shape. Applied with
   `IDurableOperationExpirationProcessor`; hosted worker remains deferred.
4. Add operator query APIs for terminal outbox rows. Applied with
   `IDurableOutboxInspector` and `IDurableOutboxInspectionStore`.
5. Add documented reset/replay guidance for terminal outbox rows. Applied as
   read-only inspection guidance; reset/replay remains application-owned.
6. Add documented inspection guidance for already-received unprocessed inbox
   rows. Applied with `IDurableInboxInspector` and
   `IDurableInboxInspectionStore`; row mutation and handler replay remain
   application-owned.
7. Add tests proving `Failed` operation results are observable and do not poll
   forever. Applied.

### Then: Scalability And API Curation

1. Add module-hinted operation result read overloads. Applied.
2. Keep global operation reads as compatibility behavior. Applied.
3. Review public implementation types and classify cleanup candidates.
   Applied for the current persistence inspection surface; no additional
   concrete implementation type is promoted to cleanup candidate yet.
4. Reduce public exposure where compatibility allows. Deferred until a whole
   obsolete capability, package, or misleading setup path is identified.
5. Add XML docs for normal setup and user application contracts first.
   Applied for operation-state reads, inbox inspection, and outbox inspection.
6. Keep advanced/provider public APIs documented as advanced rather than
   preferred setup paths. Applied for inspection stores and module
   inspection-store registrations.

### Then: Diagnostics Simplification

1. Keep diagnostics around persistence, outbox dispatch, receive pipeline
   execution, inbox idempotency, and operation results. Applied by keeping
   diagnostics on runtime-owned failure boundaries instead of restoring
   provider-neutral topology reports.
2. Add stable log event ids for the outbox worker and receive helper failure
   paths. Applied for `DurableOutboxWorker` dispatch failures and RabbitMQ
   receive worker delivery handling failures.
3. Add a small diagnostic report only if the simplified model still needs one.
   Deferred; the simplified model does not currently justify a new report.

### Applied: Runtime Pipeline Simplification

Preparatory work applied on 2026-06-16:

- Domain event contracts moved into the core `Bondstone` package under
  `Bondstone.DomainEvents`.
- EF-backed domain event persistence moved into
  `Bondstone.Persistence.EntityFrameworkCore`.
- Removed the old `Bondstone.Capabilities.DomainEvents` and
  `Bondstone.Capabilities.DomainEvents.EntityFrameworkCore` packages and their
  standalone tests.
- Removed `IDomainEventHandler<TDomainEvent>` from the active public API
  because Bondstone still has no accepted local domain-event dispatch model.

Applied on 2026-06-16:

- Accepted ADR 0059 for the fixed module runtime sequence.
- Removed public module pipeline contribution records, contribution registry,
  public step-kind/order types, and module builder contribution methods.
- Replaced contribution registry/planner selection with fixed command and
  event subscriber runtime sequences.
- Superseded ADR 0059 with ADR 0060 and removed public application pipeline
  behavior contracts.
- Replaced fixed-slot pipeline behavior contracts with direct runtime
  services.
- Moved EF transaction behavior from generic pipeline contribution to a
  hidden transaction runner.
- Moved EF domain event persistence from generalized capability contribution
  to EF-owned module opt-in metadata plus a hidden post-handler action.
- Kept `ModuleRuntimeFeatureCollection`, `IModuleRuntimeExecutionContext`,
  and `IModuleTransactionFeature` as hidden provider/runtime contracts because
  EF transaction and EF domain event behavior still need cross-package
  observed-commit coordination.
- Refreshed public API baselines, module architecture docs,
  EF/domain-event architecture docs, and affected runtime tests.

### Then: Extraction Proof

1. Add a service-split sample or test slice that extracts one module into a
   separate host.
2. Keep contract assemblies, message identities, and handler patterns stable.
3. Use RabbitMQ as the first broker-boundary proof, or app-owned native
   transport code if a later ADR reintroduces another thin adapter.
4. Prove operation result observation across the split.
5. Document what changes and what stays stable during extraction.

## Open Questions

- Should operation-state expiry be package-owned in `Bondstone.Hosting`, or
  provider-owned beside operation-state stores?
- Resolved: `DurableOperationHandle` is a first-class public value and
  `DurableCommandSendResult.Operation` carries it when a durable operation id
  is supplied.
- Should receive failure attempts be persisted in a new receive-failure table,
  or should Bondstone avoid that until a provider-specific need is proven?
- Should terminal outbox replay stay only documented app-owned SQL/provider
  procedure, or should a future provider-specific mutation helper be accepted
  after a real runbook emerges?
- Should XML API docs be required by a pack artifact test for every package

## Added Pivot Work

### ADR History Restart

After the current simplification plan is implemented and verified, rebuild the
active ADR set from the applied architecture rather than continuing to layer
new amendments on top of the existing circular history.

Intent:

- create a small current ADR set that describes the architecture Bondstone is
  actually keeping;
- archive or supersede stale, deferred, amended, and superseded ADR material
  while preserving traceability;
- keep stable docs as the current operating contract;
- make future decisions easier to navigate for humans and agents.

Constraints:

- This is explicitly approved as an unusual repository reset step.
- Do not erase accepted decision history; move obsolete material to archive or
  mark it superseded.
- Use repository ADR skills for the consolidation workflow.
- New ADRs should describe current durable decisions and near-term planned
  direction, not every discarded experiment.

Candidate new ADR groups:

- module communication and modular-monolith boundaries;
- durable command/event envelope model;
- EF/PostgreSQL persistence and provider boundaries;
- operation state, handles, polling, finalization, and expiry;
- transport adapter ownership;
- domain event persistence;
- runtime sequence and provider runtime hooks;
- package surface and compatibility posture.

### Cleanup Sweeps

Run two broad cleanup passes after the feature/pivot slices settle.

Code cleanup sweep:

- remove dead types, stale names, unused tests, and obsolete helpers;
- dedupe similar diagnostics and registration validation paths;
- reduce public API where it is still merely accidental;
- keep provider/runtime contracts only when cross-package collaboration truly
  requires them.

Documentation cleanup sweep:

- collapse duplicated setup/package/architecture language;
- remove stale transport, pipeline, and direct PostgreSQL guidance;
- align samples with the simplified library story;
- ensure package discovery, public API inventory, ADRs, and setup docs agree.

Test cleanup sweep:

- rename stale pipeline-era test names;
- remove tests that only verify deleted extension points;
- keep focused regression tests for local transport, inbox/outbox, operation
  handles, result polling, EF transactions, and domain event persistence.
  before publication?

## Non-Goals For The Next Slice

- Do not build a provider-neutral broker administration layer.
- Do not build a provider-neutral receive retry or DLQ abstraction yet.
- Do not add or expand production transport runtimes before simplifying the
  current transport surface.
- Do not remove result-returning commands.
- Do not collapse commands, integration events, and domain events into one
  generic message abstraction.
