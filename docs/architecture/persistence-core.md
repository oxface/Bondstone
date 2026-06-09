# Core Persistence

Core persistence contracts live in `Bondstone` and stay independent from EF
Core, PostgreSQL, transport adapters, SQL locking, schema migration, and
background dispatch mechanics.

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

`IDurableOutboxTransport` is the minimal transport boundary for sending a
claimed `DurableOutboxRecord`. Transport adapters own routing, serialization,
broker-specific acknowledgement, and transport-native behavior.

`IDurableOutboxDispatcher` is the dispatch boundary. The default
`DurableOutboxDispatcher` implementation is a plain composable class for
dispatching one batch when called. It composes claiming, per-record lease
renewal, transport send, failure decision, and outcome recording. It is not a
hosted service and does not own polling, leader election, singleton sweeper
coordination, route circuit breaking, archiving, or dead-letter routing.
Hosted worker composition lives outside core in `Bondstone.Hosting`.

For module-owned durable persistence, module-specific outbox writers and
outbox dispatchers can be registered by provider packages. Durable sends
resolve the source module writer, while the app-facing dispatcher can aggregate
dispatch across configured local module outboxes. The underlying claim, lease,
transport, and outcome-recording contracts remain per persistence boundary.
The `IDurableModule*` persistence contracts are provider-extension contracts;
application code should normally configure them through provider setup helpers
such as PostgreSQL module persistence registration.

## Inbox

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
handler delegate, processed-marker staging, and a caller-supplied commit
delegate. It does not start database transactions, call EF Core
`SaveChangesAsync`, acknowledge transports, discover handlers, or wrap
ordinary in-process calls in a mediator.

Transport adapters that compose the executor should acknowledge transport
messages only after the executor returns a handled or already-processed result
and the relevant commit boundary has succeeded. Already-received but
unprocessed rows remain unresolved until a later inbox lease or stale receive
recovery decision; adapters should not silently treat them as handled.

`IDurableInboxStore` exposes lower-level inbox store operations: read a record,
add a receive record, and mark it processed. Provider implementations own the
unique constraint, transaction, savepoint, and concurrency behavior that make
those operations reliable.

## Operation State

`IDurableOperationStateStore` saves durable operation state by durable
operation id and inherits `IDurableOperationReader` for read access. The core
contract does not enforce concurrency tokens, polling, timeouts, or result
deserialization.

Current command-loop integration uses the store only when an envelope or send
request carries a caller-supplied durable operation id. Command send stages
`Pending` when the operation is unknown. Successful durable command receive
stages `Completed` inside module command execution. Failure states, running
states, retry state, stale receive recovery, cancellation, and result payloads
remain later policy.

When module-owned operation stores are configured, operation reads can
aggregate across local module stores. Completed state takes precedence over
pending state for the same operation id in the current command loop.

## Provider Boundaries

Core contracts are intentionally provider-neutral.
`Bondstone.Persistence.Dapper.Postgres` is the accepted first non-EF proof
provider. It should implement these contracts directly, own its
PostgreSQL-specific connection/session and transaction boundary in its own
package, and pass explicit commit delegates to core orchestration primitives
where needed. Non-EF providers should not depend on EF entity mappings,
`DbContext`, or `IEntityFrameworkCorePersistenceScope`.
