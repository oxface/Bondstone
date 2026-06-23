# Implementation Plan: PostgreSQL Direct Inbox Persistence

**Branch**: `N/A - migrated existing functionality` | **Date**: 2026-06-23 | **Spec**: [spec.md](spec.md)

**Input**: Migrated feature specification from `specs/013-postgresql-direct-inbox-persistence/spec.md`

## Summary

PostgreSQL direct inbox persistence is an existing provider-specific capability in `Bondstone.Persistence.EntityFrameworkCore.Postgres`. It provides atomic direct inbox registration through provider-owned SQL, PostgreSQL duplicate classification for direct inbox primary-key violations, root and module setup for direct inbox registrar/executor services, and Testcontainers-backed verification of schema, transaction, duplicate, and fallback receive behavior. Generic EF Core direct inbox mapping and store behavior remain in the EF Core package and were migrated separately.

## Technical Context

**Language/Version**: C# on `net10.0`

**Primary Dependencies**: `Bondstone`, `Bondstone.Persistence`, `Bondstone.Persistence.EntityFrameworkCore`, EF Core `10.0.8`, Npgsql/PostgreSQL provider, Microsoft dependency injection

**Storage**: PostgreSQL through EF Core `DbContext` connection/transaction, with provider-owned SQL for direct inbox registration.

**Testing**: xUnit `Unit` and Testcontainers-backed `Integration` tests in `tests/Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests`.

**Target Platform**: Packable PostgreSQL EF Core provider package for production durable persistence.

**Project Type**: Library package in a .NET monorepo.

**Performance Goals**: No explicit performance target is documented for the migrated feature.

**Constraints**:

- Preserve package boundary: generic mapping/store behavior stays in `Bondstone.Persistence.EntityFrameworkCore`; PostgreSQL-specific SQL and classification stay here.
- Preserve direct inbox versus durable incoming inbox semantics.
- Registration SQL must not poison the caller's PostgreSQL transaction when a duplicate receive occurs.
- Consumers own EF migrations and schema rollout.
- Public setup and duplicate-classification APIs are compatibility-sensitive.

**Scale/Scope**:

- Source provider implementation/setup: 6 files, 549 lines under `src/Bondstone.Persistence.EntityFrameworkCore.Postgres`.
- Focused/shared tests: 8 files, 1,237 lines under `tests/Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests`.
- Total migrated scope: 14 files and 1,786 lines excluding ignored build output.

## Constitution Check

_GATE: Passed for migrated behavior based on current implementation evidence._

- **Library Boundary First**: Pass. The feature is a provider package capability, not an application host.
- **Durable Identities And Message Semantics**: Pass. Direct inbox registration preserves message id, module name, and handler identity.
- **Package Boundaries And Public API Compatibility**: Pass with caution. Public setup APIs and duplicate classifier behavior are compatibility-sensitive.
- **Persistence And Transport Ownership**: Pass. PostgreSQL owns provider-specific direct inbox SQL; generic EF Core mapping and transports are excluded.
- **Evidence-Based Verification**: Pass. Behavior is covered by unit tests plus real PostgreSQL integration tests.

## Project Structure

### Documentation (this feature)

```text
specs/013-postgresql-direct-inbox-persistence/
├── spec.md
├── plan.md
└── tasks.md
```

### Source Code

```text
src/Bondstone.Persistence.EntityFrameworkCore.Postgres/
├── Inbox/
│   ├── PostgreSqlDurableInboxRegistrar.cs
│   └── PostgreSqlModuleDurableInboxHandlerExecutor.cs
└── Persistence/
    ├── BondstonePostgreSqlBuilderExtensions.cs
    ├── BondstonePostgreSqlServiceCollectionExtensions.cs
    ├── PostgreSqlPersistenceExceptionClassifier.cs
    └── PostgreSqlTableIdentifier.cs
```

### Tests

```text
tests/Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests/
├── PostgreSqlSchemaTestDbContext.cs
├── PostgreSqlTestDbContext.cs
└── Persistence/
    ├── BondstonePostgreSqlServiceCollectionExtensionsTests.cs
    ├── PostgreSqlPersistenceExceptionClassifierTests.cs
    ├── PostgreSqlPersistenceInboxTests.cs
    ├── PostgreSqlPersistenceRegistrationTests.cs
    ├── PostgreSqlPersistenceSchemaTests.cs
    └── PostgreSqlSingleRootFallbackTests.cs
```

**Structure Decision**: Keep this migration scoped to PostgreSQL direct inbox provider behavior. Generic EF Core direct inbox mapping/store behavior remains under `specs/012-efcore-direct-inbox-persistence`; PostgreSQL outbox and durable incoming inbox behavior remain separate migrated slices.

## Reconstructed Implementation Approach

### Phase 1: Provider SQL And Registration

`PostgreSqlDurableInboxRegistrar<TDbContext>` builds a schema-aware quoted table name from the EF direct inbox mapping and runs a `WITH inserted AS (...)` SQL statement. The insert uses `ON CONFLICT ON CONSTRAINT "PK_inbox_messages" DO NOTHING`, returns the inserted row when new, or returns the existing row when duplicate. It enlists in the current EF transaction and preserves connection ownership.

### Phase 2: Duplicate Classification

`PostgreSqlPersistenceExceptionClassifier` walks nested exceptions for `PostgresException`, checks `PostgresErrorCodes.UniqueViolation`, and matches optional constraint names. `IsInboxMessageDuplicate(...)` specializes this to `InboxMessageEntityConfiguration.PrimaryKeyName`.

### Phase 3: Root And Module Setup

`AddBondstonePostgreSqlPersistence<TDbContext>()` registers the DbContext, generic EF Core persistence services, PostgreSQL direct inbox registrar, and root direct inbox handler executor. Module setup registers `DurableModuleInboxHandlerExecutorRegistration` with `PostgreSqlModuleDurableInboxHandlerExecutor<TDbContext>` and `DurableModuleInboxInspectionStoreRegistration` with the generic EF Core inspection store. Builder extensions expose the same setup through root and module Bondstone builders.

### Phase 4: Module Execution And Fallback

`PostgreSqlModuleDurableInboxHandlerExecutor<TDbContext>` composes the PostgreSQL registrar, generic EF direct inbox store, and optional `TimeProvider` through the provider-neutral `DurableInboxHandlerExecutor`. Integration tests prove single-root fallback receive paths persist and mark direct inbox rows processed through root PostgreSQL persistence.

## Verification Strategy

- **Focused checks**:
  - `dotnet test tests/Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests/Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests.csproj --configuration Release --filter "Category=Unit|Category=Integration"`
- **Default gate**: `pnpm check`
- **Provider gate**: `pnpm backend:test:integration`
- **Package gate**: `pnpm backend:pack` when package metadata or public API baselines are affected.

## Complexity Tracking

> Fill only if Constitution Check has violations that must be justified.

No constitution violations were identified for the migrated feature.

## Gaps And Follow-Up Candidates

- Consumer EF migration generation and rollout are application-owned and are not automated by this feature.
- Direct inbox cleanup, purge, replay, retention, and operator repair flows are not implemented.
- Generic EF Core direct inbox mapping/store behavior is outside this migration and is tracked under `specs/012-efcore-direct-inbox-persistence`.
- Durable incoming inbox PostgreSQL claim/lease/outcome behavior is separate and already migrated under the incoming inbox provider slice.
- Transport receive, broker settlement, and hosted workers are outside this feature.
