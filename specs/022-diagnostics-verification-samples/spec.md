# Feature Specification: Diagnostics, Verification, And Samples

**Feature Branch**: `N/A - migrated existing functionality`

**Created**: 2026-06-23

**Status**: migrated

**Input**: Existing diagnostics helpers, public API/package verification tests, and sample adoption proofs across `src`, `tests`, and `samples`

## Scope And Boundaries _(mandatory)_

**Bondstone Capability**: OpenTelemetry-oriented diagnostics, package/public API verification, package artifact checks, and sample adoption proofs that validate Bondstone package composition in realistic scenarios.

**Affected Packages/Areas**:

- `src/Bondstone/Messaging/BondstoneMessagingDiagnostics.cs`
- `src/Bondstone.Persistence/Persistence/BondstonePersistenceDiagnostics.cs`
- `src/Bondstone.Persistence/Diagnostics`
- `tests/Bondstone.Tests/Diagnostics`
- `tests/Bondstone.PublicApi.Tests`
- `tests/Bondstone.Package.Tests`
- `tests/Bondstone.Samples.Tests`
- `samples`
- `docs/testing.md`
- `docs/samples.md`
- `docs/packaging.md`

**Out Of Scope**:

- Feature-specific business behavior already covered by dedicated migrations.
- Publishing packages or changing release metadata.
- Broker topology provisioning or application-owned monitoring.

## User Scenarios & Testing _(mandatory)_

### User Story 1 - Emit Low-Cardinality Diagnostics (Priority: P1)

As an operator, I want activities and metrics that identify module/package behavior without leaking high-cardinality message ids, operation ids, payloads, or broker internals.

### User Story 2 - Guard Public API And Package Artifacts (Priority: P2)

As a maintainer, I want public API baselines and package artifact checks so packable packages remain compatible and complete.

### User Story 3 - Prove Sample Adoption Paths (Priority: P3)

As a consumer, I want sample tests that prove modular monolith and broker-backed adoption paths compose with the current packages.

## Requirements _(mandatory)_

- **FR-001**: Diagnostics MUST prefer OpenTelemetry Activity/Meter APIs.
- **FR-002**: Diagnostics MUST avoid high-cardinality ids, payloads, exception messages, and topology details as tags.
- **FR-003**: Public API tests MUST compare packable package public/protected surface to checked-in baselines.
- **FR-004**: Package tests MUST inspect produced package artifacts after packing.
- **FR-005**: Sample tests MUST exercise realistic package composition and durable messaging flows.
- **FR-006**: Integration sample tests MUST remain out of the default fast test filter when they require infrastructure.

## Success Criteria _(mandatory)_

- **SC-001**: Unit tests prove diagnostic activity/metric shapes.
- **SC-002**: Public API baseline tests protect packable package surface.
- **SC-003**: Package artifact tests inspect produced `.nupkg` outputs.
- **SC-004**: Sample tests prove modular monolith and broker adapter scenarios.

## Assumptions

- Publishing and release tagging are explicit maintainer actions, not test behavior.
