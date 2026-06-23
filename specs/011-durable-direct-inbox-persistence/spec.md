# Feature Specification: Durable Direct Inbox Persistence

**Feature Branch**: `N/A - migrated existing functionality`

**Created**: 2026-06-23

**Status**: migrated

**Input**: Existing provider-neutral direct inbox implementation in `src/Bondstone.Persistence` and `src/Bondstone`, with focused tests in `tests/Bondstone.Tests/Persistence`

## Scope And Boundaries _(mandatory)_

**Bondstone Capability**: Provider-neutral direct receive idempotency keys, records, handler execution, registration results, inspection contracts, and module resolution

**Affected Packages/Areas**:

- `src/Bondstone.Persistence/Persistence/Inbox`
- `src/Bondstone.Persistence/Persistence/Contracts`
- `src/Bondstone.Persistence/Persistence/Registration`
- `src/Bondstone/Persistence/Inbox`
- `src/Bondstone/Persistence/Resolution/DurableModuleInboxHandlerExecutorResolver.cs`
- `tests/Bondstone.Tests/Persistence/DurableInbox*.cs`
- `tests/Bondstone.Tests/Persistence/DurableModulePersistenceRegistrationTests.cs`
- `docs/architecture.md`
- `docs/operations.md`
- `docs/package-discovery.md`
- `docs/packaging.md`
- `docs/testing.md`

**Out Of Scope**:

- Durable incoming inbox transport ingestion, claim, retry, stale, and terminal receive failure ledger.
- EF Core direct inbox entity mapping, stores, and inspection queries.
- PostgreSQL direct inbox registration and handler-executor SQL.
- Hosted incoming inbox processing workers.
- Transport-specific native receive and broker settlement behavior.
- Cleanup, purge, replay, retention, and operator repair automation.

## User Scenarios & Testing _(mandatory)_

### User Story 1 - Represent Direct Inbox Receive Identity And State (Priority: P1)

As a persistence provider implementer, I want provider-neutral direct inbox
keys and records so module receive idempotency can be represented consistently
across provider stores and runtime execution.

**Why this priority**: Direct inbox identity and state are the contract shared
by handler execution, duplicate detection, stores, and inspection.

**Independent Test**: Run `dotnet test tests/Bondstone.Tests/Bondstone.Tests.csproj --configuration Release --filter "Category=Unit"` after build.

**Acceptance Scenarios**:

1. **Given** a message id, module name, and handler identity, **When** a direct inbox key is created, **Then** values are normalized and empty message ids are rejected.
2. **Given** a command handler identity, **When** `ForCommandHandler(...)` is used, **Then** the key represents the command handler receive identity.
3. **Given** an event subscriber identity, **When** `ForEventSubscriber(...)` is used, **Then** the key represents the subscriber receive identity.
4. **Given** a key and UTC received timestamp, **When** a record is created, **Then** it starts unprocessed.
5. **Given** a processed timestamp, **When** a record is marked processed, **Then** the processed timestamp is recorded and must not be earlier than the received timestamp.

---

### User Story 2 - Execute A Handler Once Through Direct Inbox Registration (Priority: P2)

As a module receive pipeline, I want a direct inbox handler executor so each
handler/subscriber receive identity is registered before execution and
duplicates are skipped according to provider registration state.

**Why this priority**: This is the runtime idempotency guard around immediate
same-process module handler execution.

**Independent Test**: Run focused direct inbox unit tests after build.

**Acceptance Scenarios**:

1. **Given** provider registration returns `Registered`, **When** `HandleOnceAsync(...)` runs, **Then** the handler executes, the store marks the record processed with the configured clock, and the result status is `Handled`.
2. **Given** provider registration returns `AlreadyReceived`, **When** `HandleOnceAsync(...)` runs, **Then** the handler is not executed and result status is `AlreadyReceived`.
3. **Given** provider registration returns `AlreadyProcessed`, **When** `HandleOnceAsync(...)` runs, **Then** the handler is not executed and result status is `AlreadyProcessed`.
4. **Given** the handler throws, **When** `HandleOnceAsync(...)` runs, **Then** the exception propagates and the record is not marked processed.
5. **Given** a result with `AlreadyReceived`, **When** `DurableInboxAlreadyReceivedException` is created, **Then** it carries the result and rejects non-`AlreadyReceived` statuses.

---

### User Story 3 - Inspect Unprocessed Direct Inbox Rows By Module (Priority: P3)

As an operator-facing tool or application maintenance path, I want a
provider-neutral direct inbox inspector so unprocessed receive markers can be
listed by module through module-specific provider stores.

**Why this priority**: Inspection is the read-only visibility surface for
direct inbox receive markers that were received but not marked processed.

**Independent Test**: Run focused direct inbox unit tests after build.

**Acceptance Scenarios**:

1. **Given** a module with an inbox inspection store, **When** `FindUnprocessedAsync(...)` runs, **Then** the module store is called with normalized module name, max count, and optional cutoff.
2. **Given** blank module name or non-positive max count, **When** inspection runs, **Then** validation fails.
3. **Given** no matching module registration, **When** inspection runs, **Then** a setup diagnostic names the missing durable module inbox inspection store.
4. **Given** a module without an inspection store, **When** inspection runs, **Then** a setup diagnostic names the missing durable module inbox inspection store.

---

### User Story 4 - Resolve Module-Specific Direct Inbox Handler Executors (Priority: P4)

As a runtime maintainer, I want direct inbox handler executors resolved by
module so module-specific persistence boundaries are used, with fallback only
when no module persistence registrations exist.

**Why this priority**: Correct resolution prevents cross-module idempotency
state from being written to the wrong persistence boundary.

**Independent Test**: Run unit tests plus provider setup tests after build.

**Acceptance Scenarios**:

1. **Given** no durable module persistence registrations and a fallback executor, **When** the resolver runs, **Then** it returns the fallback executor.
2. **Given** a module with a registered handler executor, **When** the resolver runs, **Then** it returns that module-specific executor.
3. **Given** missing module or missing executor registration, **When** the resolver runs, **Then** it fails with durable module inbox handler executor setup diagnostics.

### Edge Cases

- Message ids must not be empty.
- Module name and handler identity must be non-empty after normalization.
- Received and processed timestamps must be UTC and non-default.
- Processed timestamp must not be earlier than received timestamp.
- Registration and handle result statuses must be supported enum values.
- `AlreadyReceived` means the handler has been accepted before but not marked processed; callers may surface this through `DurableInboxAlreadyReceivedException`.
- Direct inbox idempotency is distinct from the durable incoming inbox transport receive ledger.

## Requirements _(mandatory)_

### Functional Requirements

- **FR-001**: Direct inbox keys MUST carry message id, module name, and handler identity.
- **FR-002**: Direct inbox records MUST carry key, received timestamp, optional processed timestamp, and processed status.
- **FR-003**: Direct inbox records MUST validate UTC timestamps and processed-after-received ordering.
- **FR-004**: Direct inbox registration results MUST represent `Registered`, `AlreadyReceived`, and `AlreadyProcessed`.
- **FR-005**: Direct inbox handle results MUST represent `Handled`, `AlreadyReceived`, and `AlreadyProcessed`.
- **FR-006**: Direct inbox handler executor MUST call `IDurableInboxRegistrar.RegisterAsync(...)` before executing a handler.
- **FR-007**: Direct inbox handler executor MUST skip handler execution for `AlreadyReceived` and `AlreadyProcessed` registrations.
- **FR-008**: Direct inbox handler executor MUST mark records processed after successful handler execution.
- **FR-009**: Direct inbox handler executor MUST propagate handler exceptions without marking records processed.
- **FR-010**: Direct inbox stores MUST support get, add, and mark processed operations.
- **FR-011**: Direct inbox inspection stores MUST expose unprocessed records by count, optional received cutoff, and optional module filter.
- **FR-012**: Direct inbox inspector MUST resolve module-specific inspection stores by module name.
- **FR-013**: Direct inbox handler executor resolver MUST resolve module-specific executors and support fallback only when no durable module persistence registrations exist.
- **FR-014**: Missing module, missing inspection store, or missing handler executor conditions MUST produce clear setup diagnostics.

### Compatibility And Public API

- **API-001**: Public contracts include `IDurableInboxStore`, `IDurableInboxRegistrar`, `IDurableInboxHandlerExecutor`, `IDurableInboxInspectionStore`, and `IDurableInboxInspector`.
- **API-002**: Public value/result types include `DurableInboxMessageKey`, `DurableInboxRecord`, `DurableInboxRegistrationResult`, `DurableInboxRegistrationStatus`, `DurableInboxHandleResult`, `DurableInboxHandleStatus`, `DurableInboxHandlerExecutor`, and `DurableInboxAlreadyReceivedException`.
- **API-003**: Advanced composition registration types include `DurableModuleInboxHandlerExecutorRegistration`, `DurableModuleInboxInspectionStoreRegistration`, and inbox entries in `DurableModulePersistenceRegistrationRegistry`.
- **API-004**: This feature spans package IDs `Bondstone` and `Bondstone.Persistence`, with public namespace `Bondstone.Persistence`.
- **API-005**: Public/protected changes in this feature are compatibility-sensitive and MUST be reviewed against `tests/Bondstone.PublicApi.Tests`.

### Durable Semantics

- **DS-001**: Direct inbox idempotency marks immediate module receive attempts and is not the transport-facing durable incoming inbox ledger.
- **DS-002**: Handler execution must happen at most once per provider-registered direct inbox receive identity unless provider state reports an incomplete prior receive.
- **DS-003**: Provider-neutral direct inbox contracts must not own EF Core, PostgreSQL, or transport broker behavior.

### Documentation Requirements

- **DOC-001**: Architecture docs MUST distinguish direct inbox idempotency markers from the durable incoming inbox receive ledger.
- **DOC-002**: Operations docs MUST describe direct inbox inspection as received-but-unprocessed marker visibility.
- **DOC-003**: Package discovery docs MUST identify provider-neutral direct inbox contracts in `Bondstone.Persistence`.

### Key Entities

- **DurableInboxMessageKey**: Provider-neutral direct receive identity made of message id, module name, and handler identity.
- **DurableInboxRecord**: Provider-neutral direct receive marker carrying key, received timestamp, and optional processed timestamp.
- **DurableInboxHandlerExecutor**: Runtime idempotency guard that registers, executes, and marks processed through provider contracts.
- **DurableInboxInspector**: Module-aware read facade for unprocessed direct inbox records.
- **DurableModuleInboxHandlerExecutorResolver**: Runtime resolver for module-specific direct inbox handler executors.

## Success Criteria _(mandatory)_

### Measurable Outcomes

- **SC-001**: Unit tests prove direct inbox key, record, registration result, handle result, and already-received exception validation.
- **SC-002**: Unit tests prove handler execution, duplicate skipping, processed marking, and exception propagation.
- **SC-003**: Unit tests prove module inspection routing and missing registration diagnostics.
- **SC-004**: Provider migrations prove EF Core and PostgreSQL direct inbox storage behavior over concrete stores.

## Assumptions

- The migrated feature describes behavior already implemented before SpecKit adoption.
- EF Core and PostgreSQL storage behavior are separate provider-specific migration slices.
- Direct inbox idempotency remains an implementation detail around module receive pipelines, not a replacement for durable incoming inbox transport ingestion.
- The source of truth for durable persistence ownership is `../../docs/architecture.md`.

## Review Notes

- Source scope: provider-neutral direct inbox records/contracts plus runtime executor, inspector, resolver, and module persistence registration entries.
- Test scope: focused direct inbox tests plus direct inbox registration coverage in `tests/Bondstone.Tests/Persistence`.
- Known gaps are listed in `tasks.md`; they are candidates for future specs or provider-specific migrations, not behavior claimed as fully covered here.
