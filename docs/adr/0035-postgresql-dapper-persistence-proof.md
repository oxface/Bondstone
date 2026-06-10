# 0035 PostgreSQL Dapper Persistence Proof

Status: Amended
Application: Applied
Date: 2026-06-09

## Context

Phase 6 uses adapter-diversity proofs to keep Bondstone from hardening around
the first Rebus plus EF Core plus PostgreSQL path. ADR 0034 accepted transport
proof adapters for Service Bus and RabbitMQ. The remaining Phase 6 persistence
pressure point is whether Bondstone's durable module boundary can work without
`DbContext`.

The current core persistence contracts are provider-neutral, but the applied
module transaction behavior is EF-specific. A non-EF proof must test outbox,
inbox, operation-state, and module transaction boundaries directly against a
real relational provider. PostgreSQL is already part of the repository's
integration-test infrastructure, so it gives real SQL locking, conflicts, and
transaction behavior without adding another database dependency.

This decision affects package boundaries, provider support, durable
persistence behavior, transaction APIs, tests, and the modular monolith sample.

## Decision

Bondstone will add a proof-oriented non-EF persistence package:

- `Bondstone.Persistence.Dapper.Postgres`

The package is PostgreSQL-specific and Dapper-backed. It depends on
`Bondstone`, `Npgsql`, and `Dapper`. It does not depend on EF Core,
`Bondstone.EntityFrameworkCore`, Rebus, transport packages, hosting, samples,
or consumer domain assemblies.

The first implementation scope is durable module messaging persistence:

- outbox writing for commands and integration events;
- inbox registration and handle-once execution;
- operation-state read/write for current `Pending` and successful
  `Completed` command-loop states;
- module-owned transaction behavior for command handlers and integration
  event subscribers;
- module outbox dispatch across configured PostgreSQL/Dapper module stores;
- a narrow connection/session abstraction that module handlers can use to
  execute application SQL inside the same transaction as Bondstone durable
  records.

The package owns PostgreSQL SQL text, schema targeting, identifier quoting,
duplicate classification, row locking, and transaction management. It should
reuse the same durable table and column shape as the current EF Core
PostgreSQL path where practical so mixed EF and non-EF modules are easy to
inspect, but it does not depend on EF entity classes or mappings.

Dapper is an implementation helper, not the product abstraction. Public
consumer-facing APIs should expose Bondstone/PostgreSQL concepts such as a
module session or connection/transaction access, not a generic Dapper unit of
work.

Schema creation and migrations remain application-owned for production. The
proof package may provide explicit test/sample helpers to create the current
durable tables in a schema, but it must not silently create or migrate
production schemas during normal registration.

The modular monolith sample will be extended with a small third module, such
as billing, that uses the PostgreSQL/Dapper provider while ordering and
fulfillment remain EF-backed. The sample should prove mixed-persistence
durable event handling, not replace the EF sample path.

## Amendment 2026-06-09: Package Identity Renamed

ADR 0037 renames the public package identity to
`Bondstone.Persistence.Postgres` so Dapper is treated as an internal
implementation helper rather than the provider identity. The proof remains
PostgreSQL-specific and Dapper-backed internally.

## Consequences

Bondstone gets direct pressure on the provider-neutral persistence contracts
and module transaction model without introducing a generic mediator or a
generic public unit-of-work abstraction.

Some SQL will duplicate the EF PostgreSQL provider's behavior. That
duplication is intentional during proof work and should be consolidated only
after repeated provider behavior is clear.

The provider will initially support the current MVP happy path. Stale claim
recovery, stale inbox receive recovery, advanced operation-state transitions,
schema migration tooling, cleanup workers, and provider-backed performance
tuning remain later reliability or operational slices.

## Related Decisions

- [0010 PostgreSQL Provider And Integration Testing](0010-postgresql-provider-and-integration-testing.md)
- [0015 Inbox Handle-Once Orchestration](0015-inbox-handle-once-orchestration.md)
- [0016 EF Core Persistence Scope](0016-ef-core-persistence-scope.md)
- [0027 Optional EF Core Persistence Mapping](0027-optional-ef-core-persistence-mapping.md)
- [0031 Durable Operation State Integration](0031-durable-operation-state-integration.md)
- [0032 Module-Owned Durable EF Persistence](0032-module-owned-durable-ef-persistence.md)
- [0034 Adapter Diversity Proof Transports](0034-adapter-diversity-proof-transports.md)
- [0037 PostgreSQL Persistence Package Identity](0037-postgresql-persistence-package-identity.md)

## Application Notes

- Current contract: Phase 6 adds `Bondstone.Persistence.Postgres` as the
  current public package identity for the PostgreSQL-specific, Dapper-backed
  proof provider for durable module messaging persistence without EF Core.
  The current proof supports one `NpgsqlDataSource` per service provider;
  multiple modules can use separate schemas in the same database.
- Stable docs: Package and persistence direction are reflected in
  [docs/packaging.md](../packaging.md),
  [docs/architecture/persistence.md](../architecture/persistence.md),
  [docs/architecture/persistence-core.md](../architecture/persistence-core.md),
  [docs/architecture/persistence-postgres.md](../architecture/persistence-postgres.md),
  [docs/mvp-plan.md](../mvp-plan.md), and this ADR.
- Agent guidance: Root [AGENTS.md](../../AGENTS.md) requires ADR review for
  provider support and now points at adapter-diversity proof work.
- Application evidence: `Bondstone.Persistence.Postgres` is scaffolded
  and implements durable outbox, inbox, operation-state, module transaction,
  and module outbox dispatch proof behavior. Integration tests verify schema
  creation, transaction commit/rollback, inbox handle-once, and operation
  state. The modular monolith sample includes a Dapper/PostgreSQL-backed
  billing module alongside EF-backed ordering and fulfillment modules.
- Pending or deferred: Production migration helpers, stale receive recovery,
  stale claim recovery, cleanup workers, advanced operation-state policy, and
  multi-data-source selection, and generic Dapper/provider abstraction are
  deferred.

## Verification

Read back this ADR and affected stable docs before implementation.

Executable verification:

- `dotnet build Bondstone.slnx --configuration Release --no-restore --disable-build-servers`
- `dotnet test tests/Bondstone.Persistence.Postgres.Tests/Bondstone.Persistence.Postgres.Tests.csproj --configuration Release --no-build --filter "Category=Integration" --disable-build-servers`
- `dotnet test tests/Bondstone.Samples.Tests/Bondstone.Samples.Tests.csproj --configuration Release --no-build --filter "Category=Integration" --disable-build-servers`
- `dotnet test Bondstone.slnx --configuration Release --no-build --filter "Category=Unit|Category=Application" --disable-build-servers`
- `pnpm format:check`
- `git diff --check`
