# Persistence Architecture

## Current Contract

Core persistence contracts live in `Bondstone` and stay independent from EF
Core, PostgreSQL, Rebus, SQL locking, schema migration, and background
dispatch mechanics.

`IDurableOutboxWriter` is the core write boundary for outgoing durable
messages. It accepts a `DurableMessageEnvelope`; provider implementations are
expected to persist that envelope inside the caller's local persistence
transaction when the caller needs atomic source-state-plus-outbox behavior.

`IDurableOutboxClaimer` is the core claim boundary for outgoing durable
messages that are ready for dispatch. It accepts a stable worker identity, a
positive lease duration, and a positive maximum row count, then returns the
claimed `DurableOutboxRecord` instances. Claim implementations mark rows as
`Processing`, populate claim ownership and lease expiry, and increment attempt
count at claim time. The claim boundary does not dispatch messages,
acknowledge transport delivery, renew leases, schedule retries, dead-letter
messages, or clean up stale work.

`DurableOutboxRecord` is a persistence-neutral record for a stored envelope,
the UTC time it was stored, and its current `DurableOutboxDispatchState`.

`DurableOutboxDispatchState` records provider-neutral outbox dispatch state:
`DurableOutboxStatus`, attempt count, optional next-attempt timestamp,
optional dispatched or failed timestamp, and optional failure reason. This lets
providers and future dispatchers share a common state shape without deciding
claim leases, retry-delay policy, dead-letter ownership, or transport
acknowledgement semantics in core.

Dispatch state also carries optional claim ownership and lease expiration:
`ClaimedBy` and `ClaimedUntilUtc`. These fields let provider-specific claim
implementations record who owns a processing lease and when it expires without
making the current core package own a dispatcher loop, lease renewal policy, or
stale-claim recovery policy.

`DurableInboxMessageKey` identifies receive-side deduplication by stable
message id, target module name, and handler identity. Handler identity is
free-form stable text; it should not be derived from handler CLR names.

`DurableInboxRecord` represents the persistence-neutral receive-side inbox
state for that key. It records when a message was received and, when complete,
when processing finished.

`IDurableInboxStore` exposes the minimal store operations needed by inbox
implementations: read a record, add a receive record, and mark it processed.
Provider implementations own the unique constraint, transaction, savepoint,
and concurrency behavior that make those operations reliable.

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

`AddBondstoneEntityFrameworkCorePersistence<TDbContext>` registers the
provider-neutral EF Core implementations for `IDurableOutboxWriter`,
`IDurableInboxStore`, `IDurableOperationStateStore`, and
`IDurableOperationReader`. It uses the consumer-owned DbContext type and keeps
service registration provider-neutral; it does not configure a database
provider, migrations, hosted dispatchers, locks, or retries.

`EntityFrameworkCoreDurableOutboxWriter<TDbContext>` stages outgoing outbox
messages in the current EF Core `DbContext`. It does not call
`SaveChangesAsync`; callers keep control of the transaction that commits
source state and outbox messages atomically.

`EntityFrameworkCoreDurableOperationStateStore<TDbContext>` reads and stages
durable operation state in the current EF Core `DbContext`. It does not own
transition policy, optimistic concurrency, or automatic transaction boundaries.

`EntityFrameworkCoreDurableInboxStore<TDbContext>` reads and stages inbox
records in the current EF Core `DbContext`. It does not treat a fast
change-tracker `AddAsync` as proof that a duplicate message cannot exist;
unique-constraint conflicts and races are relational/provider behavior that
must be verified with integration tests.

EF Core persistence components stage data. The caller-owned DbContext,
transaction, or module unit-of-work owns `SaveChangesAsync` so source state,
outbox messages, and operation state can commit atomically.

`Bondstone.EntityFrameworkCore.Postgres` owns PostgreSQL-specific persistence
behavior. Its first applied slice is Testcontainers-backed verification that
the provider-neutral EF Core mappings and stores work against PostgreSQL for
schema creation, transaction commit/rollback, and inbox uniqueness. It also
owns the first outbox claim implementation:
`PostgreSqlDurableOutboxClaimer<TDbContext>` claims due pending rows and
expired processing rows with `FOR UPDATE SKIP LOCKED`, writes claim lease
state, respects scheduled pending rows, and returns the claimed records. Public
PostgreSQL registration is available through
`AddBondstonePostgreSqlPersistence<TDbContext>`, which configures Npgsql for a
consumer-owned DbContext and composes the provider-neutral Bondstone EF
registrations. When the consumer maps Bondstone persistence to a non-default
schema, PostgreSQL registration must receive that same schema so provider-owned
SQL targets the mapped tables. The provider-neutral EF mappings own stable
primary-key names for the Bondstone tables; only names that need cross-package
reuse should be exposed as constants. `PostgreSqlPersistenceExceptionClassifier`
recognizes Npgsql unique-constraint violations, including inbox-message
duplicate violations against those mapped constraints, for provider-aware
orchestration code. PostgreSQL migration-helper, lease renewal, dispatch
acknowledgement, retry/dead-letter policy, and inbox duplicate-result
orchestration APIs remain deferred.

PostgreSQL integration tests verify two provider primitives that future public
APIs can build on: duplicate inbox inserts can be rolled back to a savepoint
without aborting the surrounding transaction, and pending outbox rows can be
selected with `FOR UPDATE SKIP LOCKED` so concurrent claimers do not block each
other. They also verify that the public PostgreSQL outbox claimer claims due
rows, writes lease state, skips locked rows, reclaims expired processing rows,
ignores active processing leases, respects scheduled pending rows, and works
through schema-aware service registration.

## Deferred Persistence Decisions

The current core contracts intentionally do not decide:

- a public unit-of-work abstraction;
- Bondstone-owned migration helpers or provider-specific migration
  conventions;
- outbox dispatcher loops, dispatch acknowledgement, lease renewal,
  retry-delay policy, or dead-letter ownership;
- inbox handle-once orchestration across a user handler and processed marker;
- inbox duplicate-result orchestration across real database providers;
- operation-state transition policy or optimistic concurrency;
- provider-specific schemas or migration commands.

Those decisions require ADR review before broad implementation.
