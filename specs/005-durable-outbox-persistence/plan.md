# Implementation Plan: Durable Outbox Persistence

**Branch**: `N/A - migrated existing functionality` | **Date**: 2026-06-23 | **Spec**: [spec.md](spec.md)

**Input**: Migrated feature specification from `specs/005-durable-outbox-persistence/spec.md`

## Summary

Durable outbox persistence is an existing provider-neutral `Bondstone.Persistence`
capability. It defines outbox row records and state, claim/lease/recording
contracts, retry and terminal failure policy, batch dispatch orchestration,
routed envelope dispatch, and module dispatch aggregation. Concrete storage
providers implement the contracts; hosted workers schedule the dispatcher; and
transport adapters publish envelopes through `IDurableEnvelopeDispatcher`.

## Technical Context

**Language/Version**: C# on `net10.0`

**Primary Dependencies**: `Bondstone.Persistence`, `Bondstone.Messaging` contracts, `System.Diagnostics`, `System.Diagnostics.Metrics`

**Storage**: Provider-neutral only. EF Core and PostgreSQL storage providers are outside this migration.

**Testing**: xUnit unit tests in `tests/Bondstone.Tests/Persistence`.

**Target Platform**: Packable provider-neutral .NET library package consumed by core runtime, hosting, persistence providers, and transport adapters.

**Project Type**: Library package in a .NET monorepo.

**Performance Goals**: No explicit performance target is documented for the migrated feature.

**Constraints**:

- Preserve provider-neutral package boundary; no EF Core or PostgreSQL storage
  behavior belongs in this feature.
- Preserve durable outbox semantics from `../../docs/architecture.md`.
- Preserve OpenTelemetry diagnostic behavior and low-cardinality tag choices.
- Preserve public API compatibility review for broad outbox contracts and
  public implementation types.
- Preserve collaboration through explicit contracts rather than production
  friend assemblies.

**Scale/Scope**:

- Source contracts/primitives: 20 files, 928 lines under `src/Bondstone.Persistence`.
- Focused tests: 9 files, 1,449 lines under `tests/Bondstone.Tests/Persistence`.
- Total migrated scope: 29 files and 2,377 lines excluding ignored build output.

## Constitution Check

_GATE: Passed for migrated behavior based on current implementation evidence._

- **Library Boundary First**: Pass. The feature is provider-neutral library surface and does not own executable host behavior.
- **Durable Identities And Message Semantics**: Pass. Outbox records carry durable envelopes and preserve source-module outgoing message semantics.
- **Package Boundaries And Public API Compatibility**: Pass with caution. The surface is broad public API guarded by public API baselines.
- **Persistence And Transport Ownership**: Pass. Provider-neutral contracts coordinate storage providers and transport dispatchers without owning either implementation.
- **Evidence-Based Verification**: Pass. Behavior is covered by focused unit tests for records, state, dispatch, failure policy, routing, aggregation, diagnostics, and inspection.

## Project Structure

### Documentation (this feature)

```text
specs/005-durable-outbox-persistence/
├── spec.md
├── plan.md
└── tasks.md
```

### Source Code

```text
src/Bondstone.Persistence/
├── Persistence/
│   ├── Contracts/
│   │   ├── IDurableEnvelopeDispatcher.cs
│   │   ├── IDurableEnvelopeDispatchRoute.cs
│   │   ├── IDurableOutboxClaimer.cs
│   │   ├── IDurableOutboxDispatchRecorder.cs
│   │   ├── IDurableOutboxDispatcher.cs
│   │   ├── IDurableOutboxFailurePolicy.cs
│   │   ├── IDurableOutboxInspectionStore.cs
│   │   ├── IDurableOutboxInspector.cs
│   │   ├── IDurableOutboxLeaseRenewer.cs
│   │   └── IDurableOutboxWriter.cs
│   ├── Outbox/
│   │   ├── DurableOutboxDispatchResult.cs
│   │   ├── DurableOutboxDispatchState.cs
│   │   ├── DurableOutboxDispatcher.cs
│   │   ├── DurableOutboxFailureDecision.cs
│   │   ├── DurableOutboxFailureDecisionKind.cs
│   │   ├── DurableOutboxFailurePolicy.cs
│   │   ├── DurableOutboxRecord.cs
│   │   ├── DurableOutboxStatus.cs
│   │   └── RoutedDurableEnvelopeDispatcher.cs
│   └── Resolution/
│       └── DurableModuleOutboxDispatchAggregator.cs
```

### Tests

```text
tests/Bondstone.Tests/Persistence/
├── DurableModuleOutboxDispatchAggregatorTests.cs
├── DurableOutboxDispatchResultTests.cs
├── DurableOutboxDispatchStateTests.cs
├── DurableOutboxDispatcherTests.cs
├── DurableOutboxFailureDecisionTests.cs
├── DurableOutboxFailurePolicyTests.cs
├── DurableOutboxInspectorTests.cs
├── DurableOutboxRecordTests.cs
└── RoutedDurableEnvelopeDispatcherTests.cs
```

**Structure Decision**: Keep this migration scoped to provider-neutral durable outbox behavior. EF Core and PostgreSQL persistence implementations are later migrations.

## Reconstructed Implementation Approach

### Phase 1: Provider-Neutral Contracts And Records

The feature defines public outbox writer, claim, lease renewal, dispatch
recording, dispatcher, failure policy, inspection, envelope dispatcher, and
route contracts. It also defines immutable record/state/result types that
validate UTC timestamps, attempt counts, claim leases, and outcome counts.

### Phase 2: Batch Dispatch Orchestration

`DurableOutboxDispatcher` validates dispatch arguments, starts an outbox batch
activity, claims records, records claimed metrics, renews each row lease before
sending, dispatches envelopes, records successful sends, schedules retries, or
marks terminal failures. Stale rows are counted when lease renewal or outcome
recording fails.

### Phase 3: Failure Policy

`DurableOutboxFailurePolicy` validates max attempts and retry delays, requires
processing records with positive attempt counts, normalizes failure reasons,
and chooses retry or terminal failure decisions. Retry delay selection uses the
attempt index and falls back to the final configured delay.

### Phase 4: Routing And Aggregation

`RoutedDurableEnvelopeDispatcher` finds all routes that can send a record and
requires exactly one match. Missing or ambiguous matches produce setup errors
with actionable messages. `DurableModuleOutboxDispatchAggregator` iterates
registered module dispatchers in order, passes remaining max count, aggregates
counts, and stops once the requested maximum is reached.

## Verification Strategy

- **Focused checks**:
  - `dotnet test tests/Bondstone.Tests/Bondstone.Tests.csproj --configuration Release --filter "Category=Unit"`
- **Default gate**: `pnpm check`
- **Package gate**: `pnpm backend:pack` when package metadata or public API baselines are affected.

## Complexity Tracking

> Fill only if Constitution Check has violations that must be justified.

No constitution violations were identified for the migrated feature.

## Gaps And Follow-Up Candidates

- This migration intentionally excludes EF Core and PostgreSQL outbox storage
  behavior; those should be migrated as provider-specific features.
- The public outbox surface is broad and compatibility-sensitive, including
  public implementation types that remain exposed for now.
- Focused diagnostics coverage exists for dispatcher activities and metrics,
  but public API and documentation should remain synchronized whenever
  diagnostic tag names or metric names change.
- `IDurableOutboxInspector` behavior is represented by focused tests, but
  provider-specific terminal-failure query ordering and filters belong to EF
  and PostgreSQL migrations.
