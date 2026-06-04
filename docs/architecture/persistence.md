# Persistence Architecture

## Current Contract

Core persistence contracts live in `Bondstone` and stay independent from EF
Core, PostgreSQL, Rebus, SQL locking, schema migration, and background
dispatch mechanics.

`IDurableOutboxWriter` is the core write boundary for outgoing durable
messages. It accepts a `DurableMessageEnvelope`; provider implementations are
expected to persist that envelope inside the caller's local persistence
transaction when the caller needs atomic source-state-plus-outbox behavior.

`DurableOutboxRecord` is a persistence-neutral record for a stored envelope,
the UTC time it was stored, and its current `DurableOutboxDispatchState`.

`DurableOutboxDispatchState` records provider-neutral outbox dispatch state:
`DurableOutboxStatus`, attempt count, optional next-attempt timestamp,
optional dispatched or failed timestamp, and optional failure reason. This lets
providers and future dispatchers share a common state shape without deciding
claim leases, retry-delay policy, dead-letter ownership, or transport
acknowledgement semantics in core.

`DurableInboxMessageKey` identifies receive-side deduplication by stable
message id, target module name, and handler identity. Handler identity is
free-form stable text; it should not be derived from handler CLR names.

`DurableInboxRecord` represents the persistence-neutral receive-side inbox
state for that key. It records when a message was received and, when complete,
when processing finished.

`IDurableInboxStore` exposes the minimal store operations needed by future
inbox implementations: read a record, try to add a receive record, and mark it
processed. Provider implementations own the unique constraint, transaction,
savepoint, and concurrency behavior that make those operations reliable.

`IDurableOperationStateStore` is the persistence boundary for durable operation
state. It inherits `IDurableOperationReader` for read access and saves the
current `DurableOperationState` by durable operation id. The core contract
does not enforce transition rules, concurrency tokens, or timeout behavior.

`Bondstone.EntityFrameworkCore` owns provider-neutral EF Core entity classes
and model mappings for outbox, inbox, and operation-state persistence. Entity
classes use an `Entity` suffix to keep EF persistence implementation separate
from core records and states.

`ApplyBondstonePersistence` applies the generic EF Core mappings to a
consumer-owned `ModelBuilder`. Consumers own migrations for now; Bondstone
does not ship migrations or provider-specific migration conventions in the
generic EF Core package.

## Deferred Persistence Decisions

The current core contracts intentionally do not decide:

- a public unit-of-work abstraction;
- EF Core store implementation and DbContext registration helpers;
- Bondstone-owned migration helpers or provider-specific migration
  conventions;
- outbox dispatch claiming, leases, retry-delay policy, or dead-letter
  ownership;
- inbox handle-once orchestration across a user handler and processed marker;
- operation-state transition policy or optimistic concurrency;
- provider-specific locking, SQL, schemas, or migration commands.

Those decisions require ADR review before broad implementation.
