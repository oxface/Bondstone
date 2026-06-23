# Feature Specification: Durable Incoming Inbox Persistence

**Feature Branch**: `N/A - migrated existing functionality`

**Created**: 2026-06-23

**Status**: migrated

**Input**: Existing durable incoming inbox implementation in `src/Bondstone` and `src/Bondstone.Persistence`, with focused tests in `tests/Bondstone.Tests/Persistence`

## Scope And Boundaries _(mandatory)_

**Bondstone Capability**: Provider-neutral durable incoming inbox ingestion, records, processing, failure policy, diagnostics, and module boundary resolution

**Affected Packages/Areas**:

- `src/Bondstone/Persistence/IncomingInbox`
- `src/Bondstone.Persistence/Persistence/IncomingInbox`
- `src/Bondstone.Persistence/Persistence/Contracts`
- `src/Bondstone.Persistence/Persistence/Registration`
- `tests/Bondstone.Tests/Persistence`
- `docs/architecture.md`
- `docs/package-discovery.md`
- `docs/operations.md`
- `docs/observability.md`
- `docs/packaging.md`
- `docs/public-api.md`

**Out Of Scope**:

- EF Core entity mappings, stores, and transaction behavior.
- PostgreSQL-specific claim, lease, ingestion, outcome, and inspection SQL.
- Hosted incoming inbox worker setup and polling loops.
- Transport-specific receive workers and native broker settlement.
- Direct durable inbox processing around immediate same-process receive.

## User Scenarios & Testing _(mandatory)_

### User Story 1 - Represent Durable Incoming Inbox Receive Identities And State (Priority: P1)

As a persistence provider implementer, I want provider-neutral incoming inbox
keys, records, and state so transport ingestion, provider storage, processing
workers, and inspection tools share one durable receive contract.

**Why this priority**: The key, record, and state model is the ledger shape for
idempotent durable receive ingestion and later handler processing.

**Independent Test**: Run `dotnet test tests/Bondstone.Tests/Bondstone.Tests.csproj --configuration Release --filter "Category=Unit"` after build.

**Acceptance Scenarios**:

1. **Given** a command message id, target module, and handler identity, **When** a command incoming inbox key is created, **Then** it normalizes receiver module and handler identity.
2. **Given** an event message id, subscriber module, and subscriber identity, **When** an event incoming inbox key is created, **Then** it represents the subscriber receive identity.
3. **Given** a durable envelope, matching incoming inbox key, UTC ingested timestamp, and optional source transport name, **When** a record is created, **Then** it carries the envelope, receiver module, handler identity, source transport name, ingested timestamp, and pending state.
4. **Given** command key receiver module does not match the envelope target module, **When** a record is created, **Then** construction fails.
5. **Given** state values for pending, processing, processed, retry scheduled, or terminal failed, **When** `DurableIncomingInboxState` is created, **Then** status-specific timestamps, failure details, and claim lease fields are validated.

---

### User Story 2 - Ingest Native Deliveries Into A Durable Incoming Inbox Boundary (Priority: P2)

As a transport adapter maintainer, I want a provider-neutral ingestion boundary
so native transport deliveries can be recorded durably and idempotently before
native message settlement.

**Why this priority**: Transport receive workers rely on this boundary to
commit incoming inbox rows before acknowledging broker-native messages.

**Independent Test**: Run focused transport receive worker tests and provider tests after build.

**Acceptance Scenarios**:

1. **Given** an incoming inbox record and ingestion boundary, **When** `IngestAndSaveAsync(...)` is called, **Then** it runs store ingestion inside the persistence scope and saves changes before returning.
2. **Given** the store inserts a new record, **When** ingestion completes, **Then** the result reports `Ingested`.
3. **Given** the same receive identity was already ingested, **When** ingestion completes, **Then** the result reports `AlreadyIngested`.
4. **Given** a receiver module, **When** the ingestion boundary resolver runs, **Then** it resolves the module-specific ingestion boundary or the configured fallback boundary when no module-specific boundaries exist.
5. **Given** a missing module or missing boundary, **When** the resolver runs, **Then** it fails with setup diagnostics for the missing durable module incoming inbox ingestion boundary.

---

### User Story 3 - Process Claimed Incoming Inbox Rows Through Module Receive Pipelines (Priority: P3)

As a hosted worker or custom scheduler, I want a provider-neutral incoming
inbox dispatcher that claims durable receive rows, invokes command/event receive
pipelines once, records outcomes, and reports batch counts.

**Why this priority**: This completes the durable receive path after transport
ingestion records native deliveries.

**Independent Test**: Run `dotnet test tests/Bondstone.Tests/Bondstone.Tests.csproj --configuration Release --filter "Category=Unit"` after build.

**Acceptance Scenarios**:

1. **Given** claimed command and event rows, **When** `DurableIncomingInboxDispatcher.ProcessAsync(...)` runs, **Then** it invokes command and event receive pipelines with the expected envelope and subscriber binding details, records processed outcomes, and returns claimed/processed counts.
2. **Given** a handler failure below max attempts, **When** processing runs, **Then** it records a retry outcome with failure reason and next attempt timestamp.
3. **Given** a handler failure at max attempts, **When** processing runs, **Then** it records terminal failure.
4. **Given** a processed, retry, or terminal outcome cannot be recorded because the claim is stale, **When** processing runs, **Then** the row is counted stale instead of completed.
5. **Given** one row fails and another can be processed, **When** processing runs, **Then** processing continues for the remaining batch.
6. **Given** an unsupported message kind, **When** processing runs, **Then** it is treated as a processing failure.

---

### User Story 4 - Apply Incoming Inbox Failure Policy, Diagnostics, And Module Aggregation (Priority: P4)

As a runtime maintainer, I want failure policy, diagnostics, and module
dispatch aggregation around incoming inbox processing so processing behavior is
consistent across hosts and module persistence boundaries.

**Why this priority**: These behaviors make incoming inbox processing
operable, retryable, and composable across module persistence registrations.

**Independent Test**: Run unit tests plus provider integration tests after build.

**Acceptance Scenarios**:

1. **Given** a processing record below max attempts, **When** failure policy evaluates it, **Then** it returns a retry decision using configured retry delays.
2. **Given** a processing record at max attempts, **When** failure policy evaluates it, **Then** it returns a terminal failure decision.
3. **Given** registered module incoming inbox dispatchers, **When** the aggregator runs, **Then** it processes registrations in order, passes remaining max count, aggregates counts, and stops at max count.
4. **Given** no module dispatchers are registered, **When** the aggregator runs, **Then** it fails with a clear setup error.
5. **Given** incoming inbox processing runs, **When** diagnostics are emitted, **Then** activity and metric tags avoid high-cardinality message ids and claim-owner values.

### Edge Cases

- Message ids must not be empty.
- Receiver module, handler identity, claim owner, and failure reason values are
  required and normalized.
- Ingested, processed, failed, retry, and claim lease timestamps must be UTC and
  non-default.
- State timestamps must not be earlier than the ingested timestamp.
- Claim owner and claim expiration must be present together or absent together.
- Processed state requires processed timestamp and no failure/retry data.
- Retry scheduled state requires failed timestamp, failure reason, and next
  attempt timestamp.
- Terminal failed state requires failed timestamp and failure reason and must
  not include a next attempt timestamp.
- Long-running handler lease renewal is not completed by this dispatcher; the
  implementation notes this requires a separate heartbeat loop tied to handler
  lifetime.

## Requirements _(mandatory)_

### Functional Requirements

- **FR-001**: Incoming inbox keys MUST carry message id, receiver module, and handler identity.
- **FR-002**: Incoming inbox records MUST carry key, durable envelope, UTC ingested timestamp, state, and optional normalized source transport name.
- **FR-003**: Command incoming inbox records MUST require the key receiver module to match the envelope target module.
- **FR-004**: Incoming inbox state MUST validate supported status, non-negative attempt count, UTC timestamps, claim lease pairs, and status-specific shape.
- **FR-005**: Incoming inbox ingestion results MUST represent `Ingested` and `AlreadyIngested` outcomes and reject invalid status/record values.
- **FR-006**: Incoming inbox ingestion boundary MUST execute ingestion through `IDurableIncomingInboxIngestionPersistenceScope`, call `IDurableIncomingInboxIngestionStore.IngestAsync(...)`, and save changes before returning.
- **FR-007**: Incoming inbox ingestion boundary resolver MUST resolve module-specific ingestion boundaries by receiver module.
- **FR-008**: Incoming inbox ingestion boundary resolver MAY use a fallback boundary when no module-specific incoming inbox boundaries or durable module persistence registrations exist.
- **FR-009**: Incoming inbox processing MUST claim records through `IDurableIncomingInboxClaimer`.
- **FR-010**: Incoming inbox processing MUST invoke the command receive pipeline for command envelopes.
- **FR-011**: Incoming inbox processing MUST invoke the event receive pipeline with receiver module and handler identity for event envelopes.
- **FR-012**: Incoming inbox processing MUST mark successful receives through `IDurableIncomingInboxOutcomeRecorder.MarkProcessedAsync(...)`.
- **FR-013**: Incoming inbox processing MUST use `IDurableIncomingInboxFailurePolicy` after non-cancellation receive failures.
- **FR-014**: Retry decisions MUST be recorded through `ScheduleRetryAsync(...)` with failure reason, failure time, and next attempt time.
- **FR-015**: Terminal failure decisions MUST be recorded through `MarkTerminalFailedAsync(...)` with failure reason and failure time.
- **FR-016**: Incoming inbox processing MUST count stale rows when processed, retry, or terminal failure outcomes cannot be recorded.
- **FR-017**: Incoming inbox processing MUST continue processing remaining rows after one row fails.
- **FR-018**: Incoming inbox processing MUST emit OpenTelemetry activity and metric diagnostics for claimed, processed, retry scheduled, terminal failed, and stale outcomes.
- **FR-019**: Incoming inbox processing options MUST support configurable max attempts and retry delays.
- **FR-020**: Incoming inbox failure policy MUST terminal-fail processing records whose attempt count has reached max attempts.
- **FR-021**: Incoming inbox failure policy MUST retry processing records below max attempts and use the last configured retry delay when attempts exceed configured delay count.
- **FR-022**: Module incoming inbox dispatcher aggregation MUST process registered module dispatchers in order and pass remaining max count to each dispatcher.
- **FR-023**: Module incoming inbox dispatcher aggregation MUST aggregate processing result counts across module dispatchers.
- **FR-024**: Module incoming inbox dispatcher aggregation MUST fail when no module dispatchers are registered.

### Compatibility And Public API

- **API-001**: Public contracts include `IDurableIncomingInboxClaimer`, `IDurableIncomingInboxDispatcher`, `IDurableIncomingInboxIngestionBoundaryResolver`, `IDurableIncomingInboxIngestionPersistenceScope`, `IDurableIncomingInboxIngestionStore`, `IDurableIncomingInboxInspectionStore`, `IDurableIncomingInboxLeaseRenewer`, `IDurableIncomingInboxOutcomeRecorder`, and `IDurableIncomingInboxFailurePolicy`.
- **API-002**: Public value and policy types include `DurableIncomingInboxKey`, `DurableIncomingInboxRecord`, `DurableIncomingInboxState`, `DurableIncomingInboxStatus`, `DurableIncomingInboxIngestionResult`, `DurableIncomingInboxIngestionStatus`, `DurableIncomingInboxProcessingResult`, `DurableIncomingInboxFailureDecision`, `DurableIncomingInboxFailureDecisionKind`, `DurableIncomingInboxProcessingOptions`, `DurableIncomingInboxFailurePolicy`, `DurableIncomingInboxDispatcher`, `DurableIncomingInboxIngestionBoundary`, and `DurableModuleIncomingInboxDispatcherAggregator`.
- **API-003**: This feature spans package IDs `Bondstone` and `Bondstone.Persistence`, with public namespace `Bondstone.Persistence`.
- **API-004**: Public/protected changes in this feature are compatibility-sensitive and MUST be reviewed against `tests/Bondstone.PublicApi.Tests`.

### Durable Semantics

- **DS-001**: Native transport deliveries must be ingested into durable incoming inbox rows before native broker settlement.
- **DS-002**: Durable incoming inbox processing must invoke module receive pipelines at most once per claimed processing attempt.
- **DS-003**: Retry and terminal failure outcomes must be recorded durably through provider implementations of the outcome recorder contract.
- **DS-004**: Provider-neutral incoming inbox processing must not own provider-specific storage schema, SQL, transaction behavior, or transport broker behavior.

### Documentation Requirements

- **DOC-001**: Architecture docs MUST state that the durable inbox receive ledger handles durable ingestion, claim, retry, processed, stale, and terminal receive failure state.
- **DOC-002**: Setup and package-discovery docs MUST identify incoming inbox processing as separate from transport ingestion and hosted worker setup.
- **DOC-003**: Operations docs MUST describe durable incoming inbox inspection for terminal failures and stale processing claims.
- **DOC-004**: Observability docs MUST describe incoming inbox processing diagnostics without high-cardinality message ids.

### Key Entities

- **DurableIncomingInboxKey**: Provider-neutral receive identity made of message id, receiver module, and handler identity.
- **DurableIncomingInboxRecord**: Provider-neutral receive ledger record carrying key, envelope, ingested timestamp, state, and source transport name.
- **DurableIncomingInboxIngestionBoundary**: Provider-neutral ingestion transaction boundary used by transport adapters before native settlement.
- **DurableIncomingInboxDispatcher**: Provider-neutral batch processor that claims rows, invokes receive pipelines, records outcomes, and reports processing counts.
- **DurableIncomingInboxFailurePolicy**: Retry/terminal failure policy for failed receive processing attempts.
- **DurableModuleIncomingInboxDispatcherAggregator**: Aggregates multiple module-level incoming inbox dispatchers behind one dispatcher contract.

## Success Criteria _(mandatory)_

### Measurable Outcomes

- **SC-001**: Unit tests prove incoming inbox key, record, state, ingestion result, failure decision, and processing result validation.
- **SC-002**: Unit tests prove command/event processing, retry scheduling, terminal failure recording, stale outcome recording, and continuation after per-row failure.
- **SC-003**: Registration tests prove module-specific ingestion boundary registration and resolution diagnostics.
- **SC-004**: Provider integration tests prove durable ingestion and incoming inbox processing over concrete storage.
- **SC-005**: Public API baselines classify this incoming inbox surface as compatibility-sensitive package API.

## Assumptions

- The migrated feature describes behavior already implemented before SpecKit adoption.
- Concrete storage behavior is implemented by EF Core and PostgreSQL provider packages.
- Hosted scheduling is implemented by `Bondstone.Hosting`.
- Transport-native receive and settlement behavior is implemented by transport adapter packages or custom application listeners.
- The source of truth for durable incoming inbox ownership is `../../docs/architecture.md`.

## Review Notes

- Source scope: provider-neutral incoming inbox files in `src/Bondstone` and `src/Bondstone.Persistence`.
- Test scope: focused incoming inbox persistence tests in `tests/Bondstone.Tests/Persistence`.
- Known gaps are listed in `tasks.md`; they are candidates for future specs, not behavior claimed as fully covered here.
