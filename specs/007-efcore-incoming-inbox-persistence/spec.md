# Feature Specification: EF Core Incoming Inbox Persistence

**Feature Branch**: `N/A - migrated existing functionality`

**Created**: 2026-06-23

**Status**: migrated

**Input**: Existing EF Core incoming inbox implementation in `src/Bondstone.Persistence.EntityFrameworkCore`, with focused tests in `tests/Bondstone.Persistence.EntityFrameworkCore.Tests`

## Scope And Boundaries _(mandatory)_

**Bondstone Capability**: Generic EF Core mapping, ingestion, inspection, and setup wiring for durable incoming inbox rows

**Affected Packages/Areas**:

- `src/Bondstone.Persistence.EntityFrameworkCore/IncomingInbox`
- `src/Bondstone.Persistence.EntityFrameworkCore/Persistence/BondstoneModelBuilderExtensions.cs`
- `src/Bondstone.Persistence.EntityFrameworkCore/Persistence/BondstoneEntityFrameworkCoreServiceCollectionExtensions.cs`
- `src/Bondstone.Persistence.EntityFrameworkCore/Persistence/BondstoneEntityFrameworkCoreModuleBuilderExtensions.cs`
- `tests/Bondstone.Persistence.EntityFrameworkCore.Tests/IncomingInbox`
- `tests/Bondstone.Persistence.EntityFrameworkCore.Tests/Persistence`
- `docs/architecture.md`
- `docs/package-discovery.md`
- `docs/operations.md`
- `docs/packaging.md`
- `docs/testing.md`

**Out Of Scope**:

- Provider-neutral incoming inbox keys, records, processing dispatcher, failure policy, and diagnostics.
- PostgreSQL-specific claim, lease renewal, retry scheduling, terminal failure mutation, locking, SQL, and concurrency semantics.
- Application-owned EF migrations, migration history, schema rollout, and rollback planning.
- Hosted incoming inbox worker scheduling.
- Transport-specific native receive and settlement behavior.
- Direct durable inbox idempotency marker storage.

## User Scenarios & Testing _(mandatory)_

### User Story 1 - Map Durable Incoming Inbox Rows Into EF Core (Priority: P1)

As an EF-backed module owner, I want durable incoming inbox rows mapped into my
application `DbContext` so transport ingestion and operator inspection use a
stable relational table shape.

**Why this priority**: Without the EF entity and model mapping, no EF-backed
module can persist durable incoming inbox records.

**Independent Test**: Run `dotnet test tests/Bondstone.Persistence.EntityFrameworkCore.Tests/Bondstone.Persistence.EntityFrameworkCore.Tests.csproj --configuration Release --filter "Category=Unit|Category=Application"` after build.

**Acceptance Scenarios**:

1. **Given** a durable incoming inbox record for a command, **When** `IncomingInboxMessageEntity.FromRecord(...)` maps it, **Then** message envelope, receiver identity, source transport, ingested timestamp, and state fields are copied into the EF entity.
2. **Given** an incoming inbox EF entity, **When** `ToRecord()` maps it back, **Then** the durable incoming inbox record round-trips.
3. **Given** an event subscriber record, **When** it is mapped to and from the EF entity, **Then** receiver module and handler identity remain the durable subscriber identity.
4. **Given** `ApplyBondstoneIncomingInbox("bondstone")`, **When** EF builds the model, **Then** the incoming inbox entity maps to the configured schema and `incoming_inbox_messages` table.
5. **Given** `ApplyBondstonePersistence(...)`, **When** EF builds the model, **Then** the incoming inbox entity is included with the rest of the Bondstone durable persistence mappings.

---

### User Story 2 - Ingest Incoming Inbox Records Idempotently With EF Core (Priority: P2)

As a transport ingestion boundary, I want an EF Core incoming inbox ingestion
store so native deliveries can be inserted durably before native settlement and
duplicate deliveries can return the existing durable row.

**Why this priority**: This is the generic EF store behind
`DurableIncomingInboxIngestionBoundary`; transport adapters and custom receive
loops rely on it before acknowledging native messages.

**Independent Test**: Run the EF Core persistence tests after build.

**Acceptance Scenarios**:

1. **Given** a pending incoming inbox record that is not already tracked or persisted, **When** `IngestAsync(...)` runs, **Then** an `IncomingInboxMessageEntity` is added to the `DbContext` and the result status is `Ingested`.
2. **Given** an incoming inbox record already persisted with the same receiver module, message id, and handler identity, **When** a duplicate record is ingested, **Then** the result status is `AlreadyIngested` and the existing record is returned.
3. **Given** an incoming inbox record already tracked before `SaveChanges`, **When** the same record is ingested again, **Then** the result status is `AlreadyIngested` and no duplicate entity is tracked.
4. **Given** a new record whose state is not `Pending`, **When** ingestion is attempted, **Then** ingestion fails before adding it to the context.
5. **Given** a `DbContext` missing the incoming inbox mapping, **When** ingestion is attempted, **Then** a Bondstone setup error identifies the missing `ApplyBondstoneIncomingInbox()` mapping.

---

### User Story 3 - Inspect Incoming Inbox Rows With EF Core (Priority: P3)

As an operator-facing tool or application maintenance path, I want generic EF
Core incoming inbox inspection queries so pending, failed, and stale processing
rows can be listed by status, receiver module, source transport, and cutoff
time.

**Why this priority**: Inspection is the read-only visibility surface for
durable receive rows independent of provider-specific mutation behavior.

**Independent Test**: Run the EF Core persistence tests after build.

**Acceptance Scenarios**:

1. **Given** incoming inbox rows with different statuses, receiver modules, source transports, and ingested timestamps, **When** `FindAsync(...)` runs with filters, **Then** matching rows are returned in deterministic ingested-time order up to `maxCount`.
2. **Given** processing rows with expired and unexpired claim leases, **When** `FindStaleProcessingAsync(...)` runs with a UTC cutoff, **Then** only expired processing claims matching the optional filters are returned in deterministic claim-expiration order.
3. **Given** terminal failed rows, retry scheduled rows, and other statuses, **When** `FindTerminalFailedAsync(...)` runs with filters, **Then** only matching terminal failures are returned in deterministic failure-time order.
4. **Given** non-UTC cutoff timestamps, **When** inspection methods are called, **Then** they reject the argument.
5. **Given** a non-positive `maxCount`, **When** inspection methods are called, **Then** they reject the argument.
6. **Given** a `DbContext` missing the incoming inbox mapping, **When** inspection is attempted, **Then** a Bondstone setup error identifies the missing `ApplyBondstoneIncomingInbox()` mapping.

---

### User Story 4 - Register EF Core Incoming Inbox Services And Module Boundaries (Priority: P4)

As an application host configuring EF-backed modules, I want the EF Core setup
helpers to register incoming inbox stores and module-specific ingestion
boundaries so each receiving module writes to the correct module `DbContext`.

**Why this priority**: Correct setup prevents cross-module ingestion into the
wrong persistence boundary and keeps EF-backed durable receive behavior
composable with the rest of Bondstone.

**Independent Test**: Run EF Core persistence setup and transaction behavior tests after build.

**Acceptance Scenarios**:

1. **Given** `AddBondstoneEntityFrameworkCorePersistence<TDbContext>()`, **When** services are inspected, **Then** incoming inbox ingestion store, ingestion persistence scope, and inspection store registrations are present.
2. **Given** an existing service registration, **When** EF Core persistence setup runs, **Then** setup uses `TryAdd` behavior and does not replace existing registrations.
3. **Given** two modules with different EF Core `DbContext` types, **When** an incoming inbox ingestion boundary is resolved for one receiver module, **Then** the record is written to that receiver module's `DbContext`.
4. **Given** `UseEntityFrameworkCoreModulePersistence<TDbContext>()`, **When** the module is configured, **Then** it registers an EF Core incoming inbox ingestion boundary for that module.

### Edge Cases

- Incoming inbox table identity is receiver module, message id, and handler identity.
- Message kind and incoming inbox status are stored through string conversions.
- Source transport name is optional and length-limited.
- Trace context, causation id, partition key, metadata, operation id, and payload are preserved by entity conversion.
- Inspection filters normalize non-null receiver module and source transport values.
- Inspection cutoffs must be UTC and non-default.
- Ingestion and inspection fail fast when the EF incoming inbox entity is not mapped.
- EF InMemory tests are package-local checks only; they are not proof of relational uniqueness, SQL shape, locking, or concurrency behavior.

## Requirements _(mandatory)_

### Functional Requirements

- **FR-001**: EF Core MUST provide an `IncomingInboxMessageEntity` that maps durable envelope fields, receive identity fields, source transport, ingested timestamp, status, attempt count, retry, processed, failed, failure reason, and claim fields.
- **FR-002**: `IncomingInboxMessageEntity.FromRecord(...)` MUST convert a provider-neutral `DurableIncomingInboxRecord` into an EF entity.
- **FR-003**: `IncomingInboxMessageEntity.ToRecord()` MUST convert an EF entity back into a provider-neutral `DurableIncomingInboxRecord`.
- **FR-004**: EF Core mapping MUST store incoming inbox rows in `incoming_inbox_messages`.
- **FR-005**: EF Core mapping MUST use receiver module, message id, and handler identity as the primary key.
- **FR-006**: EF Core mapping MUST configure required fields, length limits, string enum conversions, and indexes used by processing and inspection queries.
- **FR-007**: `ApplyBondstoneIncomingInbox(...)` MUST apply only incoming inbox mapping.
- **FR-008**: `ApplyBondstonePersistence(...)` MUST include incoming inbox mapping with outbox, direct inbox, and operation-state mappings.
- **FR-009**: EF Core ingestion store MUST validate that incoming inbox mapping exists on the `DbContext`.
- **FR-010**: EF Core ingestion store MUST return `AlreadyIngested` with the existing record when a row with the same receive identity exists.
- **FR-011**: EF Core ingestion store MUST add new pending records to the `DbContext` and return `Ingested`.
- **FR-012**: EF Core ingestion store MUST reject new records that do not start in pending state.
- **FR-013**: EF Core ingestion persistence scope MUST delegate execution and save changes to the shared EF Core persistence scope.
- **FR-014**: EF Core inspection store MUST support `FindAsync(...)` by status, max count, ingested cutoff, receiver module, and source transport.
- **FR-015**: EF Core inspection store MUST support stale processing inspection by claim-expiration cutoff, max count, receiver module, and source transport.
- **FR-016**: EF Core inspection store MUST support terminal failure inspection by failed cutoff, max count, receiver module, and source transport.
- **FR-017**: EF Core inspection store MUST use no-tracking queries and return provider-neutral records.
- **FR-018**: EF Core inspection store MUST validate positive `maxCount` and UTC cutoff timestamps.
- **FR-019**: EF Core service setup MUST register incoming inbox ingestion store, ingestion persistence scope, and inspection store.
- **FR-020**: EF Core module setup MUST register module-specific incoming inbox ingestion boundaries.

### Compatibility And Public API

- **API-001**: Public setup API includes `ApplyBondstoneIncomingInbox(...)`, `ApplyBondstonePersistence(...)`, and `AddBondstoneEntityFrameworkCorePersistence<TDbContext>()`.
- **API-002**: Public EF Core incoming inbox types include `IncomingInboxMessageEntity`, `IncomingInboxMessageEntityConfiguration`, `EntityFrameworkCoreDurableIncomingInboxIngestionStore<TDbContext>`, and `EntityFrameworkCoreDurableIncomingInboxInspectionStore<TDbContext>`.
- **API-003**: Internal EF Core incoming inbox type `EntityFrameworkCoreDurableIncomingInboxIngestionPersistenceScope` is package implementation detail.
- **API-004**: This feature belongs to package ID `Bondstone.Persistence.EntityFrameworkCore` and namespace `Bondstone.Persistence.EntityFrameworkCore.IncomingInbox`.
- **API-005**: Public/protected changes in this feature are compatibility-sensitive and MUST be reviewed against `tests/Bondstone.PublicApi.Tests`.

### Durable Semantics

- **DS-001**: EF Core incoming inbox ingestion must happen before native transport settlement through the provider-neutral ingestion boundary.
- **DS-002**: EF Core incoming inbox ingestion must be idempotent by receiver module, message id, and handler identity.
- **DS-003**: Generic EF Core incoming inbox storage must not claim, renew leases, schedule retries, mark processed, or mark terminal failures; provider-specific storage owns those mutations.
- **DS-004**: Bondstone provides EF mappings and stores, while applications own EF migrations, schema rollout, and rollback planning.

### Documentation Requirements

- **DOC-001**: Package discovery docs MUST identify `Bondstone.Persistence.EntityFrameworkCore` as the generic EF Core mapping/store package for durable incoming inbox rows.
- **DOC-002**: Operations docs MUST distinguish generic EF inspection from provider-specific mutation and recovery behavior.
- **DOC-003**: Packaging docs MUST remind consumers that applications own migrations for mapped Bondstone EF tables.
- **DOC-004**: Testing docs MUST classify EF InMemory tests as mapping/change-tracker checks rather than relational durability proof.

### Key Entities

- **IncomingInboxMessageEntity**: EF Core entity for durable incoming inbox rows.
- **IncomingInboxMessageEntityConfiguration**: EF Core mapping for incoming inbox table, key, columns, conversions, length limits, and indexes.
- **EntityFrameworkCoreDurableIncomingInboxIngestionStore**: Generic EF Core ingestion store implementing idempotent pending-row insertion.
- **EntityFrameworkCoreDurableIncomingInboxInspectionStore**: Generic EF Core read-only inspection store for status, stale processing, and terminal failed rows.
- **EntityFrameworkCoreDurableIncomingInboxIngestionPersistenceScope**: Adapter from provider-neutral ingestion scope to the shared EF Core persistence scope.

## Success Criteria _(mandatory)_

### Measurable Outcomes

- **SC-001**: Unit tests prove EF incoming inbox entity conversion round-trips command and event records.
- **SC-002**: Unit tests prove model builder setup maps the incoming inbox table, primary key, length limits, and indexes.
- **SC-003**: Application tests prove EF ingestion stages new rows, returns already-ingested rows, rejects non-pending rows, and fails clearly when mapping is missing.
- **SC-004**: Application tests prove EF inspection filters, ordering, stale processing queries, terminal failure queries, UTC validation, and missing mapping errors.
- **SC-005**: Setup tests prove incoming inbox EF services and module-specific ingestion boundaries are registered.

## Assumptions

- The migrated feature describes behavior already implemented before SpecKit adoption.
- Generic EF Core tests use EF InMemory for fast mapping and change-tracker behavior only.
- PostgreSQL-specific relational semantics are implemented in `Bondstone.Persistence.EntityFrameworkCore.Postgres` and should be migrated separately.
- Provider-neutral incoming inbox contracts are documented by `specs/006-durable-incoming-inbox-persistence`.
- The source of truth for durable persistence ownership is `../../docs/architecture.md`.

## Review Notes

- Source scope: EF Core incoming inbox entity, mapping, ingestion store, inspection store, model-builder setup, service setup, and module-specific ingestion boundary setup.
- Test scope: focused incoming inbox EF Core tests plus setup tests that mention incoming inbox mapping/service/boundary behavior.
- Known gaps are listed in `tasks.md`; they are candidates for future specs or provider-specific migrations, not behavior claimed as fully covered here.
