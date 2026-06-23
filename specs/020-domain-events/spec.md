# Feature Specification: Domain Events

**Feature Branch**: `N/A - migrated existing functionality`

**Created**: 2026-06-23

**Status**: migrated

**Input**: Existing module-local domain event contracts and EF Core domain event persistence support in `src/Bondstone/DomainEvents` and `src/Bondstone.Persistence.EntityFrameworkCore/DomainEvents`

## Scope And Boundaries _(mandatory)_

**Bondstone Capability**: Module-local domain event contracts, domain event identity, EF Core domain event collection, optional domain event record mapping, and provider transaction proof.

**Affected Packages/Areas**:

- `src/Bondstone/DomainEvents`
- `src/Bondstone.Persistence.EntityFrameworkCore/DomainEvents`
- `src/Bondstone.Persistence.EntityFrameworkCore/Persistence/BondstoneDomainEventModelBuilderExtensions.cs`
- `src/Bondstone.Persistence.EntityFrameworkCore/Persistence/BondstoneEntityFrameworkCoreDomainEventModuleBuilderExtensions.cs`
- `tests/Bondstone.Tests/DomainEvents`
- `tests/Bondstone.Persistence.EntityFrameworkCore.Tests/DomainEvents`
- `tests/Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests/DomainEvents`

**Out Of Scope**:

- Automatic conversion of domain events to integration events.
- Durable outbox staging of domain events.
- Cross-module integration event publishing.

## User Scenarios & Testing _(mandatory)_

### User Story 1 - Define Module-Local Domain Events (Priority: P1)

As domain code, I want module-local domain event contracts and identity attributes so aggregates can expose local facts without implying durable integration publishing.

### User Story 2 - Collect Domain Events During EF Core Work (Priority: P2)

As EF Core module code, I want domain events collected from tracked entities inside the module transaction boundary.

### User Story 3 - Optionally Persist Domain Event Records (Priority: P3)

As a module with local audit needs, I want EF Core domain event record mapping and PostgreSQL transaction proof.

## Requirements _(mandatory)_

- **FR-001**: Domain events MUST be distinct from integration events.
- **FR-002**: Domain event identity MUST be explicit when persisted.
- **FR-003**: EF Core collector MUST gather domain events from tracked domain event sources.
- **FR-004**: Collector behavior MUST remain opt-in and module-local.
- **FR-005**: Domain event record mapping MUST preserve event id, module, type name, payload, and timestamps.
- **FR-006**: PostgreSQL tests MUST prove domain event records participate in the module transaction when enabled.

## Success Criteria _(mandatory)_

- **SC-001**: Unit tests prove domain event contract behavior.
- **SC-002**: EF Core tests prove collection and optional persistence mapping.
- **SC-003**: PostgreSQL tests prove transaction participation.

## Assumptions

- Publishing integration events from domain events is explicit application code.
