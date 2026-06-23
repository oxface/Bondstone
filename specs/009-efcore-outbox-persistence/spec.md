# Feature Specification: EF Core Outbox Persistence

**Feature Branch**: `N/A - migrated existing functionality`

**Created**: 2026-06-23

**Status**: migrated

**Input**: Existing EF Core outbox implementation in `src/Bondstone.Persistence.EntityFrameworkCore`, with focused tests in `tests/Bondstone.Persistence.EntityFrameworkCore.Tests`

## Scope And Boundaries _(mandatory)_

**Bondstone Capability**: Generic EF Core mapping, writing, inspection, setup, and mapping diagnostics for durable outbox rows

**Affected Packages/Areas**:

- `src/Bondstone.Persistence.EntityFrameworkCore/Outbox`
- `src/Bondstone.Persistence.EntityFrameworkCore/Persistence/BondstoneModelBuilderExtensions.cs`
- `src/Bondstone.Persistence.EntityFrameworkCore/Persistence/BondstoneEntityFrameworkCoreServiceCollectionExtensions.cs`
- `src/Bondstone.Persistence.EntityFrameworkCore/Persistence/BondstoneEntityFrameworkCoreModuleBuilderExtensions.cs`
- `src/Bondstone.Persistence.EntityFrameworkCore/Persistence/EntityFrameworkCoreModuleTransactionRunner.cs`
- `tests/Bondstone.Persistence.EntityFrameworkCore.Tests/Outbox`
- `tests/Bondstone.Persistence.EntityFrameworkCore.Tests/Persistence`
- `docs/architecture.md`
- `docs/package-discovery.md`
- `docs/operations.md`
- `docs/packaging.md`
- `docs/testing.md`

**Out Of Scope**:

- Provider-neutral durable outbox records, dispatch state, failure policy, dispatch route, and dispatcher algorithm.
- PostgreSQL-specific claiming, lease renewal, dispatch outcome mutation, locking, SQL, and concurrency semantics.
- Application-owned EF migrations, migration history, schema rollout, and rollback planning.
- Hosted outbox worker scheduling.
- Transport-specific envelope dispatch.
- Cleanup, purge, replay, retention, and operator repair flows.

## User Scenarios & Testing _(mandatory)_

### User Story 1 - Map Durable Outbox Rows Into EF Core (Priority: P1)

As an EF-backed module owner, I want durable outbox rows mapped into my
application `DbContext` so outgoing durable command and integration-event
envelopes can be staged in the same persistence boundary as source-module
state.

**Why this priority**: The EF entity and mapping are the storage shape for all
generic EF-backed outbox writes and downstream provider dispatch.

**Independent Test**: Run `dotnet test tests/Bondstone.Persistence.EntityFrameworkCore.Tests/Bondstone.Persistence.EntityFrameworkCore.Tests.csproj --configuration Release --filter "Category=Unit|Category=Application"` after build.

**Acceptance Scenarios**:

1. **Given** a durable outbox record for a command, **When** `OutboxMessageEntity.FromRecord(...)` maps it, **Then** message envelope, trace, stored timestamp, status, retry, failure, and claim fields are copied into the EF entity.
2. **Given** an outbox EF entity, **When** `ToRecord()` maps it back, **Then** the durable outbox record round-trips.
3. **Given** an integration-event outbox record without a target module, **When** it is mapped to and from the EF entity, **Then** the missing target module is preserved.
4. **Given** `ApplyBondstoneOutbox("bondstone")`, **When** EF builds the model, **Then** the outbox entity maps to the configured schema and `outbox_messages` table.
5. **Given** `ApplyBondstonePersistence(...)`, **When** EF builds the model, **Then** the outbox entity is included with the rest of the Bondstone durable persistence mappings.

---

### User Story 2 - Stage Outbox Messages With EF Core (Priority: P2)

As a durable sender running inside a module transaction, I want an EF Core
outbox writer so outgoing durable envelopes are staged in the current
`DbContext` and committed with source-module state.

**Why this priority**: Atomic source-state plus outbox staging is the core
durable send/publish guarantee.

**Independent Test**: Run the EF Core persistence tests after build.

**Acceptance Scenarios**:

1. **Given** a valid durable envelope and EF Core outbox writer, **When** `WriteAsync(...)` runs, **Then** an `OutboxMessageEntity` is added to the `DbContext` change tracker.
2. **Given** a configured `TimeProvider`, **When** the writer stages a row, **Then** `StoredAtUtc` uses the provider's UTC time.
3. **Given** a newly staged row, **When** it is inspected before provider dispatch, **Then** status is `Pending`.
4. **Given** `SaveChangesAsync` is called by the transaction boundary, **When** the row is persisted, **Then** it can be found by message id.
5. **Given** a module-specific EF Core outbox writer, **When** it is constructed, **Then** module name is normalized and required while write behavior delegates to the generic writer.

---

### User Story 3 - Inspect Terminal Outbox Failures With EF Core (Priority: P3)

As an operator-facing tool or application maintenance path, I want generic EF
Core outbox inspection so terminal dispatch failures can be listed by source
module and cutoff time.

**Why this priority**: Inspection is the read-only visibility surface for
terminal outbox rows independent of provider-specific dispatch mutation.

**Independent Test**: Run the EF Core persistence tests after build.

**Acceptance Scenarios**:

1. **Given** outbox rows with different statuses, source modules, and failure timestamps, **When** `FindTerminalFailedAsync(...)` runs with filters, **Then** matching terminal rows are returned in deterministic failure-time order up to `maxCount`.
2. **Given** a terminal row without `FailedAtUtc`, **When** inspection orders and filters by cutoff, **Then** `StoredAtUtc` is used as fallback evidence.
3. **Given** non-UTC cutoff timestamps, **When** inspection is called, **Then** it rejects the argument.
4. **Given** a non-positive `maxCount`, **When** inspection is called, **Then** it rejects the argument.
5. **Given** a source module filter, **When** inspection runs, **Then** the filter is normalized before querying.

---

### User Story 4 - Register EF Core Outbox Services And Mapping Diagnostics (Priority: P4)

As an application host configuring EF-backed modules, I want EF Core setup
helpers to register outbox services and fail clearly when durable messaging
uses a `DbContext` that lacks required outbox mapping.

**Why this priority**: Normal setup must provide the writer/inspection store
and protect consumers from silent durable messaging misconfiguration.

**Independent Test**: Run EF Core persistence setup and transaction behavior tests after build.

**Acceptance Scenarios**:

1. **Given** `AddBondstoneEntityFrameworkCorePersistence<TDbContext>()`, **When** services are inspected, **Then** EF Core outbox writer and inspection store registrations are present.
2. **Given** an existing service registration, **When** EF Core persistence setup runs, **Then** setup uses `TryAdd` behavior and does not replace existing registrations.
3. **Given** `UseEntityFrameworkCoreModulePersistence<TDbContext>()`, **When** a module is configured, **Then** the module is registered with EF Core persistence and a module outbox writer can be resolved through module runtime registration.
4. **Given** a durable messaging module whose `DbContext` lacks outbox mapping, **When** module command execution starts, **Then** a Bondstone setup error names the missing outbox mapping and mentions `ApplyBondstoneOutbox()` or `ApplyBondstonePersistence()`.
5. **Given** a durable messaging module whose `DbContext` maps outbox and direct inbox, **When** module command execution runs, **Then** execution is allowed.

### Edge Cases

- Outbox table identity is message id.
- Message kind and outbox status are stored through string conversions.
- Trace context, causation id, partition key, metadata, operation id, and payload are preserved by entity conversion.
- `TargetModule` may be null for integration events.
- Terminal inspection uses `FailedAtUtc` first and falls back to `StoredAtUtc`.
- EF InMemory tests are package-local checks only; they are not proof of relational uniqueness, SQL shape, locking, or concurrency behavior.

## Requirements _(mandatory)_

### Functional Requirements

- **FR-001**: EF Core MUST provide an `OutboxMessageEntity` that maps durable envelope fields, stored timestamp, status, attempt count, retry, dispatched, failed, failure reason, and claim fields.
- **FR-002**: `OutboxMessageEntity.FromRecord(...)` MUST convert a provider-neutral `DurableOutboxRecord` into an EF entity.
- **FR-003**: `OutboxMessageEntity.ToRecord()` MUST convert an EF entity back into a provider-neutral `DurableOutboxRecord`.
- **FR-004**: EF Core mapping MUST store outbox rows in `outbox_messages`.
- **FR-005**: EF Core mapping MUST use message id as the primary key.
- **FR-006**: EF Core mapping MUST configure required fields, length limits, string enum conversions, and indexes used by dispatch and inspection queries.
- **FR-007**: `ApplyBondstoneOutbox(...)` MUST apply only outbox mapping.
- **FR-008**: `ApplyBondstonePersistence(...)` MUST include outbox mapping with direct inbox, incoming inbox, and operation-state mappings.
- **FR-009**: EF Core outbox writer MUST add new pending outbox entities to the current `DbContext`.
- **FR-010**: EF Core outbox writer MUST use `TimeProvider.GetUtcNow()` for `StoredAtUtc`.
- **FR-011**: EF Core module outbox writer MUST normalize and expose module name while delegating write behavior to the generic writer.
- **FR-012**: EF Core outbox inspection store MUST support terminal failure inspection by max count, failed cutoff, and source module.
- **FR-013**: EF Core outbox inspection store MUST use no-tracking queries and return provider-neutral records.
- **FR-014**: EF Core outbox inspection store MUST validate positive `maxCount` and UTC cutoff timestamps.
- **FR-015**: EF Core service setup MUST register outbox writer and outbox inspection store.
- **FR-016**: EF Core module transaction runner MUST fail durable messaging execution when required outbox mapping is missing.

### Compatibility And Public API

- **API-001**: Public setup API includes `ApplyBondstoneOutbox(...)`, `ApplyBondstonePersistence(...)`, `AddBondstoneEntityFrameworkCorePersistence<TDbContext>()`, and `UseEntityFrameworkCorePersistence<TDbContext>()`.
- **API-002**: Public EF Core outbox types include `OutboxMessageEntity`, `OutboxMessageEntityConfiguration`, `EntityFrameworkCoreDurableOutboxWriter<TDbContext>`, `EntityFrameworkCoreModuleDurableOutboxWriter<TDbContext>`, and `EntityFrameworkCoreDurableOutboxInspectionStore<TDbContext>`.
- **API-003**: This feature belongs to package ID `Bondstone.Persistence.EntityFrameworkCore` and namespace `Bondstone.Persistence.EntityFrameworkCore.Outbox`.
- **API-004**: Public/protected changes in this feature are compatibility-sensitive and MUST be reviewed against `tests/Bondstone.PublicApi.Tests`.

### Durable Semantics

- **DS-001**: Source-module state and outgoing outbox envelopes must commit atomically in the EF Core module transaction boundary.
- **DS-002**: Generic EF Core outbox storage must not claim, renew leases, record dispatch outcomes, or own PostgreSQL-specific concurrency semantics.
- **DS-003**: Bondstone provides EF mappings and stores, while applications own EF migrations, schema rollout, and rollback planning.

### Documentation Requirements

- **DOC-001**: Package discovery docs MUST identify `Bondstone.Persistence.EntityFrameworkCore` as the generic EF Core mapping/store package for durable outbox rows.
- **DOC-002**: Operations docs MUST distinguish generic EF terminal failure inspection from provider-specific dispatch mutation and recovery behavior.
- **DOC-003**: Packaging docs MUST remind consumers that applications own migrations for mapped Bondstone EF tables.
- **DOC-004**: Testing docs MUST classify EF InMemory tests as mapping/change-tracker checks rather than relational durability proof.

### Key Entities

- **OutboxMessageEntity**: EF Core entity for durable outbox rows.
- **OutboxMessageEntityConfiguration**: EF Core mapping for outbox table, key, columns, conversions, length limits, and indexes.
- **EntityFrameworkCoreDurableOutboxWriter**: Generic EF Core writer that stages pending outbox rows.
- **EntityFrameworkCoreModuleDurableOutboxWriter**: Module-named wrapper for EF Core outbox writes.
- **EntityFrameworkCoreDurableOutboxInspectionStore**: Generic EF Core read-only inspection store for terminal outbox failures.
- **EntityFrameworkCoreModuleTransactionRunner**: EF Core transaction behavior that validates durable messaging mappings before module execution.

## Success Criteria _(mandatory)_

### Measurable Outcomes

- **SC-001**: Unit tests prove EF outbox entity conversion round-trips command and event records.
- **SC-002**: Unit tests prove model builder setup maps the outbox table, primary key, length limits, and indexes.
- **SC-003**: Application tests prove EF outbox writer stages and persists pending rows with the configured clock.
- **SC-004**: Application tests prove EF outbox inspection filters, ordering, max count, source module, and UTC validation.
- **SC-005**: Setup and transaction tests prove outbox services are registered and missing mapping diagnostics are emitted.

## Assumptions

- The migrated feature describes behavior already implemented before SpecKit adoption.
- Generic EF Core tests use EF InMemory for fast mapping and change-tracker behavior only.
- PostgreSQL-specific outbox dispatch mutation is implemented in `Bondstone.Persistence.EntityFrameworkCore.Postgres` and should be migrated separately.
- Provider-neutral durable outbox contracts are documented by `specs/005-durable-outbox-persistence`.
- The source of truth for durable persistence ownership is `../../docs/architecture.md`.

## Review Notes

- Source scope: EF Core outbox entity, mapping, writer, module writer, inspection store, model-builder setup, service setup, module setup, and mapping diagnostics.
- Test scope: focused outbox EF Core tests plus setup/transaction tests that mention outbox mapping or service behavior.
- Known gaps are listed in `tasks.md`; they are candidates for future specs or provider-specific migrations, not behavior claimed as fully covered here.
