# EF Core Persistence

`Bondstone.EntityFrameworkCore` owns provider-neutral EF Core entity classes,
model mappings, staging stores, and the EF persistence scope.

## Model Mappings

Entity classes use an `Entity` suffix to keep EF persistence implementation
separate from core records and states.

The provider-neutral EF mappings own canonical Bondstone table names, column
names, constraint names, and shared model limits. Provider packages adapt those
names to their SQL dialect instead of redefining them.

`ApplyBondstonePersistence` applies the generic EF Core mappings to a
consumer-owned `ModelBuilder`. Consumers own migrations for now; Bondstone does
not ship migrations or provider-specific migration conventions in the generic
EF Core package.

## Registration And Stores

`AddBondstoneEntityFrameworkCorePersistence<TDbContext>` registers the
provider-neutral EF Core implementations for:

- `IDurableOutboxWriter`;
- `IDurableInboxStore`;
- `IDurableOperationStateStore`;
- `IDurableOperationReader`;
- `IEntityFrameworkCorePersistenceScope`.

The registration uses the consumer-owned DbContext type and stays
provider-neutral. It does not configure a database provider, migrations, hosted
dispatchers, locks, or retries.

`EntityFrameworkCoreDurableOutboxWriter<TDbContext>` stages outgoing outbox
messages in the current EF Core `DbContext`. It does not call
`SaveChangesAsync`; callers keep control of the transaction that commits
source state and outbox messages atomically.

`EntityFrameworkCoreDurableInboxStore<TDbContext>` reads and stages inbox
records in the current EF Core `DbContext`. It does not treat a fast
change-tracker `AddAsync` as proof that a duplicate message cannot exist.
Unique-constraint conflicts and races are relational/provider behavior that
must be verified with integration tests.

`EntityFrameworkCoreDurableOperationStateStore<TDbContext>` reads and stages
durable operation state in the current EF Core `DbContext`. It does not own
transition policy, optimistic concurrency, or automatic transaction boundaries.

## Persistence Scope

`IEntityFrameworkCorePersistenceScope` is the EF-specific transaction companion
for lower-level durable primitives. It:

- executes a caller-supplied operation inside an EF Core transaction when one
  is not already active;
- joins the current transaction when one exists;
- exposes `SaveChangesAsync` as an explicit commit delegate;
- commits or rolls back only transactions it started.

It does not discover handlers, publish messages, acknowledge transports,
capture domain events, or introduce a generic mediator.

The EF persistence scope is not a core abstraction and is not a required shape
for non-EF providers.

Future module command pipeline behavior should wrap
`IEntityFrameworkCorePersistenceScope` so validation, handler state changes,
inbox markers, outbox messages, operation-state updates, `SaveChangesAsync`,
and transaction commit happen in one module boundary. The current EF scope is
the lower-level transaction companion, not the final module unit-of-work API.
