# Feature Specification: PostgreSQL Incoming Inbox Persistence

**Feature Branch**: `N/A - migrated existing functionality`

**Created**: 2026-06-23

**Status**: migrated

**Input**: Existing PostgreSQL incoming inbox implementation in `src/Bondstone.Persistence.EntityFrameworkCore.Postgres`, with integration tests in `tests/Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests`

## Scope And Boundaries _(mandatory)_

**Bondstone Capability**: PostgreSQL-specific durable incoming inbox claiming, lease renewal, outcome mutation, and module dispatcher setup over EF Core mappings

**Affected Packages/Areas**:

- `src/Bondstone.Persistence.EntityFrameworkCore.Postgres/IncomingInbox`
- `src/Bondstone.Persistence.EntityFrameworkCore.Postgres/Persistence/BondstonePostgreSqlServiceCollectionExtensions.cs`
- `src/Bondstone.Persistence.EntityFrameworkCore.Postgres/Persistence/BondstonePostgreSqlBuilderExtensions.cs`
- `tests/Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests/Persistence/PostgreSqlIncomingInboxMutationTests.cs`
- `tests/Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests/Persistence/PostgreSqlIncomingInboxProcessingTests.cs`
- `tests/Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests/Persistence/BondstonePostgreSqlServiceCollectionExtensionsTests.cs`
- `tests/Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests/Persistence/PostgreSqlPersistenceRegistrationTests.cs`
- `tests/Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests/PostgreSqlIncomingInboxTestDbContext.cs`
- `docs/architecture.md`
- `docs/operations.md`
- `docs/package-discovery.md`
- `docs/packaging.md`
- `docs/testing.md`

**Out Of Scope**:

- Provider-neutral incoming inbox contracts, state model, dispatcher algorithm, failure policy, and diagnostics.
- Generic EF Core incoming inbox entity mapping, ingestion store, and inspection store.
- Application-owned EF migrations, schema rollout, and rollback planning.
- Hosted incoming inbox worker scheduling and long-running lease heartbeat loops.
- Transport-specific native receive and settlement behavior.
- Cleanup, purge, replay, retention, operator repair, and broker dead-letter movement.

## User Scenarios & Testing _(mandatory)_

### User Story 1 - Claim Due Incoming Inbox Rows With PostgreSQL (Priority: P1)

As a hosted incoming inbox processor, I want PostgreSQL to atomically claim due
incoming inbox rows so competing workers do not process the same receive row at
the same time.

**Why this priority**: Claiming is the provider-specific mutation that makes
durable incoming inbox processing safe under real relational concurrency.

**Independent Test**: Run `dotnet test tests/Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests/Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests.csproj --configuration Release --filter "Category=Integration"` with PostgreSQL test infrastructure available.

**Acceptance Scenarios**:

1. **Given** pending incoming inbox rows, **When** `ClaimAsync(...)` runs, **Then** PostgreSQL updates rows to `Processing`, increments attempt count, records claim owner and claim expiration, clears prior outcome fields, and returns claimed records in deterministic order.
2. **Given** retry scheduled rows whose next attempt time is due, **When** claiming runs, **Then** those rows are eligible and prior failure outcome data is cleared from the processing state.
3. **Given** retry scheduled rows whose next attempt time is in the future, **When** claiming runs, **Then** they are not claimed.
4. **Given** processing rows whose claim lease is expired, **When** claiming runs, **Then** they can be reclaimed.
5. **Given** processing rows whose claim lease is still active, **When** claiming runs, **Then** they are not claimed.
6. **Given** a receiver-module-scoped claimer, **When** modules share the same incoming inbox table, **Then** only rows for that receiver module are claimed.
7. **Given** a `DbContext` missing the incoming inbox mapping, **When** claiming runs, **Then** a Bondstone setup error identifies the missing `ApplyBondstoneIncomingInbox()` mapping.

---

### User Story 2 - Renew Active Incoming Inbox Processing Leases (Priority: P2)

As a long-running handler coordinator, I want PostgreSQL lease renewal support
so an active processing claim can be extended only by the current claim owner
before the lease expires.

**Why this priority**: Lease renewal is the provider-specific primitive needed
for safe long-running processing even though the dispatcher heartbeat loop is
not implemented in this feature.

**Independent Test**: Run PostgreSQL integration tests after build.

**Acceptance Scenarios**:

1. **Given** a processing row with an active lease and matching claim owner, **When** `RenewAsync(...)` runs, **Then** `ClaimedUntilUtc` is extended and the method returns `true`.
2. **Given** a processing row whose lease is stale, **When** renewal runs, **Then** no row is updated and the method returns `false`.
3. **Given** a missing row, wrong owner, non-processing status, or expired lease, **When** renewal runs, **Then** renewal returns `false`.
4. **Given** an invalid claim owner or non-positive lease duration, **When** renewal runs, **Then** validation fails before SQL mutation.
5. **Given** a `DbContext` missing incoming inbox mapping, **When** renewal runs, **Then** a Bondstone setup error identifies the missing mapping.

---

### User Story 3 - Record Processed, Retry, And Terminal Outcomes With PostgreSQL (Priority: P3)

As an incoming inbox dispatcher, I want PostgreSQL to record processing
outcomes only for the active claim so stale workers cannot overwrite newer
state.

**Why this priority**: Outcome mutation completes the durable receive attempt
by marking successful, retryable, or terminal failure state.

**Independent Test**: Run PostgreSQL integration tests after build.

**Acceptance Scenarios**:

1. **Given** a claimed processing row and matching owner with an active lease, **When** `MarkProcessedAsync(...)` runs, **Then** status becomes `Processed`, processed timestamp is set, failure/retry data is cleared, and claim fields are cleared.
2. **Given** a stale claim, **When** `MarkProcessedAsync(...)` runs, **Then** no row is updated and the method returns `false`.
3. **Given** a claimed processing row below terminal failure threshold, **When** `ScheduleRetryAsync(...)` runs, **Then** status becomes `RetryScheduled`, failed timestamp, failure reason, and next-attempt timestamp are recorded, and claim fields are cleared.
4. **Given** a stale claim, **When** retry scheduling runs, **Then** no row is updated and the method returns `false`.
5. **Given** a claimed processing row at terminal failure threshold, **When** `MarkTerminalFailedAsync(...)` runs, **Then** status becomes `TerminalFailed`, failed timestamp and failure reason are recorded, retry data is cleared, and claim fields are cleared.
6. **Given** a stale claim, **When** terminal failure recording runs, **Then** no row is updated and the method returns `false`.
7. **Given** non-UTC or default timestamps, blank claim owner, blank failure reason, or next-attempt earlier than failed timestamp, **When** outcome recording is requested, **Then** validation fails before SQL mutation.
8. **Given** a `DbContext` missing incoming inbox mapping, **When** outcome recording runs, **Then** a Bondstone setup error identifies the missing mapping.

---

### User Story 4 - Process Incoming Inbox Rows Through PostgreSQL Module Dispatchers (Priority: P4)

As an application host using PostgreSQL-backed modules, I want module-specific
incoming inbox dispatchers registered through normal PostgreSQL setup so each
module claims and processes only its own receive rows.

**Why this priority**: This joins provider-specific SQL mutation with the
provider-neutral dispatcher and module receive pipelines.

**Independent Test**: Run PostgreSQL integration and setup tests after build.

**Acceptance Scenarios**:

1. **Given** `AddBondstonePostgreSqlPersistence<TDbContext>()`, **When** services are resolved, **Then** PostgreSQL incoming inbox claimer, lease renewer, and outcome recorder are registered.
2. **Given** `UsePostgreSqlPersistence<TDbContext>()` on a module, **When** module persistence is registered, **Then** a PostgreSQL module incoming inbox dispatcher is registered and the dispatcher aggregator is used.
3. **Given** modules that share a table, **When** the module incoming inbox dispatcher processes, **Then** it claims only rows for its receiver module.
4. **Given** a pending command row, **When** processing runs, **Then** the command receive pipeline is invoked and PostgreSQL records a processed outcome.
5. **Given** duplicate delivery already ingested, **When** processing runs again, **Then** the handler is not rerun.
6. **Given** handler failure below max attempts, **When** processing runs, **Then** PostgreSQL records retry scheduled state.
7. **Given** handler failure at max attempts, **When** processing runs, **Then** PostgreSQL records terminal failed state visible through incoming inbox inspection.
8. **Given** stale processing claims, **When** inspection runs, **Then** stale evidence is returned.

### Edge Cases

- Claim owner, receiver module, and module names are normalized and required where applicable.
- Claiming uses PostgreSQL `FOR UPDATE SKIP LOCKED` to avoid competing worker overlap.
- Claiming is limited by `maxCount`, and `maxCount` plus lease duration must be positive.
- Provider SQL quotes configured table and column identifiers.
- Optional schema names are applied to the incoming inbox table identifier.
- Claiming clears prior failure/retry/processed fields before handing a row to processing.
- Outcome mutation requires status `Processing`, matching claim owner, and an active claim lease at the outcome timestamp.
- PostgreSQL integration tests require Testcontainers-backed PostgreSQL or equivalent provider infrastructure.

## Requirements _(mandatory)_

### Functional Requirements

- **FR-001**: PostgreSQL incoming inbox claimer MUST validate the EF incoming inbox mapping exists before mutation.
- **FR-002**: PostgreSQL incoming inbox claimer MUST normalize and validate claim owner, positive lease duration, and positive max count.
- **FR-003**: PostgreSQL incoming inbox claimer MUST select pending rows, due retry rows, and stale processing rows as claim candidates.
- **FR-004**: PostgreSQL incoming inbox claimer MUST optionally restrict candidates to a receiver module.
- **FR-005**: PostgreSQL incoming inbox claimer MUST use `FOR UPDATE SKIP LOCKED` and `LIMIT @maxCount`.
- **FR-006**: PostgreSQL incoming inbox claimer MUST update selected rows to `Processing`, increment attempt count, clear outcome fields, set claim owner, and set claim expiration.
- **FR-007**: PostgreSQL incoming inbox claimer MUST return claimed rows as provider-neutral `DurableIncomingInboxRecord` values.
- **FR-008**: PostgreSQL incoming inbox lease renewer MUST renew only active processing claims for the matching key and claim owner.
- **FR-009**: PostgreSQL incoming inbox lease renewer MUST return `false` when no active matching claim is updated.
- **FR-010**: PostgreSQL outcome recorder MUST mark active matching claims processed and clear claim/failure/retry fields.
- **FR-011**: PostgreSQL outcome recorder MUST schedule retry for active matching claims and record failed timestamp, failure reason, and next-attempt timestamp.
- **FR-012**: PostgreSQL outcome recorder MUST mark active matching claims terminal failed and record failed timestamp and failure reason.
- **FR-013**: PostgreSQL outcome recorder MUST return `false` when the claim is stale, missing, not processing, or owned by a different worker.
- **FR-014**: PostgreSQL outcome recorder MUST validate UTC timestamps, failure reason, claim owner, and retry timestamp ordering.
- **FR-015**: PostgreSQL table identifier MUST apply the configured schema to `incoming_inbox_messages`.
- **FR-016**: PostgreSQL service setup MUST register provider-specific incoming inbox claimer, lease renewer, and outcome recorder.
- **FR-017**: PostgreSQL module setup MUST register module-specific incoming inbox dispatchers and enable the module incoming inbox dispatcher aggregator.
- **FR-018**: PostgreSQL module incoming inbox dispatcher MUST compose provider-specific claimer and outcome recorder with the provider-neutral `DurableIncomingInboxDispatcher`.
- **FR-019**: PostgreSQL incoming inbox processing MUST preserve durable duplicate ingestion semantics and avoid rerunning handlers for already processed duplicate deliveries.
- **FR-020**: PostgreSQL incoming inbox integration tests MUST cover claim, lease, outcome, setup, duplicate, retry, terminal, and stale inspection behavior against real PostgreSQL.

### Compatibility And Public API

- **API-001**: Public setup API includes `AddBondstonePostgreSqlPersistence<TDbContext>(...)`, `AddBondstonePostgreSqlModulePersistence<TDbContext>(...)`, and `UsePostgreSqlPersistence<TDbContext>(...)`.
- **API-002**: PostgreSQL incoming inbox concrete mutation classes are internal package implementation details.
- **API-003**: This feature belongs to package ID `Bondstone.Persistence.EntityFrameworkCore.Postgres`.
- **API-004**: Public/protected setup API changes in this feature are compatibility-sensitive and MUST be reviewed against `tests/Bondstone.PublicApi.Tests`.

### Durable Semantics

- **DS-001**: PostgreSQL is the supported production durable persistence provider for incoming inbox mutation semantics.
- **DS-002**: Claim and outcome updates must be guarded by durable receive identity and active claim ownership.
- **DS-003**: Stale workers must not be able to record processed, retry, or terminal outcomes after their lease expires.
- **DS-004**: Bondstone provides provider SQL and helpers, while applications own EF migrations, schema deployment, recovery policy, and retention.
- **DS-005**: Native transport deliveries still must be durably ingested before native settlement; this feature owns post-ingestion processing mutation, not broker settlement.

### Documentation Requirements

- **DOC-001**: Package discovery docs MUST identify the PostgreSQL package as the provider-specific durable mutation package for incoming inbox processing.
- **DOC-002**: Operations docs MUST describe terminal failure and stale claim inspection without implying Bondstone owns cleanup or repair automation.
- **DOC-003**: Packaging docs MUST remind consumers that EF migrations and schema rollout are application-owned.
- **DOC-004**: Testing docs MUST require real PostgreSQL integration tests for claim, lease, locking, retry, and terminal state behavior.

### Key Entities

- **PostgreSqlDurableIncomingInboxClaimer**: Provider-specific SQL claimer for due incoming inbox rows.
- **PostgreSqlDurableIncomingInboxLeaseRenewer**: Provider-specific SQL lease renewer for active processing claims.
- **PostgreSqlDurableIncomingInboxOutcomeRecorder**: Provider-specific SQL recorder for processed, retry scheduled, and terminal failed outcomes.
- **PostgreSqlModuleDurableIncomingInboxDispatcher**: Module-scoped dispatcher that wires PostgreSQL mutation primitives into the provider-neutral incoming inbox dispatcher.
- **PostgreSqlIncomingInboxTableIdentifier**: Incoming inbox table identifier builder with optional schema support.

## Success Criteria _(mandatory)_

### Measurable Outcomes

- **SC-001**: Integration tests prove pending, due retry, and stale processing rows can be claimed and active leases cannot be double-claimed.
- **SC-002**: Integration tests prove active leases can be renewed and stale leases are not renewed.
- **SC-003**: Integration tests prove processed, retry, and terminal outcomes mutate only active matching claims.
- **SC-004**: Integration tests prove PostgreSQL module incoming inbox dispatchers process commands, avoid duplicate reruns, restrict claims by receiver module, and expose stale/terminal evidence.
- **SC-005**: Setup tests prove PostgreSQL incoming inbox mutation services and module dispatcher registrations are available through normal setup APIs.

## Assumptions

- The migrated feature describes behavior already implemented before SpecKit adoption.
- Generic EF Core incoming inbox mapping is documented by `specs/007-efcore-incoming-inbox-persistence`.
- Provider-neutral incoming inbox dispatcher behavior is documented by `specs/006-durable-incoming-inbox-persistence`.
- PostgreSQL integration tests use Testcontainers-backed provider infrastructure.
- The source of truth for persistence ownership is `../../docs/architecture.md`.

## Review Notes

- Source scope: PostgreSQL incoming inbox claimer, lease renewer, outcome recorder, table identifier, module dispatcher, and setup registration hooks.
- Test scope: PostgreSQL incoming inbox processing, mutation, setup registration, and persistence registration tests.
- Known gaps are listed in `tasks.md`; they are candidates for future specs, not behavior claimed as fully covered here.
