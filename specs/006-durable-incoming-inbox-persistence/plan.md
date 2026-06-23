# Implementation Plan: Durable Incoming Inbox Persistence

**Branch**: `N/A - migrated existing functionality` | **Date**: 2026-06-23 | **Spec**: [spec.md](spec.md)

**Input**: Migrated feature specification from `specs/006-durable-incoming-inbox-persistence/spec.md`

## Summary

Durable incoming inbox persistence is an existing provider-neutral Bondstone
capability spanning `Bondstone` runtime processing and `Bondstone.Persistence`
records/contracts. It defines incoming inbox receive identities, records,
state, ingestion boundaries, processing contracts, retry and terminal failure
policy, diagnostics, and module dispatcher aggregation. Concrete storage
providers implement the contracts; transport adapters ingest native deliveries
before settlement; and hosted workers schedule processing through the dispatcher.

## Technical Context

**Language/Version**: C# on `net10.0`

**Primary Dependencies**: `Bondstone`, `Bondstone.Persistence`, `Bondstone.Messaging` contracts, module receive pipelines, `System.Diagnostics`, `System.Diagnostics.Metrics`

**Storage**: Provider-neutral only. EF Core and PostgreSQL storage providers are outside this migration.

**Testing**: xUnit unit tests in `tests/Bondstone.Tests/Persistence`; related hosted worker and PostgreSQL integration tests provide downstream evidence but are out of scope for this artifact.

**Target Platform**: Packable provider-neutral .NET library surface consumed by core runtime, hosting, persistence providers, and transport adapters.

**Project Type**: Library package in a .NET monorepo.

**Performance Goals**: No explicit performance target is documented for the migrated feature.

**Constraints**:

- Preserve provider-neutral boundary; no EF Core or PostgreSQL storage behavior
  belongs in this feature.
- Preserve durable incoming inbox semantics from `../../docs/architecture.md`.
- Preserve transport-native settlement boundary: ingestion completes durably
  before native acknowledgement.
- Preserve OpenTelemetry diagnostic behavior and low-cardinality tag choices.
- Preserve public API compatibility review for broad incoming inbox contracts
  and public implementation types.

**Scale/Scope**:

- Source contracts/primitives/processing: 28 files, 1,614 lines under `src/Bondstone` and `src/Bondstone.Persistence`.
- Focused unit tests: 6 files, 1,148 lines under `tests/Bondstone.Tests/Persistence`.
- Total migrated scope: 34 files and 2,762 lines excluding ignored build output.

## Constitution Check

_GATE: Passed for migrated behavior based on current implementation evidence._

- **Library Boundary First**: Pass. The feature is provider-neutral library surface and runtime processing, not an executable host or storage provider.
- **Durable Identities And Message Semantics**: Pass. Incoming inbox keys preserve durable handler/subscriber identities.
- **Package Boundaries And Public API Compatibility**: Pass with caution. The surface spans `Bondstone` and `Bondstone.Persistence` public API and is baseline-guarded.
- **Persistence And Transport Ownership**: Pass. Provider-neutral contracts coordinate storage providers, transport ingestion, and module receive processing without owning concrete storage or broker behavior.
- **Evidence-Based Verification**: Pass. Behavior is covered by focused unit tests and downstream hosted/provider integration tests.

## Project Structure

### Documentation (this feature)

```text
specs/006-durable-incoming-inbox-persistence/
├── spec.md
├── plan.md
└── tasks.md
```

### Source Code

```text
src/Bondstone/
└── Persistence/IncomingInbox/
    ├── DurableIncomingInboxDefaultDispatcher.cs
    ├── DurableIncomingInboxDispatcher.cs
    ├── DurableIncomingInboxFailurePolicy.cs
    ├── DurableIncomingInboxIngestionBoundaryResolver.cs
    ├── DurableIncomingInboxProcessingOptions.cs
    ├── DurableModuleIncomingInboxDispatcherAggregator.cs
    ├── IDurableIncomingInboxFailurePolicy.cs
    └── IncomingInboxProcessingDiagnostics.cs

src/Bondstone.Persistence/
└── Persistence/
    ├── Contracts/
    │   ├── IDurableIncomingInboxClaimer.cs
    │   ├── IDurableIncomingInboxDispatcher.cs
    │   ├── IDurableIncomingInboxIngestionBoundaryResolver.cs
    │   ├── IDurableIncomingInboxIngestionPersistenceScope.cs
    │   ├── IDurableIncomingInboxIngestionStore.cs
    │   ├── IDurableIncomingInboxInspectionStore.cs
    │   ├── IDurableIncomingInboxLeaseRenewer.cs
    │   └── IDurableIncomingInboxOutcomeRecorder.cs
    ├── IncomingInbox/
    │   ├── DurableIncomingInboxFailureDecision.cs
    │   ├── DurableIncomingInboxFailureDecisionKind.cs
    │   ├── DurableIncomingInboxIngestionBoundary.cs
    │   ├── DurableIncomingInboxIngestionResult.cs
    │   ├── DurableIncomingInboxIngestionStatus.cs
    │   ├── DurableIncomingInboxKey.cs
    │   ├── DurableIncomingInboxProcessingResult.cs
    │   ├── DurableIncomingInboxRecord.cs
    │   ├── DurableIncomingInboxState.cs
    │   └── DurableIncomingInboxStatus.cs
    └── Registration/
        ├── DurableModuleIncomingInboxDispatcherRegistration.cs
        └── DurableModuleIncomingInboxIngestionBoundaryRegistration.cs
```

### Tests

```text
tests/Bondstone.Tests/Persistence/
├── DurableIncomingInboxDispatcherTests.cs
├── DurableIncomingInboxFailureDecisionTests.cs
├── DurableIncomingInboxIngestionResultTests.cs
├── DurableIncomingInboxKeyTests.cs
├── DurableIncomingInboxRecordTests.cs
└── DurableIncomingInboxStateTests.cs
```

**Structure Decision**: Keep this migration scoped to provider-neutral durable incoming inbox behavior. Hosted worker setup and concrete provider storage remain separate migrations.

## Reconstructed Implementation Approach

### Phase 1: Provider-Neutral Receive Ledger

The feature defines durable incoming inbox keys, records, state, status enums,
ingestion results, processing results, and failure decisions. Constructors
validate receive identity, UTC timestamps, source transport names, state shape,
and key/envelope consistency.

### Phase 2: Transport Ingestion Boundary

Transport adapters build `DurableIncomingInboxRecord` instances and resolve an
`IDurableIncomingInboxIngestionBoundaryResolver` by receiver module. The
boundary executes ingestion through a provider persistence scope, calls the
provider ingestion store idempotently, saves changes, and returns whether the
record was newly ingested or already present.

### Phase 3: Processing Dispatcher And Failure Policy

`DurableIncomingInboxDispatcher` validates processing arguments, claims records,
invokes command or event module receive pipelines, records processed outcomes,
and converts receive failures into retry or terminal failure outcomes through
`DurableIncomingInboxFailurePolicy`. Processing continues across later rows
after one row fails, and stale outcome writes are counted separately.

### Phase 4: Diagnostics And Module Aggregation

Incoming inbox processing emits activity and metric diagnostics for claimed,
processed, retry scheduled, terminal failed, and stale rows. The module
dispatcher aggregator iterates registered module dispatchers in order, passes
remaining max count, aggregates result counts, and fails when no dispatchers
are registered.

## Verification Strategy

- **Focused checks**:
  - `dotnet test tests/Bondstone.Tests/Bondstone.Tests.csproj --configuration Release --filter "Category=Unit"`
- **Default gate**: `pnpm check`
- **Provider gate**: `pnpm backend:test:integration` when validating PostgreSQL incoming inbox storage behavior.
- **Package gate**: `pnpm backend:pack` when package metadata or public API baselines are affected.

## Complexity Tracking

> Fill only if Constitution Check has violations that must be justified.

No constitution violations were identified for the migrated feature.

## Gaps And Follow-Up Candidates

- This migration intentionally excludes EF Core and PostgreSQL incoming inbox
  storage behavior; those should be migrated as provider-specific features.
- Hosted incoming inbox worker setup is already migrated under
  `specs/004-hosted-workers` and is not duplicated here.
- Transport receive worker ingestion behavior is already migrated under
  transport specs and is not duplicated here.
- Incoming inbox processing diagnostics exist in source but have less focused
  unit coverage than durable outbox diagnostics.
- Incoming inbox failure policy behavior is covered mainly through dispatcher
  tests; focused policy option tests are thinner than durable outbox policy
  coverage.
- The dispatcher source notes that long-running handler lease renewal requires
  a separate heartbeat loop tied to handler lifetime; this is not implemented
  by the current dispatcher.
