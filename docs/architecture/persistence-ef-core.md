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
mapping helpers:

```csharp
modelBuilder.ApplyBondstoneOutbox();
modelBuilder.ApplyBondstoneInbox();
modelBuilder.ApplyBondstoneOperationState();
```

`ApplyBondstoneOutbox` maps outbox messages, `ApplyBondstoneInbox` maps inbox
messages, and `ApplyBondstoneOperationState` maps durable operation state.
Modules that only need module-owned EF transactions do not need to map durable
messaging tables unless their chosen durable capabilities use those stores.
Consumers own migrations. Bondstone does not ship migrations or
provider-specific migration conventions in the generic EF Core package.

For modules that use the current `UseDurableMessaging` capability with EF
persistence, Bondstone validates the module DbContext model during module
command and event subscriber execution. The model must include outbox and
inbox mappings, either by calling `ApplyBondstoneOutbox` and
`ApplyBondstoneInbox`, or by using the full `ApplyBondstonePersistence`
helper. Operation-state mapping validation remains tied to operation-state
store usage.

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

Those root-level services can also serve the advanced non-module fallback path
when no module-owned durable persistence implementations are registered. Normal
durable module-boundary setup should prefer provider-specific module helpers so
source-module sends and target-module receives use the owning module
persistence boundary.

Service registration and model mapping remain separate. Registering the EF
Core durable stores does not force every DbContext model to map every
Bondstone table; callers choose the full or granular mappings in
`OnModelCreating` according to the capabilities the DbContext supports.

Modules opt into EF-backed command and event subscriber transactions with
`UseEntityFrameworkCorePersistence<TDbContext>` on `BondstoneModuleBuilder`.
That module-owned registration records EF persistence metadata, reuses the
provider-neutral EF durable store registrations, and attaches EF system
pipeline behaviors for modules that declare that capability. System behaviors
are ordered by the module command and event subscriber runtimes, so the EF
transaction boundary wraps normal application pipeline behaviors. The host
still owns the environment-specific DbContext provider configuration,
connection strings, schema policy, and operational topology.

The recorded EF metadata is the current module persistence provider name plus
the module `DbContext` type. The context type is used by EF transaction
behavior and mapping validation; it is not a general requirement for non-EF
persistence providers.

For modular-monolith durable messaging, the command loop resolves durable EF
stores by module name when module-specific provider registrations are present.
Source-module sends stage outgoing outbox rows and caller-supplied `Pending`
operation state through the source module `DbContext`. Target-module receives
stage inbox markers, handler state, successful `Completed` operation state,
and any outgoing outbox rows through the target module `DbContext`.
`IDurableOperationReader` can aggregate operation states across configured
module stores and returns completed state ahead of pending state for the same
operation id.

`EntityFrameworkCoreDurableOutboxWriter<TDbContext>` stages outgoing outbox
messages in the current EF Core `DbContext`. It does not call
`SaveChangesAsync`; callers keep control of the transaction that commits
source state and outbox messages atomically.

`EntityFrameworkCoreDurableInboxStore<TDbContext>` reads and stages inbox
records in the current EF Core `DbContext`. It does not treat a fast
change-tracker `AddAsync` as proof that a duplicate message cannot exist.
Unique-constraint conflicts and races are relational/provider behavior that
must be verified with integration tests.

Already-received but unprocessed inbox rows remain a loud receive outcome, not
an EF Core stale-row recovery feature.

`EntityFrameworkCoreDurableOperationStateStore<TDbContext>` reads and stages
durable operation state in the current EF Core `DbContext`. It does not own
transition policy, optimistic concurrency, or automatic transaction boundaries.
The store requires the operation-state entity mapping and fails with a clear
`ApplyBondstoneOperationState()` mapping error if it is used with a DbContext
that does not map operation state.

ADR 0028 accepts EF Core as the first provider for optional module-local domain
event collection and persistence. The implementation is pending and tracked in
[../backlog/10-domain-events.md](../backlog/10-domain-events.md), after the
runtime pipeline and capability planning tracked in
[../backlog/09-module-pipeline-and-capability-runtime.md](../backlog/09-module-pipeline-and-capability-runtime.md).

The accepted EF collection mechanism is narrow: the module transaction
behavior collects domain events through `DbContext.ChangeTracker` entries
whose entities implement the `Bondstone.DomainEvents.IDomainEventSource`
contract. EF Core must not require a Bondstone aggregate base class, a custom
DbContext base class, `SaveChangesAsync` interception, arbitrary method-name
reflection, or automatic publication from EF interceptors.

EF-backed domain event collection belongs inside module command execution and
module integration event subscriber execution. After application handlers and
application pipeline behaviors complete, the EF behavior collects pending
domain events, stages configured module-local domain event records in the same
`DbContext` transaction, saves them with handler state, inbox markers,
operation-state updates where applicable, and outgoing outbox rows, then
clears the source pending events only after successful staging, save, and
transaction commit.

Persisted domain event records remain module-local. They are not outgoing
outbox records and are not transport events. Mapping selected domain events to
public integration events must remain explicit module code that publishes a
separate registered `IIntegrationEvent`.

## Persistence Scope

`IEntityFrameworkCorePersistenceScope` is the EF-specific transaction companion
for lower-level durable primitives. It:

- executes a caller-supplied operation inside an EF Core transaction when one
  is not already active;
- joins the current transaction when one exists;
- exposes `SaveChangesAsync` for callers that own an explicit EF save point;
- commits or rolls back only transactions it started.

The lower-level scope does not discover handlers, publish messages,
acknowledge transports, capture domain events by itself, or introduce a
generic mediator.

The EF persistence scope is not a core abstraction and is not a required shape
for non-EF providers.

Module command and event subscriber pipeline behaviors wrap
`IEntityFrameworkCorePersistenceScope` so validation, handler state changes,
inbox markers, outbox messages, operation-state updates where applicable,
`SaveChangesAsync`, and transaction commit happen in one module boundary when
those capabilities are used. The current applied EF module behavior wraps
opted-in module command execution and event subscriber execution. Command
receive can also save successful operation-state completion updates through
the EF scope. Event receive operation-state completion, receive failure state,
retry state, stale receive recovery, and receive acknowledgement policy are
outside the current EF persistence contract. This outer module transaction is
the commit owner; the low-level inbox executor only stages the processed marker
in the current `DbContext`. The EF scope remains the lower-level transaction
companion, not a standalone public unit-of-work API.

Sample and integration verification should assert persisted state after the EF
transaction commits. In-handler signals are not durable completion evidence
because the module transaction may still be open.
