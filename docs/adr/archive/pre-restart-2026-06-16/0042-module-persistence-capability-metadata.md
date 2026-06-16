# 0042 Module Persistence Capability Metadata

Status: Archived
Application: Not Applicable
Date: 2026-06-10

## Context

Bondstone module registration currently records persistence with
`PersistenceProviderName` and optional `PersistenceContextType`.
`PersistenceProviderName` marks persistence capability, lets provider
transaction behaviors decide whether they own a module execution, and improves
missing-service diagnostics. `PersistenceContextType` is primarily EF Core
metadata, because EF transaction behavior needs a module DbContext type.

The current shape works, but EF-specific metadata lives on the provider-neutral
module registration. Non-EF providers such as `Bondstone.Persistence.Postgres`
do not need equivalent CLR context metadata. Several module persistence
resolvers also repeat similar logic: normalize the module name, validate the
module exists, find the module-owned service by module name, and throw a
module-aware diagnostic when it is missing.

The current fallback non-module persistence services are functional through
root-level persistence registration paths such as
`AddBondstoneEntityFrameworkCorePersistence<TDbContext>` and
`AddBondstonePostgreSqlPersistence<TDbContext>`. They support single-store or
low-level advanced composition when no module-owned implementations are
registered. Current module-boundary samples and preferred setup use
module-owned persistence instead, so the fallback path may be compatibility
residue rather than a desired long-term user path.

## Decision

Keep the current module registration persistence metadata for this package
shape. `PersistenceProviderName` remains on general module registration because
it is the provider-neutral marker that a module declares persistence, lets
provider transaction behaviors decide whether they own a module execution, and
keeps missing module-persistence diagnostics specific to the configured
provider.

Keep `PersistenceContextType` on the current registration shape for EF Core
module persistence. It is EF-specific metadata, but moving it now would require
a public registration and provider-extension contract change without removing
an active runtime need. Non-EF providers must not infer a required CLR context
model from this property; they should record provider-specific details such as
schema, session, connection, or SQL configuration in provider-owned services.

Do not introduce a shared provider-owned module capability registry in this
pass. That remains a possible future compatibility-sensitive refactor, but the
current resolver duplication and EF metadata leakage do not justify broad API
movement now.

Keep fallback non-module persistence services as supported advanced
composition for now. When no module-owned durable persistence implementations
are registered, core resolvers may use root-level `IDurableOutboxWriter`,
`IDurableInboxHandlerExecutor`, `IDurableOperationStateStore`, and
`IDurableOperationReader` services registered by lower-level provider setup.
This is not the preferred module-boundary setup. The preferred durable
messaging path remains module-owned persistence through provider-specific
module helpers. Retiring fallback behavior or removing public low-level service
composition requires later compatibility/API decision work.

## Amendment 2026-06-11: Runtime Factory Registrations

Separate module-owned durable persistence metadata from executable persistence
services. Provider module helpers must contribute passive runtime
registrations that carry the module name plus a factory for the executable
writer, inbox handler executor, or operation-state store. Core runtime
assembly builds module maps from those passive registrations and invokes only
the factory for the selected module inside the current DI scope.

Executable services such as `IDurableOutboxWriter`,
`IDurableInboxHandlerExecutor`, and `IDurableOperationStateStore` remain the
runtime contracts that provider factories create. Core runtime discovery must
not enumerate executable module-owned writer, inbox executor, or
operation-state store services merely to read `ModuleName`. Enumerating
executable services can construct unrelated provider dependencies such as an
EF `DbContext` or non-EF PostgreSQL session while another module is executing.
The older executable module-owned writer, inbox executor, and operation-state
store contracts are not retained as active extension contracts because
Bondstone does not need backward compatibility for this narrow provider
composition surface before its first stable, compatibility-bearing release.

Do not introduce module-specific `IServiceProvider` instances or child
containers for this change. Factories use the current execution scope's
service provider only when the selected module actually needs the provider
service. Provider factories should return lightweight executable wrappers over
scoped dependencies resolved from the current scope; they should not create
new owned disposable resources that the DI scope cannot dispose. A broader
provider capability registry, keyed DI model, or fallback retirement remains
future compatibility/API work.

## Amendment 2026-06-11: Durable Runtime Registrations Use A Registry

This amendment narrows the storage detail of the runtime factory registration
decision above without replacing it. Provider module helpers still contribute
passive runtime registrations with module names and factories, but those
registrations are stored in a provider-neutral
`DurableModulePersistenceRegistrationRegistry` rather than as individual
service descriptors.

`IServiceProvider` remains the factory input for creating the selected
module-owned writer, inbox executor, operation-state store, or outbox
dispatcher in the current execution scope. DI is not the metadata store for
individual module-owned durable runtime registrations.

Provider packages use Bondstone's advanced service-collection helper to get or
create the shared durable module persistence registration registry. This keeps
registry ownership, default-instance validation, and module outbox dispatcher
aggregation as Bondstone-owned composition rules rather than provider-local
copies.

The registry rejects duplicate registrations for the same module durable role
when provider setup adds them. Runtime map validation remains defensive, but
provider composition errors should fail as close to registration as possible.

## Amendment 2026-06-12: Module Outbox Dispatchers Use Registry Factories

Module outbox dispatchers are module-owned durable runtime services and use
the same passive registration pattern as module outbox writers, inbox handler
executors, and operation-state stores.

Provider module persistence setup contributes a
`DurableModuleOutboxDispatcherRegistration` with module name and factory to
`DurableModulePersistenceRegistrationRegistry`. The aggregate outbox
dispatcher reads those registrations and creates dispatchers from the current
scope only when dispatching. Bondstone no longer stores individual module
outbox dispatcher executable services in host DI or selects them through
`IEnumerable<T>`.

The process-level `IDurableOutboxDispatcher` service remains the public
dispatch surface. It may be the aggregate dispatcher for module-owned
persistence or a custom host dispatcher. Individual provider dispatchers are
created from registry factories and implement the normal dispatcher contract.

## Amendment 2026-06-12: Operation Reads Use Module-Owned Stores Only

Narrow the fallback decision for operation reads. The app-facing
`IDurableOperationReader` remains public, but Bondstone's default registration
is now the module operation reader that aggregates configured module-owned
`IDurableOperationStateStore` runtime registrations. It no longer preserves or
delegates to root-level `IDurableOperationReader` registrations, and it no
longer treats a root-level `IDurableOperationStateStore` as a read fallback.

This removes the special descriptor-reconstruction and fallback-disposal
behavior for `IDurableOperationReader`. Root-level low-level stores can still be
useful for provider primitives and existing send/receive fallback paths, but
operation status queries are tied to module-owned operation stores so reads
match the durable module runtime model.

If a host has no module-owned operation-state store registrations, the default
operation reader returns no state. Custom operation-read models should be
registered outside Bondstone's default reader contract rather than relying on a
hidden root reader fallback.

## Consequences

The current public module registration shape remains stable for existing
consumers and provider extensions.

Core continues to carry an EF-oriented context type in module registration.
Stable docs must describe that as current EF module metadata rather than a
general provider requirement.

Resolver duplication remains acceptable until a later public API policy or
provider metadata ADR accepts a migration path.

Fallback non-module persistence remains available for advanced single-store
composition and tests where the remaining low-level resolvers support it, but
operation reads no longer use root-level fallback readers or stores. Stable
docs should steer normal modular-monolith durable messaging toward module-owned
provider helpers.

## Related Decisions

- [0003 Package Boundaries And Target Framework](0003-package-boundaries-and-target-framework.md)
- [0016 EF Core Persistence Scope](0016-ef-core-persistence-scope.md)
- [0021 Fluent Service Composition Guardrails](0021-fluent-service-composition-guardrails.md)
- [0032 Module-Owned Durable EF Persistence](0032-module-owned-durable-ef-persistence.md)
- [0035 PostgreSQL Dapper Persistence Proof](0035-postgresql-dapper-persistence-proof.md)
- [0037 PostgreSQL Persistence Package Identity](0037-postgresql-persistence-package-identity.md)
- [0046 Public API Surface Policy](0046-public-api-surface-policy.md)

## Application Notes

- Current contract: module persistence declarations continue to use
  `UsePersistence(...)` or provider-specific module helpers. The provider name
  is the active module persistence marker. EF Core module persistence also
  records the module `DbContext` type. Provider-specific module helpers
  contribute passive durable module runtime registrations into
  `DurableModulePersistenceRegistrationRegistry` with module names and scoped
  factories for executable writer, inbox executor, operation-state store, and
  module outbox dispatcher services. `IDurableOperationReader` aggregates those
  module-owned operation-state stores only and does not delegate to root-level
  operation readers or root-level operation-state stores. Non-EF provider
  details stay provider-owned.
- Stable docs: applied to module architecture, persistence architecture, core
  persistence, EF Core persistence, PostgreSQL EF persistence, non-EF
  PostgreSQL persistence, and setup guidance.
- Agent guidance: no root AGENTS update is required because package boundaries
  and provider ownership rules are unchanged. The small public provider
  composition additions are documented in this ADR and stable persistence docs.
- Application evidence: current code stores provider name and optional context
  type in `BondstoneModuleRegistration`; uses passive durable module runtime
  registrations stored in `DurableModulePersistenceRegistrationRegistry` for
  command/event execution writer, inbox executor, operation-state store, and
  module outbox dispatcher selection. PostgreSQL EF integration tests prove the
  supported single-root fallback path for command send outbox/operation-state
  writes and command receive inbox/handler/operation-state writes when no
  module-owned runtime registrations are present. Operation-reader tests prove
  reads aggregate module-owned operation-state stores and ignore root operation
  reader/store registrations. Runtime tests prove unrelated EF DbContext and
  non-EF PostgreSQL session dependencies are not resolved while another module
  executes.
- Pending or deferred: a broader provider-owned metadata registry, resolver
  consolidation, keyed DI model, or retirement of the remaining fallback
  resolver paths remains future compatibility/API work and should be handled
  with ADR 0046 or a later focused ADR.

## Verification

The 2026-06-11 runtime factory registration amendment was verified with:

- `pnpm format:check`
- `pnpm backend:build`
- `pnpm backend:test:fast`

Verified decision application with:

- `pnpm format:check`
- `pnpm backend:restore`
- `pnpm backend:build`
- `pnpm backend:test:fast`
- `dotnet test tests/Bondstone.EntityFrameworkCore.Postgres.Tests/Bondstone.EntityFrameworkCore.Postgres.Tests.csproj --configuration Release --no-build --filter "FullyQualifiedName~SingleRootEntityFrameworkCorePersistence"`

The 2026-06-12 operation-read amendment was verified with:

- `dotnet test tests/Bondstone.Tests/Bondstone.Tests.csproj --configuration Release --filter "FullyQualifiedName~DurableOperationReaderTests"`
- `dotnet test tests/Bondstone.Persistence.EntityFrameworkCore.Tests/Bondstone.Persistence.EntityFrameworkCore.Tests.csproj --configuration Release --filter "FullyQualifiedName~BondstoneEntityFrameworkCoreServiceCollectionExtensionsTests"`
- `dotnet test tests/Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests/Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests.csproj --configuration Release --filter "FullyQualifiedName~BondstonePostgreSqlServiceCollectionExtensionsTests"`
- `dotnet build Bondstone.slnx --configuration Release --no-restore`
- `pnpm backend:test`
- `pnpm format:check`
- `pnpm check`
