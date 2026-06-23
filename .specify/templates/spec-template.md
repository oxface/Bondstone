# Feature Specification: [FEATURE NAME]

**Feature Branch**: `[###-feature-name]`

**Created**: [DATE]

**Status**: Draft

**Input**: User description: "$ARGUMENTS"

## Scope And Boundaries _(mandatory)_

<!-- Replace every placeholder before finalizing the spec. Keep requirements
consumer-observable and implementation-agnostic unless the feature is a
brownfield migration spec. -->

**Bondstone Capability**: [module model | durable commands | integration events | domain events | persistence | hosting | local transport | RabbitMQ transport | Service Bus transport | observability | packaging | documentation | repository workflow]

**Affected Packages/Areas**:

- `src/[package]`
- `tests/[test-project]`
- `docs/[doc].md`
- `samples/[sample]`

**Out Of Scope**:

- [Explicitly list package/runtime/docs areas this feature will not change]

## User Scenarios & Testing _(mandatory)_

### User Story 1 - [Brief Title] (Priority: P1)

[Describe the maintainer or consumer journey in plain language]

**Why this priority**: [Explain why this is the first independently valuable slice]

**Independent Test**: [Command or observable behavior that proves this story works on its own]

**Acceptance Scenarios**:

1. **Given** [initial state], **When** [action], **Then** [expected outcome]
2. **Given** [initial state], **When** [action], **Then** [expected outcome]

---

### User Story 2 - [Brief Title] (Priority: P2)

[Describe this independently testable journey]

**Why this priority**: [Explain value and sequencing]

**Independent Test**: [Command or observable behavior]

**Acceptance Scenarios**:

1. **Given** [initial state], **When** [action], **Then** [expected outcome]

---

[Add more user stories as needed. Each story must be independently testable.]

### Edge Cases

- [Durable identity, missing binding, duplicate delivery, retry, stale state, or compatibility boundary]
- [Persistence, transport, diagnostics, or package-boundary failure mode]

## Requirements _(mandatory)_

### Functional Requirements

- **FR-001**: Bondstone MUST [specific observable capability]
- **FR-002**: Bondstone MUST [specific validation or failure behavior]
- **FR-003**: Bondstone MUST NOT [explicitly prohibited ownership or behavior]

### Compatibility And Public API

- **API-001**: [Public/protected API added, changed, removed, or explicitly unchanged]
- **API-002**: [Named parameter, package ID, namespace, or baseline expectation]

### Durable Semantics _(include when messaging, persistence, hosting, or transport changes)_

- **DS-001**: [Durable command, integration-event, domain-event, outbox, inbox, operation, or identity invariant]
- **DS-002**: [App-owned topology, migration, retry, dead-letter, retention, or monitoring boundary]

### Documentation Requirements

- **DOC-001**: [Consumer-facing doc, package README, operations doc, architecture doc, project profile, or GitHub issue update]

### Key Entities _(include if feature involves contracts, persistence, configuration, or transport state)_

- **[Entity/Contract]**: [Meaning, stable identity, relationships, and consumer-visible attributes]

## Success Criteria _(mandatory)_

### Measurable Outcomes

- **SC-001**: [Focused command or test category passes, e.g. `pnpm backend:test` or a package-specific `dotnet test`]
- **SC-002**: [Consumer-visible behavior, compatibility result, or docs outcome]
- **SC-003**: [Persistence/transport/diagnostics outcome when relevant]

## Assumptions

- [Assumption selected because the request did not specify it]
- [Dependency on existing package, sample, docs, or GitHub issue]

## Review Notes

- Constitution authorities: `.specify/memory/constitution.md`, `.specify/memory/project-profile.md`
- Architecture authority: `docs/architecture.md`
- Testing authority: `docs/testing.md`
- Packaging authority: `docs/packaging.md`
