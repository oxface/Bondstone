# Feature Specification: Durable Outbox Persistence

**Feature Branch**: `N/A - migrated existing functionality`

**Created**: 2026-06-23

**Status**: migrated

**Input**: Existing provider-neutral durable outbox implementation in `src/Bondstone.Persistence` and focused tests in `tests/Bondstone.Tests/Persistence`

## Scope And Boundaries _(mandatory)_

**Bondstone Capability**: Provider-neutral durable outbox records, dispatch contracts, failure policy, routed dispatch, and module dispatch aggregation

**Affected Packages/Areas**:

- `src/Bondstone.Persistence/Persistence/Outbox`
- `src/Bondstone.Persistence/Persistence/Contracts`
- `src/Bondstone.Persistence/Persistence/Resolution`
- `tests/Bondstone.Tests/Persistence`
- `docs/architecture.md`
- `docs/package-discovery.md`
- `docs/operations.md`
- `docs/observability.md`
- `docs/packaging.md`
- `docs/public-api.md`

**Out Of Scope**:

- EF Core entity mappings, stores, and transaction behavior.
- PostgreSQL-specific claim, lease, mutation, and duplicate-classification SQL.
- Hosted worker loops in `Bondstone.Hosting`.
- Transport adapter serialization and broker-native publish/receive behavior.
- Durable incoming inbox processing and direct receive inbox behavior.

## User Scenarios & Testing _(mandatory)_

### User Story 1 - Represent Durable Outbox Rows And Dispatch State (Priority: P1)

As a persistence provider implementer, I want provider-neutral outbox record and
dispatch-state types so storage providers, workers, diagnostics, and transports
share one durable outbox contract.

**Why this priority**: The record and state model is the durable ledger shape
that all outbox claim, dispatch, retry, inspection, and provider code depends
on.

**Independent Test**: Run `dotnet test tests/Bondstone.Tests/Bondstone.Tests.csproj --configuration Release --filter "Category=Unit"` after build.

**Acceptance Scenarios**:

1. **Given** a durable envelope and UTC stored timestamp, **When** a `DurableOutboxRecord` is created without state, **Then** it carries the envelope, stored timestamp, and `Pending` dispatch state.
2. **Given** a dispatch state, **When** a `DurableOutboxRecord` is created, **Then** the record preserves that state and rejects dispatch timestamps earlier than the stored timestamp.
3. **Given** an invalid dispatch state status, negative attempt count, non-UTC timestamp, default timestamp, or incomplete claim lease pair, **When** a `DurableOutboxDispatchState` is created, **Then** construction fails with an argument error.
4. **Given** result counts from dispatch, **When** `DurableOutboxDispatchResult` is created, **Then** it rejects negative counts and exposes completed count as dispatched plus retry scheduled plus terminal failed.

---

### User Story 2 - Dispatch Claimed Outbox Rows Through A Transport Dispatcher (Priority: P2)

As a hosted worker or custom scheduler, I want a provider-neutral outbox
dispatcher that claims source outbox rows, renews leases, publishes durable
envelopes, records outcomes, and reports batch counts.

**Why this priority**: This is the runtime dispatch path used by
`Bondstone.Hosting` and provider packages to move durable outbox rows into a
transport adapter.

**Independent Test**: Run `dotnet test tests/Bondstone.Tests/Bondstone.Tests.csproj --configuration Release --filter "Category=Unit"` after build.

**Acceptance Scenarios**:

1. **Given** claimed processing records and renewable leases, **When** `DurableOutboxDispatcher.DispatchAsync(...)` runs, **Then** it dispatches each envelope, marks each row dispatched, records diagnostics, and returns claimed/dispatched counts.
2. **Given** a claimed record whose lease cannot be renewed, **When** dispatch runs, **Then** the record is skipped, counted stale, and not sent to the envelope dispatcher.
3. **Given** envelope dispatch failure and a retry decision, **When** dispatch runs, **Then** it schedules retry with failure reason and next attempt timestamp.
4. **Given** envelope dispatch failure and a terminal failure decision, **When** dispatch runs, **Then** it records a terminal failed outcome.
5. **Given** retry, terminal failure, or dispatched outcome recording fails because the row is stale, **When** dispatch runs, **Then** the row is counted stale instead of counted as completed.
6. **Given** host cancellation, **When** the envelope dispatcher throws `OperationCanceledException` for the requested token, **Then** dispatch rethrows cancellation.
7. **Given** blank claim owner, non-positive lease duration, or non-positive max count, **When** dispatch is invoked, **Then** validation fails before claim work.

---

### User Story 3 - Decide Outbox Retry Or Terminal Failure (Priority: P3)

As a persistence provider or hosted worker maintainer, I want a provider-neutral
failure policy so failed outbox dispatches consistently become retry schedules
or terminal failures.

**Why this priority**: Failure handling decides whether durable work remains
retryable or becomes operationally terminal.

**Independent Test**: Run `dotnet test tests/Bondstone.Tests/Bondstone.Tests.csproj --configuration Release --filter "Category=Unit"` after build.

**Acceptance Scenarios**:

1. **Given** a processing outbox record below max attempts, **When** failure policy evaluates it, **Then** it returns a retry decision with normalized failure reason and next attempt timestamp.
2. **Given** the retry delay list is shorter than the attempt count, **When** failure policy evaluates a retryable record, **Then** it uses the last configured delay.
3. **Given** no retry delays, **When** failure policy evaluates a retryable record, **Then** retry is immediate.
4. **Given** max attempts reached, **When** failure policy evaluates the record, **Then** it returns a terminal failure decision.
5. **Given** invalid max attempts, negative retry delays, non-processing records, processing records without attempts, blank failure reasons, or non-UTC failure timestamps, **When** policy or decision types are used, **Then** validation fails.

---

### User Story 4 - Route Durable Envelopes And Aggregate Module Dispatchers (Priority: P4)

As a host or advanced composition maintainer, I want provider-neutral routing
and aggregation around outbox dispatch so exactly one envelope route can own
each durable message and multiple module outbox dispatchers can be processed
through one host worker.

**Why this priority**: This supports both single-transport default dispatch
and advanced multi-module or multi-transport composition without hidden
adapter accumulation.

**Independent Test**: Run `dotnet test tests/Bondstone.Tests/Bondstone.Tests.csproj --configuration Release --filter "Category=Unit"` after build.

**Acceptance Scenarios**:

1. **Given** exactly one `IDurableEnvelopeDispatchRoute` can send a record, **When** `RoutedDurableEnvelopeDispatcher` dispatches, **Then** it sends through that route.
2. **Given** no routes can send a record, **When** routed dispatch runs, **Then** it fails with `BondstoneSetupCodes.MissingDispatcher` and an actionable message describing the durable message.
3. **Given** multiple routes can send a record, **When** routed dispatch runs, **Then** it fails with `BondstoneSetupCodes.AmbiguousDispatchRoute` and lists matching transport names.
4. **Given** module outbox dispatcher registrations, **When** `DurableModuleOutboxDispatchAggregator` runs, **Then** it dispatches each module dispatcher in registration order, passes remaining max count, aggregates counts, and stops when max count is reached.
5. **Given** no module outbox dispatchers are registered, **When** the aggregator runs, **Then** it fails with a clear setup error.

### Edge Cases

- Claim owner values are required and trimmed.
- Lease duration and max count must be positive.
- Dispatch timestamps and failure timestamps must use UTC offsets and must not
  be default values.
- Claim owner and claim expiration must be present together or absent together.
- Failure reasons are required and normalized for failure decisions.
- Routed dispatch must select exactly one route.
- Diagnostics must avoid high-cardinality message ids and claim-owner tags.

## Requirements _(mandatory)_

### Functional Requirements

- **FR-001**: Durable outbox records MUST carry a `DurableMessageEnvelope`, UTC stored timestamp, and dispatch state.
- **FR-002**: Durable outbox records MUST default to `DurableOutboxDispatchState.Pending` when no state is supplied.
- **FR-003**: Durable outbox dispatch state MUST validate supported status, non-negative attempt count, UTC timestamps, and complete claim lease pairs.
- **FR-004**: Durable outbox dispatch results MUST reject negative counts and expose `CompletedCount`.
- **FR-005**: Durable outbox dispatch MUST claim records through `IDurableOutboxClaimer`.
- **FR-006**: Durable outbox dispatch MUST renew a record lease through `IDurableOutboxLeaseRenewer` before sending the envelope.
- **FR-007**: Durable outbox dispatch MUST skip and count stale rows when lease renewal fails.
- **FR-008**: Durable outbox dispatch MUST send envelopes through `IDurableEnvelopeDispatcher`.
- **FR-009**: Durable outbox dispatch MUST mark successful sends through `IDurableOutboxDispatchRecorder.MarkDispatchedAsync(...)`.
- **FR-010**: Durable outbox dispatch MUST count stale rows when dispatched, retry, or terminal failure outcomes cannot be recorded.
- **FR-011**: Durable outbox dispatch MUST use `IDurableOutboxFailurePolicy` after non-cancellation envelope dispatch failures.
- **FR-012**: Retry decisions MUST be recorded through `ScheduleRetryAsync(...)` with failure reason, failure time, and next attempt time.
- **FR-013**: Terminal failure decisions MUST be recorded through `MarkTerminalFailedAsync(...)` with failure reason and failure time.
- **FR-014**: Durable outbox dispatch MUST rethrow host-requested cancellation.
- **FR-015**: Durable outbox dispatch MUST emit OpenTelemetry activity and metric diagnostics for claimed, dispatched, retry scheduled, terminal failed, and stale outcomes.
- **FR-016**: Durable outbox failure policy MUST support configurable max attempts and retry delays.
- **FR-017**: Durable outbox failure policy MUST terminal-fail processing records whose attempt count has reached max attempts.
- **FR-018**: Durable outbox failure policy MUST retry processing records below max attempts and use the last configured retry delay when attempts exceed configured delay count.
- **FR-019**: Routed durable envelope dispatch MUST send through exactly one matching `IDurableEnvelopeDispatchRoute`.
- **FR-020**: Routed durable envelope dispatch MUST report missing routes with `BondstoneSetupCodes.MissingDispatcher`.
- **FR-021**: Routed durable envelope dispatch MUST report ambiguous routes with `BondstoneSetupCodes.AmbiguousDispatchRoute`.
- **FR-022**: Module outbox dispatch aggregation MUST dispatch registered module dispatchers in order and pass remaining max count to each dispatcher.
- **FR-023**: Module outbox dispatch aggregation MUST aggregate dispatch result counts across module dispatchers.
- **FR-024**: Module outbox dispatch aggregation MUST fail when no module dispatchers are registered.

### Compatibility And Public API

- **API-001**: Public contracts include `IDurableOutboxClaimer`, `IDurableOutboxLeaseRenewer`, `IDurableOutboxDispatchRecorder`, `IDurableOutboxDispatcher`, `IDurableOutboxFailurePolicy`, `IDurableEnvelopeDispatcher`, `IDurableEnvelopeDispatchRoute`, `IDurableOutboxWriter`, `IDurableOutboxInspector`, and `IDurableOutboxInspectionStore`.
- **API-002**: Public value and policy types include `DurableOutboxRecord`, `DurableOutboxDispatchState`, `DurableOutboxDispatchResult`, `DurableOutboxStatus`, `DurableOutboxFailureDecision`, `DurableOutboxFailureDecisionKind`, `DurableOutboxFailurePolicy`, `DurableOutboxDispatcher`, `RoutedDurableEnvelopeDispatcher`, and `DurableModuleOutboxDispatchAggregator`.
- **API-003**: The package ID is `Bondstone.Persistence`, and the public namespace is `Bondstone.Persistence`.
- **API-004**: Public/protected changes in this feature are compatibility-sensitive and MUST be reviewed against `tests/Bondstone.PublicApi.Tests`.

### Durable Semantics

- **DS-001**: The outbox is the source-module ledger for outgoing durable commands and integration events.
- **DS-002**: Claimed outbox rows must be leased before transport dispatch and stale rows must not be marked completed.
- **DS-003**: Retry and terminal failure outcomes must be recorded durably through provider implementations of the recorder contract.
- **DS-004**: Provider-neutral outbox dispatch must not own provider-specific storage schema, SQL, transaction behavior, or transport broker behavior.

### Documentation Requirements

- **DOC-001**: Architecture docs MUST state that the outbox is a source-module ledger and that hosted workers claim rows, dispatch envelopes, and record outcomes.
- **DOC-002**: Package discovery and setup docs MUST identify `Bondstone.Persistence` outbox contracts for custom persistence and advanced dispatch composition.
- **DOC-003**: Operations docs MUST describe terminal outbox failure inspection through `IDurableOutboxInspector`.
- **DOC-004**: Observability docs MUST describe outbox dispatch activities and metrics without high-cardinality message ids.

### Key Entities

- **DurableOutboxRecord**: Provider-neutral outbox row carrying envelope, stored timestamp, and dispatch state.
- **DurableOutboxDispatchState**: Provider-neutral outbox state for pending, processing, dispatched, failed, and terminal failed rows.
- **DurableOutboxDispatcher**: Provider-neutral batch dispatcher that claims, renews, sends, records, and reports outbox outcomes.
- **DurableOutboxFailurePolicy**: Retry/terminal failure policy for failed dispatch attempts.
- **RoutedDurableEnvelopeDispatcher**: Envelope dispatcher that chooses exactly one configured dispatch route.
- **DurableModuleOutboxDispatchAggregator**: Aggregates multiple module-level outbox dispatchers behind one dispatcher contract.

## Success Criteria _(mandatory)_

### Measurable Outcomes

- **SC-001**: Unit tests prove outbox record/state/result validation and normalized claim owner behavior.
- **SC-002**: Unit tests prove dispatch success, lease-renewal stale handling, retry scheduling, terminal failure recording, stale outcome recording, cancellation propagation, diagnostics activities, and diagnostics metrics.
- **SC-003**: Unit tests prove failure policy retry delay selection, terminal failure selection, and validation errors.
- **SC-004**: Unit tests prove routed dispatch success, missing route diagnostics, ambiguous route diagnostics, and module dispatcher aggregation.
- **SC-005**: Public API baselines classify this outbox surface as compatibility-sensitive package API.

## Assumptions

- The migrated feature describes behavior already implemented before SpecKit adoption.
- Concrete storage behavior is implemented by EF Core and PostgreSQL provider packages.
- Hosted scheduling is implemented by `Bondstone.Hosting`.
- Transport-native publish behavior is implemented by transport adapter packages or custom application dispatchers.
- The source of truth for durable outbox ownership is `../../docs/architecture.md`.

## Review Notes

- Source scope: provider-neutral durable outbox files in `src/Bondstone.Persistence`.
- Test scope: focused outbox persistence tests in `tests/Bondstone.Tests/Persistence`.
- Known gaps are listed in `tasks.md`; they are candidates for future specs, not behavior claimed as fully covered here.
