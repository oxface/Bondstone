# Feature Specification: Configuration And Composition Validation

**Feature Branch**: `N/A - migrated existing functionality`

**Created**: 2026-06-23

**Status**: migrated

**Input**: Existing Bondstone builder, service registration, durable messaging validation, and composition tests in `src/Bondstone/Configuration`

## Scope And Boundaries _(mandatory)_

**Bondstone Capability**: Root Bondstone service composition, builder APIs, durable payload options, outbox/persistence configuration validation, module persistence validation, and composition smoke tests.

**Affected Packages/Areas**:

- `src/Bondstone/Configuration`
- `tests/Bondstone.Tests/Configuration`
- `tests/Bondstone.Composition.Tests`
- `docs/architecture.md`
- `docs/packaging.md`

**Out Of Scope**:

- Provider-specific EF/PostgreSQL service registration internals.
- Runtime module execution behavior after composition.
- Package artifact creation.

## User Scenarios & Testing _(mandatory)_

### User Story 1 - Compose Bondstone Services (Priority: P1)

As an application host, I want `AddBondstone` and builder APIs to register core services consistently.

### User Story 2 - Validate Durable Messaging Setup (Priority: P2)

As an application host, I want setup validation to catch missing durable identities, routes, persistence providers, and module persistence registrations.

### User Story 3 - Configure Payload JSON (Priority: P3)

As an application host, I want durable payload JSON options to be configurable through DI.

## Requirements _(mandatory)_

- **FR-001**: `AddBondstone` MUST register core module, messaging, persistence resolver, and observation services.
- **FR-002**: Builder APIs MUST support root and module configuration.
- **FR-003**: Durable messaging validation MUST catch invalid or incomplete setup before runtime use.
- **FR-004**: Module persistence validation MUST require stores for durable modules where needed.
- **FR-005**: Payload JSON options MUST be configurable once through service registration.
- **FR-006**: Composition tests MUST prove default service graph construction.

## Success Criteria _(mandatory)_

- **SC-001**: Unit tests prove builder/service registration behavior.
- **SC-002**: Unit tests prove validation diagnostics.
- **SC-003**: Composition tests prove core service graph builds.

## Assumptions

- Provider packages extend setup through their own migrations.
