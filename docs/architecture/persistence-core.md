# Core Persistence

Core persistence contracts live in `Bondstone` and stay independent from EF
Core, PostgreSQL, Rebus, SQL locking, schema migration, and background
dispatch mechanics.

## Outbox

`IDurableOutboxWriter` is the write boundary for outgoing durable messages. It
accepts a `DurableMessageEnvelope`; provider implementations persist that
envelope inside the caller's local persistence transaction when the caller
needs atomic source-state-plus-outbox behavior.

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
renew leases, schedule retries, dead-letter messages, or clean up stale work.

`IDurableOutboxLeaseRenewer` extends the active lease for one claimed outbox
message. Implementations update the lease only when the row is still
`Processing`, still owned by the supplied claimant, and still inside the active
lease. The renewal boundary does not claim rows, renew batches, dispatch
messages, recover stale claims, or schedule retries.

`IDurableOutboxDispatchRecorder` records the result of a claimed delivery
attempt. It records dispatch success, schedules retry after a failure, or marks
a claimed row as dead-lettered. These updates are claim-owner and lease-time
aware.

`IDurableOutboxFailurePolicy` decides whether a failed claimed delivery attempt
should be retried or dead-lettered. The default
`DurableOutboxFailurePolicy` uses a maximum-attempt threshold and retry delay
sequence to produce a deterministic `DurableOutboxFailureDecision`. It is a
pure policy and does not claim rows, send transport messages, update
persistence, renew leases, route dead letters, or register background workers.

## Inbox

`DurableInboxMessageKey` identifies receive-side deduplication by stable
message id, target module name, and handler identity. Handler identity is
free-form stable text; it should not be derived from handler CLR names.

`DurableInboxRecord` represents the persistence-neutral receive-side inbox
state for that key. It records when a message was received and, when complete,
when processing finished.

`IDurableInboxRegistrar` idempotently records that a message-handler pair has
been seen. It returns whether the row was newly registered, already received,
or already processed, and carries the effective `DurableInboxRecord`.

`IDurableInboxHandlerExecutor` is the narrow core orchestration boundary for
handle-once execution. It composes inbox registration, a caller-supplied
handler delegate, processed-marker staging, and a caller-supplied commit
delegate. It does not start database transactions, call EF Core
`SaveChangesAsync`, acknowledge transports, discover handlers, or wrap
ordinary in-process calls in a mediator.

`IDurableInboxStore` exposes lower-level inbox store operations: read a record,
add a receive record, and mark it processed. Provider implementations own the
unique constraint, transaction, savepoint, and concurrency behavior that make
those operations reliable.

## Operation State

`IDurableOperationStateStore` saves durable operation state by durable
operation id and inherits `IDurableOperationReader` for read access. The core
contract does not enforce transition rules, concurrency tokens, polling,
timeouts, or result deserialization.

## Provider Boundaries

Core contracts are intentionally provider-neutral. A future Dapper or direct
ADO.NET package should implement these contracts directly, own its
provider-specific connection or transaction boundary in its own package, and
pass explicit commit delegates to core orchestration primitives where needed.
Such providers should not depend on EF entity mappings, `DbContext`, or
`IEntityFrameworkCorePersistenceScope`.
