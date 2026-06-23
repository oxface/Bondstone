# Implementation Plan: Local Transport

**Branch**: `N/A - migrated existing functionality` | **Date**: 2026-06-23 | **Spec**: [spec.md](spec.md)

**Input**: Migrated feature specification from `specs/local-transport/spec.md`

## Summary

Local transport is an existing Bondstone package that routes durable outbox
envelopes into in-process module receive pipelines for samples, tests, and
local development. It provides local queue topology configuration, command
route resolution, event subscriber fan-out, setup diagnostics, and integration
with durable inbox idempotency through existing receive pipelines.

## Technical Context

**Language/Version**: C# on `net10.0`

**Primary Dependencies**: `Bondstone`, `Bondstone.Persistence`, `Microsoft.Extensions.DependencyInjection.Abstractions`

**Storage**: Local transport owns no storage. Integration tests exercise durable inbox/outbox persistence through EF Core PostgreSQL packages.

**Testing**: xUnit unit and integration tests in `tests/Bondstone.Transport.Local.Tests`

**Target Platform**: Packable .NET library package for samples, tests, and local development

**Project Type**: Library package in a .NET monorepo

**Performance Goals**: No explicit performance target is documented for the migrated feature.

**Constraints**:

- Preserve local/dev/test positioning; do not present local transport as production broker durability.
- Preserve durable command and event semantics from `../../docs/architecture.md`.
- Preserve package dependency direction from `docs/packaging.md`.
- Preserve public API compatibility review for exposed builder APIs.

**Scale/Scope**:

- Source/docs: 16 files, about 771 lines under `src/Bondstone.Transport.Local`.
- Tests/docs: 5 files, about 887 lines under `tests/Bondstone.Transport.Local.Tests`.
- Total migrated scope: about 1,658 lines excluding ignored build output.

## Constitution Check

_GATE: Passed for migrated behavior based on current implementation evidence._

- **Library Boundary First**: Pass. The package is a library adapter for samples, tests, and local development, not an application framework or broker manager.
- **Durable Identities And Message Semantics**: Pass. Commands and integration events are routed separately; event subscribers carry explicit subscriber module and subscriber identity.
- **Package Boundaries And Public API Compatibility**: Pass with caution. The package exposes builder APIs and is packable; future API changes require public API review.
- **Persistence And Transport Ownership**: Pass. Local transport owns envelope routing only; durable inbox/outbox persistence remains in persistence packages and broker durability is explicitly out of scope.
- **Evidence-Based Verification**: Pass. Behavior is covered by focused unit tests plus PostgreSQL-backed integration tests.

## Project Structure

### Documentation (this feature)

```text
specs/local-transport/
├── spec.md
├── plan.md
└── tasks.md
```

### Source Code

```text
src/Bondstone.Transport.Local/
├── Bondstone.Transport.Local.csproj
├── README.md
├── AGENTS.md
├── Outbox/
│   ├── BondstoneLocalBuilderExtensions.cs
│   ├── BondstoneLocalServiceCollectionExtensions.cs
│   ├── LocalDurableEnvelopeDispatchRoute.cs
│   └── Topology/
└── Utility/StringExtensions.cs
```

### Tests

```text
tests/Bondstone.Transport.Local.Tests/
├── Bondstone.Transport.Local.Tests.csproj
├── README.md
├── AGENTS.md
├── LocalDurableEnvelopeDispatcherTests.cs
└── LocalTransportInboxPersistenceTests.cs
```

**Structure Decision**: Keep the migrated feature aligned with existing package and test project boundaries. No source movement is part of this migration.

## Reconstructed Implementation Approach

### Phase 1: Public Setup API

The feature exposes `UseLocalTransport(...)` from `BondstoneBuilder` and
`BondstoneOutboxBuilder`, builds a `BondstoneLocalTransportBuilder`, registers
local dispatch services, and marks the outbox transport as `Local`.

### Phase 2: Topology Configuration

The feature models local topology with command module routes, event message
routes, local queue registrations, accepted command modules, event subscriber
bindings, and optional queue naming conventions.

### Phase 3: Dispatch Route

The dispatch route checks whether an outbox record can be sent by local
transport, dispatches command envelopes through command receive pipelines, and
fans out event envelopes to matching subscriber receive pipelines.

### Phase 4: Durable Inbox Integration

Integration tests compose local transport with EF Core PostgreSQL persistence
and hosted outbox dispatch to prove inbox records are persisted, processed,
and treated idempotently on duplicate delivery.

## Complexity Tracking

> **Fill ONLY if Constitution Check has violations that must be justified**

No constitution violations were identified for the migrated feature.

## Gaps And Follow-Up Candidates

- Event queue convention behavior exists in source but lacks direct focused test coverage.
- Duplicate or conflicting route registration behavior exists in source but lacks focused tests.
- Required value normalization is centralized but lacks direct edge-case tests for all route/binding entrypoints.
- Unsupported message kind behavior is implemented in the route but not directly covered by a focused unit test.
