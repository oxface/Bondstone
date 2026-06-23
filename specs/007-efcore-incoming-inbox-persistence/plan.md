# Implementation Plan: EF Core Incoming Inbox Persistence

**Branch**: `N/A - migrated existing functionality` | **Date**: 2026-06-23 | **Spec**: [spec.md](spec.md)

**Input**: Migrated feature specification from `specs/007-efcore-incoming-inbox-persistence/spec.md`

## Summary

EF Core incoming inbox persistence is an existing generic provider package
capability in `Bondstone.Persistence.EntityFrameworkCore`. It maps durable
incoming inbox rows into EF Core, converts records to and from provider-neutral
contracts, stages idempotent pending ingestion rows, exposes read-only
inspection queries, and wires generic EF Core setup for services and
module-specific ingestion boundaries. PostgreSQL-specific claim, lease,
retry, processed, terminal failure, locking, and SQL behavior is intentionally
outside this migration.

## Technical Context

**Language/Version**: C# on `net10.0`

**Primary Dependencies**: `Bondstone`, `Bondstone.Persistence`, `Microsoft.EntityFrameworkCore`, `Microsoft.Extensions.DependencyInjection`

**Storage**: Generic EF Core mapping and stores. Tests use EF Core InMemory for package-local mapping and change-tracker behavior.

**Testing**: xUnit unit and application tests in `tests/Bondstone.Persistence.EntityFrameworkCore.Tests`.

**Target Platform**: Packable EF Core persistence package consumed by application `DbContext` projects and provider-specific packages.

**Project Type**: Library package in a .NET monorepo.

**Performance Goals**: No explicit performance target is documented for the migrated feature.

**Constraints**:

- Preserve generic EF Core boundary and avoid PostgreSQL-specific SQL or
  locking behavior in this feature.
- Preserve application-owned EF migrations and schema rollout.
- Preserve durable incoming inbox receive identity as receiver module, message
  id, and handler identity.
- Preserve public API compatibility for EF Core setup and public incoming
  inbox entity/store types.
- Preserve testing distinction: EF InMemory checks are not relational
  durability proof.

**Scale/Scope**:

- Source and setup files: 8 files, 885 lines under `src/Bondstone.Persistence.EntityFrameworkCore`.
- Focused and setup-related tests: 7 files, 1,646 lines under `tests/Bondstone.Persistence.EntityFrameworkCore.Tests`.
- Total migrated scope: 15 files and 2,531 lines excluding ignored build output.

## Constitution Check

_GATE: Passed for migrated behavior based on current implementation evidence._

- **Library Boundary First**: Pass. The feature provides library mappings,
  stores, and setup helpers; applications still own migrations and host
  behavior.
- **Durable Identities And Message Semantics**: Pass. EF keys preserve explicit
  receive identity and do not derive handler identity from CLR type names.
- **Package Boundaries And Public API Compatibility**: Pass with caution. The
  feature exposes public EF Core setup and store types guarded by public API
  review expectations.
- **Persistence And Transport Ownership**: Pass. Generic EF Core owns mapping
  and ingestion/inspection stores, while PostgreSQL and transports own their
  respective provider/native behavior.
- **Evidence-Based Verification**: Pass. Tests cover mapping, ingestion,
  inspection, service registration, and module boundary wiring.

## Project Structure

### Documentation (this feature)

```text
specs/007-efcore-incoming-inbox-persistence/
├── spec.md
├── plan.md
└── tasks.md
```

### Source Code

```text
src/Bondstone.Persistence.EntityFrameworkCore/
├── IncomingInbox/
│   ├── EntityFrameworkCoreDurableIncomingInboxIngestionPersistenceScope.cs
│   ├── EntityFrameworkCoreDurableIncomingInboxIngestionStore.cs
│   ├── EntityFrameworkCoreDurableIncomingInboxInspectionStore.cs
│   ├── IncomingInboxMessageEntity.cs
│   └── IncomingInboxMessageEntityConfiguration.cs
└── Persistence/
    ├── BondstoneEntityFrameworkCoreModuleBuilderExtensions.cs
    ├── BondstoneEntityFrameworkCoreServiceCollectionExtensions.cs
    └── BondstoneModelBuilderExtensions.cs
```

### Tests

```text
tests/Bondstone.Persistence.EntityFrameworkCore.Tests/
├── IncomingInbox/
│   ├── EntityFrameworkCoreDurableIncomingInboxIngestionStoreTests.cs
│   ├── EntityFrameworkCoreDurableIncomingInboxInspectionStoreTests.cs
│   └── IncomingInboxMessageEntityTests.cs
├── MissingIncomingInboxMappingDbContext.cs
└── Persistence/
    ├── BondstoneEntityFrameworkCoreServiceCollectionExtensionsTests.cs
    ├── BondstoneModelBuilderExtensionsTests.cs
    └── EntityFrameworkCoreModuleTransactionBehaviorTests.cs
```

**Structure Decision**: Keep this migration scoped to generic EF Core incoming
inbox persistence. PostgreSQL-specific mutation and relational concurrency
behavior remains a separate migration target.

## Reconstructed Implementation Approach

### Phase 1: EF Core Incoming Inbox Entity And Mapping

The feature defines `IncomingInboxMessageEntity` as the EF representation of a
provider-neutral `DurableIncomingInboxRecord`. The mapping stores durable
envelope fields, trace context, receive identity, source transport name,
ingested timestamp, status, retry, processed, failed, failure reason, and
claim fields. `IncomingInboxMessageEntityConfiguration` maps the table name,
composite receive identity key, required fields, max lengths, string enum
conversions, and inspection/processing indexes.

### Phase 2: Model Builder Setup

`ApplyBondstoneIncomingInbox(...)` applies only incoming inbox mapping.
`ApplyBondstonePersistence(...)` includes incoming inbox mapping with outbox,
direct inbox, and operation-state mappings. This lets applications choose
granular mapping or the whole Bondstone durable persistence table set.

### Phase 3: EF Core Ingestion Store

`EntityFrameworkCoreDurableIncomingInboxIngestionStore<TDbContext>` validates
that the incoming inbox entity is mapped, finds existing rows by receiver
module, message id, and handler identity, returns `AlreadyIngested` when a row
exists, rejects new non-pending records, and adds new pending entities to the
`DbContext`. The ingestion persistence scope delegates transaction execution
and save behavior to the shared EF Core persistence scope.

### Phase 4: EF Core Inspection Store

`EntityFrameworkCoreDurableIncomingInboxInspectionStore<TDbContext>` exposes
no-tracking read models for general status inspection, stale processing claims,
and terminal failures. Queries normalize optional filters, validate UTC
cutoffs and positive counts, apply deterministic ordering, and return
provider-neutral records.

### Phase 5: Service And Module Registration

EF Core service setup registers incoming inbox ingestion and inspection stores
alongside the broader durable persistence stores. Module setup registers a
module-specific incoming inbox ingestion boundary that resolves the receiver
module's `DbContext`, preventing ingestion into the wrong module persistence
boundary.

## Verification Strategy

- **Focused checks**:
  - `dotnet test tests/Bondstone.Persistence.EntityFrameworkCore.Tests/Bondstone.Persistence.EntityFrameworkCore.Tests.csproj --configuration Release --filter "Category=Unit|Category=Application"`
- **Default gate**: `pnpm check`
- **Provider gate**: `pnpm backend:test:integration` when validating PostgreSQL relational claim, retry, terminal failure, and concurrency behavior.
- **Package gate**: `pnpm backend:pack` when package metadata or public API baselines are affected.

## Complexity Tracking

> Fill only if Constitution Check has violations that must be justified.

No constitution violations were identified for the migrated feature.

## Gaps And Follow-Up Candidates

- EF InMemory tests do not prove relational uniqueness, SQL shape, locking, or
  concurrency behavior; PostgreSQL integration tests cover those concerns and
  should be migrated separately.
- Generic EF Core incoming inbox does not implement claim, lease renewal,
  processed, retry, terminal failure, or stale mutation behavior.
- Application EF migrations are not shipped by Bondstone and remain
  application-owned.
- Incoming inbox cleanup, purge, replay, retention, and operator repair flows
  are not implemented by this generic EF Core feature.
- Public API surface includes concrete EF Core incoming inbox entity and store
  types; further visibility cleanup would require compatibility review.
