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

## Adapter Diversity

Rebus, EF Core, and PostgreSQL are the first implementation path, not a
compatibility ceiling. After the first-class event loop has enough shape,
Bondstone should add thin adapter proof slices for additional transports such
as Azure Service Bus and RabbitMQ, and for at least one non-EF persistence
adapter such as direct ADO.NET or Dapper. The goal is to expose abstraction
gaps early while keeping each provider's topology and transaction model
native.

[ADR 0034](../adr/0034-adapter-diversity-proof-transports.md) accepts the
first transport proof packages: `Bondstone.Transport.ServiceBus` and
`Bondstone.Transport.RabbitMq`. Their first scope is outgoing durable outbox
dispatch with provider-native topology vocabulary. Receive workers,
provider-backed broker tests, and broker administration remain later slices.
See [transport-servicebus.md](transport-servicebus.md) and
[transport-rabbitmq.md](transport-rabbitmq.md) for the current proof scope.

## Topic Docs

- [messaging.md](messaging.md) records durable command, message identity, and
  messaging-boundary rules.
- [modules.md](modules.md) records module ownership, host topology, and
  module command execution rules.
- [hosting.md](hosting.md) records reusable hosted worker composition rules.
- [persistence.md](persistence.md) is the persistence entrypoint.
- [persistence-core.md](persistence-core.md) records provider-neutral durable
  persistence contracts.
- [persistence-ef-core.md](persistence-ef-core.md) records EF Core mapping,
  store, and persistence-scope rules.
- [persistence-postgresql.md](persistence-postgresql.md) records PostgreSQL
  provider behavior.
- [transport-rebus.md](transport-rebus.md) records Rebus transport adapter
  behavior.
- [transport-servicebus.md](transport-servicebus.md) records Azure Service
  Bus outgoing transport proof behavior.
- [transport-rabbitmq.md](transport-rabbitmq.md) records RabbitMQ outgoing
  transport proof behavior.

## Related Docs

The current package split is documented in [../packaging.md](../packaging.md).
The user-facing setup example is documented in [../setup.md](../setup.md).
The migration strategy for bringing source into this repository is documented
in [../archive/extraction.md](../archive/extraction.md).
Sample direction is documented in [../samples.md](../samples.md).
