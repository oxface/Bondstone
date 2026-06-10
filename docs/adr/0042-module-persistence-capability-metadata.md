# 0042 Module Persistence Capability Metadata

Status: Accepted
Application: Applied
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

## Consequences

The current public module registration shape remains stable for existing
consumers and provider extensions.

Core continues to carry an EF-oriented context type in module registration.
Stable docs must describe that as current EF module metadata rather than a
general provider requirement.

Resolver duplication remains acceptable until a later public API policy or
provider metadata ADR accepts a migration path.

Fallback non-module persistence remains available for advanced single-store
composition and tests, but stable docs should steer normal modular-monolith
durable messaging toward module-owned provider helpers.

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
  records the module `DbContext` type. Non-EF provider details stay
  provider-owned.
- Stable docs: applied to module architecture, persistence architecture, core
  persistence, EF Core persistence, PostgreSQL EF persistence, non-EF
  PostgreSQL persistence, and setup guidance.
- Agent guidance: no root AGENTS update is required because package
  boundaries, public API shape, and provider ownership rules are unchanged.
- Application evidence: current code stores provider name and optional context
  type in `BondstoneModuleRegistration` and uses module-owned service
  resolvers for writers, inbox executors, dispatchers, and operation stores.
  PostgreSQL EF integration tests prove the supported single-root fallback
  path for command send outbox/operation-state writes and command receive
  inbox/handler/operation-state writes when no module-owned services are
  registered.
- Pending or deferred: a provider-owned metadata registry, resolver
  consolidation, or fallback-service retirement remains future
  compatibility/API work and should be handled with ADR 0046 or a later
  focused ADR.

## Verification

Verified decision application with:

- `pnpm format:check`
- `pnpm backend:restore`
- `pnpm backend:build`
- `pnpm backend:test:fast`
- `dotnet test tests/Bondstone.EntityFrameworkCore.Postgres.Tests/Bondstone.EntityFrameworkCore.Postgres.Tests.csproj --configuration Release --no-build --filter "FullyQualifiedName~SingleRootEntityFrameworkCorePersistence"`
