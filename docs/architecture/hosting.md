# Hosting Architecture

`Bondstone.Hosting` owns reusable .NET hosted workers that compose Bondstone
core abstractions.

The package depends on `Bondstone` and
`Microsoft.Extensions.Hosting`/options/logging abstractions. It does not own EF
Core mappings, PostgreSQL SQL, provider routing, transport envelopes, handler
discovery, or broker configuration.

## Outbox Worker

`DurableOutboxWorker` is a standard .NET hosted service over
`IDurableOutboxDispatcher`. It does not send directly through a transport and
does not own provider-specific SQL.

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
in registration order. The aggregate
batch uses one shared `BatchSize` budget; each module dispatcher receives only
the remaining budget, and later modules are skipped after the shared budget is
exhausted.

The aggregate worker is the only built-in worker topology. It is not a
fairness or noisy-neighbor isolation model: a slow module dispatcher can delay
later modules, and a module dispatcher failure stops the current aggregate
batch by bubbling to the hosted worker. The hosted worker logs the failed
batch with event id `1001` / `DispatchBatchFailed`, waits for `FailureDelay`,
and continues with a later batch. Module outboxes remain ownership boundaries;
selected-module worker registration, per-module worker options, parallel
aggregate dispatch, dispatch timeouts, and per-module concurrency controls are
not part of the current worker contract.

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
The current built-in example is the hosted outbox worker moving outbox records
through claim, dispatch, retry, and terminal-failure outcomes.

Bondstone workers must not manage provider-native infrastructure. Queue,
exchange, topic, subscription, rule, binding, broker retry, dead-letter,
prefetch, concurrency, subscription policy, and provider monitoring remain
application-owned or provider-native.

Transport adapter receive workers are explicit opt-in ergonomics around native
deliveries and Bondstone envelopes. They may read a native delivery, call
`IDurableEnvelopeReceiver`, and perform native settlement according to adapter
options. They must not infer topology from module registrations or become a
provider-neutral bus host. Application hosts may bypass adapter workers and
call `IDurableMessageEnvelopeSerializer` plus `IDurableEnvelopeReceiver` from
their own native receive loops.

Provider SQL remains in provider packages. Transport-specific send, receive,
and envelope behavior remains in transport adapter packages. Production worker
and broker ownership guidance lives in [../operations.md](../operations.md).
