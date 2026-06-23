# Feature Specification: PostgreSQL Direct Inbox Persistence

**Feature Branch**: `N/A - migrated existing functionality`

**Created**: 2026-06-23

**Status**: migrated

**Input**: Existing PostgreSQL-specific direct inbox implementation in `src/Bondstone.Persistence.EntityFrameworkCore.Postgres`, with Testcontainers-backed integration tests in `tests/Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests`

## Scope And Boundaries _(mandatory)_

**Bondstone Capability**: PostgreSQL-backed direct inbox registration, duplicate classification, schema-aware SQL, module handler-executor registration, and production provider setup over EF Core mappings

**Affected Packages/Areas**:

- `src/Bondstone.Persistence.EntityFrameworkCore.Postgres/Inbox`
- `src/Bondstone.Persistence.EntityFrameworkCore.Postgres/Persistence/BondstonePostgreSqlServiceCollectionExtensions.cs`
- `src/Bondstone.Persistence.EntityFrameworkCore.Postgres/Persistence/BondstonePostgreSqlBuilderExtensions.cs`
- `src/Bondstone.Persistence.EntityFrameworkCore.Postgres/Persistence/PostgreSqlPersistenceExceptionClassifier.cs`
- `src/Bondstone.Persistence.EntityFrameworkCore.Postgres/Persistence/PostgreSqlTableIdentifier.cs`
- `tests/Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests/Persistence/PostgreSqlPersistenceInboxTests.cs`
- `tests/Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests/Persistence/PostgreSqlPersistenceSchemaTests.cs`
- `tests/Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests/Persistence/PostgreSqlPersistenceRegistrationTests.cs`
- `tests/Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests/Persistence/BondstonePostgreSqlServiceCollectionExtensionsTests.cs`
- `tests/Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests/Persistence/PostgreSqlSingleRootFallbackTests.cs`
- `tests/Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests/Persistence/PostgreSqlPersistenceExceptionClassifierTests.cs`
- `tests/Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests/PostgreSqlTestDbContext.cs`
- `tests/Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests/PostgreSqlSchemaTestDbContext.cs`
- `docs/architecture.md`
- `docs/testing.md`
- `docs/packaging.md`

**Out Of Scope**:

- Provider-neutral direct inbox contracts, records, handler executor, inspector, and module resolver.
- Generic EF Core direct inbox entity mapping, store, inspection store, and model-builder setup.
- PostgreSQL outbox claiming, lease renewal, dispatch recording, and module outbox dispatcher behavior.
- PostgreSQL durable incoming inbox claim, lease, outcome, and terminal failure mutation behavior.
- Transport adapters, broker topology, native settlement, and hosted durable inbox workers.
- Consumer-owned EF migrations and schema rollout.
- Cleanup, purge, replay, retention, and operator repair automation.

## User Scenarios & Testing _(mandatory)_

### User Story 1 - Atomically Register Direct Inbox Receives In PostgreSQL (Priority: P1)

As a module receive pipeline, I want PostgreSQL direct inbox registration to insert or identify an existing receive marker without aborting the surrounding transaction so duplicate receives can be skipped safely.

**Why this priority**: Direct handler idempotency depends on atomic registration semantics that remain usable inside PostgreSQL transactions.

**Independent Test**: Run `dotnet test tests/Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests/Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests.csproj --configuration Release --filter "Category=Integration"` with Docker/Testcontainers available.

**Acceptance Scenarios**:

1. **Given** no matching direct inbox row, **When** the PostgreSQL registrar registers a record, **Then** it inserts the row and returns `Registered`.
2. **Given** a matching unprocessed row, **When** the registrar registers the same direct receive identity, **Then** it returns `AlreadyReceived` and the existing row.
3. **Given** a matching processed row, **When** the registrar registers the same direct receive identity, **Then** it returns `AlreadyProcessed` and the existing processed row.
4. **Given** duplicate registration inside a transaction, **When** the registrar returns a duplicate result, **Then** the transaction remains usable and can commit other durable work.

---

### User Story 2 - Classify PostgreSQL Direct Inbox Duplicates (Priority: P2)

As provider code and tests, I want PostgreSQL unique-violation classification for direct inbox rows so duplicate store failures can be recognized by the direct inbox primary key.

**Why this priority**: Generic EF Core store failures are provider exceptions; PostgreSQL tests and advanced callers need provider-specific duplicate recognition.

**Independent Test**: Run PostgreSQL unit and integration tests after build.

**Acceptance Scenarios**:

1. **Given** a nested PostgreSQL unique-violation exception, **When** classification runs without a constraint filter, **Then** it returns true.
2. **Given** a nested PostgreSQL unique-violation exception for `PK_inbox_messages`, **When** direct inbox duplicate classification runs, **Then** it returns true.
3. **Given** a unique violation for another constraint, **When** direct inbox duplicate classification runs, **Then** it returns false.
4. **Given** the generic EF Core direct inbox store saves a duplicate row, **When** PostgreSQL throws, **Then** the exception is classified as a unique violation and direct inbox duplicate.

---

### User Story 3 - Use Schema-Aware Direct Inbox SQL And Provider Setup (Priority: P3)

As an application composer, I want PostgreSQL persistence setup to register direct inbox registrar/executor services and respect optional schema names so root and module-scoped direct inbox persistence use the intended table.

**Why this priority**: Production setups may use schema-qualified durable tables, and module-specific persistence must not accidentally use root registrations.

**Independent Test**: Run PostgreSQL setup unit tests and schema integration tests.

**Acceptance Scenarios**:

1. **Given** root PostgreSQL persistence setup, **When** services are registered, **Then** direct inbox registrar, handler executor, store, and inspection services are available.
2. **Given** schema-qualified PostgreSQL setup, **When** direct inbox registration and handler execution run, **Then** provider SQL targets the configured schema.
3. **Given** module PostgreSQL persistence setup, **When** module persistence registrations are inspected, **Then** the module has a direct inbox handler executor and inspection store registration.
4. **Given** module-only PostgreSQL setup, **When** services are inspected, **Then** no root `IDurableInboxHandlerExecutor` is registered.

---

### User Story 4 - Execute Direct Inbox Handlers Through PostgreSQL Module Persistence (Priority: P4)

As a module runtime, I want PostgreSQL module direct inbox handler executors so command/event receives use the module's PostgreSQL DbContext, registrar, EF store, and configured time provider.

**Why this priority**: Module-specific persistence must preserve direct inbox idempotency and processed marking inside the correct module boundary.

**Independent Test**: Run PostgreSQL integration tests after build.

**Acceptance Scenarios**:

1. **Given** a single-root PostgreSQL fallback setup, **When** a command is received through a durable receive context, **Then** the fallback root direct inbox stores the receive marker and marks it processed.
2. **Given** schema-qualified root setup, **When** the root handler executor handles a direct inbox record, **Then** the handler runs once and the handled result is returned.
3. **Given** a module-specific PostgreSQL persistence registration, **When** the runtime resolves the module direct inbox executor, **Then** it uses the module-specific PostgreSQL executor registration.

### Edge Cases

- Registration SQL must not use a failed insert/savepoint pattern that aborts the current PostgreSQL transaction on duplicates.
- Table names and schema names must be quoted before being embedded in provider-owned SQL.
- Existing rows determine duplicate status from `ProcessedAtUtc`.
- PostgreSQL integration tests require real provider behavior; EF Core InMemory tests are not sufficient for this feature.
- Direct inbox table schema is still generated by the consumer-owned EF model and migration flow.

## Requirements _(mandatory)_

### Functional Requirements

- **FR-001**: PostgreSQL direct inbox registrar MUST implement `IDurableInboxRegistrar`.
- **FR-002**: Registration SQL MUST insert into `inbox_messages` using the direct inbox composite primary key.
- **FR-003**: Registration SQL MUST use `ON CONFLICT ON CONSTRAINT "PK_inbox_messages" DO NOTHING` to avoid aborting the current transaction on duplicates.
- **FR-004**: Registration MUST return the inserted row when insertion succeeds.
- **FR-005**: Registration MUST return the existing row when insertion conflicts.
- **FR-006**: Registration MUST map inserted rows to `Registered`, existing unprocessed rows to `AlreadyReceived`, and existing processed rows to `AlreadyProcessed`.
- **FR-007**: Registration MUST execute on the current DbContext connection and enlist in the current EF Core transaction when one exists.
- **FR-008**: Registration MUST preserve the caller's connection state by closing only connections it opened.
- **FR-009**: Provider SQL MUST quote table, schema, and column identifiers.
- **FR-010**: PostgreSQL duplicate classification MUST detect unique violations and direct inbox primary-key duplicates.
- **FR-011**: Root PostgreSQL setup MUST register `IDurableInboxRegistrar` and root `IDurableInboxHandlerExecutor`.
- **FR-012**: Root PostgreSQL setup MUST compose direct inbox handler execution from the PostgreSQL registrar, EF Core direct inbox store, and optional `TimeProvider`.
- **FR-013**: Module PostgreSQL setup MUST register a module-specific direct inbox handler executor.
- **FR-014**: Module PostgreSQL setup MUST register a module-specific direct inbox inspection store.
- **FR-015**: Module-only PostgreSQL setup MUST avoid registering root direct inbox executor services.
- **FR-016**: PostgreSQL schema integration tests MUST prove `inbox_messages` and `PK_inbox_messages` are created by the EF model.

### Compatibility And Public API

- **API-001**: Public setup APIs include `AddBondstonePostgreSqlPersistence<TDbContext>()`, `AddBondstonePostgreSqlModulePersistence<TDbContext>()`, and `UsePostgreSqlPersistence<TDbContext>(...)`.
- **API-002**: Public duplicate classification API includes `PostgreSqlPersistenceExceptionClassifier.IsInboxMessageDuplicate(...)`.
- **API-003**: PostgreSQL direct inbox registrar and module handler executor are internal provider implementation types.
- **API-004**: This feature belongs to package ID `Bondstone.Persistence.EntityFrameworkCore.Postgres`.
- **API-005**: Public/protected changes in this feature are compatibility-sensitive and MUST be reviewed against `tests/Bondstone.PublicApi.Tests`.

### Durable Semantics

- **DS-001**: PostgreSQL direct inbox persistence stores immediate module receive idempotency markers, not the transport-facing durable incoming inbox ledger.
- **DS-002**: Duplicate registration MUST skip handler execution through provider-neutral direct inbox handler semantics.
- **DS-003**: Registration and handler execution MUST participate in the current DbContext transaction and persistence scope.
- **DS-004**: PostgreSQL owns provider-specific duplicate/race behavior; generic EF Core owns the table mapping and store mechanics.

### Documentation Requirements

- **DOC-001**: Architecture docs MUST identify EF Core plus PostgreSQL as the supported production durable persistence path.
- **DOC-002**: Testing docs MUST require real PostgreSQL/Testcontainers coverage for uniqueness, transaction, and provider-specific direct inbox behavior.
- **DOC-003**: Packaging docs MUST keep PostgreSQL direct inbox behavior in `Bondstone.Persistence.EntityFrameworkCore.Postgres`.

### Key Entities

- **PostgreSqlDurableInboxRegistrar**: Provider-specific registrar that atomically inserts or reads direct inbox rows with PostgreSQL SQL.
- **PostgreSqlModuleDurableInboxHandlerExecutor**: Module-specific direct inbox executor that composes PostgreSQL registration with the generic EF Core inbox store.
- **PostgreSqlPersistenceExceptionClassifier**: Public provider helper for classifying PostgreSQL unique violations and direct inbox duplicates.
- **BondstonePostgreSqlServiceCollectionExtensions**: DI setup for root and module PostgreSQL persistence services.
- **BondstonePostgreSqlBuilderExtensions**: Bondstone builder setup for root and module PostgreSQL persistence.
- **PostgreSqlTableIdentifier**: Internal identifier quoting helper for schema-aware provider-owned SQL.

## Success Criteria _(mandatory)_

### Measurable Outcomes

- **SC-001**: Integration tests prove PostgreSQL direct inbox registration returns `Registered`, `AlreadyReceived`, and `AlreadyProcessed`.
- **SC-002**: Integration tests prove duplicate direct inbox registration inside a transaction does not abort subsequent durable work.
- **SC-003**: Unit and integration tests prove direct inbox duplicate classification by PostgreSQL primary-key violation.
- **SC-004**: Unit tests prove root and module PostgreSQL setup registers direct inbox services correctly.
- **SC-005**: Schema integration tests prove `inbox_messages` and `PK_inbox_messages` exist in the PostgreSQL database.
- **SC-006**: Single-root fallback integration tests prove received commands persist and mark direct inbox rows processed through root PostgreSQL persistence.

## Assumptions

- The migrated feature describes behavior already implemented before SpecKit adoption.
- Generic EF Core direct inbox mapping and store behavior are already migrated under `specs/012-efcore-direct-inbox-persistence`.
- Consumer applications generate and apply EF migrations; Bondstone does not ship migrations.
- PostgreSQL integration tests require Docker/Testcontainers or equivalent real provider infrastructure.
- The source of truth for durable persistence ownership is `../../docs/architecture.md`.

## Review Notes

- Source scope: PostgreSQL direct inbox registrar, module executor, direct inbox entries in PostgreSQL setup, duplicate classifier, and schema-aware SQL identifier helper.
- Test scope: PostgreSQL direct inbox registration, duplicate classification, schema, service setup, schema-qualified setup, and single-root fallback receive behavior.
- Known gaps are listed in `tasks.md`; they are candidates for future specs, not behavior claimed as covered here.
