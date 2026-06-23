# Implementation Plan: Hosted Workers

**Branch**: `N/A - migrated existing functionality` | **Date**: 2026-06-23 | **Spec**: [spec.md](spec.md)

**Input**: Migrated feature specification from `specs/004-hosted-workers/spec.md`

## Summary

Hosted workers are an existing `Bondstone.Hosting` package capability that
composes reusable background services around Bondstone's durable outbox and
durable incoming inbox processing contracts. The package provides normal setup
APIs for executable hosts, provider-neutral worker options, hosted polling
loops, and diagnostic log events while leaving provider-specific persistence,
transport, broker topology, and broker lifecycle behavior to other packages or
applications.

## Technical Context

**Language/Version**: C# on `net10.0`

**Primary Dependencies**: `Bondstone`, `Bondstone.Persistence`, `Microsoft.Extensions.DependencyInjection.Abstractions`, `Microsoft.Extensions.Hosting.Abstractions`, `Microsoft.Extensions.Logging.Abstractions`, `Microsoft.Extensions.Options`

**Storage**: Hosting owns no storage. Workers use provider-neutral persistence dispatchers resolved from host/module persistence services.

**Testing**: xUnit unit tests in `tests/Bondstone.Hosting.Tests`.

**Target Platform**: Packable .NET library package for executable hosts that run durable workers.

**Project Type**: Library package in a .NET monorepo.

**Performance Goals**: No explicit performance target is documented for the migrated feature.

**Constraints**:

- Preserve hosted worker composition as reusable library code, not provider or
  broker ownership.
- Preserve dependency direction from `Bondstone.Hosting` to `Bondstone` and
  `Bondstone.Persistence`.
- Preserve public API compatibility review for setup extensions and options
  types.
- Preserve outbox worker diagnostic event id `1001` and incoming inbox worker
  diagnostic event id `2001`.
- Preserve durable worker roles from `../../docs/architecture.md`.

**Scale/Scope**:

- Source/docs: 17 files, 608 lines under `src/Bondstone.Hosting`.
- Tests/docs: 9 files, 996 lines under `tests/Bondstone.Hosting.Tests`.
- Total migrated scope: 26 files and 1,604 lines excluding ignored build output.

## Constitution Check

_GATE: Passed for migrated behavior based on current implementation evidence._

- **Library Boundary First**: Pass. Hosting is packable worker composition, not an executable host or broker owner.
- **Durable Identities And Message Semantics**: Pass. Workers delegate durable outbox and incoming inbox processing to provider-neutral contracts.
- **Package Boundaries And Public API Compatibility**: Pass with caution. Setup APIs and options types are public package surface; future changes require API review.
- **Persistence And Transport Ownership**: Pass. Hosting owns worker loops and service registration, while persistence providers and transport adapters own provider-specific behavior.
- **Evidence-Based Verification**: Pass. Behavior is covered by focused unit tests for registration, options, loops, failure handling, and cancellation paths.

## Project Structure

### Documentation (this feature)

```text
specs/004-hosted-workers/
├── spec.md
├── plan.md
└── tasks.md
```

### Source Code

```text
src/Bondstone.Hosting/
├── Bondstone.Hosting.csproj
├── README.md
├── AGENTS.md
├── IncomingInbox/
│   ├── BondstoneIncomingInboxHostingBuilderExtensions.cs
│   ├── BondstoneIncomingInboxHostingServiceCollectionExtensions.cs
│   ├── DurableIncomingInboxWorker.cs
│   ├── DurableIncomingInboxWorkerLogEvents.cs
│   ├── DurableIncomingInboxWorkerOptions.cs
│   └── DurableIncomingInboxWorkerOptionsValidator.cs
├── Outbox/
│   ├── BondstoneHostingBuilderExtensions.cs
│   ├── BondstoneHostingServiceCollectionExtensions.cs
│   ├── DurableOutboxWorker.cs
│   ├── DurableOutboxWorkerLogEvents.cs
│   ├── DurableOutboxWorkerOptions.cs
│   └── DurableOutboxWorkerOptionsValidator.cs
├── Utility/StringExtensions.cs
└── Properties/AssemblyInfo.cs
```

### Tests

```text
tests/Bondstone.Hosting.Tests/
├── Bondstone.Hosting.Tests.csproj
├── README.md
├── AGENTS.md
├── IncomingInbox/
│   ├── BondstoneIncomingInboxHostingServiceCollectionExtensionsTests.cs
│   ├── DurableIncomingInboxWorkerOptionsTests.cs
│   └── DurableIncomingInboxWorkerTests.cs
└── Outbox/
    ├── BondstoneHostingServiceCollectionExtensionsTests.cs
    ├── DurableOutboxWorkerOptionsTests.cs
    └── DurableOutboxWorkerTests.cs
```

**Structure Decision**: Keep the migrated feature aligned with the existing `Bondstone.Hosting` package and its test project. No source movement is part of this migration.

## Reconstructed Implementation Approach

### Phase 1: Outbox Setup API

The feature exposes `UseDurableDispatcher()` and `UseWorker(...)` from
`BondstoneOutboxBuilder`, plus service-collection APIs for adding the default
durable outbox dispatcher and hosted outbox worker. Setup uses `TryAdd*`
registration patterns so application-provided dispatchers can override the
default dispatcher.

### Phase 2: Outbox Worker Loop

The outbox worker validates options, verifies dispatcher registration during
startup, repeatedly creates scopes, resolves `IDurableOutboxDispatcher`, and
calls `DispatchAsync(...)` with configured worker id, lease duration, and batch
size. It immediately loops again when rows were claimed, waits for
`PollingInterval` when none were claimed, and logs `DispatchBatchFailed` event
id `1001` before waiting `FailureDelay` after unexpected failures.

### Phase 3: Incoming Inbox Setup API

The feature exposes `UseDurableIncomingInboxWorker(...)` from
`BondstoneBuilder`, plus a service-collection API for adding the hosted
incoming inbox worker. Setup registers option validation and derives
`DurableIncomingInboxProcessingOptions` from worker retry configuration so
persistence processing receives max-attempt and retry-delay policy.

### Phase 4: Incoming Inbox Worker Loop

The incoming inbox worker validates options, verifies dispatcher registration
during startup, repeatedly creates scopes, resolves
`IDurableIncomingInboxDispatcher`, and calls `ProcessAsync(...)` with
configured worker id, lease duration, and batch size. It immediately loops
again when rows were claimed, waits for `PollingInterval` when none were
claimed, logs `ProcessBatchFailed` event id `2001` with consecutive failure
count, waits `FailureDelay` after unexpected failures, and exits cleanly on
host cancellation.

## Verification Strategy

- **Focused checks**:
  - `dotnet test tests/Bondstone.Hosting.Tests/Bondstone.Hosting.Tests.csproj --configuration Release --filter "Category=Unit"`
- **Default gate**: `pnpm check`
- **Package gate**: `pnpm backend:pack` when package metadata or public API baselines are affected.

## Complexity Tracking

> Fill only if Constitution Check has violations that must be justified.

No constitution violations were identified for the migrated feature.

## Gaps And Follow-Up Candidates

- Outbox setup lacks focused duplicate-hosted-worker registration coverage.
- `UseDurableDispatcher(...)` has less direct coverage than `UseWorker(...)`.
- Outbox worker lacks the explicit clean cancellation/blocked dispatcher test
  that incoming inbox worker has.
- Option validation exists but focused tests do not cover every
  positive-duration field.
- Incoming inbox option validation lacks focused tests for `MaxAttempts <= 0`
  and `RetryDelays = null`.
