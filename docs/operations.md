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
- read-only inspection contracts for terminal outbox rows and unprocessed inbox
  rows;
- explicit operation finalization and expiration APIs that application policy
  can call.

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

Already received but unprocessed inbox rows are ambiguous. They can mean the
process failed after recording the receive but before the processed marker, or
they can mean application side effects happened but the process stopped before
Bondstone could record completion. Bondstone therefore raises
`DurableInboxAlreadyReceivedException` through the module receive path instead
of re-running the handler or silently treating the message as handled.

Bondstone does not currently provide inbox leases, a stale-row sweeper, a
failed receive state, provider-neutral receive retry, or a durable receive
buffer. A future durable receive buffer is accepted as design direction, but it
is not current behavior.

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
`IDurableOperationResultReader.WaitForResultAsync<TResult>()` only when an
explicit timeout-bounded wait is acceptable. Current wait behavior is
throwing: if the timeout expires before the operation reaches a terminal state,
`WaitForResultAsync<TResult>()` throws `TimeoutException`. That timeout is
caller patience; it does not write `Failed`, `Cancelled`, or any other durable
operation state.

Applications can mark explicit terminal non-success outcomes with
`IDurableOperationFinalizer` when application policy has enough evidence that
the operation should stop being observed as pending or running. Applications
that need recurring expiry can schedule their own job and call
`IDurableOperationExpirationProcessor` for each module store. Bondstone does
not register a hosted operation expiry worker by default.

Do not infer operation failure automatically from outbox terminal failure,
inbox idempotency, broker retry, or dead-letter behavior unless application or
provider-specific policy has made that terminal user-visible outcome explicit.

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
local transport, broker transport, receive workers, or the hosted outbox
worker.

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
but it does not yet expose finalized metric instruments or stable
misconfiguration error codes. Where Bondstone does not yet expose a signal,
keep monitoring in application code, provider tooling, broker-native
telemetry, and database queries over app-owned tables.
