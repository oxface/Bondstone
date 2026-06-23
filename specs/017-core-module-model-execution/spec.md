# Feature Specification: Core Module Model And Execution

**Feature Branch**: `N/A - migrated existing functionality`

**Created**: 2026-06-23

**Status**: migrated

**Input**: Existing module registration, routing, execution, validator, query, command, and subscriber runtime in `src/Bondstone/Modules`, with focused tests in `tests/Bondstone.Tests/Modules`

## Scope And Boundaries _(mandatory)_

**Bondstone Capability**: Core modular monolith model, module registration, command/query routing, command/query execution, validator execution, receive pipelines, event subscriber registration/execution, runtime registry, and module execution context.

**Affected Packages/Areas**:

- `src/Bondstone/Modules`
- `tests/Bondstone.Tests/Modules`
- `docs/architecture.md`
- `docs/testing.md`

**Out Of Scope**:

- Durable outbox persistence, transport dispatch, broker adapters, and hosted workers.
- Provider-specific EF/PostgreSQL persistence.
- Domain event persistence and EF collection.

## User Scenarios & Testing _(mandatory)_

### User Story 1 - Register Modules And Routes (Priority: P1)

As an application composer, I want modules to register commands, queries, published events, and subscribers with stable module names and identities.

### User Story 2 - Execute In-Process Commands And Queries (Priority: P2)

As module code, I want immediate command/query execution inside module boundaries with validators and module execution context.

### User Story 3 - Execute Durable Receives Through Pipelines (Priority: P3)

As durable transport/runtime code, I want command and event receive pipelines that deserialize envelopes, resolve routes/subscribers, apply direct inbox handling, and return receive outcomes.

## Requirements _(mandatory)_

- **FR-001**: Module names MUST be normalized and unique.
- **FR-002**: Command, query, published-event, and subscriber registrations MUST validate durable identities where applicable.
- **FR-003**: Command routes MUST resolve by module and command type/message type.
- **FR-004**: Query routes MUST resolve by module and query type.
- **FR-005**: Command validators MUST run before handlers.
- **FR-006**: Module execution context MUST flow during handler execution.
- **FR-007**: Receive pipelines MUST deserialize durable envelopes and invoke the correct module runtime.
- **FR-008**: Direct inbox duplicate outcomes MUST skip handler execution where provider-neutral direct inbox says so.
- **FR-009**: Missing routes, duplicate routes, and invalid identities MUST produce clear setup/runtime diagnostics.

## Success Criteria _(mandatory)_

- **SC-001**: Unit tests prove module registration and duplicate validation.
- **SC-002**: Unit tests prove command/query execution and validator behavior.
- **SC-003**: Unit tests prove receive pipelines and subscriber execution.

## Assumptions

- Durable send/publish staging is migrated separately.
- Persistence providers are separate feature slices.
