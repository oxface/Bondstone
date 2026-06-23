# Feature Specification: Durable Operation Observation

**Feature Branch**: `N/A - migrated existing functionality`

**Created**: 2026-06-23

**Status**: migrated

**Input**: Existing provider-neutral durable operation observation implementation in `src/Bondstone.Persistence` and `src/Bondstone`, with focused tests in `tests/Bondstone.Tests`

## Scope And Boundaries _(mandatory)_

**Bondstone Capability**: Provider-neutral operation state records, operation handles, module-aware operation-state reads, typed result reads and waits, explicit terminal finalization, expiry processing, and receive-side completion state writes

**Affected Packages/Areas**:

- `src/Bondstone.Persistence/Messaging/Operations`
- `src/Bondstone.Persistence/Messaging/Contracts/IDurableOperationReader.cs`
- `src/Bondstone.Persistence/Persistence/Contracts/IDurableOperationStateStore.cs`
- `src/Bondstone.Persistence/Persistence/Contracts/IDurableOperationExpirationStore.cs`
- `src/Bondstone.Persistence/Persistence/Registration/DurableModuleOperationStateStoreRegistration.cs`
- `src/Bondstone/Messaging/Contracts/IDurableOperationResultReader.cs`
- `src/Bondstone/Messaging/Contracts/IDurableOperationFinalizer.cs`
- `src/Bondstone/Messaging/Contracts/IDurableOperationExpirationProcessor.cs`
- `src/Bondstone/Messaging/Sending/DurableOperation*.cs`
- `src/Bondstone/Messaging/Serialization/DurableOperationResultPayloadSerializer.cs`
- `src/Bondstone/Persistence/Operations`
- `src/Bondstone/Modules/Execution/ModuleCommandRuntime.cs`
- `src/Bondstone/Modules/Execution/ModuleCommandExecutionContext.cs`
- `src/Bondstone/Modules/Execution/ModuleCommandReceiveContext.cs`
- `src/Bondstone/Modules/Routing/ModuleCommandRoute.cs`
- `src/Bondstone/Configuration/BondstoneServiceCollectionExtensions.cs`
- `tests/Bondstone.Tests/Messaging/DurableOperation*.cs`
- `tests/Bondstone.Tests/Modules/ModuleReceivePipelineTests.cs`
- `tests/Bondstone.Tests/Persistence/DurableModulePersistenceRegistrationTests.cs`
- `docs/architecture.md`
- `docs/testing.md`

**Out Of Scope**:

- EF Core operation-state entity mapping, stores, and expiration queries.
- PostgreSQL operation-state persistence, transactions, schema, and integration behavior.
- Durable command sending acceptance and source outbox staging, except for operation handles and operation-state read model ownership.
- Durable outbox dispatch retry, durable inbox receive retry, broker retry, and dead-letter state.
- Workflow, saga, process-manager, orchestration, or durable continuation behavior.
- Automatic timeout-to-failure behavior for result waits.
- Cleanup, purge, replay, retention, and operator repair automation.

## User Scenarios & Testing _(mandatory)_

### User Story 1 - Represent Durable Operation State And Handles (Priority: P1)

As an application caller, I want durable operation state and handles so accepted durable work can be observed by durable operation id and module ownership.

**Why this priority**: Operation observation depends on stable identifiers, module hints, statuses, timestamps, optional result payloads, and diagnostics.

**Independent Test**: Run `dotnet test tests/Bondstone.Tests/Bondstone.Tests.csproj --configuration Release --filter "Category=Unit"` after build.

**Acceptance Scenarios**:

1. **Given** a durable operation id, status, UTC update timestamp, optional result payload, optional reason, and diagnostic context, **When** operation state is created, **Then** values are preserved and whitespace-only optional payloads become null.
2. **Given** an empty operation id, unsupported status, default timestamp, or non-UTC timestamp, **When** operation state is created, **Then** validation fails.
3. **Given** a durable operation id plus source and target modules, **When** an operation handle is created, **Then** module names are normalized and carried as source/target hints.
4. **Given** diagnostic context values, **When** context is created, **Then** at least one value is required and blank optional values become null.

---

### User Story 2 - Read Operation State Across Module Stores (Priority: P2)

As an operation observer, I want a module-aware operation reader so state can be read from the module that owns the result, or aggregated across registered module stores when no module hint is available.

**Why this priority**: Operation state is module-owned in multi-module persistence; readers must not accidentally use an unrelated root store.

**Independent Test**: Run focused operation reader unit tests after build.

**Acceptance Scenarios**:

1. **Given** multiple module operation-state stores with pending and terminal states for the same operation id, **When** aggregate read runs, **Then** terminal state wins.
2. **Given** multiple states with the same rank, **When** aggregate read runs, **Then** the newest update timestamp wins.
3. **Given** a module hint, **When** read runs, **Then** only that module's operation-state store is queried.
4. **Given** an operation handle, **When** read runs, **Then** the target module store is queried.
5. **Given** missing module operation-state store, **When** hinted read runs, **Then** a clear setup diagnostic is raised.
6. **Given** module stores exist, **When** a root operation reader or root operation-state store was also registered, **Then** module stores remain authoritative for module operation observation.

---

### User Story 3 - Read Typed Operation Results And Wait Without Mutating State (Priority: P3)

As an edge caller or test, I want typed operation result reads and waits so I can distinguish unknown, pending, running, completed, failed, cancelled, and deserialization-failed outcomes.

**Why this priority**: Operation observation exposes accepted-work outcomes without becoming retry, orchestration, or timeout-finalization logic.

**Independent Test**: Run focused operation result reader unit tests after build.

**Acceptance Scenarios**:

1. **Given** no operation state exists, **When** result is read, **Then** the result state is `Unknown`.
2. **Given** a completed operation with a valid result payload, **When** typed result is read, **Then** the payload is deserialized and state is `CompletedWithResult`.
3. **Given** a completed operation without result payload, **When** typed result is read, **Then** state is `CompletedWithoutResult`.
4. **Given** pending or running operation state, **When** result is read, **Then** non-terminal state is returned without a result payload.
5. **Given** failed or cancelled operation state, **When** result is read or waited for, **Then** terminal state and reason are returned without polling forever.
6. **Given** a completed operation with an incompatible result payload, **When** typed result is read, **Then** state is `ResultDeserializationFailed` with diagnostic failure details.
7. **Given** a wait timeout before terminal state, **When** `WaitForResultAsync(...)` runs, **Then** it throws `TimeoutException` and does not write operation state.
8. **Given** a wait timeout before terminal state, **When** `TryWaitForResultAsync(...)` runs, **Then** it returns the latest result with `CompletedWithinTimeout = false` and does not write operation state.

---

### User Story 4 - Explicitly Finalize Or Expire Operations (Priority: P4)

As application policy, I want explicit finalization and expiry APIs so stale operations can be marked failed or cancelled only when policy decides that terminal outcome.

**Why this priority**: Bondstone distinguishes caller wait timeout from durable operation failure; terminal writes must be explicit application-owned decisions.

**Independent Test**: Run focused finalizer and expiration processor unit tests after build.

**Acceptance Scenarios**:

1. **Given** an unknown operation in a module store, **When** it is marked failed, **Then** a failed terminal state is written with the configured clock and reason.
2. **Given** a pending operation with diagnostic context, **When** it is marked cancelled without new diagnostics, **Then** existing diagnostics are preserved.
3. **Given** an already terminal operation, **When** finalization runs, **Then** existing terminal state is returned and not overwritten.
4. **Given** missing module operation-state store, **When** finalization runs, **Then** a clear setup diagnostic is raised.
5. **Given** an expiration-capable store with pending/running candidates, **When** expiry runs, **Then** candidates at or before cutoff are finalized as failed or cancelled up to max count.
6. **Given** an operation-state store that does not implement expiration lookup, **When** expiry runs, **Then** a clear capability error is raised.
7. **Given** expiry terminal status is not failed or cancelled, **When** expiry runs, **Then** validation fails.

---

### User Story 5 - Complete Operation State From Durable Receives (Priority: P5)

As a durable command receiver, I want successful command receive handling to write completed operation state so callers can later observe target-module results.

**Why this priority**: Operation observation needs the target receive path to store completion state and optional result payloads.

**Independent Test**: Run module receive pipeline unit tests after build.

**Acceptance Scenarios**:

1. **Given** a durable command receive envelope with operation id and a result-producing handler, **When** the receive is handled successfully, **Then** completed operation state is saved with serialized result payload.
2. **Given** a durable command receive envelope with operation id, **When** the direct inbox result is already received or already processed, **Then** operation completion state is not rewritten as a new completed result.
3. **Given** operation id is supplied without a receive inbox record or for a non-durable command route, **When** execution runs, **Then** operation completion validation fails.
4. **Given** completed operation state is written, **Then** diagnostic context includes module, message type, and handler identity.

### Edge Cases

- Operation ids must not be empty.
- Operation-state timestamps and expiration cutoffs must be non-default UTC values.
- Operation result waits do not write terminal state on timeout.
- Completed operations may have no result payload.
- Result deserialization failure is a read outcome, not a mutation of stored operation state.
- Expiration requires stores to implement both state save and expiration candidate lookup.
- Aggregate operation reads rank terminal states above running and pending states.

## Requirements _(mandatory)_

### Functional Requirements

- **FR-001**: Durable operation state MUST carry operation id, status, updated timestamp, optional result payload, optional failure/cancellation reason, and optional diagnostic context.
- **FR-002**: Durable operation handles MUST carry operation id, source module, and target module.
- **FR-003**: Operation state and handles MUST validate non-empty operation ids and supported statuses.
- **FR-004**: Operation-state timestamps and expiration cutoffs MUST use UTC offset and non-default values.
- **FR-005**: Module operation-state store registrations MUST normalize module names and create stores from the current service provider.
- **FR-006**: Aggregate operation reads MUST query registered module operation-state stores when module persistence registrations exist.
- **FR-007**: Aggregate operation reads MUST choose the highest-ranked state and use newest timestamp as a tie-breaker.
- **FR-008**: Hinted reads MUST query only the hinted module operation-state store.
- **FR-009**: Operation-handle reads MUST query the handle target module.
- **FR-010**: Missing hinted module operation-state stores MUST produce clear setup diagnostics.
- **FR-011**: Typed result reads MUST expose unknown, pending, running, completed-with-result, completed-without-result, failed, cancelled, and deserialization-failed states.
- **FR-012**: Completed operation result payloads MUST deserialize through the configured durable payload JSON options.
- **FR-013**: Result deserialization failures MUST include operation id, requested result type, exception type when available, and diagnostic context when available.
- **FR-014**: Result waits MUST poll until terminal state, timeout, or cancellation.
- **FR-015**: `WaitForResultAsync(...)` MUST throw on timeout without writing operation state.
- **FR-016**: `TryWaitForResultAsync(...)` MUST return latest observed result on timeout without writing operation state.
- **FR-017**: Explicit operation finalizer MUST write failed or cancelled terminal state for unknown or non-terminal operations.
- **FR-018**: Explicit operation finalizer MUST not overwrite existing terminal operation state.
- **FR-019**: Expiration processor MUST find pending/running candidates through `IDurableOperationExpirationStore` and finalize each candidate.
- **FR-020**: Expiration processor MUST reject terminal statuses other than failed or cancelled.
- **FR-021**: Successful durable command receive with operation id MUST save completed operation state in the target module operation-state store.
- **FR-022**: Completed receive operation state MUST include serialized result payload when the handler produces one.
- **FR-023**: Runtime setup MUST register operation result reader, module operation reader, finalizer, expiration processor, operation-state store resolver, and payload serializer.

### Compatibility And Public API

- **API-001**: Public provider-neutral operation types include `DurableOperationState`, `DurableOperationStatus`, `DurableOperationHandle`, and `DurableOperationDiagnosticContext`.
- **API-002**: Public result-observation types include `DurableOperationResult<TResult>`, `DurableOperationResultState`, `DurableOperationWaitResult<TResult>`, `DurableOperationResultDeserializationFailure`, `DurableOperationFinalizationResult`, and `DurableOperationExpirationResult`.
- **API-003**: Public contracts include `IDurableOperationReader`, `IDurableOperationStateStore`, `IDurableOperationExpirationStore`, `IDurableOperationResultReader`, `IDurableOperationFinalizer`, and `IDurableOperationExpirationProcessor`.
- **API-004**: Advanced composition registration includes `DurableModuleOperationStateStoreRegistration`.
- **API-005**: This feature spans package IDs `Bondstone` and `Bondstone.Persistence`, with public namespace `Bondstone.Messaging` and provider-neutral persistence contracts in `Bondstone.Persistence`.
- **API-006**: Public/protected changes in this feature are compatibility-sensitive and MUST be reviewed against `tests/Bondstone.PublicApi.Tests`.

### Durable Semantics

- **DS-001**: Operation observation answers what is known about accepted durable work; it is not orchestration, saga state, process-manager state, retry state, or durable continuation state.
- **DS-002**: Caller timeout while waiting for a result MUST NOT mark an operation failed or cancelled.
- **DS-003**: Explicit finalizer and expiration APIs are application-owned terminal outcome decisions.
- **DS-004**: Receive-side completion state belongs to the target module operation-state store.
- **DS-005**: Broker retry, native dead-letter state, source outbox retry, and durable inbox ambiguity are not inferred as operation failure unless application code records a terminal operation state.

### Documentation Requirements

- **DOC-001**: Architecture docs MUST describe operation observation as accepted-work/result state, not orchestration.
- **DOC-002**: Testing docs MUST keep operation observation behavior in fast unit tests unless provider persistence semantics are involved.

### Key Entities

- **DurableOperationState**: Provider-neutral read model for accepted durable operation status and optional result or terminal reason.
- **DurableOperationHandle**: Operation id plus source/target module hint returned by durable sends.
- **DurableModuleOperationReader**: Module-aware aggregate and hinted operation-state reader.
- **DurableOperationResultReader**: Typed read/wait facade over operation state and payload deserialization.
- **DurableOperationFinalizer**: Explicit application-owned failed/cancelled terminal state writer.
- **DurableOperationExpirationProcessor**: Application policy processor for stale non-terminal operation states.
- **ModuleCommandRuntime**: Receive-side runtime that writes completed operation state after successful durable command handling.

## Success Criteria _(mandatory)_

### Measurable Outcomes

- **SC-001**: Unit tests prove operation state, diagnostic context, and operation handle validation.
- **SC-002**: Unit tests prove module-aware aggregate reads, hinted reads, target-module handle reads, and missing-store diagnostics.
- **SC-003**: Unit tests prove typed result states, payload deserialization, deserialization failure reporting, wait success, wait timeout, and cancellation behavior.
- **SC-004**: Unit tests prove explicit finalization writes failed/cancelled states, preserves terminal states, emits telemetry, and reports missing stores.
- **SC-005**: Unit tests prove expiration processing finds candidates, respects max count, emits metrics, and requires expiration-capable stores.
- **SC-006**: Module receive tests prove successful durable command receives save completed operation state and result payloads.

## Assumptions

- The migrated feature describes behavior already implemented before SpecKit adoption.
- EF Core and PostgreSQL operation-state persistence are separate provider-specific migration slices.
- Operation finalization policy is application-owned; Bondstone provides APIs to record outcomes but does not decide timeout policy automatically.
- The source of truth for operation observation ownership is `../../docs/architecture.md`.

## Review Notes

- Source scope: provider-neutral operation state/contracts/result types, module operation reader/resolver, result reader, finalizer, expiration processor, service registration, and durable receive completion path.
- Test scope: focused operation tests plus module receive and module persistence registration coverage.
- Known gaps are listed in `tasks.md`; they are candidates for provider-specific migrations or future specs, not behavior claimed as covered here.
