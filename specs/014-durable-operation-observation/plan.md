# Implementation Plan: Durable Operation Observation

**Branch**: `N/A - migrated existing functionality` | **Date**: 2026-06-23 | **Spec**: [spec.md](spec.md)

**Input**: Migrated feature specification from `specs/014-durable-operation-observation/spec.md`

## Summary

Durable operation observation is an existing provider-neutral Bondstone capability spanning `Bondstone.Persistence` records/contracts and `Bondstone` runtime services. It defines operation state and handles, module operation-state store registration, module-aware reads, typed result reads and waits, explicit failed/cancelled finalization, expiry processing, and receive-side completion writes for durable command handlers. EF Core and PostgreSQL operation-state storage are outside this migration.

## Technical Context

**Language/Version**: C# on `net10.0`

**Primary Dependencies**: `Bondstone`, `Bondstone.Persistence`, module runtime registry, durable module persistence registrations, Microsoft dependency injection, `System.Text.Json`, `TimeProvider`

**Storage**: Provider-neutral operation-state store contracts only. Concrete EF Core/PostgreSQL stores are outside this migration.

**Testing**: xUnit `Unit` tests in `tests/Bondstone.Tests`.

**Target Platform**: Packable provider-neutral .NET library surface consumed by core runtime and persistence providers.

**Project Type**: Library package in a .NET monorepo.

**Performance Goals**: No explicit performance target is documented for the migrated feature.

**Constraints**:

- Preserve operation observation as accepted-work/result read model, not orchestration or retry policy.
- Preserve module-owned operation-state store boundaries.
- Preserve timeout semantics: caller wait timeout never writes terminal state.
- Preserve provider-neutral boundary; EF Core/PostgreSQL storage behavior belongs in provider packages.
- Public API changes are compatibility-sensitive and baseline-guarded.

**Scale/Scope**:

- Source contracts/primitives/runtime/setup: 28 files, 2,751 lines under `src/Bondstone` and `src/Bondstone.Persistence`.
- Focused/shared unit tests: 8 files, 3,441 lines under `tests/Bondstone.Tests`.
- Total migrated scope: 36 files and 6,192 lines excluding ignored build output.

## Constitution Check

_GATE: Passed for migrated behavior based on current implementation evidence._

- **Library Boundary First**: Pass. The feature is provider-neutral library/runtime behavior.
- **Durable Identities And Message Semantics**: Pass. Operation ids and source/target module hints are explicit durable identities.
- **Package Boundaries And Public API Compatibility**: Pass with caution. The public operation observation surface spans `Bondstone` and `Bondstone.Persistence`.
- **Persistence And Transport Ownership**: Pass. Provider-neutral contracts coordinate stores without owning EF/PostgreSQL schema or broker behavior.
- **Evidence-Based Verification**: Pass. Behavior is covered by focused unit tests for records, readers, waits, finalization, expiry, telemetry, and receive completion.

## Project Structure

### Documentation (this feature)

```text
specs/014-durable-operation-observation/
├── spec.md
├── plan.md
└── tasks.md
```

### Source Code

```text
src/Bondstone.Persistence/
├── Messaging/
│   ├── Contracts/IDurableOperationReader.cs
│   └── Operations/
│       ├── DurableOperationDiagnosticContext.cs
│       ├── DurableOperationHandle.cs
│       ├── DurableOperationState.cs
│       └── DurableOperationStatus.cs
└── Persistence/
    ├── Contracts/
    │   ├── IDurableOperationExpirationStore.cs
    │   └── IDurableOperationStateStore.cs
    └── Registration/DurableModuleOperationStateStoreRegistration.cs

src/Bondstone/
├── Configuration/BondstoneServiceCollectionExtensions.cs
├── Messaging/
│   ├── Contracts/
│   │   ├── IDurableOperationExpirationProcessor.cs
│   │   ├── IDurableOperationFinalizer.cs
│   │   └── IDurableOperationResultReader.cs
│   ├── Sending/DurableOperation*.cs
│   └── Serialization/DurableOperationResultPayloadSerializer.cs
├── Modules/
│   ├── Execution/
│   │   ├── ModuleCommandExecutionContext.cs
│   │   ├── ModuleCommandReceiveContext.cs
│   │   └── ModuleCommandRuntime.cs
│   └── Routing/ModuleCommandRoute.cs
└── Persistence/Operations/
    ├── DurableModuleOperationReader.cs
    ├── DurableModuleOperationStateStoreResolver.cs
    ├── DurableOperationExpirationProcessor.cs
    └── DurableOperationFinalizer.cs
```

### Tests

```text
tests/Bondstone.Tests/
├── Messaging/
│   ├── DurableOperationExpirationProcessorTests.cs
│   ├── DurableOperationFinalizerTests.cs
│   ├── DurableOperationHandleTests.cs
│   ├── DurableOperationReaderTests.cs
│   ├── DurableOperationResultReaderTests.cs
│   └── DurableOperationStateTests.cs
├── Modules/ModuleReceivePipelineTests.cs
└── Persistence/DurableModulePersistenceRegistrationTests.cs
```

**Structure Decision**: Keep this migration scoped to provider-neutral operation observation and receive completion. EF Core/PostgreSQL operation-state persistence remain separate migration targets.

## Reconstructed Implementation Approach

### Phase 1: Operation State And Identity

The feature defines `DurableOperationState`, `DurableOperationStatus`, `DurableOperationHandle`, and `DurableOperationDiagnosticContext`. Constructors validate operation ids, statuses, UTC timestamps, and required diagnostic values while normalizing module hints and optional payload/reason fields.

### Phase 2: Operation Store Contracts And Module Registration

`IDurableOperationStateStore` extends `IDurableOperationReader` with save behavior. `IDurableOperationExpirationStore` provides expiration candidate lookup. `DurableModuleOperationStateStoreRegistration` attaches module-specific stores to module runtime registration.

### Phase 3: Module-Aware Reads

`DurableModuleOperationReader` reads across module operation-state stores, preferring terminal states over running/pending states and newer timestamps for equal ranks. Hinted reads and operation-handle reads query one target module store. `DurableModuleOperationStateStoreResolver` resolves module stores for writes and raises setup diagnostics for missing stores.

### Phase 4: Typed Results And Waits

`DurableOperationResultReader` maps operation state into typed `DurableOperationResult<TResult>` values. It deserializes completed result payloads with `DurableOperationResultPayloadSerializer`, reports deserialization failures as result state, and polls with `TimeProvider` for wait APIs. Wait timeout returns or throws without writing operation state.

### Phase 5: Explicit Finalization And Expiry

`DurableOperationFinalizer` writes failed/cancelled terminal states when application policy asks for it, preserving existing terminal states and existing diagnostic context where appropriate. `DurableOperationExpirationProcessor` finds stale candidates through `IDurableOperationExpirationStore`, finalizes each candidate, and records telemetry.

### Phase 6: Receive Completion

`ModuleCommandRuntime`, `ModuleCommandRoute`, and command execution context types capture result payloads during durable command receive handling and save completed operation state to the target module operation-state store only after the direct inbox handler result is `Handled`.

## Verification Strategy

- **Focused checks**:
  - `dotnet test tests/Bondstone.Tests/Bondstone.Tests.csproj --configuration Release --filter "Category=Unit"`
- **Default gate**: `pnpm check`
- **Provider gate**: run EF/PostgreSQL tests when provider operation-state persistence changes.
- **Package gate**: `pnpm backend:pack` when package metadata or public API baselines are affected.

## Complexity Tracking

> Fill only if Constitution Check has violations that must be justified.

No constitution violations were identified for the migrated feature.

## Gaps And Follow-Up Candidates

- EF Core operation-state entity mapping, store, and expiration query behavior are outside this migration.
- PostgreSQL operation-state persistence, transaction, and schema behavior are outside this migration.
- Operation finalization policy is application-owned; Bondstone does not automatically fail operations on caller wait timeout.
- Operation observation does not model workflow/saga/process-manager progress or broker dead-letter state.
- Cleanup, purge, replay, retention, and operator repair flows are not implemented by this provider-neutral feature.
