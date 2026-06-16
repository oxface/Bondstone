# 0032 Module-Owned Durable EF Persistence

Status: Amended
Application: Applied
Date: 2026-06-08

## Context

Bondstone targets modular monoliths first. A modular monolith is not just a
single process with several code namespaces; each module should be able to own
its persistence boundary so it can later move behind a service boundary with
minimal semantic change.

The first Phase 4 sample proved the durable command loop, but it also exposed
an adoption gap: the easiest current wiring puts ordering state, fulfillment
state, outbox rows, inbox rows, and operation state in one host `DbContext`.
That proves the command loop mechanics, but it does not fully prove module
persistence ownership.

The Phase 4 sample is an adoption-proof sample while the MVP API settles. It
is expected to expose friction and may later be polished or replaced.

The current EF and PostgreSQL registrations also use normal scoped service
registrations for durable stores such as `IDurableOutboxWriter`,
`IDurableInboxHandlerExecutor`, and `IDurableOperationStateStore`. With more
than one module `DbContext`, those services need module identity to choose the
right persistence boundary. Source-module sends must write to the source
module outbox. Target-module receive must write handler state, inbox markers,
operation completion, and any outgoing outbox messages in the target module
transaction.

This decision narrows the current MVP command-loop behavior only. It does not
decide cross-database distributed transactions, event fan-out, receive retry
state, stale receive recovery, migration helpers, or provider-specific schema
validation beyond the current PostgreSQL integration.

## Decision

Bondstone will support module-owned durable EF persistence for the current
command loop.

Modules that opt into EF persistence keep declaring their module-owned
`DbContext` with `UseEntityFrameworkCorePersistence<TDbContext>()`. Provider
registration can then bind PostgreSQL durable infrastructure for that module
context. The sample-preferred shape is one module-owned `DbContext` per module,
with separate PostgreSQL schemas or databases when a sample wants clear local
ownership.

Durable command send resolves outbox and operation-state persistence from the
current source module execution context. A source module command that stages a
durable command writes the outgoing envelope, source module state, and
caller-supplied `Pending` operation state through the source module
transaction.

Durable command receive resolves inbox, operation-state, and outgoing outbox
persistence from the target module execution context. A target module handler
must commit handler state, receive inbox markers, successful `Completed`
operation state, and any outgoing outbox messages in one target module EF
transaction.

Outbox dispatch must be able to dispatch from every configured local module
outbox. Dispatching remains a per-module outbox operation under the hood; the
app-facing dispatcher may aggregate results across registered module outbox
dispatchers so existing worker and smoke-test entrypoints stay simple.

`IDurableOperationReader` may aggregate operation state across configured
module operation stores. If an operation id appears in more than one module
store during the current command loop, a completed state is the useful
caller-visible result and takes precedence over pending state. Richer
operation-state ownership, consistency, and concurrency policy remain future
work.

Existing single-`DbContext` setups remain supported. Module-aware resolution
is the preferred path for modular-monolith samples and applications with
separate module persistence ownership.

## Amendment 2026-06-08: Module-Bound Provider Registration

The preferred PostgreSQL setup shape is module-bound. Applications should be
able to configure module-owned EF metadata and PostgreSQL durable provider
binding through the module builder, for example
`module.UsePostgreSqlPersistence<TDbContext>(connectionString, schema: "...")`.

Root-level PostgreSQL provider binding can remain available for compatibility
or advanced composition, but stable setup docs and samples should prefer the
module-bound provider API so module ownership is visible at the registration
site.

## Consequences

The Phase 4 modular monolith sample can prove the real adoption pattern:
ordering and fulfillment own separate EF persistence boundaries while the
durable command loop still stages, dispatches, receives, and commits each
module's durable state correctly.

The command loop becomes less dependent on ambient "the one registered EF
store" services. It must resolve durable stores by module name for send,
receive, operation tracking, and dispatch.

Provider registration has to bind module names to provider-owned durable SQL
components. This is a small public setup-surface change for PostgreSQL, but it
keeps provider-specific connection strings, schemas, and SQL behavior in the
provider package.

Operation-state lookup across module stores is useful for the current sample
but intentionally modest. It is not a distributed consistency model, a
polling API, or a result payload contract.

Sample verification should assert persisted state after the module transaction
commits. In-handler signals are not durable completion evidence because the
outer EF transaction may still be open.

## Related Decisions

- [0016 EF Core Persistence Scope](0016-ef-core-persistence-scope.md)
- [0025 Module Command Execution Boundary](0025-module-command-execution-boundary.md)
- [0027 Optional EF Core Persistence Mapping](0027-optional-ef-core-persistence-mapping.md)
- [0031 Durable Operation State Integration](0031-durable-operation-state-integration.md)

## Application Notes

- Current contract: Module-owned durable EF persistence is supported for the
  current command loop. Source-module sends resolve module-specific outbox and
  pending operation-state persistence. Target-module receives resolve
  module-specific inbox handling, successful operation completion, handler
  state, and outgoing outbox persistence. Local module outbox dispatch and
  operation reads aggregate across configured module stores. Terminal outbox
  inspection resolves the named module's inspection store so operator queries
  use the same module persistence boundary as dispatch. PostgreSQL provider
  binding should be available from the module builder as the preferred public
  setup shape.
- Stable docs: Current module-owned durable persistence behavior is described
  in [docs/architecture/modules.md](../architecture/modules.md),
  [docs/architecture/messaging.md](../architecture/messaging.md),
  [docs/architecture/persistence.md](../architecture/persistence.md),
  [docs/architecture/persistence-core.md](../architecture/persistence-core.md),
  [docs/architecture/persistence-ef-core.md](../architecture/persistence-ef-core.md),
  [docs/architecture/persistence-postgresql.md](../architecture/persistence-postgresql.md),
  [docs/setup.md](../setup.md), [docs/samples.md](../samples.md), and
  [docs/archive/mvp-plan.md](../archive/mvp-plan.md).
- Agent guidance: Root [AGENTS.md](../../AGENTS.md) requires ADR review before
  durable behavior, provider support, migration policy, transport strategy, or
  public API changes.
- Application evidence: Core module-aware durable persistence contracts and
  resolvers are applied for outbox writing, receive inbox execution,
  operation-state stores, aggregate operation reading, and aggregate outbox
  dispatch through `DurableModuleOutboxDispatchAggregator`. EF Core module
  outbox writer and operation-state store wrappers are applied. PostgreSQL
  module-specific inbox handler executor, outbox dispatcher, and outbox
  inspection-store bindings are applied. The modular monolith adoption-proof
  sample now uses separate
  `OrderingDbContext` and `FulfillmentDbContext` types with separate
  PostgreSQL schemas. Module-bound PostgreSQL provider registration is applied
  through `module.UsePostgreSqlPersistence<TDbContext>(...)`. The sample is now
  a minimal ASP.NET Core API project using explicit local transport routing by
  default and a preferred direct RabbitMQ path instead of Rebus. Ordering,
  fulfillment contracts, and fulfillment implementation now live in
  module-owned sample assemblies, and module command handlers are registered with
  `RegisterFromAssemblyContaining<TMarker>()`.
- Pending or deferred: None for the module-owned durable EF persistence
  decision. Cross-database distributed transactions, provider-specific
  migration helpers, operation-state concurrency policy, failure states, stale
  receive recovery, and richer service-extraction operation lookup remain
  separate future decisions.

## Verification

Read back affected stable docs and verified with:

- `dotnet restore Bondstone.slnx --disable-build-servers -p:NuGetAudit=false`
- `dotnet build Bondstone.slnx --configuration Release --no-restore --disable-build-servers`
- `dotnet test tests/Bondstone.Tests/Bondstone.Tests.csproj --configuration Release --no-build --filter "Category=Unit" --disable-build-servers`
- `dotnet test tests/Bondstone.EntityFrameworkCore.Postgres.Tests/Bondstone.EntityFrameworkCore.Postgres.Tests.csproj --configuration Release --no-build --filter "Category=Unit" --disable-build-servers`
- `dotnet test Bondstone.slnx --configuration Release --no-build --filter "Category=Unit|Category=Application" --disable-build-servers`
- `dotnet test tests/Bondstone.Samples.Tests/Bondstone.Samples.Tests.csproj --configuration Release --no-build --filter "Category=Integration" --disable-build-servers`
- `pnpm format:check`

After the 2026-06-08 amendment, read back affected docs and verified the
module-bound provider API and service-provider hosted sample path with:

- `dotnet build Bondstone.slnx --configuration Release --no-restore --disable-build-servers`
- `dotnet test tests/Bondstone.EntityFrameworkCore.Postgres.Tests/Bondstone.EntityFrameworkCore.Postgres.Tests.csproj --configuration Release --no-build --filter "Category=Unit" --disable-build-servers`
- `dotnet test tests/Bondstone.Samples.Tests/Bondstone.Samples.Tests.csproj --configuration Release --no-build --filter "Category=Integration" --disable-build-servers`

After splitting the sample into module-owned assemblies and reshaping the app
entrypoint into a minimal API, verified with:

- `dotnet restore Bondstone.slnx --disable-build-servers -p:NuGetAudit=false`
- `dotnet build Bondstone.slnx --configuration Release --no-restore --disable-build-servers`
- `dotnet test Bondstone.slnx --configuration Release --no-build --filter "Category=Unit|Category=Application" --disable-build-servers`
- `dotnet test tests/Bondstone.Samples.Tests/Bondstone.Samples.Tests.csproj --configuration Release --no-build --filter "Category=Integration" --disable-build-servers`
- `pnpm format:check`
