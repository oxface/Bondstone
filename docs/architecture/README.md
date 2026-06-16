# Architecture

Bondstone is a .NET library for durable module boundaries.

## Positioning

Bondstone supports modular monoliths first, including a first-class path for
splitting a module into a separately deployed service when that module needs
independent scalability, deployment, or operational isolation.

Bondstone should also be usable in microservice setups that require internal
durability, stable message identities, local transactional outbox processing,
receive-side inbox deduplication, and transport adapter integration.

The intended continuity matters: a team should not need to throw away module
contracts, message identities, outbox/inbox behavior, or handler patterns just
because a module moves from in-process composition to a separate service.

## Durable Boundary Principles

- Module boundaries are explicit in code and persistence.
- Durable message identities are stable and not derived from CLR names.
- Source state and outgoing durable messages commit atomically.
- Receive-side handlers are protected by an inbox boundary.
- Transport adapters connect module boundaries without owning domain behavior.
- The same message contracts and handler patterns should survive service
  extraction when practical.
- Service extraction should be a supported evolution path, not a separate
  rewrite story.

## Transport And Persistence Surface

Bondstone is simplifying after MVP around EF Core/PostgreSQL durable
persistence and smaller transport adapter boundaries. The supported durable
persistence path is `Bondstone.Persistence.EntityFrameworkCore` plus
`Bondstone.Persistence.EntityFrameworkCore.Postgres`.

`Bondstone.Transport.RabbitMq` remains the only active direct broker adapter.
It keeps provider-native vocabulary and app-owned broker setup while the
transport surface is simplified. Bondstone owns persisted outbox retry and
terminal failure state; broker retry, dead-letter policy, topology, and
consumer lifecycle remain app-owned. See
[transport-rabbitmq.md](transport-rabbitmq.md).

Provider-neutral durable persistence contracts live in `Bondstone.Persistence`.
There is no active provider-neutral transport diagnostics package. The
`Bondstone` core package owns module execution, module registration, and
module-aware runtime resolution over the persistence contracts.

`Bondstone.Transport.Local` is an explicit local queue adapter for samples,
tests, and local development. It is not a fallback and does not replace broker
durability. See [transport-local.md](transport-local.md).

`Bondstone.Capabilities.DomainEvents` contains small module-local domain event
capability contracts. It is not a transport, message bus, or provider runtime
package. `Bondstone.Capabilities.DomainEvents.EntityFrameworkCore` is the EF
Core bridge that provides the first runtime implementation for collection and
persistence.

## Topic Docs

- [messaging.md](messaging.md) records durable command, integration event,
  message identity, receive pipeline, and messaging-boundary rules.
- [modules.md](modules.md) records module ownership, module registration, and
  module command/event execution rules.
- [hosting.md](hosting.md) records reusable hosted worker composition rules.
- [persistence.md](persistence.md) is the persistence entrypoint.
- [persistence-core.md](persistence-core.md) records provider-neutral durable
  persistence contracts.
- [persistence-ef-core.md](persistence-ef-core.md) records EF Core mapping,
  store, and persistence-scope rules.
- [persistence-postgresql.md](persistence-postgresql.md) records PostgreSQL
  provider behavior.
- [transport-rabbitmq.md](transport-rabbitmq.md) records RabbitMQ direct
  transport behavior.
- [transport-local.md](transport-local.md) records explicit local queue
  transport behavior for samples, tests, and local development.

## Related Docs

The current package split is documented in [../packaging.md](../packaging.md).
The user-facing setup example is documented in [../setup.md](../setup.md).
Sample direction is documented in [../samples.md](../samples.md).
