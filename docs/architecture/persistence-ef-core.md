# EF Core Persistence

`Bondstone.Persistence.EntityFrameworkCore` owns provider-neutral EF Core
entity classes, model mappings, staging stores, the EF persistence scope, and
optional EF-backed domain event persistence.

## Model Mappings

Entity classes use an `Entity` suffix to keep EF persistence implementation
separate from core records and states.

The provider-neutral EF mappings own canonical Bondstone table names, column
names, constraint names, and shared model limits. Provider packages adapt those
names to their SQL dialect instead of redefining them.

`ApplyBondstonePersistence` applies the durable EF Core persistence bundle to
a consumer-owned `ModelBuilder`: outbox, inbox, and operation state. Domain
event persistence is optional and is not included in this bundle.

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
messaging tables unless their configured module behavior uses those stores.
Consumers own migrations. Bondstone does not ship migrations or
provider-specific migration conventions in the generic EF Core package.

`ApplyBondstoneIncomingInbox` maps the optional durable inbox incoming-ledger
table accepted by ADR 0017. The mapping is intentionally granular and not
included in `ApplyBondstonePersistence`. Mapping the table does not register
durable inbox hosted workers, transport handoff, provider-specific mutation
stores, or direct receive behavior.

For modules that call `UseDurableMessaging()` with EF persistence, Bondstone
validates the module DbContext model during module command and event
subscriber execution. The model must include outbox and inbox mappings, either
by calling `ApplyBondstoneOutbox` and `ApplyBondstoneInbox`, or by using the
durable `ApplyBondstonePersistence` helper. Operation-state mapping validation
remains tied to operation-state store usage. Modules that opt into EF domain
event persistence must map domain event records explicitly with
`ApplyBondstoneDomainEvents()`.

## Registration And Stores

`AddBondstoneEntityFrameworkCorePersistence<TDbContext>` registers the
provider-neutral EF Core implementations for:

- `IDurableOutboxWriter`;
- `IDurableOutboxInspectionStore`;
- `IDurableInboxStore`;
- `IDurableInboxInspectionStore`;
- `IDurableIncomingInboxIngestionStore`;
- `IDurableIncomingInboxInspectionStore`;
- `IDurableOperationStateStore`;
- `IEntityFrameworkCorePersistenceScope`.

The registration uses the consumer-owned DbContext type and stays
provider-neutral. It does not configure a database provider, migrations, hosted
dispatchers, locks, or retries.

Those root-level services can also serve advanced non-module fallback write and
receive paths when no module-owned durable runtime registrations are
registered. Normal durable module-boundary setup should prefer
provider-specific module helpers so source-module sends, target-module
receives, and operation reads use the owning module persistence boundary.

Service registration and model mapping remain separate. Registering the EF
Core durable stores does not force every DbContext model to map every
Bondstone table; callers choose the full or granular mappings in
`OnModelCreating` according to the capabilities the DbContext supports.

Modules opt into EF-backed command and event subscriber transactions with
`UseEntityFrameworkCorePersistence<TDbContext>` on `BondstoneModuleBuilder`.
That module-owned registration records EF persistence metadata, reuses the
provider-neutral EF durable store registrations, and registers EF transaction
behavior as Bondstone's hidden provider transaction runner. The host
still owns the environment-specific DbContext provider configuration,
connection strings, schema policy, and operational topology.

The recorded EF metadata is the current module persistence provider name plus
the module `DbContext` type. The context type is used by EF transaction
behavior and mapping validation; it is not a general requirement for non-EF
persistence providers.

For modular-monolith durable messaging, the command loop resolves durable EF
stores by module name when module-specific provider runtime registrations are
present in `DurableModulePersistenceRegistrationRegistry`.
Source-module sends stage outgoing outbox rows and caller-supplied `Pending`
operation state through the source module `DbContext`. Target-module receives
stage inbox markers, handler state, successful `Completed` operation state,
and any outgoing outbox rows through the target module `DbContext`.
`IDurableOperationReader` aggregates operation states across configured module
stores and returns completed state ahead of pending state for the same
operation id. It does not use root-level EF operation stores as a read fallback.

`EntityFrameworkCoreDurableOutboxWriter<TDbContext>` stages outgoing outbox
messages in the current EF Core `DbContext`. It does not call
`SaveChangesAsync`; callers keep control of the transaction that commits
source state and outbox messages atomically.

`EntityFrameworkCoreDurableOutboxInspectionStore<TDbContext>` reads
`TerminalFailed` outbox rows with optional source-module and failed-at cutoff
filters. It orders oldest terminal failures first and returns
`DurableOutboxRecord` values without tracking entities. It does not claim,
reset, replay, purge, or archive rows.

`EntityFrameworkCoreDurableInboxStore<TDbContext>` reads and stages inbox
records in the current EF Core `DbContext`. It does not treat a fast
change-tracker `AddAsync` as proof that a duplicate message cannot exist.
Unique-constraint conflicts and races are relational/provider behavior that
must be verified with integration tests.

Already-received but unprocessed inbox rows remain a loud receive outcome, not
an EF Core stale-row recovery feature.

`EntityFrameworkCoreDurableInboxInspectionStore<TDbContext>` reads
unprocessed inbox rows with optional module and received-at cutoff filters. It
orders oldest receives first and returns `DurableInboxRecord` values without
tracking entities. It does not mark rows processed, delete rows, re-run
handlers, or settle transport messages.

`EntityFrameworkCoreDurableOperationStateStore<TDbContext>` reads and stages
durable operation state in the current EF Core `DbContext`. Operation-state
mapping includes nullable diagnostic context columns for module name, durable
message type name, and handler identity. It does not own transition policy,
optimistic concurrency, or automatic transaction boundaries. The store
requires the operation-state entity mapping and fails with a clear
`ApplyBondstoneOperationState()` mapping error if it is used with a DbContext
that does not map operation state.

The EF operation-state store implements the operation expiration query
contract by returning `Pending` and `Running` states whose `UpdatedAtUtc` is at
or before the caller's UTC cutoff, ordered by oldest update first and bounded
by the requested maximum count. The query is for application-owned expiry
jobs; the EF package does not schedule expiry or choose terminal status
policy.

The durable incoming inbox mapping uses `IncomingInboxMessageEntity`, mapped
to `incoming_inbox_messages` by default. Its shape is the accepted durable
inbox incoming ledger: durable receive identity, structural durable envelope
fields, source transport diagnostic name, ingested timestamp, status, attempt
count, retry and terminal outcome timestamps, failure reason, and claim
owner/lease fields.

`EntityFrameworkCoreDurableIncomingInboxIngestionStore<TDbContext>` stages a
new pending `IncomingInboxMessageEntity` when no row exists for the durable
receive identity, or returns the existing row as already ingested. It does not
call `SaveChangesAsync`, execute handlers, settle broker messages, claim work,
or infer operation state. Relational duplicate races remain provider-specific
behavior until a provider-specific ingestion store is added. The store requires the
incoming inbox entity mapping and fails with a clear
`ApplyBondstoneIncomingInbox()` mapping error if it is used with a DbContext
that does not map the incoming ledger.

`AddBondstoneEntityFrameworkCorePersistence<TDbContext>` also registers an
`IDurableIncomingInboxIngestionPersistenceScope` adapter over the existing EF
persistence scope. Transport adapters that opt into durable incoming inbox
ingestion use that scope to save staged incoming rows before native broker
settlement. The scope is still only an ingestion commit boundary; it does not
run handlers, record processing outcomes, or own transport behavior.

When a module declares EF persistence with
`UseEntityFrameworkCorePersistence<TDbContext>` or
`UseEntityFrameworkCoreModulePersistence<TDbContext>`, the EF package also
registers a module incoming inbox ingestion boundary. The boundary resolves
the receiver module's `TDbContext`, creates the matching EF ingestion store,
and saves through an EF persistence scope for that same context. This mirrors
the module transaction runner's DbContext selection without making transport
packages know EF Core or DbContext types. Root-level EF ingestion services
remain the fallback only for advanced single-store composition when no module
runtime registrations or module ingestion boundaries are present.

`EntityFrameworkCoreDurableIncomingInboxInspectionStore<TDbContext>` reads
incoming inbox rows without tracking. It supports broad status inspection,
stale processing claim inspection by claim-lease cutoff, and terminal
receive-failure inspection by failed-at cutoff. The queries can filter by
receiver module and source transport diagnostic name. They do not claim,
renew, schedule retry, mark processed, reset, replay, purge, or archive rows.
The store requires the incoming inbox entity mapping and fails with a clear
`ApplyBondstoneIncomingInbox()` mapping error if it is used with a DbContext
that does not map the incoming ledger. The EF package does not implement
durable inbox claiming, lease renewal, outcome recording, hosted workers,
transport handoff, or processing behavior. PostgreSQL-specific incoming inbox
mutation stores live in the PostgreSQL provider package.

### Operation-State Diagnostic Column Migration

The operation-state result diagnostic context uses nullable columns on the
`operation_states` table:

| Column            | CLR property                           | Max length |
| ----------------- | -------------------------------------- | ---------- |
| `ModuleName`      | `OperationStateEntity.ModuleName`      | 128        |
| `MessageTypeName` | `OperationStateEntity.MessageTypeName` | 256        |
| `HandlerIdentity` | `OperationStateEntity.HandlerIdentity` | 512        |

Consumers own EF migrations. Applications upgrading a database created before
these columns existed should add the nullable columns to every mapped
operation-state table, including per-module schemas when modules map
Bondstone persistence separately. No backfill is required. Existing operation
rows without diagnostic context remain valid and result-reader diagnostics
fall back to the operation id and requested result type.

The columns are diagnostic only. They are not part of the operation-state key,
are not indexed by Bondstone, and do not change operation-state precedence,
completion, or result deserialization behavior.

EF Core is the first provider implementation for optional module-local domain
event collection and persistence. That implementation lives in
`Bondstone.Persistence.EntityFrameworkCore`.

The accepted EF collection mechanism is narrow: the module transaction
behavior collects domain events through `DbContext.ChangeTracker` entries
whose entities implement the `Bondstone.DomainEvents.IDomainEventSource`
contract. The EF behavior must not require a Bondstone aggregate base class, a
custom DbContext base class, `SaveChangesAsync` interception, arbitrary
method-name reflection, automatic publication from EF interceptors, or hidden
handler dispatch.

EF-backed domain event persistence is EF-owned runtime behavior. It activates
only when modules declare EF Core persistence and explicitly opt into domain
event persistence with `UseEntityFrameworkCoreDomainEventPersistence()`. The
opt-in is narrow EF-owned module metadata and requires EF persistence to be
declared first. Bondstone does not provide a public capability-step registry,
public named pipeline slots, public contribution records, or public
application middleware contracts. The EF implementation registers with
Bondstone's hidden provider runtime service contracts.

EF-backed domain event collection belongs inside module command execution and
module integration event subscriber execution. The placement is:

- inside the EF module transaction;
- after operation-state and receive-inbox behavior have opened the inner
  handler path;
- while the module execution context is active;
- after handler logic completes;
- before the transaction owner calls `SaveChangesAsync`;
- before the transaction commits.

The EF behavior stages configured module-local domain event records in the
same `DbContext` transaction as handler state, inbox markers, operation-state
updates where applicable, and outgoing outbox rows. It clears source pending
events only after collection, staging, `SaveChangesAsync`, and transaction
commit all succeed. Collection, staging, save, or commit failure must leave
pending events uncleared by Bondstone. When the EF scope joins an already
active transaction it does not own, Bondstone stages and saves through that
scope but does not clear pending events because it cannot observe the outer
commit.

EF module transaction behavior opens an observed-transaction callback scope on
the current execution context while the module transaction boundary is active.
The context reports whether Bondstone observes commit and accepts commit or
rollback callbacks. EF domain event persistence stages records after handler
execution and registers source clearing through that context only when commit
is observable. The transaction runner remains generic and does not know about
domain events. Those callbacks are lightweight runtime cleanup hooks; domain
event records are already staged before `SaveChangesAsync`, and callback
failures can surface after the EF transaction has committed.

The EF behavior is not a hidden domain-event bus. Calling
`UseEntityFrameworkCoreDomainEventPersistence()` does not resolve or invoke
local domain event handlers or map domain events to integration events. EF
persistence remains the clear-on-observed-commit owner for the sources it
stages.

The EF record shape is `DomainEventRecordEntity`, mapped to
`domain_event_records` by default. It stores a stable record id, owning module,
`DomainEventIdentityAttribute` name, payload type name, serialized JSON
payload, optional payload metadata, occurred/captured timestamps, and optional
trace or causation metadata. Domain event JSON uses Bondstone's configured
durable payload JSON options, but domain events are not serialized through
`IDurablePayloadSerializer` because `IDomainEvent` does not implement
`IMessage`.

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

Module command and event subscriber runtime execution uses
`IEntityFrameworkCorePersistenceScope` so validation, handler state changes,
inbox markers, outbox messages, operation-state updates where applicable,
`SaveChangesAsync`, and transaction commit happen in one module boundary when
those stores are used. The current applied EF module transaction runner wraps
opted-in module command execution and event subscriber execution. Command
receive can also save successful operation-state completion updates through
the EF scope. Event receive operation-state completion, receive failure state,
retry state, stale receive recovery, and receive acknowledgement policy are
outside the current EF persistence contract. This outer module transaction is
the commit owner; the low-level inbox executor only stages the processed
marker in the current `DbContext`. The EF scope remains the lower-level
transaction companion, not a standalone public unit-of-work API.

Sample and integration verification should assert persisted state after the EF
transaction commits. In-handler signals are not durable completion evidence
because the module transaction may still be open.
