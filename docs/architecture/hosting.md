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

The current public worker registration creates one aggregate worker over the
configured local module outbox dispatch graph. That keeps the first command
loop simple, but it is not the final isolation story for modular monoliths.
Module outboxes are ownership and backlog boundaries, so future worker
registration should be able to target all local modules by default or selected
module outboxes when a host needs to avoid noisy-neighbor dispatch behavior.

`AddBondstone` is the preferred host registration path. Package-specific
extensions mark what they contribute, and the builder rejects hosted outbox
processing when persistence or transport capability is missing. Runtime
packages can also register configuration validators with the shared builder so
cross-package host topology checks run once after host configuration completes.
These validators are for composed Bondstone graph checks, such as modules,
routes, capabilities, and transport topology. Normal .NET options objects
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

## Future Workers

Future module-targeted outbox workers, inbox workers, and maintenance workers
should live in `Bondstone.Hosting` when their underlying core abstractions are
stable. Examples include selected-module outbox dispatch, stale-claim
recovery, dead-letter retention, archiving, cleanup, and receive-side inbox
workers.

Provider SQL remains in provider packages. Transport-specific send, receive,
and envelope behavior remains in transport adapter packages.
