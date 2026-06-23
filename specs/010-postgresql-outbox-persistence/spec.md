# Feature Specification: PostgreSQL Outbox Persistence

**Feature Branch**: `N/A - migrated existing functionality`

**Created**: 2026-06-23

**Status**: migrated

**Input**: Existing PostgreSQL outbox implementation in `src/Bondstone.Persistence.EntityFrameworkCore.Postgres`, with integration tests in `tests/Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests`

## Scope And Boundaries _(mandatory)_

**Bondstone Capability**: PostgreSQL-specific durable outbox claiming, lease renewal, dispatch outcome mutation, and module dispatcher setup over EF Core mappings

**Affected Packages/Areas**:

- `src/Bondstone.Persistence.EntityFrameworkCore.Postgres/Outbox`
- `src/Bondstone.Persistence.EntityFrameworkCore.Postgres/Persistence/BondstonePostgreSqlServiceCollectionExtensions.cs`
- `src/Bondstone.Persistence.EntityFrameworkCore.Postgres/Persistence/BondstonePostgreSqlBuilderExtensions.cs`
- `tests/Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests/Outbox`
- `tests/Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests/Persistence/PostgreSqlOutboxClaimTests.cs`
- `tests/Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests/Persistence/PostgreSqlOutboxLeaseTests.cs`
- `tests/Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests/Persistence/PostgreSqlOutboxDispatchTests.cs`
- `tests/Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests/Persistence/PostgreSqlOutboxDispatcherTests.cs`
- `tests/Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests/Persistence/PostgreSqlPersistenceRegistrationTests.cs`
- `tests/Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests/Persistence/BondstonePostgreSqlServiceCollectionExtensionsTests.cs`
- `docs/architecture.md`
- `docs/operations.md`
- `docs/package-discovery.md`
- `docs/packaging.md`
- `docs/testing.md`

**Out Of Scope**:

- Provider-neutral durable outbox records, dispatch state, failure policy, dispatch result, and dispatcher algorithm.
- Generic EF Core outbox entity mapping, outbox writer, and terminal failure inspection store.
- Application-owned EF migrations, schema rollout, and rollback planning.
- Hosted outbox worker scheduling and long-running lease heartbeat loops.
- Transport-specific envelope dispatch implementations.
- Cleanup, purge, replay, retention, operator repair, and broker dead-letter movement.

## User Scenarios & Testing _(mandatory)_

### User Story 1 - Claim Due Outbox Rows With PostgreSQL (Priority: P1)

As a hosted outbox dispatcher, I want PostgreSQL to atomically claim due
outbox rows so competing workers do not dispatch the same envelope at the same
time.

**Why this priority**: Claiming is the provider-specific mutation that makes
durable outbox dispatch safe under real relational concurrency.

**Independent Test**: Run `dotnet test tests/Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests/Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests.csproj --configuration Release --filter "Category=Integration"` with PostgreSQL test infrastructure available.

**Acceptance Scenarios**:

1. **Given** pending outbox rows, **When** `ClaimAsync(...)` runs, **Then** PostgreSQL updates rows to `Processing`, increments attempt count, records claim owner and claim expiration, and returns claimed records in deterministic stored-time order.
2. **Given** pending rows with future `NextAttemptAtUtc`, **When** claiming runs before that timestamp, **Then** those rows are not claimed.
3. **Given** pending rows whose next-attempt time is due, **When** claiming runs, **Then** those rows are eligible for claim.
4. **Given** processing rows whose claim lease is expired, **When** claiming runs, **Then** they can be reclaimed.
5. **Given** processing rows whose claim lease is still active, **When** claiming runs, **Then** they are not claimed.
6. **Given** one dispatcher transaction holds a candidate row lock, **When** another dispatcher claims rows, **Then** PostgreSQL skips the locked row and claims the next eligible row.
7. **Given** a source-module-scoped claimer, **When** modules share the same outbox table, **Then** only rows for that source module are claimed.

---

### User Story 2 - Renew Active Outbox Dispatch Leases (Priority: P2)

As a long-running outbox dispatcher, I want PostgreSQL lease renewal support so
an active processing claim can be extended only by the current claim owner
before the lease expires.

**Why this priority**: Lease renewal is the provider-specific primitive needed
for safe long-running dispatch even when the worker heartbeat loop lives
outside this feature.

**Independent Test**: Run PostgreSQL integration tests after build.

**Acceptance Scenarios**:

1. **Given** a processing row with an active lease and matching claim owner, **When** `RenewAsync(...)` runs, **Then** `ClaimedUntilUtc` is extended and the method returns `true`.
2. **Given** a processing row owned by another worker, **When** renewal runs, **Then** no row is updated and the method returns `false`.
3. **Given** a processing row whose claim lease is expired, **When** renewal runs, **Then** no row is updated and the method returns `false`.
4. **Given** a row that is not `Processing`, **When** renewal runs, **Then** no row is updated and the method returns `false`.
5. **Given** an invalid claim owner or non-positive lease duration, **When** renewal runs, **Then** validation fails before SQL mutation.

---

### User Story 3 - Record Dispatched, Retry, And Terminal Outcomes With PostgreSQL (Priority: P3)

As an outbox dispatcher, I want PostgreSQL to record dispatch outcomes only for
the active claim so stale workers cannot overwrite newer outbox state.

**Why this priority**: Outcome mutation completes each durable dispatch
attempt by marking success, retryable failure, or terminal failure.

**Independent Test**: Run PostgreSQL integration tests after build.

**Acceptance Scenarios**:

1. **Given** a claimed processing row and matching owner with an active lease, **When** `MarkDispatchedAsync(...)` runs, **Then** status becomes `Dispatched`, dispatched timestamp is set, failure/retry data is cleared, and claim fields are cleared.
2. **Given** a claimed processing row below terminal failure threshold, **When** `ScheduleRetryAsync(...)` runs, **Then** status becomes `Pending`, failed timestamp, failure reason, and next-attempt timestamp are recorded, and claim fields are cleared.
3. **Given** a claimed processing row at terminal failure threshold, **When** `MarkTerminalFailedAsync(...)` runs, **Then** status becomes `TerminalFailed`, failed timestamp and failure reason are recorded, retry data is cleared, and claim fields are cleared.
4. **Given** a claim owned by another worker, **When** outcome recording runs, **Then** no row is updated and the method returns `false`.
5. **Given** an expired claim lease, **When** outcome recording runs, **Then** no row is updated and the method returns `false`.
6. **Given** non-UTC or default timestamps, blank claim owner, blank failure reason, or next-attempt earlier than failed timestamp, **When** outcome recording is requested, **Then** validation fails before SQL mutation.

---

### User Story 4 - Dispatch Outbox Rows Through PostgreSQL Module Dispatchers (Priority: P4)

As an application host using PostgreSQL-backed modules, I want module-specific
outbox dispatchers registered through normal PostgreSQL setup so each module
claims and dispatches only its own source rows.

**Why this priority**: This joins provider-specific SQL mutation with the
provider-neutral outbox dispatcher and envelope dispatch route.

**Independent Test**: Run PostgreSQL integration and setup tests after build.

**Acceptance Scenarios**:

1. **Given** `AddBondstonePostgreSqlPersistence<TDbContext>()`, **When** services are resolved, **Then** PostgreSQL outbox claimer, lease renewer, and dispatch recorder are registered.
2. **Given** `UsePostgreSqlPersistence<TDbContext>()` on a module, **When** module persistence is registered, **Then** a PostgreSQL module outbox dispatcher is registered and the dispatcher aggregator is used.
3. **Given** modules that share a table, **When** the module outbox dispatcher processes, **Then** it claims only rows for its source module.
4. **Given** pending outbox rows and a successful envelope dispatcher, **When** dispatch runs, **Then** rows are claimed, envelopes are dispatched, and PostgreSQL records dispatched outcomes.
5. **Given** envelope dispatch failure below max attempts, **When** dispatch runs, **Then** PostgreSQL records retry state.
6. **Given** envelope dispatch failure at max attempts, **When** dispatch runs, **Then** PostgreSQL records terminal failed state.
7. **Given** a host with another EF/PostgreSQL module, **When** durable send resolves persistence services, **Then** it does not resolve the other module's `DbContext`.

### Edge Cases

- Claim owner, source module, and module names are normalized and required where applicable.
- Claiming uses PostgreSQL `FOR UPDATE SKIP LOCKED` to avoid competing worker overlap.
- Claiming is limited by `maxCount`, and `maxCount` plus lease duration must be positive.
- Provider SQL quotes configured table and column identifiers.
- Optional schema names are applied to the outbox table identifier.
- Outcome mutation requires status `Processing`, matching claim owner, and an active claim lease at the outcome timestamp.
- Retry scheduling returns status to `Pending` and uses `NextAttemptAtUtc` to delay future claims.
- PostgreSQL integration tests require Testcontainers-backed PostgreSQL or equivalent provider infrastructure.

## Requirements _(mandatory)_

### Functional Requirements

- **FR-001**: PostgreSQL outbox claimer MUST normalize and validate claim owner, positive lease duration, and positive max count.
- **FR-002**: PostgreSQL outbox claimer MUST select pending rows whose next-attempt time is null or due, and processing rows whose claim lease has expired.
- **FR-003**: PostgreSQL outbox claimer MUST optionally restrict candidates to a source module.
- **FR-004**: PostgreSQL outbox claimer MUST use `FOR UPDATE SKIP LOCKED` and `LIMIT @maxCount`.
- **FR-005**: PostgreSQL outbox claimer MUST update selected rows to `Processing`, increment attempt count, set claim owner, and set claim expiration.
- **FR-006**: PostgreSQL outbox claimer MUST return claimed rows as provider-neutral `DurableOutboxRecord` values.
- **FR-007**: PostgreSQL outbox lease renewer MUST renew only active processing claims for the matching message id and claim owner.
- **FR-008**: PostgreSQL outbox lease renewer MUST return `false` when no active matching claim is updated.
- **FR-009**: PostgreSQL dispatch recorder MUST mark active matching claims dispatched and clear claim/failure/retry fields.
- **FR-010**: PostgreSQL dispatch recorder MUST schedule retry for active matching claims and record failed timestamp, failure reason, and next-attempt timestamp.
- **FR-011**: PostgreSQL dispatch recorder MUST mark active matching claims terminal failed and record failed timestamp and failure reason.
- **FR-012**: PostgreSQL dispatch recorder MUST return `false` when the claim is stale, missing, not processing, or owned by a different worker.
- **FR-013**: PostgreSQL dispatch recorder MUST validate UTC timestamps, failure reason, claim owner, and retry timestamp ordering.
- **FR-014**: PostgreSQL table identifier MUST apply the configured schema to `outbox_messages`.
- **FR-015**: PostgreSQL service setup MUST register provider-specific outbox claimer, lease renewer, and dispatch recorder.
- **FR-016**: PostgreSQL module setup MUST register module-specific outbox dispatchers and enable the module outbox dispatcher aggregator.
- **FR-017**: PostgreSQL module outbox dispatcher MUST compose provider-specific claimer, lease renewer, and dispatch recorder with the provider-neutral `DurableOutboxDispatcher`.
- **FR-018**: PostgreSQL outbox integration tests MUST cover claim, skip-locked, lease, dispatch outcome, setup, module scoping, retry, and terminal behavior against real PostgreSQL.

### Compatibility And Public API

- **API-001**: Public setup API includes `AddBondstonePostgreSqlPersistence<TDbContext>(...)`, `AddBondstonePostgreSqlModulePersistence<TDbContext>(...)`, and `UsePostgreSqlPersistence<TDbContext>(...)`.
- **API-002**: PostgreSQL outbox concrete mutation classes are internal package implementation details.
- **API-003**: This feature belongs to package ID `Bondstone.Persistence.EntityFrameworkCore.Postgres`.
- **API-004**: Public/protected setup API changes in this feature are compatibility-sensitive and MUST be reviewed against `tests/Bondstone.PublicApi.Tests`.

### Durable Semantics

- **DS-001**: PostgreSQL is the supported production durable persistence provider for outbox mutation semantics.
- **DS-002**: Claim and outcome updates must be guarded by durable message id and active claim ownership.
- **DS-003**: Stale workers must not be able to record dispatched, retry, or terminal outcomes after their lease expires.
- **DS-004**: Bondstone provides provider SQL and helpers, while applications own EF migrations, schema deployment, recovery policy, and retention.
- **DS-005**: Transport adapters remain responsible only for envelope dispatch; broker topology, retry, dead-letter, and monitoring remain application-owned.

### Documentation Requirements

- **DOC-001**: Package discovery docs MUST identify the PostgreSQL package as the provider-specific durable mutation package for outbox dispatch.
- **DOC-002**: Operations docs MUST describe terminal failure and stale claim inspection without implying Bondstone owns cleanup or repair automation.
- **DOC-003**: Packaging docs MUST remind consumers that EF migrations and schema rollout are application-owned.
- **DOC-004**: Testing docs MUST require real PostgreSQL integration tests for claim, lease, locking, retry, and terminal state behavior.

### Key Entities

- **PostgreSqlDurableOutboxClaimer**: Provider-specific SQL claimer for due outbox rows.
- **PostgreSqlDurableOutboxLeaseRenewer**: Provider-specific SQL lease renewer for active processing claims.
- **PostgreSqlDurableOutboxDispatchRecorder**: Provider-specific SQL recorder for dispatched, retry scheduled, and terminal failed outcomes.
- **PostgreSqlModuleDurableOutboxDispatcher**: Module-scoped dispatcher that wires PostgreSQL mutation primitives into the provider-neutral outbox dispatcher.
- **PostgreSqlOutboxTableIdentifier**: Outbox table identifier builder with optional schema support.

## Success Criteria _(mandatory)_

### Measurable Outcomes

- **SC-001**: Integration tests prove pending, due retry, stale processing, and locked rows are claimed or skipped correctly.
- **SC-002**: Integration tests prove active leases can be renewed and stale, wrong-owner, or non-processing rows are not renewed.
- **SC-003**: Integration tests prove dispatched, retry, and terminal outcomes mutate only active matching claims.
- **SC-004**: Integration tests prove PostgreSQL module outbox dispatchers dispatch, restrict claims by source module, retry failed dispatch, and terminal-fail poison dispatch.
- **SC-005**: Setup tests prove PostgreSQL outbox mutation services and module dispatcher registrations are available through normal setup APIs.

## Assumptions

- The migrated feature describes behavior already implemented before SpecKit adoption.
- Generic EF Core outbox mapping is documented by `specs/009-efcore-outbox-persistence`.
- Provider-neutral durable outbox dispatcher behavior is documented by `specs/005-durable-outbox-persistence`.
- PostgreSQL integration tests use Testcontainers-backed provider infrastructure.
- The source of truth for persistence ownership is `../../docs/architecture.md`.

## Review Notes

- Source scope: PostgreSQL outbox claimer, lease renewer, dispatch recorder, table identifier, module dispatcher, and setup registration hooks.
- Test scope: PostgreSQL outbox claim, lease, dispatch recorder, dispatch processing, setup registration, and service validation tests.
- Known gaps are listed in `tasks.md`; they are candidates for future specs, not behavior claimed as fully covered here.
