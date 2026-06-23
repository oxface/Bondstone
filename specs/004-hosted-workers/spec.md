# Feature Specification: Hosted Workers

**Feature Branch**: `N/A - migrated existing functionality`

**Created**: 2026-06-23

**Status**: migrated

**Input**: Existing implementation in `src/Bondstone.Hosting` and tests in `tests/Bondstone.Hosting.Tests`

## Scope And Boundaries _(mandatory)_

**Bondstone Capability**: Hosted durable outbox and durable incoming inbox workers

**Affected Packages/Areas**:

- `src/Bondstone.Hosting`
- `tests/Bondstone.Hosting.Tests`
- `docs/setup.md`
- `docs/package-discovery.md`
- `docs/operations.md`
- `docs/observability.md`
- `docs/packaging.md`
- `docs/public-api.md`

**Out Of Scope**:

- Durable outbox storage, durable incoming inbox storage, transport dispatch
  implementations, broker listeners, broker topology, and provider-specific
  retry/dead-letter policy.
- Module command/event routing and handler execution internals owned by
  `Bondstone` and `Bondstone.Persistence`.
- EF Core/PostgreSQL persistence implementation details.

## User Scenarios & Testing _(mandatory)_

### User Story 1 - Register Hosted Durable Outbox Worker (Priority: P1)

As a Bondstone host maintainer, I want setup APIs that register the default
durable outbox dispatcher and hosted outbox worker so executable hosts can
dispatch persisted outbox rows without provider-specific hosting code.

**Why this priority**: Outbox worker setup is the normal production host entry
point for durable command and event dispatch.

**Independent Test**: Run `dotnet test tests/Bondstone.Hosting.Tests/Bondstone.Hosting.Tests.csproj --configuration Release --filter "Category=Unit"` after build.

**Acceptance Scenarios**:

1. **Given** a service collection, **When** `AddBondstoneDurableOutboxWorker(...)` is called, **Then** it registers `DurableOutboxWorker` as an `IHostedService`, registers the default `IDurableOutboxDispatcher`, and registers options validation.
2. **Given** a custom `IDurableOutboxDispatcher` already registered, **When** `AddBondstoneDurableOutboxWorker(...)` is called, **Then** the custom dispatcher is preserved.
3. **Given** a Bondstone outbox builder, **When** `UseWorker(...)` is called, **Then** outbox dispatcher and worker capabilities are marked.
4. **Given** a service collection, **When** `AddBondstoneDurableOutboxDispatcher()` is called, **Then** it registers the default durable outbox dispatcher and failure policy.

---

### User Story 2 - Dispatch Durable Outbox Batches (Priority: P2)

As a Bondstone host maintainer, I want the hosted outbox worker to claim and
dispatch durable outbox batches using configured worker identity, lease, batch
size, polling, and failure-delay options.

**Why this priority**: This worker is the host-side loop that turns durable
outbox rows into transport dispatch attempts.

**Independent Test**: Run `dotnet test tests/Bondstone.Hosting.Tests/Bondstone.Hosting.Tests.csproj --configuration Release --filter "Category=Unit"` after build.

**Acceptance Scenarios**:

1. **Given** configured outbox worker options, **When** the worker starts, **Then** it validates options and passes worker id, lease duration, and batch size to `IDurableOutboxDispatcher.DispatchAsync(...)`.
2. **Given** a dispatch batch that claims one or more rows, **When** the dispatcher returns, **Then** the worker immediately attempts the next batch without waiting for the polling interval.
3. **Given** a dispatch batch that claims no rows, **When** the dispatcher returns, **Then** the worker waits for the configured polling interval.
4. **Given** dispatcher failure, **When** the worker catches the exception, **Then** it logs event id `1001` / `DispatchBatchFailed`, waits for `FailureDelay`, and continues.
5. **Given** no `IDurableOutboxDispatcher` is registered, **When** the worker starts, **Then** startup fails with dependency-resolution feedback.

---

### User Story 3 - Register Hosted Durable Incoming Inbox Worker (Priority: P3)

As a Bondstone host maintainer, I want setup APIs that register the durable
incoming inbox processing worker and retry policy options so transport
ingestion can be separated from durable handler processing.

**Why this priority**: Transport adapters ingest native deliveries into durable
incoming inbox rows, and this worker is the standard host-side processor for
those rows.

**Independent Test**: Run `dotnet test tests/Bondstone.Hosting.Tests/Bondstone.Hosting.Tests.csproj --configuration Release --filter "Category=Unit"` after build.

**Acceptance Scenarios**:

1. **Given** a service collection, **When** `AddBondstoneDurableIncomingInboxWorker(...)` is called, **Then** it registers `DurableIncomingInboxWorker` as an `IHostedService`, registers options validation, and registers durable incoming inbox processing options.
2. **Given** the incoming inbox worker is registered repeatedly, **When** the registrations are inspected, **Then** only one hosted incoming inbox worker is registered.
3. **Given** no incoming inbox options are configured, **When** services are built, **Then** default processing options use the persistence default max-attempt policy.
4. **Given** a Bondstone builder, **When** `UseDurableIncomingInboxWorker(...)` is configured with retry options, **Then** the registered processing options preserve max attempts and retry delays.

---

### User Story 4 - Process Durable Incoming Inbox Batches (Priority: P4)

As a Bondstone host maintainer, I want the hosted incoming inbox worker to
claim and process durable incoming inbox batches using configured worker
identity, lease, batch size, polling, failure-delay, and retry policy options.

**Why this priority**: This worker completes the durable receive path after
transport-specific ingestion has safely recorded native deliveries.

**Independent Test**: Run `dotnet test tests/Bondstone.Hosting.Tests/Bondstone.Hosting.Tests.csproj --configuration Release --filter "Category=Unit"` after build.

**Acceptance Scenarios**:

1. **Given** configured incoming inbox worker options, **When** the worker starts, **Then** it validates options and passes worker id, lease duration, and batch size to `IDurableIncomingInboxDispatcher.ProcessAsync(...)`.
2. **Given** a processing batch that claims one or more rows, **When** the dispatcher returns, **Then** the worker immediately attempts the next batch without waiting for the polling interval.
3. **Given** a processing batch that claims no rows, **When** the dispatcher returns, **Then** the worker waits for the configured polling interval.
4. **Given** processing failures, **When** the worker catches exceptions, **Then** it logs event id `2001` / `ProcessBatchFailed` with consecutive failure count, waits for `FailureDelay`, and continues.
5. **Given** the dispatcher observes cancellation, **When** the host stops the worker, **Then** the worker stops cleanly.
6. **Given** no `IDurableIncomingInboxDispatcher` is registered, **When** the worker starts, **Then** startup fails with dependency-resolution feedback.

### Edge Cases

- Worker ids default to `{Environment.MachineName}:{Environment.ProcessId}` and
  are trimmed during validation.
- Lease duration, polling interval, and failure delay must be positive.
- Batch size must be positive.
- Incoming inbox max attempts must be positive.
- Incoming inbox retry delays must not be null and must not contain negative
  durations.
- Worker delay cancellation is swallowed only when the host cancellation token
  has been requested.
- Hosted workers are generic processing loops; provider-specific transport
  connection lifecycle and broker administration stay outside `Bondstone.Hosting`.

## Requirements _(mandatory)_

### Functional Requirements

- **FR-001**: Hosting MUST provide `UseWorker(...)` on `BondstoneOutboxBuilder`.
- **FR-002**: Hosting MUST provide `UseDurableDispatcher()` on `BondstoneOutboxBuilder`.
- **FR-003**: Hosting MUST provide `AddBondstoneDurableOutboxDispatcher()` on `IServiceCollection`.
- **FR-004**: Hosting MUST provide `AddBondstoneDurableOutboxWorker(...)` on `IServiceCollection`.
- **FR-005**: Outbox worker setup MUST preserve an already registered custom `IDurableOutboxDispatcher`.
- **FR-006**: Outbox worker setup MUST register `DurableOutboxWorker` as an `IHostedService` and register `DurableOutboxWorkerOptionsValidator`.
- **FR-007**: Outbox worker options MUST validate worker id, lease duration, batch size, polling interval, and failure delay.
- **FR-008**: Outbox worker execution MUST resolve `IDurableOutboxDispatcher` from a scoped service provider for each batch.
- **FR-009**: Outbox worker execution MUST pass configured worker id, lease duration, and batch size to `DispatchAsync(...)`.
- **FR-010**: Outbox worker execution MUST immediately dispatch the next batch when claimed rows are returned.
- **FR-011**: Outbox worker execution MUST wait for `PollingInterval` when no rows are claimed.
- **FR-012**: Outbox worker execution MUST log dispatch failures with event id `1001` and event name `DispatchBatchFailed`.
- **FR-013**: Outbox worker execution MUST wait for `FailureDelay` after unexpected dispatcher failures and then continue.
- **FR-014**: Hosting MUST provide `UseDurableIncomingInboxWorker(...)` on `BondstoneBuilder`.
- **FR-015**: Hosting MUST provide `AddBondstoneDurableIncomingInboxWorker(...)` on `IServiceCollection`.
- **FR-016**: Incoming inbox worker setup MUST register `DurableIncomingInboxWorker` as an `IHostedService`, register `DurableIncomingInboxWorkerOptionsValidator`, and register `DurableIncomingInboxProcessingOptions`.
- **FR-017**: Incoming inbox worker setup MUST avoid duplicate hosted worker registrations when called repeatedly.
- **FR-018**: Incoming inbox worker options MUST validate worker id, lease duration, batch size, polling interval, failure delay, max attempts, and retry delays.
- **FR-019**: Incoming inbox worker execution MUST resolve `IDurableIncomingInboxDispatcher` from a scoped service provider for each batch.
- **FR-020**: Incoming inbox worker execution MUST pass configured worker id, lease duration, and batch size to `ProcessAsync(...)`.
- **FR-021**: Incoming inbox worker execution MUST immediately process the next batch when claimed rows are returned.
- **FR-022**: Incoming inbox worker execution MUST wait for `PollingInterval` when no rows are claimed.
- **FR-023**: Incoming inbox worker execution MUST log processing failures with event id `2001`, event name `ProcessBatchFailed`, and consecutive failure count.
- **FR-024**: Incoming inbox worker execution MUST wait for `FailureDelay` after unexpected dispatcher failures and then continue.
- **FR-025**: `Bondstone.Hosting` MUST NOT own provider-specific transport, persistence-provider implementation, broker topology, or broker lifecycle behavior.

### Compatibility And Public API

- **API-001**: Public setup APIs are `UseDurableDispatcher()`, `UseWorker(...)`, `AddBondstoneDurableOutboxDispatcher()`, `AddBondstoneDurableOutboxWorker(...)`, `UseDurableIncomingInboxWorker(...)`, and `AddBondstoneDurableIncomingInboxWorker(...)`.
- **API-002**: Public options types are `DurableOutboxWorkerOptions` and `DurableIncomingInboxWorkerOptions`.
- **API-003**: The package ID is `Bondstone.Hosting`, and public namespaces are `Bondstone.Hosting.Outbox` and `Bondstone.Hosting.IncomingInbox`.
- **API-004**: Internal worker and validator types are implementation details exposed to tests through friend assembly only.

### Durable Semantics

- **DS-001**: Hosted outbox workers claim source-module outbox rows through provider-neutral persistence contracts and delegate actual transport dispatch to the durable outbox dispatcher.
- **DS-002**: Hosted incoming inbox workers claim durable incoming inbox rows through provider-neutral persistence contracts and delegate receive processing to the durable incoming inbox dispatcher.
- **DS-003**: Hosting workers MUST not execute provider-specific broker receive or topology work.
- **DS-004**: Incoming inbox retry policy options MUST be available to persistence processing through `DurableIncomingInboxProcessingOptions`.

### Documentation Requirements

- **DOC-001**: Package README and consumer docs MUST state that `Bondstone.Hosting` contains hosted worker composition, not provider-specific transport behavior.
- **DOC-002**: Setup and package-discovery docs MUST show normal host usage of `bondstone.Outbox.UseWorker(...)` and optional `bondstone.UseDurableIncomingInboxWorker(...)`.
- **DOC-003**: Operations and observability docs MUST record outbox worker event id `1001` and incoming inbox worker event id `2001`.

### Key Entities

- **DurableOutboxWorkerOptions**: Public options for worker id, lease duration, batch size, polling interval, and failure delay.
- **DurableOutboxWorker**: Internal hosted service that loops over durable outbox dispatch batches.
- **DurableIncomingInboxWorkerOptions**: Public options for worker id, lease duration, batch size, polling interval, failure delay, max attempts, and retry delays.
- **DurableIncomingInboxWorker**: Internal hosted service that loops over durable incoming inbox processing batches.
- **DurableIncomingInboxProcessingOptions**: Provider-neutral persistence options derived from incoming inbox worker retry policy configuration.

## Success Criteria _(mandatory)_

### Measurable Outcomes

- **SC-001**: Unit tests prove outbox worker service registration, dispatcher preservation, default dispatcher registration, capability marking, option validation, worker option forwarding, immediate next-batch dispatch, failure logging, and missing dispatcher startup failure.
- **SC-002**: Unit tests prove incoming inbox worker service registration, duplicate hosted worker prevention, default processing options, configured retry policy propagation, option validation, worker option forwarding, immediate next-batch processing, failure logging with consecutive failure count, clean cancellation, and missing dispatcher startup failure.
- **SC-003**: Package README states `Bondstone.Hosting` composes reusable hosted workers and does not own provider-specific transport behavior.
- **SC-004**: Public API baselines classify the public hosting setup APIs and options types as normal setup surface.

## Assumptions

- The migrated feature describes behavior already implemented before SpecKit adoption.
- Durable persistence behavior is owned by `Bondstone.Persistence` and concrete persistence packages.
- Transport-specific dispatch and receive ingestion are owned by transport packages or applications.
- The source of truth for worker roles is `../../docs/architecture.md`.

## Review Notes

- Source scope: `src/Bondstone.Hosting`.
- Test scope: `tests/Bondstone.Hosting.Tests`.
- Known gaps are listed in `tasks.md`; they are candidates for future specs, not behavior claimed as fully covered here.
