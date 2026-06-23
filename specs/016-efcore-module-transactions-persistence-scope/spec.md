# Feature Specification: EF Core Module Transactions And Persistence Scope

**Feature Branch**: `N/A - migrated existing functionality`

**Created**: 2026-06-23

**Status**: migrated

**Input**: Existing EF Core persistence-scope and module transaction implementation in `src/Bondstone.Persistence.EntityFrameworkCore/Persistence`, with package tests under `tests/Bondstone.Persistence.EntityFrameworkCore.Tests/Persistence`

## Scope And Boundaries _(mandatory)_

**Bondstone Capability**: Generic EF Core transaction runner, persistence scope, module runtime registration, model-builder extension aggregation, and service registration for module-owned durable persistence.

**Affected Packages/Areas**:

- `src/Bondstone.Persistence.EntityFrameworkCore/Persistence`
- `src/Bondstone.Persistence.EntityFrameworkCore/Persistence/Contracts`
- `tests/Bondstone.Persistence.EntityFrameworkCore.Tests/Persistence`
- `docs/architecture.md`
- `docs/testing.md`

**Out Of Scope**:

- Individual outbox, inbox, incoming inbox, and operation-state store implementations already migrated separately.
- PostgreSQL provider-specific setup and transaction behavior.
- Domain event collection and EF domain event persistence.

## User Scenarios & Testing _(mandatory)_

### User Story 1 - Execute Work In EF Core Persistence Scope (Priority: P1)

As runtime code, I want an EF Core persistence scope so operations can run inside a DbContext save boundary and transaction runner.

### User Story 2 - Register Module EF Core Persistence (Priority: P2)

As a module composer, I want module EF Core persistence registration so module runtime descriptors know which DbContext owns module durable persistence.

### User Story 3 - Validate Required EF Core Mappings (Priority: P3)

As an application setup author, I want clear errors when a module DbContext misses required Bondstone mappings.

## Requirements _(mandatory)_

- **FR-001**: EF Core persistence scope MUST expose save behavior through the current DbContext.
- **FR-002**: Module transaction runner MUST execute module runtime work inside EF Core transaction behavior.
- **FR-003**: Module persistence setup MUST record module provider name and context type.
- **FR-004**: Runtime mapping validation MUST detect missing outbox, inbox, incoming inbox, or operation-state mappings when required.
- **FR-005**: Generic EF Core setup MUST compose with module-specific runtime registrations without registering unrelated root services.
- **FR-006**: Tests MUST distinguish EF Core InMemory behavior from provider-backed PostgreSQL semantics.

## Success Criteria _(mandatory)_

- **SC-001**: Application tests prove persistence scope calls `SaveChanges`.
- **SC-002**: Application tests prove module transaction behavior wraps handler execution.
- **SC-003**: Unit/application tests prove missing mapping diagnostics.
- **SC-004**: Setup tests prove module EF Core persistence registrations.

## Assumptions

- Store-specific behavior is owned by separate persistence migrations.
- Real database transaction semantics are proven in PostgreSQL migrations.
