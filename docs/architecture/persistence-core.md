# Core Persistence

Provider-neutral persistence contracts live in `Bondstone.Persistence` and
stay independent from EF Core, PostgreSQL, transport adapters, SQL locking,
schema migration, and background dispatch mechanics. The `Bondstone` core
package owns module execution and module-aware runtime resolution over those
contracts.

## Outbox

`IDurableOutboxWriter` is the write boundary for outgoing durable messages. It
accepts a `DurableMessageEnvelope`; provider implementations persist that
envelope inside the caller's local persistence transaction when the caller
needs atomic source-state-plus-outbox behavior.

Durable commands and integration events share this outbox boundary. Commands
dispatch to one target module destination. Integration events dispatch to
transport event topology, such as a Service Bus topic or RabbitMQ exchange,
and each subscriber's receive copy owns its own inbox outcome.

`DurableOutboxRecord` is a persistence-neutral record for a stored envelope,
the UTC time it was stored, and its current `DurableOutboxDispatchState`.

`DurableOutboxDispatchState` records provider-neutral outbox dispatch state:
`DurableOutboxStatus`, attempt count, optional next-attempt timestamp,
optional dispatched or failed timestamp, optional failure reason, optional
claim owner, and optional claim lease expiry.

`IDurableOutboxClaimer` claims outgoing durable messages that are ready for
dispatch. Claim implementations mark rows as `Processing`, populate claim
ownership and lease expiry, and increment attempt count at claim time. The
claim boundary does not dispatch messages, acknowledge transport delivery,
renew leases, schedule retries, terminally fail messages, or clean up stale
work.

`IDurableOutboxLeaseRenewer` extends the active lease for one claimed outbox
message. Implementations update the lease only when the row is still
`Processing`, still owned by the supplied claimant, and still inside the active
lease. The renewal boundary does not claim rows, renew batches, dispatch
messages, recover stale claims, or schedule retries.

`IDurableOutboxDispatchRecorder` records the result of a claimed delivery
attempt. It records dispatch success, schedules retry after a failure, or marks
a claimed row as terminally failed. These updates are claim-owner and
lease-time aware.

The persisted `TerminalFailed` outbox status is a terminal Bondstone outbox
failure state for an outgoing local outbox record. It does not mean Bondstone
creates or owns a provider-native broker dead-letter queue. Broker receive
retry and dead-letter policy remains transport/provider-owned.
Terminal rows remain persisted until the application or operator applies its
own retention, archival, purge, reset, or replay procedure. Bondstone provides
read-only terminal outbox inspection, but it does not provide a
provider-neutral operator mutation API.

`IDurableOutboxInspectionStore` is the provider-side read contract for
operator inspection of persisted terminal outbox rows. Implementations return
`TerminalFailed` records, optionally filtered by source module and failed-at
cutoff, ordered by oldest terminal failure first and bounded by the requested
maximum count. The query does not claim rows, reset status, replay messages,
purge rows, archive rows, or decide whether re-dispatch is safe.

`IDurableOutboxInspector` is the app-facing module-aware inspection contract.
It resolves the named module's outbox inspection store and delegates the
read-only terminal query to that module's persistence boundary. Applications
can use those records for dashboards, alerts, retention workflows, or
operator runbooks. Any replay, reset, purge, archival, or compensation remains
application-owned because only the application can prove whether the
downstream side effect already happened.

`IDurableOutboxFailurePolicy` decides whether a failed claimed delivery attempt
should be retried or terminally failed. The default
`DurableOutboxFailurePolicy` uses a maximum-attempt threshold and retry delay
sequence to produce a deterministic `DurableOutboxFailureDecision`. It is a
pure policy and does not claim rows, send transport messages, update
persistence, renew leases, route failed messages, or register background
workers.
Failure reasons are persisted as diagnostic text for the failed dispatch
attempt. They are not a normalized machine-readable remediation contract and
do not by themselves define an operator recovery action.

`IDurableEnvelopeDispatcher` is the minimal envelope dispatch boundary for a
claimed `DurableOutboxRecord`. Transport adapters own routing, serialization,
broker-specific acknowledgement, and provider-native behavior.

`IDurableOutboxDispatcher` is the dispatch boundary. The default
`DurableOutboxDispatcher` implementation is a plain composable class for
dispatching one batch when called. It composes claiming, per-record lease
renewal, envelope dispatch, failure decision, and outcome recording. It is
not a hosted service and does not own polling, leader election, singleton
sweeper coordination, route circuit breaking, archiving, terminal-failure
routing, or claim-maintenance APIs. Expired outbox processing claims are
recovered by the provider claimer when that provider supports expired-lease
claiming; active
outcome updates remain claim-owner and lease-time aware.
Hosted worker composition lives outside the persistence package in
`Bondstone.Hosting`.

For module-owned durable persistence, module-specific outbox writers and
outbox dispatchers are registered by provider packages as module-owned
runtime registrations. Durable sends resolve the source module writer, while
the app-facing dispatcher can aggregate dispatch across configured local
module outboxes. The built-in aggregate dispatcher invokes module dispatchers
sequentially in registration order,
shares the caller's `maxCount` as one aggregate batch budget, passes each
module dispatcher only the remaining budget, and propagates module dispatcher
failures to its caller. The underlying claim, lease, transport, and
outcome-recording contracts remain per persistence boundary.

Provider packages contribute module-owned command and receive runtime
persistence through passive durable module runtime registrations. Each
registration carries a module name plus a factory for the executable
`IDurableOutboxWriter`, `IDurableInboxHandlerExecutor`,
`IDurableInboxInspectionStore`, or `IDurableOperationStateStore` used by that
module, plus factories for the module's executable `IDurableOutboxDispatcher`
when the provider supports local outbox dispatch and
`IDurableOutboxInspectionStore` when the provider supports terminal-row
inspection. Providers also register one
`DurableModuleIncomingInboxIngestionBoundaryRegistration` per module for
durable incoming inbox ingestion. That boundary groups the module's
`IDurableIncomingInboxIngestionStore` and
`IDurableIncomingInboxIngestionPersistenceScope` so ingestion writes and saves
through the same receiver-module persistence boundary. Providers that support
incoming row mutation may also register module incoming inbox dispatchers so
the hosted incoming worker can aggregate processing across module-owned
persistence boundaries. Providers store command, outbox, inbox, operation, and
ingestion boundary records in `DurableModulePersistenceRegistrationRegistry`;
individual module runtime registrations are not service descriptors. Provider
packages should use Bondstone's advanced service-collection helpers to get or
create the registries so ownership rules stay consistent across packages.
`Bondstone` builds module maps from the registry and invokes only the selected
module's factory inside the current DI scope. This keeps module metadata
lookup from constructing unrelated provider dependencies such as another
module's `DbContext` or PostgreSQL session. Executable services use the
ordinary role contracts returned by those factories. Provider factories should
create lightweight wrappers around services resolved from the current DI scope
and should not create owned disposable resources outside DI ownership.

Provider packages must register at most one runtime registration for each
module command/receive durable role, at most one module outbox dispatcher
registration for each local module outbox, at most one module inbox
inspection-store registration for each module store, at most one module
outbox inspection-store registration for each module store, and at most one
module incoming inbox ingestion boundary for each module. Providers that
register module incoming inbox dispatchers must register at most one dispatcher
per module. The durable runtime registry rejects duplicate module outbox
writer, inbox handler executor, inbox inspection-store, operation-state store,
outbox dispatcher, outbox inspection-store, and incoming inbox ingestion
boundary registrations when provider setup adds them; the runtime map keeps
defensive duplicate validation when those services are resolved. The incoming
dispatcher registry rejects duplicate module incoming dispatcher registrations
when provider setup adds them. Provider
composition errors therefore fail with module-specific diagnostics. When a
module declares
persistence but the matching module-owned service is missing, runtime
diagnostics name the module and its declared persistence provider so provider
setup gaps are easier to identify. Application code should normally configure
module-owned persistence through provider setup helpers rather than directly
registering provider-facing runtime registrations.

Provider transaction runners can open an observed-transaction callback scope
on the current module execution context. This is an advanced provider/runtime
coordination contract. Optional runtime behaviors can register commit or
rollback callbacks through the execution context without depending on the
concrete provider transaction implementation. The context exposes whether
Bondstone observes the current transaction outcome; when a provider joins an
application-owned transaction that Bondstone cannot observe, behaviors must
not treat later local runtime completion as durable commit evidence. Commit
and rollback callbacks are lightweight runtime coordination hooks, not durable
work boundaries. Callback failures can surface after the underlying
transaction has already committed or rolled back.

If no module-owned runtime registrations are registered at all, core can fall
back to root-level non-module persistence services for low-level send and
receive paths such as `IDurableOutboxWriter`,
`IDurableInboxHandlerExecutor`, and `IDurableOperationStateStore`. That
fallback is supported advanced single-store composition and compatibility
behavior for those lower-level paths. It does not replace the preferred
module-owned durable messaging path.

Durable incoming inbox ingestion follows the same ownership rule but resolves
by receiver module after the adapter has built the `DurableIncomingInboxRecord`.
When a module incoming ingestion boundary is registered, ingestion uses that
boundary. When any module-owned persistence registrations exist and the
receiver module has no ingestion boundary, resolution fails with a
module-specific configuration error instead of using an unrelated root store.
Only hosts with no module-owned runtime registrations or module ingestion
boundaries use the root-level `IDurableIncomingInboxIngestionStore` plus
`IDurableIncomingInboxIngestionPersistenceScope` fallback.

`IDurableOperationReader` is intentionally different: Bondstone's default
operation reader aggregates configured module-owned operation-state store
runtime registrations only. It does not preserve or delegate to root-level
operation reader registrations, and it does not use a root-level
`IDurableOperationStateStore` as a read fallback.

## Receive Idempotency

`DurableInboxMessageKey` identifies receive-side deduplication by stable
message id, module name, and handler or subscriber identity. For commands, the
module name is the command target module and the identity is the stable
handler identity. For integration events, the module name is the subscriber
module and the identity is the stable subscriber identity. Handler and
subscriber identities are free-form stable text; they should not be derived
from handler CLR names.

`DurableInboxRecord` represents the persistence-neutral receive-side inbox
state for that key. It records when a message was received and, when complete,
when processing finished.

`IDurableInboxRegistrar` idempotently records that a message-handler pair has
been seen. It returns whether the row was newly registered, already received,
or already processed, and carries the effective `DurableInboxRecord`.

`IDurableInboxHandlerExecutor` is the narrow core orchestration boundary for
handle-once execution. It composes inbox registration, a caller-supplied
handler delegate, and processed-marker staging. It does not start database
transactions, call EF Core
`SaveChangesAsync`, acknowledge transports, discover handlers, or wrap
ordinary in-process calls in a mediator. In module-owned execution, provider
transaction behaviors own the commit and persist the handler state, inbox
marker, operation state, and outgoing outbox rows together. Low-level/root
composition must execute the inbox executor inside the caller's chosen
transaction or save boundary and commit outside the executor.

The durable inbox processing dispatcher composes the module receive pipelines,
which in turn use this executor as an implementation-detail idempotency guard.
Already-received but unprocessed rows are not treated as handled. Module
receive pipelines surface them through `DurableInboxAlreadyReceivedException`,
and the durable inbox dispatcher records retry or terminal failure for the
incoming row rather than re-running the handler.

Bondstone does not currently provide inbox leases, stale-row recovery hooks,
maintenance workers, failed receive states, or provider-neutral inbox row
mutation helpers. Bondstone provides read-only inbox inspection, but not
provider-neutral stale-row recovery. Applications may build their own
operational recovery around the persisted inbox table, but that recovery owns
the safety proof for any row mutation or handler re-execution.

`IDurableInboxInspectionStore` is the provider-side read contract for operator
inspection of unprocessed inbox rows. Implementations return records whose
`ProcessedAtUtc` is null, optionally filtered by module and received-at
cutoff, ordered by oldest receive first and bounded by the requested maximum
count. The query does not mark rows processed, delete rows, re-run handlers,
settle broker messages, or decide whether a receive attempt is safe to
recover.

`IDurableInboxInspector` is the app-facing module-aware inspection contract.
It resolves the named module's inbox inspection store and delegates the
read-only unprocessed-row query to that module's persistence boundary.
Applications can use those records for dashboards, alerts, or operator
runbooks. Any re-run, row mutation, broker action, purge, archival, or
compensation remains application-owned because only the application can prove
what happened during the ambiguous receive attempt.

`IDurableInboxStore` exposes lower-level inbox store operations: read a record,
add a receive record, and mark it processed. Provider implementations own the
unique constraint, transaction, savepoint, and concurrency behavior that make
those operations reliable.

## Durable Inbox

The durable inbox incoming ledger is the durable receive ledger for adapters
that ingest durable deliveries before settlement. ADR
[0012](../adr/0012-direct-receive-inbox-and-durable-receive-buffer.md)
preserves the superseded separate receive-buffer decision trail. The current
provider-neutral names use `DurableIncomingInbox*` and
`IDurableIncomingInbox*`. Provider-neutral EF Core ingestion and read-only
inspection stores exist. Core provides a module-aware ingestion boundary
resolver so transport ingestion can write through the receiver module's
persistence boundary when module persistence is configured. PostgreSQL-specific
claim, lease-renewal, and outcome-recording stores exist for the incoming
ledger. Core also provides a host-callable
`IDurableIncomingInboxDispatcher` that claims due rows and hands each row to
the existing module receive pipelines. `Bondstone.Hosting` provides a hosted
worker for that dispatcher. Transport adapter handoff into durable ingestion
is adapter-owned or application-owned. RabbitMQ receive workers ingest native
deliveries into this ledger before native acknowledgement.

Current module processing still writes the older `IDurableInboxStore`
`inbox_messages` row as an implementation-detail idempotency marker inside the
module receive pipeline. Durable receive status for operator-facing ingestion,
claim, retry, processed, and terminal outcomes lives on the incoming inbox row.
Removing the older marker from this processing path remains a v2 cleanup item.

The durable inbox is modeled as a single persisted incoming delivery
ledger. Its key is the durable receive binding:

- commands: message id, target module, and stable handler identity;
- events: message id, subscriber module, and stable subscriber identity.

The durable inbox row should carry the `DurableMessageEnvelope`, receive
identity, optional source transport diagnostic name, ingested timestamp, and
receive processing state. Providers should map the envelope fields
structurally so they can index and inspect message kind, message type name,
source module, optional target module, durable operation id, trace context,
causation id, partition key, payload, metadata, and envelope created-at
timestamp without deserializing an opaque envelope blob. The provider-neutral
record intentionally does not store broker settlement state, delivery counts,
dead-letter state, or topology metadata.

Provider-neutral durable inbox contracts should separate responsibilities in
the same style as outbox dispatch:

- ingestion idempotently inserts or returns a durable inbox row for a validated
  envelope and receive binding, starting new rows in `Pending` state;
- claiming marks due pending or expired-processing rows as processing, records
  claim ownership and lease expiry, and increments attempt count;
- lease renewal extends an active claim only for the owning claimant;
- outcome recording marks processed, schedules retry, marks terminal receive
  failure, or reports a stale claim when ownership or lease checks fail;
- inspection reads pending, processing, retry, or terminal rows without
  mutating them. Inspection includes broad status queries plus operational
  stale-processing and terminal-failure queries with receiver-module and
  source-transport filters.

The module receive pipeline remains the owner of target module
handler execution. Handler state, successful command operation completion,
outgoing outbox rows, and durable inbox processed state should commit in the
target module transaction where possible.

The current runtime processing slice records the incoming-ledger outcome after
the module receive pipeline returns. That leaves a crash window: handler state,
operation completion, outgoing outbox rows, and the implementation-detail
idempotency marker can commit before the incoming ledger is marked processed.
If a later retry sees the idempotency marker as already processed, the
incoming dispatcher records the incoming row as processed and catches the
ledger up. If the idempotency marker is already received but unprocessed, the
module receive pipeline continues to raise
`DurableInboxAlreadyReceivedException`; the incoming dispatcher treats that
ambiguity as a processing failure and applies incoming inbox retry or
terminal-failure policy.

The hosted incoming inbox processing worker only schedules dispatcher calls.
It does not add provider-neutral source-transport or receiver-module filters
because the current claimer contract is claimant, lease-duration, max-count,
and cancellation based. Cleanup and retention of pending, processed, retry,
stale, or terminal incoming rows remain application-owned unless a later
accepted implementation adds explicit mutation or retention contracts.

Durable inbox terminal failure must not automatically write operation `Failed`.
Operation state is still the caller-visible result model, and applications
must explicitly finalize operations when their policy has enough evidence.

## Operation State

`IDurableOperationStateStore` saves durable operation state by durable
operation id and inherits `IDurableOperationReader` for read access. The core
contract does not enforce concurrency tokens, polling, timeouts, or result
deserialization.

Current command-loop integration uses the store only when an envelope or send
request carries a caller-supplied durable operation id. Command send stages
`Pending` when the operation is unknown. Successful durable command receive
stages `Completed` inside module command execution. Result-returning durable
command receive also stores the serialized result payload and optional
diagnostic context from the receive route: module name, durable message type
name, and handler identity. Running state, retry state, stale receive
recovery, timeout policy, and cancellation are not written by Bondstone's
default command loop.

`IDurableOperationFinalizer` is the application-facing terminal outcome API.
It resolves the named module's operation-state store and writes explicit
`Failed` or `Cancelled` state for unknown, `Pending`, or `Running` operations.
It returns the existing state without overwriting when an operation is already
terminal. The finalizer is intentionally module-scoped so the write target is
explicit and matches the module-owned operation reader model. Applications can
use it from timeout/expiry jobs, administrative workflows, cancellation paths,
or other policy code that has enough evidence to produce a terminal
caller-visible outcome.

`IDurableOperationExpirationStore` is the provider-side query contract for
app-owned expiry jobs. Implementations return non-terminal `Pending` or
`Running` operation states last updated at or before a UTC cutoff, bounded by a
caller-supplied maximum count. `IDurableOperationExpirationProcessor` composes
that query with `IDurableOperationFinalizer` for one named module. It is not a
hosted scheduler, does not calculate deadlines, and does not infer failure
from broker state.

Operation reads aggregate across local module stores. This global read has no
module identity, so it intentionally creates each configured module
operation-state store from its runtime registration and queries all of them.
If no module-owned operation stores are configured, the default operation
reader returns no state. Terminal states
(`Completed`, `Failed`, and `Cancelled`) take precedence over `Running`, and
`Running` takes precedence over `Pending`. When statuses have the same
precedence, the newest `UpdatedAtUtc` wins. The default command loop currently
writes only `Pending`
and `Completed`; other statuses are read-model/storage values for
application-owned operation policies.

Operation reads can also use a module hint. The default
`IDurableOperationReader.GetStateAsync(operationId, moduleName, ...)` resolves
only the named module's operation-state store and does not query unrelated
module stores. `IDurableOperationResultReader` exposes matching hinted
single-read, throwing wait, and non-throwing wait overloads. Use hinted reads
or waits when the caller already knows the module that owns the operation
result; keep global observation for small hosts, tests, or callers that only
have an operation id. Non-throwing waits return the latest observed operation
result when caller timeout expires; they do not write terminal operation
state.

Durable command sends that include an operation id return a
`DurableOperationHandle` on `DurableCommandSendResult.Operation`. The handle
carries the operation id, source module, and target module. Passing the handle
to `IDurableOperationReader` or `IDurableOperationResultReader` queries the
target module store directly and is the preferred observation path for new
code.

Diagnostic context fields are nullable for compatibility with old rows,
manually-created operation states, and operation states written before the
result diagnostic context contract existed. The finalizer preserves existing
diagnostic context when the caller does not provide a replacement.

## Provider Boundaries

Persistence contracts are intentionally provider-neutral.
EF Core with PostgreSQL-specific behavior is the supported provider path in
the active product surface. Provider-neutral contracts remain where they keep
EF/PostgreSQL honest and support outbox, inbox, operation-state, and receive
pipeline composition. The previous direct non-EF PostgreSQL provider was
removed after MVP. Any non-EF provider requires ADR review and should not
depend on EF entity mappings, `DbContext`, or
`IEntityFrameworkCorePersistenceScope`.
