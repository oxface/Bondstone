# EF Core Persistence

`Bondstone.EntityFrameworkCore` owns provider-neutral EF Core entity classes,
model mappings, staging stores, and the EF persistence scope.

## Model Mappings

Entity classes use an `Entity` suffix to keep EF persistence implementation
separate from core records and states.

The provider-neutral EF mappings own canonical Bondstone table names, column
names, constraint names, and shared model limits. Provider packages adapt those
names to their SQL dialect instead of redefining them.

`ApplyBondstonePersistence` applies the full generic EF Core persistence shape
to a consumer-owned `ModelBuilder`. It remains the convenience helper for hosts
that want all current Bondstone durable persistence tables.

Hosts that only need selected durable persistence pieces can use the granular
mapping helpers accepted in
[ADR 0027](../adr/0027-optional-ef-core-persistence-mapping.md):

```csharp
modelBuilder.ApplyBondstoneOutbox();
modelBuilder.ApplyBondstoneInbox();
modelBuilder.ApplyBondstoneOperationState();
```

`ApplyBondstoneOutbox` maps outbox messages, `ApplyBondstoneInbox` maps inbox
messages, and `ApplyBondstoneOperationState` maps durable operation state.
Modules that only need module-owned EF transactions do not need to map durable
messaging tables unless their chosen durable capabilities use those stores.
Consumers own migrations for now; Bondstone does not ship migrations or
provider-specific migration conventions in the generic EF Core package.

For modules that use the current `UseDurableMessaging` capability with EF
persistence, Bondstone validates the module DbContext model during module
command execution. The model must include outbox and inbox mappings, either by
calling `ApplyBondstoneOutbox` and `ApplyBondstoneInbox`, or by using the full
`ApplyBondstonePersistence` helper. Operation-state mapping validation remains
tied to later operation-state integration.

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

Service registration and model mapping remain separate. Registering the EF
Core durable stores does not force every DbContext model to map every
Bondstone table; callers choose the full or granular mappings in
`OnModelCreating` according to the capabilities the DbContext supports.

Modules opt into EF-backed command transactions with
`UseEntityFrameworkCorePersistence<TDbContext>` on `BondstoneModuleBuilder`.
That module-owned registration records EF persistence metadata, reuses the
provider-neutral EF durable store registrations, and attaches an EF command
system pipeline behavior for modules that declare that capability. System
behaviors are ordered by the module command runtime, so the EF transaction
boundary wraps normal application pipeline behaviors. The host still owns the
environment-specific DbContext provider configuration, connection strings,
schema policy, and operational topology.

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
The store requires the operation-state entity mapping and fails with a clear
`ApplyBondstoneOperationState()` mapping error if it is used with a DbContext
that does not map operation state.

EF Core does not currently collect or persist domain events. Proposed future
domain event persistence should use provider abstractions and module command
pipeline behavior instead of requiring a custom DbContext base class or
overriding `SaveChangesAsync`. That proposal is tracked in
[ADR 0028](../adr/0028-domain-event-persistence-capability.md).

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

Module command pipeline behavior wraps `IEntityFrameworkCorePersistenceScope`
so validation, handler state changes, inbox markers, outbox messages,
operation-state updates, `SaveChangesAsync`, and transaction commit happen in
one module boundary when those capabilities are used. The current applied EF
module behavior wraps opted-in module command execution and saves handler
changes, receive inbox markers, outgoing outbox messages, and successful
operation-state completion updates through the EF scope. Receive failure
state, retry state, stale receive recovery, and receive acknowledgement policy
remain future durable-boundary pieces. The EF scope remains the lower-level
transaction companion, not a standalone public unit-of-work API.
