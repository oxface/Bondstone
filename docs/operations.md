# Production Operations

This guide describes the current production contract for running Bondstone
module boundaries. It is operational guidance, not a broker runtime contract:
Bondstone owns durable module state and neutral envelope handling, while the
application and chosen infrastructure own deployment policy, migrations,
broker topology, retry, dead-letter handling, and operator runbooks.

## Ownership Summary

Bondstone owns:

- durable message envelopes, stable message identity, trace context fields,
  and durable payload serialization;
- module command and event subscriber execution;
- EF mapping helpers for Bondstone outbox, inbox, operation-state, and optional
  domain event tables;
- the direct receive inbox idempotency boundary;
- persisted outbox retry state and terminal outbox failure state;
- the hosted outbox worker over Bondstone-owned outbox records;
- the opt-in hosted incoming inbox processing worker over Bondstone-owned
  durable incoming inbox records;
- read-only inspection contracts for terminal outbox rows and unprocessed inbox
  rows;
- explicit operation finalization and expiration APIs that application policy
  can call;
- OpenTelemetry-native metrics for Bondstone-owned outbox, direct receive, and
  operation finalization or expiration transitions.

The application owns:

- EF migrations, schema deployment, migration history, and production rollout;
- module names, database schemas, connection strings, and table retention
  policy;
- broker topology, provisioning, subscriptions, native consumers, retry,
  delivery counts, dead-letter policy, prefetch, concurrency, and monitoring;
- replay, reset, purge, archive, stale-inbox recovery, broker message movement,
  and compensating business actions;
- endpoint behavior around operation polling, timeout, cancellation,
  finalization, and user-visible status;
- durable contract evolution and serializer compatibility policy for its own
  message and result DTOs.

## Receive Semantics

Bondstone receive is a direct broker-to-module execution path today. A native
transport delivery is parsed into `DurableMessageEnvelope`, then
`IDurableEnvelopeReceiver` calls the command or event receive pipeline. The
pipeline resolves the stable message identity, deserializes the payload,
derives the inbox key, and executes the target handler inside the module
persistence boundary when the module has durable EF persistence.

For commands, the inbox key is the message id, target module, and stable
handler identity. For integration events, the inbox key is the message id,
subscriber module, and stable subscriber identity. Handler state, inbox
markers, operation-state updates, and outgoing outbox rows commit together in
the module transaction when EF module persistence is configured.

Already processed inbox rows are idempotent duplicate receives. Bondstone skips
the handler because the durable receive identity is already complete.
Bondstone emits a direct receive already-processed metric for this outcome.

Already received but unprocessed inbox rows are ambiguous. They can mean the
process failed after recording the receive but before the processed marker, or
they can mean application side effects happened but the process stopped before
Bondstone could record completion. Bondstone therefore raises
`DurableInboxAlreadyReceivedException` through the module receive path instead
of re-running the handler or silently treating the message as handled.
Bondstone emits a direct receive already-received metric for this ambiguous
outcome.

Bondstone does not currently provide tiny direct-inbox leases, a direct-inbox
stale-row sweeper, or direct-inbox failed receive state. ADR
[0017](adr/0017-single-durable-inbox-incoming-ledger.md) records the durable
inbox incoming-ledger direction. The incoming ledger has provider contracts,
PostgreSQL mutation stores, a host-callable processing dispatcher, and an
opt-in hosted incoming inbox processing worker. Provider-neutral transport
adapter handoff into durable ingestion is not implemented. ADR
[0012](adr/0012-direct-receive-inbox-and-durable-receive-buffer.md) preserves
the superseded receive-buffer decision trail.

Current direct receive has two runtime stages:

1. native transport delivery to Bondstone module receive;
2. handler state, inbox marker, operation state, and outgoing outbox commit in
   the target module persistence boundary.

The optional durable inbox model uses a three-worker topology:

1. the outbox dispatch worker claims and dispatches outgoing durable outbox
   rows;
2. a transport receive/ingestion worker or app-owned adapter loop reads native
   deliveries and records durable incoming inbox rows before native settlement;
3. the incoming inbox processing worker claims due durable incoming rows and
   hands them to module receive.

That topology splits receive into durable-processing stages:

1. native transport delivery to a Bondstone durable inbox incoming row;
2. host-called durable inbox processing claim and handoff to module receive;
3. handler state, operation state, outgoing outbox rows, and direct inbox
   processed state commit in the target module persistence boundary;
4. incoming durable inbox processed, retry, terminal failure, or stale outcome
   recording through claim-owner and lease-aware mutation.

That model uses one richer incoming durable inbox ledger rather than a
receive-buffer row plus a separate inbox idempotency row for the same buffered
message. Durable inbox identity is message id plus the resolved receive
binding: target module and stable handler identity for a command, or
subscriber module and stable subscriber identity for an event. The durable
inbox row stores structured durable envelope fields plus status, attempt
count, claim owner, claim lease, retry schedule, processed timestamp,
terminal failure timestamp, and failure reason.

In that optional model, native broker settlement would happen after durable
ingestion succeeds, before handler processing. The host-callable Bondstone
processing dispatcher claims due durable inbox rows and calls the existing
module receive pipeline. In the current slice, incoming-ledger outcome
recording happens after the module receive pipeline returns. If the process
crashes after handler state, successful command operation completion, outgoing
outbox rows, and the tiny direct inbox processed marker commit but before the
incoming row is marked processed, a later retry relies on the tiny direct inbox
to report already processed and let the incoming dispatcher catch the ledger
up. Durable inbox retry and terminal receive failure state is recorded with
claim-owner and lease-aware updates. Broker retry, dead-letter policy, delivery
counts, topology, and cleanup or retention remain outside Bondstone's default
ownership.

If module receive encounters a tiny direct inbox row that is already received
but not processed, it raises `DurableInboxAlreadyReceivedException`. The
incoming dispatcher treats that loud ambiguity as a processing failure and
will retry or terminally fail the incoming row according to incoming inbox
policy. Operator runbooks should inspect the direct inbox row, incoming ledger
row, handler state, operation state, outbox rows, broker state, and application
logs before deciding whether any manual mutation or compensation is safe.

Terminal durable inbox receive failure would be durable operational evidence,
not automatic operation failure. Applications that want a user-visible terminal
operation outcome would inspect the terminal durable inbox row and explicitly
finalize the operation through application policy.

The durable incoming inbox path remains opt-in. Direct receive remains the
default unless a later ADR changes it, so current operators should treat
unprocessed direct inbox rows as ambiguous receive attempts that need
application-owned investigation.

## Broker Settlement

Broker receive adapters should settle native deliveries only after Bondstone
receive succeeds. The thin RabbitMQ receive worker always consumes with manual
acknowledgement, acknowledges after `IDurableEnvelopeReceiver` completes, and
nacks failed receives according to `RequeueOnFailure`. That option maps only
to RabbitMQ's native nack requeue flag; broker retry and dead-letter behavior
remain topology and policy owned by the application.

The thin Azure Service Bus receive worker completes the message after receive
completes. Its exposed `ProcessorOptions` is an advanced native-driver escape
hatch, but `AutoCompleteMessages` must remain `false` so Bondstone can
complete messages only after durable receive succeeds. Receive exceptions flow
to the Service Bus processor error path and provider-native retry behavior.

If the module transaction commits but native settlement fails, broker
redelivery should find the processed inbox row and skip the handler. If
Bondstone receive fails, including because an unprocessed inbox row is found,
the adapter or app-owned receive loop should use the provider-native failure
handoff instead of acknowledging the message as handled.

Bondstone does not own broker retry schedules, delivery counts, error queues,
dead-letter queues, or dead-letter routing.

## Inbox Inspection And Recovery

Use `IDurableInboxInspector` to inspect unprocessed inbox rows for a module:

```csharp
IReadOnlyList<DurableInboxRecord> staleReceives = await inboxInspector
    .FindUnprocessedAsync(
        moduleName: FulfillmentModule.ModuleName,
        maxCount: 50,
        receivedAtOrBeforeUtc: DateTimeOffset.UtcNow.AddMinutes(-5),
        ct);
```

Inspection is read-only. Bondstone does not decide whether to mark the row
processed, delete it, re-run a handler, move a broker message, or issue
compensating business work. The application or operator runbook must prove
what happened during the ambiguous receive attempt before mutating durable
state.

## Outbox Terminal Failures

Bondstone owns persisted outbox dispatch retry and terminal outbox failure
state for outgoing local outbox records. A `TerminalFailed` outbox row means
Bondstone stopped retrying that local persisted dispatch according to the
configured failure policy. It does not mean a provider-native dead-letter queue
was created or used.

Bondstone emits outbox metrics when records are claimed, recorded as
dispatched, scheduled for retry, marked terminal failed, or found stale during
dispatch. These metrics describe Bondstone-owned outbox state only; broker
delivery counts, dead-letter state, queue health, and topology remain
provider-native or application-owned.

Use `IDurableOutboxInspector` to inspect terminal failures:

```csharp
IReadOnlyList<DurableOutboxRecord> failedRows = await inspector
    .FindTerminalFailedAsync(
        moduleName: FulfillmentModule.ModuleName,
        maxCount: 50,
        failedAtOrBeforeUtc: DateTimeOffset.UtcNow.AddMinutes(-5),
        ct);
```

Inspection is read-only. Resetting a row, replaying a message, purging or
archiving rows, moving broker messages, or issuing a compensating command is
application-owned because only the application can prove whether the downstream
side effect already happened.

## Operation Results

Durable command send is not request/response. A send accepts work into the
source module outbox and returns send metadata. When the caller supplies a
durable operation id, the send result carries a `DurableOperationHandle` that
can be used to observe the target module's operation state.

Use `IDurableOperationResultReader.GetResultAsync<TResult>()` to read the
current operation state once. Use
`IDurableOperationResultReader.TryWaitForResultAsync<TResult>()` when an API or
workflow wants a timeout-bounded wait that returns a value even when the caller
stops waiting. The returned `DurableOperationWaitResult<TResult>` separates
`CompletedWithinTimeout` from the latest observed durable operation result.
Use `WaitForResultAsync<TResult>()` only when exception-based timeout handling
is acceptable; if its timeout expires before the operation reaches a terminal
state, it throws `TimeoutException`. Both wait forms treat timeout as caller
patience; they do not write `Failed`, `Cancelled`, or any other durable
operation state.

Applications can mark explicit terminal non-success outcomes with
`IDurableOperationFinalizer` when application policy has enough evidence that
the operation should stop being observed as pending or running. Applications
that need recurring expiry can schedule their own job and call
`IDurableOperationExpirationProcessor` for each module store. Bondstone does
not register a hosted operation expiry worker by default. Bondstone emits
metrics for explicit operation finalizations, expiration candidates, and
expiration finalizations, but the application still owns the expiration job's
cadence, cutoff, terminal status, and reason.

Do not infer operation failure automatically from outbox terminal failure,
inbox idempotency, broker retry, or dead-letter behavior unless application or
provider-specific policy has made that terminal user-visible outcome explicit.

### Pending Operation Troubleshooting

When an operation remains `Pending` longer than expected, inspect the ledgers
that Bondstone owns before marking the operation `Failed` or `Cancelled`.
Pending means "no terminal operation state has been observed"; it does not
identify which part of the workflow is blocked.

Start with the send result. Prefer passing `DurableCommandSendResult.Operation`
to result readers because it carries the source and target module names. If
only the operation id is available, record or reconstruct the expected source
and target modules from the application workflow before investigating.

Inspect the source module outbox for the operation's outgoing command. Today
the app-facing `IDurableOutboxInspector` reads terminal failures rather than
arbitrary pending rows, so a terminal row is the clearest Bondstone-owned
evidence that source dispatch stopped retrying:

```csharp
IReadOnlyList<DurableOutboxRecord> terminalRows = await outboxInspector
    .FindTerminalFailedAsync(
        sourceModuleName,
        maxCount: 50,
        failedAtOrBeforeUtc: DateTimeOffset.UtcNow,
        ct);

DurableOutboxRecord? operationFailure = terminalRows.FirstOrDefault(row =>
    row.Envelope.DurableOperationId == operation.DurableOperationId);
```

If the source outbox has terminally failed, decide in application policy
whether to retry, compensate, or finalize the operation. Bondstone does not
turn terminal outbox failure into operation failure automatically because the
transport side effect may or may not have happened.

Inspect the target module inbox for ambiguous receives. Unprocessed inbox rows
mean Bondstone saw the message for a handler or subscriber but cannot prove
that re-running the handler is safe:

```csharp
IReadOnlyList<DurableInboxRecord> unprocessedRows = await inboxInspector
    .FindUnprocessedAsync(
        targetModuleName,
        maxCount: 50,
        receivedAtOrBeforeUtc: DateTimeOffset.UtcNow.AddMinutes(-5),
        ct);

DurableInboxRecord? matchingReceive = unprocessedRows.FirstOrDefault(row =>
    row.Key.MessageId == sendResult.SendId);
```

If an unprocessed inbox row matches the message, use an operator runbook or
application-specific evidence before mutating rows, moving broker messages, or
finalizing the operation. Direct receive intentionally treats this state as
ambiguous.

If no Bondstone terminal outbox row or stale inbox row explains the delay,
inspect provider-native transport health, broker delivery or dead-letter
state, worker logs, and application handler logs. Those surfaces are outside
Bondstone's provider-neutral operation state.

Use `IDurableOperationFinalizer` only after application policy has enough
evidence to produce a user-visible terminal outcome:

```csharp
DurableOperationFinalizationResult finalization =
    await durableOperationFinalizer.MarkFailedAsync(
        targetModuleName,
        operation.DurableOperationId,
        "Inventory reservation did not complete before the application SLA.",
        ct: ct);
```

For recurring policy, schedule an app-owned job and call
`IDurableOperationExpirationProcessor` for each module that owns operation
state. Expiration finalizes stale `Pending` or `Running` rows according to the
application's cutoff and reason; it does not inspect broker or outbox state by
itself.

## Health And Readiness Recipes

Health and readiness checks should distinguish "the host can accept traffic"
from "operators need to investigate durable evidence." Bondstone exposes
read-only inspection APIs and metrics for Bondstone-owned durable state, but
applications own thresholds, alert routing, readiness policy, and any mutation
or replay procedure.

### Terminal Outbox Failures

Use `IDurableOutboxInspector` per module to count or sample terminal failures
older than the application's incident threshold:

```csharp
IReadOnlyList<DurableOutboxRecord> terminalFailures = await outboxInspector
    .FindTerminalFailedAsync(
        moduleName,
        maxCount: 1,
        failedAtOrBeforeUtc: DateTimeOffset.UtcNow.AddMinutes(-5),
        ct);
```

A non-empty result is a strong alert signal because Bondstone has stopped
retrying those local outbox rows. Whether it should fail readiness depends on
the app: an API that accepts new durable commands for the same source module
may choose degraded readiness, while a read-only endpoint may only raise an
operator alert. Recovery remains app-owned: inspect downstream side effects
before resetting, replaying, purging, archiving, or compensating.

Pair the inspection check with the `bondstone.outbox.terminal_failed` metric
for alerting on newly terminal rows. The metric is not a substitute for
inspection when operators need the durable envelope and failure reason.

### Stale Direct Inbox Rows

Use `IDurableInboxInspector` per module to find unprocessed direct inbox rows
older than the expected handler and broker retry window:

```csharp
IReadOnlyList<DurableInboxRecord> staleReceives = await inboxInspector
    .FindUnprocessedAsync(
        moduleName,
        maxCount: 1,
        receivedAtOrBeforeUtc: DateTimeOffset.UtcNow.AddMinutes(-5),
        ct);
```

A non-empty result means a receive attempt is ambiguous. It should usually
alert operators and may fail readiness for endpoints that depend on that
module's receive progress. Do not make an automatic health check mark the row
processed, delete it, re-run the handler, or move broker messages. The runbook
must first prove whether the handler side effect happened.

Use the `bondstone.direct_receive.already_received` metric as an early warning
that native redelivery is hitting ambiguous inbox state. Broker delivery count,
dead-letter state, and retry exhaustion remain provider-native signals.

### Repeated Outbox Worker Batch Failures

The hosted outbox worker logs unexpected dispatch-batch failures with event id
`1001` and name `DispatchBatchFailed`, waits for `FailureDelay`, and continues.
Repeated failures mean the worker loop is alive but cannot complete batches,
often because a module dispatcher, persistence dependency, or transport
dispatcher is failing before Bondstone can record per-row retry or terminal
state.

Applications should monitor the log event by worker id and rate-limit window.
Treat sustained failures as an alert, and consider failing readiness for hosts
whose primary job is outbox dispatch. Correlate the log with outbox metrics:
if claimed, retry, terminal, or dispatched counters are not moving while
batch-failure logs continue, investigate dispatcher construction,
connectivity, credentials, route configuration, or provider-native transport
health.

The aggregate module dispatcher runs module dispatchers in registration order
with one shared batch budget. A failure from one module dispatcher stops the
current aggregate batch and delays later modules until a later worker
iteration. The current worker has no fairness guarantee and no selected-module
scheduling option, so noisy-neighbor isolation requires app-owned deployment
or a future Bondstone worker option.

### Repeated Incoming Inbox Worker Batch Failures

The hosted incoming inbox processing worker logs unexpected process-batch
failures with event id `2001` and name `ProcessBatchFailed`, includes the
worker id and consecutive failure count, waits for `FailureDelay`, and
continues. Repeated failures mean the worker loop is alive but cannot complete
batches, often because claim, outcome recording, module receive resolution, or
module persistence is failing before per-row retry or terminal state can be
recorded.

Applications should monitor the log event by worker id and rate-limit window.
Correlate it with incoming inbox table state, direct receive metrics, handler
logs, and provider-native transport health. Cleanup, replay, purge, archival,
and terminal-row remediation remain application-owned unless a later
Bondstone cleanup worker or mutation API is explicitly implemented.

### Operation Expiration Backlog

Applications that expose durable operation results should define an expiry
policy for old `Pending` or `Running` operations. Schedule an app-owned job per
module and call `IDurableOperationExpirationProcessor` with the application's
cutoff, terminal status, reason, and batch size:

```csharp
DurableOperationExpirationResult expiration = await expirationProcessor
    .MarkExpiredAsync(
        moduleName,
        expiresBeforeUtc: DateTimeOffset.UtcNow.AddHours(-1),
        terminalStatus: DurableOperationStatus.Failed,
        reason: "Operation exceeded the application expiry policy.",
        maxCount: 100,
        ct: ct);
```

Monitor `CandidateCount` and `FinalizedCount`, plus the
`bondstone.operation.expiration.candidates` and
`bondstone.operation.expiration.finalized` metrics. A recurring candidate
backlog means the app-owned expiration job is not running often enough, the
batch size is too small, the cutoff is too conservative, or operation
completion/finalization is blocked upstream.

Bondstone does not register a hosted operation expiration worker by default.
Keep expiration scheduling explicit in the application so the cadence, cutoff,
terminal status, and user-visible reason match product policy.

## EF Migrations And Upgrades

EF-backed Bondstone tables live in the application's `DbContext` model through
`ApplyBondstonePersistence(...)` or the granular mapping helpers. Optional
domain event records use `ApplyBondstoneDomainEvents(...)`. The application
owns EF migration generation and application for every module `DbContext` that
maps those tables. Bondstone does not run migrations for the app and does not
ship app-specific migrations.

During package upgrades, review release notes and EF mapping docs for table
shape changes. Generate and review migrations in the app repository, including
per-module schemas when each module maps Bondstone persistence separately.
Nullable diagnostic additions, such as operation-state diagnostic columns, can
usually be added without backfilling old rows; behavior remains governed by the
specific upgrade note. Bondstone release notes must call out any durable
table-shape change so applications can generate, review, and apply their own
EF migrations before deploying upgraded packages.

Migrator processes should compose the module `DbContext` provider options,
schemas, application entities, and Bondstone EF mappings. They do not need
local transport, broker transport, receive workers, the hosted outbox worker,
or the hosted incoming inbox processing worker.

## Contract Evolution

Durable message identities are stable public contracts. Do not derive command,
event, subscriber, or handler identities from CLR names. Compatible payload
changes should preserve the stable identity and serializer compatibility, such
as by adding optional fields or converters that can read old persisted
payloads. Breaking payload changes should use a new durable message identity
and an explicit migration or coexistence plan.

Durable payload JSON is configured through
`ConfigureBondstoneDurablePayloadJson(...)`. Use that shared surface for
command, integration event, domain event record, and operation result DTO
converters when compatibility matters. Keep old operation result rows and
in-flight outbox/inbox payloads in mind before removing DTO members,
renaming fields, or changing converter behavior.

## Retention And Cleanup

Retention is application-owned because durable rows can be business evidence.
Use module-specific retention windows that account for broker redelivery,
operation polling, audit needs, and incident response.

Outbox rows can be archived or purged after the application no longer needs
dispatch evidence. Treat `TerminalFailed` rows specially: inspect and resolve
them before cleanup unless an operator runbook deliberately archives unresolved
failures.

Inbox rows protect duplicate receive handling. Retain processed inbox rows at
least as long as provider redelivery, replay, or duplicate-message windows can
occur. Unprocessed rows should alert or appear in operator dashboards before
any cleanup because they represent ambiguous receive attempts.

Operation-state rows should live long enough for callers, dashboards, and
expiry policies to observe final status. Application expiry jobs may finalize
old pending or running operations, but finalization is separate from row
cleanup.

Optional domain event records are module-local records, not outgoing transport
messages. Retain, archive, or purge them according to the module's audit and
debugging policy.

## Observability

See [observability.md](observability.md) for the current diagnostic surfaces
and the OpenTelemetry-native direction. Today, Bondstone emits stable activity
names and log event ids for the minimum durable boundaries documented there,
but it does not expose finalized metric instruments or stable misconfiguration
error codes. Where Bondstone does not expose a signal, keep monitoring in
application code, provider tooling, broker-native telemetry, and database
queries over app-owned tables.
