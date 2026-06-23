# Feature Specification: EF Core Direct Inbox Persistence

**Feature Branch**: `N/A - migrated existing functionality`

**Created**: 2026-06-23

**Status**: migrated

**Input**: Existing generic EF Core direct inbox implementation in `src/Bondstone.Persistence.EntityFrameworkCore`, with focused package tests in `tests/Bondstone.Persistence.EntityFrameworkCore.Tests`

## Scope And Boundaries _(mandatory)_

**Bondstone Capability**: Generic EF Core table mapping, entity conversion, direct inbox store, inspection store, and DI/model-builder registration for provider-neutral direct inbox records

**Affected Packages/Areas**:

- `src/Bondstone.Persistence.EntityFrameworkCore/Inbox`
- `src/Bondstone.Persistence.EntityFrameworkCore/Persistence/BondstoneModelBuilderExtensions.cs`
- `src/Bondstone.Persistence.EntityFrameworkCore/Persistence/BondstoneEntityFrameworkCoreServiceCollectionExtensions.cs`
- `tests/Bondstone.Persistence.EntityFrameworkCore.Tests/Inbox`
- `tests/Bondstone.Persistence.EntityFrameworkCore.Tests/Persistence/BondstoneModelBuilderExtensionsTests.cs`
- `tests/Bondstone.Persistence.EntityFrameworkCore.Tests/Persistence/BondstoneEntityFrameworkCoreServiceCollectionExtensionsTests.cs`
- `tests/Bondstone.Persistence.EntityFrameworkCore.Tests/EntityFrameworkCoreTestDbContext.cs`
- `docs/architecture.md`
- `docs/packaging.md`
- `docs/testing.md`

**Out Of Scope**:

- Provider-neutral direct inbox contracts, records, handler executor, inspector, and module resolver.
- PostgreSQL direct inbox registrar, atomic insert behavior, duplicate classification, SQL error classification, and schema integration tests.
- Durable incoming inbox transport receive ledger, claim, retry, stale, and terminal receive failure behavior.
- Source outbox and operation-state EF Core behavior except where shared extension methods register the full persistence bundle.
- Consumer-owned EF migrations and schema rollout.
- Cleanup, purge, replay, retention, and operator repair automation.

## User Scenarios & Testing _(mandatory)_

### User Story 1 - Map Direct Inbox Records To EF Core Rows (Priority: P1)

As a persistence provider implementer, I want a direct inbox EF Core entity and mapping so provider-neutral direct inbox records persist with a stable table shape.

**Why this priority**: Stores, inspection, provider SQL, and consumer migrations all depend on the stable `inbox_messages` shape.

**Independent Test**: Run `dotnet test tests/Bondstone.Persistence.EntityFrameworkCore.Tests/Bondstone.Persistence.EntityFrameworkCore.Tests.csproj --configuration Release --filter "Category=Unit|Category=Application"` after build.

**Acceptance Scenarios**:

1. **Given** a provider-neutral direct inbox record, **When** it is converted to an EF entity, **Then** message id, module name, handler identity, received timestamp, and processed timestamp are preserved.
2. **Given** an EF direct inbox entity, **When** it is converted back to a record, **Then** the provider-neutral record round-trips.
3. **Given** `ApplyBondstoneInbox(...)`, **When** the EF model is built, **Then** only the direct inbox entity is configured with table `inbox_messages`.
4. **Given** `ApplyBondstonePersistence(...)`, **When** the EF model is built, **Then** the direct inbox entity is included in the full durable persistence model.

---

### User Story 2 - Stage And Read Direct Inbox Records Through EF Core (Priority: P2)

As a direct inbox handler executor, I want an EF Core store so received direct inbox records can be staged, read by composite identity, and marked processed within the current DbContext transaction.

**Why this priority**: Direct handler execution relies on the store to persist receive markers and processed timestamps without owning provider-specific SQL.

**Independent Test**: Run package application tests after build.

**Acceptance Scenarios**:

1. **Given** no matching row, **When** the EF Core store reads by key, **Then** it returns null.
2. **Given** a valid direct inbox record, **When** the EF Core store adds it, **Then** the row is staged on the current DbContext without saving immediately.
3. **Given** the current DbContext saves changes, **When** the store reads the record by key, **Then** it returns the mapped provider-neutral record.
4. **Given** an existing row, **When** the store marks it processed, **Then** the processed timestamp is staged and persists after SaveChanges.
5. **Given** no matching row, **When** the store marks it processed, **Then** it throws a clear failure.

---

### User Story 3 - Inspect Unprocessed Direct Inbox Rows Through EF Core (Priority: P3)

As an operations or maintenance caller, I want an EF Core inspection store so received-but-unprocessed direct inbox rows can be listed deterministically by module and cutoff.

**Why this priority**: Direct inbox inspection is the supported read-only visibility surface for incomplete direct receive markers.

**Independent Test**: Run package application tests after build.

**Acceptance Scenarios**:

1. **Given** processed and unprocessed direct inbox rows, **When** inspection runs, **Then** only unprocessed rows are returned.
2. **Given** a module filter, cutoff, and max count, **When** inspection runs, **Then** rows are filtered by normalized module name, received-at cutoff, and count.
3. **Given** multiple matching rows, **When** inspection runs, **Then** rows are ordered by received timestamp, module name, message id, and handler identity.
4. **Given** a non-positive max count or non-UTC cutoff, **When** inspection runs, **Then** validation fails before querying.

---

### User Story 4 - Register EF Core Direct Inbox Services And Mapping (Priority: P4)

As an application composer, I want EF Core direct inbox services and model-builder extensions registered with the generic EF Core persistence package so a DbContext can opt into Bondstone direct inbox persistence.

**Why this priority**: Consumers need a predictable setup path that composes with the rest of EF Core durable persistence without replacing custom registrations.

**Independent Test**: Run package unit tests after build.

**Acceptance Scenarios**:

1. **Given** `AddBondstoneEntityFrameworkCorePersistence<TDbContext>()`, **When** services are registered, **Then** `IDurableInboxStore` resolves to `EntityFrameworkCoreDurableInboxStore<TDbContext>`.
2. **Given** the same setup, **When** services are registered, **Then** `IDurableInboxInspectionStore` resolves through a scoped factory.
3. **Given** an existing `IDurableInboxStore` registration, **When** EF Core persistence setup runs, **Then** the existing registration is not replaced.
4. **Given** the full model-builder extension, **When** the model is built, **Then** the direct inbox mapping is included alongside other Bondstone durable tables.

### Edge Cases

- Direct inbox mapping uses composite identity: module name, message id, and handler identity.
- Module name and handler identity have bounded column lengths.
- Received-at inspection cutoff must use UTC offset.
- Store operations do not call `SaveChanges`; callers own transaction and save boundaries.
- EF Core InMemory tests prove mapping and change tracking only; provider-specific uniqueness and concurrency behavior requires PostgreSQL integration tests.

## Requirements _(mandatory)_

### Functional Requirements

- **FR-001**: The EF Core direct inbox entity MUST map to table `inbox_messages`.
- **FR-002**: The entity key MUST be the composite direct receive identity: module name, message id, and handler identity.
- **FR-003**: The mapping MUST include columns for message id, module name, handler identity, received timestamp, and processed timestamp.
- **FR-004**: The mapping MUST enforce configured maximum lengths for module name and handler identity.
- **FR-005**: The entity MUST convert from and to `DurableInboxRecord` without losing direct inbox identity or timestamps.
- **FR-006**: `ApplyBondstoneInbox(...)` MUST apply only direct inbox mapping and preserve optional schema selection.
- **FR-007**: `ApplyBondstonePersistence(...)` MUST include direct inbox mapping in the full durable persistence model.
- **FR-008**: `EntityFrameworkCoreDurableInboxStore<TDbContext>` MUST implement `IDurableInboxStore`.
- **FR-009**: The store MUST read records by the composite direct inbox key.
- **FR-010**: The store MUST stage new records on the current DbContext without saving changes.
- **FR-011**: The store MUST stage processed timestamp updates for existing records without saving changes.
- **FR-012**: The store MUST throw when marking a missing record processed.
- **FR-013**: `EntityFrameworkCoreDurableInboxInspectionStore<TDbContext>` MUST implement `IDurableInboxInspectionStore`.
- **FR-014**: The inspection store MUST return only rows whose processed timestamp is null.
- **FR-015**: The inspection store MUST support max count, optional UTC received cutoff, and optional module filter.
- **FR-016**: The inspection store MUST order rows deterministically by received timestamp, module name, message id, and handler identity.
- **FR-017**: EF Core persistence service registration MUST add direct inbox store and inspection services without replacing existing store registrations.

### Compatibility And Public API

- **API-001**: Public direct inbox EF Core types include `InboxMessageEntity`, `InboxMessageEntityConfiguration`, `EntityFrameworkCoreDurableInboxStore<TDbContext>`, and `EntityFrameworkCoreDurableInboxInspectionStore<TDbContext>`.
- **API-002**: Public setup APIs include `ApplyBondstoneInbox(...)`, `ApplyBondstonePersistence(...)`, and `AddBondstoneEntityFrameworkCorePersistence<TDbContext>()`.
- **API-003**: This feature belongs to package ID `Bondstone.Persistence.EntityFrameworkCore`, with public namespace `Bondstone.Persistence.EntityFrameworkCore.Inbox`.
- **API-004**: Public/protected changes in this feature are compatibility-sensitive and MUST be reviewed against `tests/Bondstone.PublicApi.Tests`.

### Durable Semantics

- **DS-001**: Generic EF Core direct inbox persistence stores immediate direct receive idempotency markers, not transport-facing durable incoming inbox ledger rows.
- **DS-002**: Generic EF Core store operations participate in the caller's DbContext transaction and MUST NOT save changes independently.
- **DS-003**: Provider-specific duplicate detection, SQL exception classification, locking, and race handling are owned by provider-specific packages.

### Documentation Requirements

- **DOC-001**: Architecture docs MUST identify EF Core as the generic mapping/store package for direct inbox persistence.
- **DOC-002**: Testing docs MUST keep EF Core InMemory direct inbox tests scoped to mapping and change-tracker behavior.
- **DOC-003**: Packaging docs MUST keep EF Core direct inbox mapping in `Bondstone.Persistence.EntityFrameworkCore` and PostgreSQL provider behavior in `Bondstone.Persistence.EntityFrameworkCore.Postgres`.

### Key Entities

- **InboxMessageEntity**: EF Core row representation of a provider-neutral direct inbox record.
- **InboxMessageEntityConfiguration**: EF Core table, key, column, length, and index mapping for direct inbox rows.
- **EntityFrameworkCoreDurableInboxStore**: Generic EF Core implementation of direct inbox get, add, and mark-processed store behavior.
- **EntityFrameworkCoreDurableInboxInspectionStore**: Generic EF Core read-only query surface for unprocessed direct inbox rows.
- **BondstoneModelBuilderExtensions**: Model-builder setup that applies direct inbox mapping individually or as part of full durable persistence.
- **BondstoneEntityFrameworkCoreServiceCollectionExtensions**: DI setup that registers direct inbox EF Core services with the generic persistence package.

## Success Criteria _(mandatory)_

### Measurable Outcomes

- **SC-001**: Unit tests prove direct inbox entity conversion and EF Core mapping metadata.
- **SC-002**: Application tests prove store read/add/mark-processed behavior through EF Core change tracking.
- **SC-003**: Application tests prove inspection filtering, ordering, max count, and cutoff validation.
- **SC-004**: Unit tests prove generic EF Core persistence setup registers direct inbox services without replacing an existing store.
- **SC-005**: PostgreSQL provider tests separately prove database-backed uniqueness, duplicate classification, and provider-specific registrar behavior.

## Assumptions

- The migrated feature describes behavior already implemented before SpecKit adoption.
- EF Core InMemory tests are adequate only for package-local mapping and change-tracker behavior.
- PostgreSQL direct inbox registration and concrete database semantics remain separate provider-specific migration slices.
- Consumer applications own EF migrations generated from these mappings.
- The source of truth for durable persistence ownership is `../../docs/architecture.md`.

## Review Notes

- Source scope: generic EF Core direct inbox entity, mapping, store, inspection store, and shared setup extension entries.
- Test scope: focused EF Core direct inbox tests plus mapping and service-registration assertions in shared EF Core persistence tests.
- Known gaps are listed in `tasks.md`; they are candidates for provider-specific migrations or future specs, not behavior claimed as covered here.
