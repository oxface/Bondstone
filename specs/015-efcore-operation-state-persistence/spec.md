# Feature Specification: EF Core Operation State Persistence

**Feature Branch**: `N/A - migrated existing functionality`

**Created**: 2026-06-23

**Status**: migrated

**Input**: Existing EF Core operation-state implementation in `src/Bondstone.Persistence.EntityFrameworkCore/Operations`, with focused tests in `tests/Bondstone.Persistence.EntityFrameworkCore.Tests/Operations`

## Scope And Boundaries _(mandatory)_

**Bondstone Capability**: Generic EF Core mapping and store behavior for durable operation state, including module-scoped store wrappers and operation expiration candidate queries.

**Affected Packages/Areas**:

- `src/Bondstone.Persistence.EntityFrameworkCore/Operations`
- `src/Bondstone.Persistence.EntityFrameworkCore/Persistence/BondstoneModelBuilderExtensions.cs`
- `src/Bondstone.Persistence.EntityFrameworkCore/Persistence/BondstoneEntityFrameworkCoreServiceCollectionExtensions.cs`
- `tests/Bondstone.Persistence.EntityFrameworkCore.Tests/Operations`
- `tests/Bondstone.Persistence.EntityFrameworkCore.Tests/Persistence/BondstoneModelBuilderExtensionsTests.cs`
- `tests/Bondstone.Persistence.EntityFrameworkCore.Tests/Persistence/BondstoneEntityFrameworkCoreServiceCollectionExtensionsTests.cs`
- `docs/architecture.md`
- `docs/testing.md`

**Out Of Scope**:

- Provider-neutral operation observation contracts and runtime readers/finalizers.
- PostgreSQL provider-specific schema, transaction, and integration behavior.
- Durable command send/receive runtime except as a consumer of operation-state stores.

## User Scenarios & Testing _(mandatory)_

### User Story 1 - Map Operation State Rows (Priority: P1)

As a persistence provider user, I want EF Core operation-state mapping so accepted-work state can persist in the application DbContext.

**Independent Test**: `dotnet test tests/Bondstone.Persistence.EntityFrameworkCore.Tests/Bondstone.Persistence.EntityFrameworkCore.Tests.csproj --configuration Release --filter "Category=Unit|Category=Application"`.

**Acceptance Scenarios**:

1. **Given** a `DurableOperationState`, **When** it is mapped to an EF entity and back, **Then** status, timestamps, result payload, failure reason, and diagnostics are preserved.
2. **Given** `ApplyBondstoneOperationState(...)`, **When** the model is built, **Then** only the operation-state mapping is added.
3. **Given** `ApplyBondstonePersistence(...)`, **When** the model is built, **Then** operation-state mapping is included with other Bondstone durable tables.

### User Story 2 - Store And Update Operation State (Priority: P2)

As operation observation runtime, I want an EF Core operation-state store so operation state can be read, inserted, and updated within the caller's DbContext transaction.

**Acceptance Scenarios**:

1. **Given** no operation state exists, **When** the store reads by id, **Then** it returns null.
2. **Given** new operation state, **When** the store saves it, **Then** the row is staged and persists after `SaveChanges`.
3. **Given** existing operation state, **When** the store saves a new state for the same id, **Then** the existing row is updated.

### User Story 3 - Find Expiration Candidates (Priority: P3)

As operation expiry policy, I want EF Core candidate lookup so pending/running stale operation states can be finalized by provider-neutral expiry processing.

**Acceptance Scenarios**:

1. **Given** pending, running, completed, failed, and fresh rows, **When** expiration candidates are queried by cutoff, **Then** only stale pending/running rows are returned.
2. **Given** max count, **When** candidates are queried, **Then** at most that many rows are returned in deterministic updated-time order.

## Requirements _(mandatory)_

- **FR-001**: Operation state MUST map to table `operation_states`.
- **FR-002**: Operation id MUST be the primary key.
- **FR-003**: Status, updated timestamp, result payload, failure reason, module name, message type name, and handler identity MUST be persisted.
- **FR-004**: Store saves MUST insert new rows and update existing rows without calling `SaveChanges`.
- **FR-005**: Store reads MUST map EF rows back to provider-neutral `DurableOperationState`.
- **FR-006**: Module store wrapper MUST constrain reads and writes to the configured module identity.
- **FR-007**: Expiration lookup MUST return non-terminal pending/running states updated at or before a UTC cutoff.
- **FR-008**: EF Core setup MUST register `IDurableOperationStateStore` and apply operation-state mapping.

## Success Criteria _(mandatory)_

- **SC-001**: Unit tests prove entity mapping and model metadata.
- **SC-002**: Application tests prove store save/read/update behavior.
- **SC-003**: Application tests prove expiration candidate filtering and ordering.

## Assumptions

- Consumer applications generate and apply EF migrations.
- PostgreSQL-specific schema and transaction behavior is migrated separately.

## Review Notes

- Source/test scope: 513 lines across EF Core operation-state source and focused tests, plus shared setup/mapping assertions.
