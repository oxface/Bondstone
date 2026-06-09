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

[ADR 0036](../adr/0036-direct-transport-adapters-and-rebus-removal.md)
removes the Rebus adapter and makes direct provider adapters the current
transport direction. Bondstone transport packages should adapt broker/client
SDKs directly while keeping provider-native topology vocabulary and app-owned
broker setup.

[ADR 0034](../adr/0034-adapter-diversity-proof-transports.md) accepts the
first transport proof packages: `Bondstone.Transport.ServiceBus` and
`Bondstone.Transport.RabbitMq`. Their first applied scope is outgoing durable
outbox dispatch. Receive workers, provider-backed broker tests, broker
administration, and provider-backed receive reliability remain later slices.
See [transport-servicebus.md](transport-servicebus.md) and
[transport-rabbitmq.md](transport-rabbitmq.md).

`Bondstone.Transport.Local` is an explicit local queue adapter for samples,
tests, and local development while direct provider receive workers are still
being hardened. It is not a fallback and does not replace broker durability.
See [transport-local.md](transport-local.md).

[ADR 0035](../adr/0035-postgresql-dapper-persistence-proof.md) accepts the
first non-EF persistence proof package:
`Bondstone.Persistence.Dapper.Postgres`. Its scope is PostgreSQL-specific,
Dapper-assisted durable module messaging persistence without EF Core.
See [persistence-dapper-postgres.md](persistence-dapper-postgres.md).

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
- [persistence-dapper-postgres.md](persistence-dapper-postgres.md) records
  PostgreSQL Dapper-assisted non-EF persistence proof behavior.
- [transport-servicebus.md](transport-servicebus.md) records Azure Service Bus
  outgoing transport proof behavior.
- [transport-rabbitmq.md](transport-rabbitmq.md) records RabbitMQ outgoing
  transport proof behavior.
- [transport-local.md](transport-local.md) records explicit local queue
  transport behavior for samples, tests, and local development.

## Related Docs

The current package split is documented in [../packaging.md](../packaging.md).
The user-facing setup example is documented in [../setup.md](../setup.md).
The migration strategy for bringing source into this repository is documented
in [../archive/extraction.md](../archive/extraction.md).
Sample direction is documented in [../samples.md](../samples.md).
