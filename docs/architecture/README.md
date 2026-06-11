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

## Direct Provider Adapters

Bondstone transport packages adapt broker/client SDKs directly while keeping
provider-native topology vocabulary and app-owned broker setup. The supported
production-oriented direct transport adapters are
`Bondstone.Transport.ServiceBus` and `Bondstone.Transport.RabbitMq`. They
include outgoing durable outbox dispatch, provider-native receive topology,
opt-in hosted receive workers, and provider-backed receive integration tests.
Bondstone owns persisted outbox retry and terminal failure state, while direct
provider receive adapters own settlement ordering and diagnostics without
owning broker retry/dead-letter policy. See
[transport-servicebus.md](transport-servicebus.md) and
[transport-rabbitmq.md](transport-rabbitmq.md).

Provider-neutral durable persistence contracts live in `Bondstone.Persistence`
and provider-neutral transport topology diagnostics live in
`Bondstone.Transport`. The `Bondstone` core package owns module execution,
module registration, and module-aware runtime resolution over those neutral
contracts.

`Bondstone.Transport.Local` is an explicit local queue adapter for samples,
tests, and local development. It is not a fallback and does not replace broker
durability. See [transport-local.md](transport-local.md).

`Bondstone.Persistence.Postgres` is PostgreSQL-specific, Dapper-backed
internally, and provides durable module messaging persistence without EF Core.
See [persistence-postgres.md](persistence-postgres.md).

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
- [persistence-postgres.md](persistence-postgres.md) records
  PostgreSQL-specific non-EF persistence proof behavior.
- [transport-servicebus.md](transport-servicebus.md) records Azure Service Bus
  direct transport behavior.
- [transport-rabbitmq.md](transport-rabbitmq.md) records RabbitMQ direct
  transport behavior.
- [transport-local.md](transport-local.md) records explicit local queue
  transport behavior for samples, tests, and local development.

## Related Docs

The current package split is documented in [../packaging.md](../packaging.md).
The user-facing setup example is documented in [../setup.md](../setup.md).
The migration strategy for bringing source into this repository is documented
in [../archive/extraction.md](../archive/extraction.md).
Sample direction is documented in [../samples.md](../samples.md).
