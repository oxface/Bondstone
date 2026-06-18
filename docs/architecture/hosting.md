# Hosting Architecture

`Bondstone.Hosting` owns reusable .NET hosted workers that compose Bondstone
core abstractions.

The package depends on `Bondstone` and
`Microsoft.Extensions.Hosting`/options/logging abstractions. It does not own EF
Core mappings, PostgreSQL SQL, provider routing, transport envelopes, handler
discovery, or broker configuration.

## Outbox Worker

The built-in outbox worker is a standard .NET hosted service over
`IDurableOutboxDispatcher`, registered through the public worker setup helpers.
Its concrete hosted-service type is an implementation detail. The worker does
not send directly through a transport and does not own provider-specific SQL.

The worker uses `DurableOutboxWorkerOptions` for worker id, lease duration,
batch size, polling interval, and failure delay. It dispatches one batch per
call, immediately continues while rows are claimed, waits for the polling
interval when no rows are claimed, logs unexpected failures, waits for the
failure delay, and then continues. Host cancellation stops the loop.

The worker follows the same competitive-consumer model as provider claimers.
Multiple workers can run when the provider claim implementation supports
skip-locked or equivalent semantics.

The public worker registration creates one aggregate worker over the configured
app-facing `IDurableOutboxDispatcher`. For module-owned persistence, that
dispatcher can be `DurableModuleOutboxDispatchAggregator`, which calls local
module outbox dispatchers from module persistence registrations sequentially
in registration order. The aggregate batch uses one shared `BatchSize` budget;
each module dispatcher receives only the remaining budget, and later modules
are skipped after the shared budget is exhausted. Module dispatcher failures
propagate to the aggregate caller.

The aggregate worker is the only built-in worker topology. It is not a
fairness or noisy-neighbor isolation model: a slow module dispatcher can delay
later modules, and a module dispatcher failure stops the current aggregate
batch by bubbling to the hosted worker. The hosted worker logs the failed batch
with event id `1001` / `DispatchBatchFailed`, waits for `FailureDelay`, and
continues with a later batch. Module outboxes remain ownership boundaries;
selected-module worker registration, selected-module worker options,
per-module worker options, direct worker construction, parallel aggregate
dispatch, dispatch timeouts, fairness guarantees, selected-module scheduling
guarantees, and per-module concurrency controls are not part of the current
worker contract.

`AddBondstone` is the preferred host registration path. Package-specific
extensions register provider, transport, and worker services. Runtime
packages can also register configuration validators with the shared builder so
cross-package graph checks run once after host configuration completes. These
validators are for composed Bondstone setup checks such as module-owned
persistence and required dispatcher services. Normal .NET options objects
should continue to use `IValidateOptions<TOptions>` or equivalent options
validation.

Validation should stay close to its lifecycle: argument guards on public
methods, options validation for option objects, `AddBondstone` configuration
validators for composed host topology, and runtime checks for state that can
only be known while executing, such as EF Core model mappings or receive
envelope contents.
The current library-user setup example is in
[../setup.md](../setup.md).

Low-level registration methods remain available for tests and advanced
composition. For example, a consumer can register persistence and transport,
call `AddBondstoneDurableOutboxDispatcher`, and run a custom scheduler that
manually calls `IDurableOutboxDispatcher`. That is an advanced path; normal
host setup should prefer `AddBondstone`.

The worker resolves the dispatcher inside a service scope for each batch so
scoped persistence services are not captured by the singleton hosted service.
Startup validates that the dispatcher graph can be resolved and fails fast
when low-level registration omits required persistence or transport
dependencies.

## Worker Ownership

A Bondstone worker may move records between Bondstone-owned durable states.
The built-in outbox worker moves outbox records through claim, dispatch,
retry, and terminal-failure outcomes. The built-in incoming inbox processing
worker moves durable incoming inbox records through claim, retry, processed,
stale, and terminal receive-failure outcomes before handing each due row to
the existing module receive pipeline.

Bondstone workers must not manage provider-native infrastructure. Queue,
exchange, topic, subscription, rule, binding, broker retry, dead-letter,
prefetch, concurrency, subscription policy, and provider monitoring remain
application-owned or provider-native.

Transport adapter receive workers are explicit opt-in ergonomics around native
deliveries and Bondstone envelopes. They may read a native delivery, ingest it
into Bondstone's durable inbox, and perform native settlement according to
adapter options. They must not infer topology from module registrations or
become a provider-neutral bus host. Application hosts may bypass adapter
workers and call `IDurableMessageEnvelopeSerializer` plus the durable incoming
inbox ingestion boundary from their own native receive loops.

A transport ingestion worker or native listener remains adapter-owned or
application-owned because it reads provider-native deliveries and settles
provider-native messages. It may call Bondstone durable inbox ingestion and
settle the native delivery only after durable ingestion succeeds.

The durable incoming inbox processing worker is Bondstone-owned because it
hosts the existing durable incoming inbox dispatcher over Bondstone durable
incoming rows. It is opt-in host composition because the application decides
which workers run in each process. The worker uses
`DurableIncomingInboxWorkerOptions` for worker id, lease duration,
batch size, polling interval, failure delay, max attempts, and retry delays.
It processes one batch per call, immediately continues while rows are claimed,
waits for the polling interval when no rows are claimed, logs unexpected
batch failures with a consecutive failure count, waits for the failure delay,
and then continues. Host cancellation stops the loop. Like the outbox worker,
it resolves the dispatcher inside a service scope for each batch.

The current worker topology has three distinct loops:

1. a Bondstone outbox dispatch worker claims and dispatches outgoing durable
   outbox rows;
2. a transport receive/ingestion worker or app-owned adapter loop reads native
   transport deliveries and records durable incoming inbox rows before native
   settlement;
3. a Bondstone incoming inbox processing worker claims durable incoming rows
   and invokes module receive.

The incoming inbox processing worker must not provision broker topology, own
broker retry or dead-letter policy, infer operation failure, or mutate
application cleanup and retention state by default. Cleanup remains
application-owned unless a later accepted implementation adds an explicit
Bondstone retention worker or mutation API.

Provider SQL remains in provider packages. Transport-specific send, receive,
ingestion, settlement, and envelope behavior remains in transport adapter
packages. `Bondstone.Transport.RabbitMq` provides the first explicit
durable-incoming-inbox ingestion mode for its opt-in receive worker. Azure
Service Bus durable inbox ingestion parity remains pending; app-owned native
loops can still ingest into the durable inbox explicitly. Production worker and
broker ownership guidance lives in
[../operations.md](../operations.md).
