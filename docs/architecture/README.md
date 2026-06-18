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
persistence and app-owned broker integration. The supported durable
persistence path is `Bondstone.Persistence.EntityFrameworkCore` plus
`Bondstone.Persistence.EntityFrameworkCore.Postgres`.

Bondstone ships thin RabbitMQ and Azure Service Bus adapter packages for
native-driver envelope plumbing. Other broker integrations, including Rebus,
remain app-owned code around Bondstone's durable envelope serializer, outbound
`IDurableEnvelopeDispatcher`, and durable inbox ingestion boundary.
Bondstone owns persisted outbox retry and terminal failure state; broker
retry, dead-letter policy, topology, provisioning, and consumer lifecycle
remain app-owned.

Provider-neutral durable persistence contracts live in `Bondstone.Persistence`.
There is no active provider-neutral transport diagnostics package. The
`Bondstone` core package owns module execution, module registration, and
module-aware runtime resolution over the persistence contracts.

`Bondstone.Transport.Local` is an explicit local queue adapter for samples,
tests, and local development. It is not a fallback and does not replace broker
durability. See [transport-local.md](transport-local.md).

Module-local domain event contracts live in `Bondstone.DomainEvents` in the
core `Bondstone` package. They are not transport messages, message-bus
contracts, or provider runtime APIs. EF-backed collection and persistence live
in `Bondstone.Persistence.EntityFrameworkCore` and activate only through
explicit module opt-in and explicit EF mapping.

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
- [transport-local.md](transport-local.md) records explicit local queue
  transport behavior for samples, tests, and local development.

## Related Docs

The current package split is documented in [../packaging.md](../packaging.md).
The user-facing setup example is documented in [../setup.md](../setup.md).
Production receive, outbox, inbox, operation, migration, retention, and
ownership guidance is documented in [../operations.md](../operations.md).
Current diagnostic surfaces and the OpenTelemetry-native direction are
documented in [../observability.md](../observability.md).
Sample direction is documented in [../samples.md](../samples.md).
