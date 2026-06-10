# Module Persistence Metadata

Status: Resolved by
[ADR 0042](../../adr/0042-module-persistence-capability-metadata.md).

Goal: decide whether module persistence metadata should stay on general module
registration or move into provider-owned module capability metadata.

## Scope

- Reviewed `PersistenceProviderName`, `PersistenceContextType`,
  `UsePersistence(...)`, provider-specific module persistence helpers, and
  module-owned persistence resolvers.
- Reviewed fallback non-module persistence services that can be used through
  root-level provider registration when no module-owned implementations exist.
- Separated current module capability metadata from provider implementation
  details such as EF DbContext type or PostgreSQL schema/session configuration.
- Evaluated whether a shared module capability registry should be introduced
  now.
- Decided the current stance for fallback non-module persistence services.

## ADRs

- [0042 Module Persistence Capability Metadata](../../adr/0042-module-persistence-capability-metadata.md)
- [0046 Public API Surface Policy](../../adr/0046-public-api-surface-policy.md)

## Resolution

- `PersistenceProviderName` stays on general module registration as the active
  module persistence marker used by validation, provider transaction behavior,
  and diagnostics.
- `PersistenceContextType` stays on the current registration shape for EF Core
  module persistence. It is current EF metadata, not a general requirement for
  non-EF providers.
- Provider-specific details such as PostgreSQL schema, session, connection,
  transaction, and SQL behavior stay provider-owned.
- A provider-owned module capability registry is deferred as future
  compatibility-sensitive work rather than implemented in this pass.
- Fallback non-module `IDurableOutboxWriter`,
  `IDurableInboxHandlerExecutor`, `IDurableOperationStateStore`, and
  `IDurableOperationReader` composition remains supported advanced
  single-store behavior when no module-owned services are registered. It is not
  the preferred module-boundary setup.
- The supported single-root EF fallback path is covered by PostgreSQL EF
  integration tests for command send and command receive.

## Follow-Up

- Handle a provider-owned metadata registry or resolver consolidation through
  later focused ADR work if the compatibility value becomes worth the churn.
- Handle fallback-service retirement or public low-level API tightening
  through ADR 0046 or a later accepted compatibility/API policy.

## Verification

- `pnpm format:check`
- `pnpm backend:restore`
- `pnpm backend:build`
- `pnpm backend:test:fast`
- `dotnet test tests/Bondstone.EntityFrameworkCore.Postgres.Tests/Bondstone.EntityFrameworkCore.Postgres.Tests.csproj --configuration Release --no-build --filter "FullyQualifiedName~SingleRootEntityFrameworkCorePersistence"`

The follow-up integration proof added executable coverage for the retained
single-root fallback behavior.
