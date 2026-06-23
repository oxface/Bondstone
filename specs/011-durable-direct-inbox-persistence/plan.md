# Implementation Plan: Durable Direct Inbox Persistence

**Branch**: `N/A - migrated existing functionality` | **Date**: 2026-06-23 | **Spec**: [spec.md](spec.md)

**Input**: Migrated feature specification from `specs/011-durable-direct-inbox-persistence/spec.md`

## Summary

Durable direct inbox persistence is an existing provider-neutral Bondstone
capability spanning `Bondstone.Persistence` contracts/records and `Bondstone`
runtime resolution. It defines direct receive idempotency keys, records,
registration and handle results, store/registrar/inspection contracts, a
default handler executor, module-aware inspection, and module-specific handler
executor resolution. Concrete EF Core and PostgreSQL storage behavior is
outside this migration.

## Technical Context

**Language/Version**: C# on `net10.0`

**Primary Dependencies**: `Bondstone`, `Bondstone.Persistence`, module runtime registry, durable module persistence registrations

**Storage**: Provider-neutral only. EF Core and PostgreSQL storage providers are outside this migration.

**Testing**: xUnit unit tests in `tests/Bondstone.Tests/Persistence`.

**Target Platform**: Packable provider-neutral .NET library surface consumed by core runtime and persistence providers.

**Project Type**: Library package in a .NET monorepo.

**Performance Goals**: No explicit performance target is documented for the migrated feature.

**Constraints**:

- Preserve provider-neutral direct inbox boundary; no EF Core or PostgreSQL
  storage behavior belongs in this feature.
- Preserve distinction between direct inbox idempotency markers and durable
  incoming inbox transport receive ledger.
- Preserve module-specific persistence boundary resolution.
- Preserve public API compatibility review for direct inbox contracts and
  public result/value types.

**Scale/Scope**:

- Source contracts/primitives/runtime/registration: 18 files, 850 lines under `src/Bondstone` and `src/Bondstone.Persistence`.
- Focused unit and registration tests: 7 files, 1,492 lines under `tests/Bondstone.Tests/Persistence`.
- Total migrated scope: 25 files and 2,342 lines excluding ignored build output.

## Constitution Check

_GATE: Passed for migrated behavior based on current implementation evidence._

- **Library Boundary First**: Pass. The feature is provider-neutral library
  surface and runtime routing, not executable host or storage provider
  behavior.
- **Durable Identities And Message Semantics**: Pass. Direct inbox keys
  preserve explicit module/handler/subscriber identities.
- **Package Boundaries And Public API Compatibility**: Pass with caution. The
  surface spans `Bondstone` and `Bondstone.Persistence` public API and is
  baseline-guarded.
- **Persistence And Transport Ownership**: Pass. Provider-neutral contracts
  coordinate storage providers without owning EF/PostgreSQL or broker
  behavior.
- **Evidence-Based Verification**: Pass. Behavior is covered by focused unit
  tests and provider-specific downstream tests.

## Project Structure

### Documentation (this feature)

```text
specs/011-durable-direct-inbox-persistence/
├── spec.md
├── plan.md
└── tasks.md
```

### Source Code

```text
src/Bondstone.Persistence/
└── Persistence/
    ├── Contracts/
    │   ├── DurableInboxAlreadyReceivedException.cs
    │   ├── IDurableInboxHandlerExecutor.cs
    │   ├── IDurableInboxInspectionStore.cs
    │   ├── IDurableInboxInspector.cs
    │   ├── IDurableInboxRegistrar.cs
    │   └── IDurableInboxStore.cs
    ├── Inbox/
        ├── DurableInboxHandleResult.cs
        ├── DurableInboxHandleStatus.cs
        ├── DurableInboxHandlerExecutor.cs
        ├── DurableInboxMessageKey.cs
        ├── DurableInboxRecord.cs
        ├── DurableInboxRegistrationResult.cs
        └── DurableInboxRegistrationStatus.cs
    └── Registration/
        ├── DurableModuleInboxHandlerExecutorRegistration.cs
        ├── DurableModuleInboxInspectionStoreRegistration.cs
        └── DurableModulePersistenceRegistrationRegistry.cs

src/Bondstone/
└── Persistence/
    ├── Inbox/DurableInboxInspector.cs
    └── Resolution/DurableModuleInboxHandlerExecutorResolver.cs
```

### Tests

```text
tests/Bondstone.Tests/Persistence/
├── DurableInboxHandleResultTests.cs
├── DurableInboxHandlerExecutorTests.cs
├── DurableInboxInspectorTests.cs
├── DurableInboxMessageKeyTests.cs
├── DurableInboxRecordTests.cs
├── DurableInboxRegistrationResultTests.cs
└── DurableModulePersistenceRegistrationTests.cs
```

**Structure Decision**: Keep this migration scoped to provider-neutral direct
inbox behavior. EF Core and PostgreSQL direct inbox storage remain separate
migration targets.

## Reconstructed Implementation Approach

### Phase 1: Provider-Neutral Direct Inbox Identity

The feature defines `DurableInboxMessageKey`, `DurableInboxRecord`,
registration result/status, handle result/status, and already-received
exception types. Constructors validate message id, module name, handler
identity, UTC timestamps, supported statuses, and processed-after-received
ordering.

### Phase 2: Direct Inbox Store And Registrar Contracts

Provider contracts define read/add/mark-processed store operations,
registration behavior, direct handler execution, and inspection access to
received-but-unprocessed rows. Module persistence registration entries attach
direct inbox executors and inspection stores to named module boundaries.

### Phase 3: Handler Execution Guard

`DurableInboxHandlerExecutor` registers the receive record before invoking the
handler. `AlreadyReceived` and `AlreadyProcessed` registrations skip handler
execution. Successful handler execution marks the record processed with
`TimeProvider.GetUtcNow()`. Handler failures propagate and leave the row
unprocessed.

### Phase 4: Module Inspection And Resolution

`DurableInboxInspector` validates module input and routes unprocessed-row
inspection to the module-specific inspection store registered in the module
runtime registry. `DurableModuleInboxHandlerExecutorResolver` resolves
module-specific handler executors and only uses a fallback executor when no
durable module persistence registrations exist.

## Verification Strategy

- **Focused checks**:
  - `dotnet test tests/Bondstone.Tests/Bondstone.Tests.csproj --configuration Release --filter "Category=Unit"`
- **Default gate**: `pnpm check`
- **Provider gate**: `pnpm backend:test:integration` when validating PostgreSQL direct inbox registration behavior.
- **Package gate**: `pnpm backend:pack` when package metadata or public API baselines are affected.

## Complexity Tracking

> Fill only if Constitution Check has violations that must be justified.

No constitution violations were identified for the migrated feature.

## Gaps And Follow-Up Candidates

- This migration intentionally excludes EF Core and PostgreSQL direct inbox
  storage behavior; those should be migrated as provider-specific features.
- Direct inbox cleanup, purge, replay, retention, and operator repair flows are
  not implemented by this provider-neutral feature.
- Direct inbox inspection exposes received-but-unprocessed records but does not
  mutate stale rows.
- Transport-facing durable incoming inbox behavior is separate and already
  migrated under `specs/006-durable-incoming-inbox-persistence`.
