# 0028 Domain Event Persistence Capability

Status: Proposed
Application: Not Applicable
Date: 2026-06-07

## Context

Bondstone's historical template source included DDD-oriented infrastructure
such as aggregate-root conventions and domain event capture. During extraction,
Bondstone has moved away from preserving a template-specific application model
and toward a library for durable module boundaries.

Domain events are still useful in modular monoliths. A module may want to
capture module-local facts raised by domain objects and persist those facts in
the same transaction as handler state. Those persisted domain events may later
support in-module dispatch, auditing, projections, or explicit mapping to
integration events.

The risk is forcing consumers to adopt Bondstone's DDD model. Consumers should
not have to inherit a Bondstone aggregate base class, implement an
`IAggregateRoot` concept, or accept automatic conversion of domain events into
integration events just to use durable module boundaries.

The better capability is narrower: collect and persist domain events inside
the module command transaction when the consumer opts in, using provider
abstractions that can adapt to the consumer's domain model.

## Decision

Bondstone should model domain event persistence as an optional module boundary
capability, not as a required DDD framework.

The capability should use narrow provider-neutral abstractions for collecting,
staging, and clearing or marking collected domain events. The exact names are
open. Avoid names that overfit a specific DDD model. Candidate concepts
include event source, event buffer, event accessor, collector, and store.

The core abstractions should let provider packages implement collection and
persistence without requiring core to depend on EF Core. For EF Core, the
provider implementation should collect domain events through `ChangeTracker`
or configured adapters and stage domain event records in the current module
transaction.

The behavior should compose through the module command pipeline instead of
hijacking `DbContext.SaveChangesAsync` or requiring consumers to inherit a
custom DbContext base class.

Domain events remain module-local. Bondstone must not automatically publish
all domain events as integration events. A later explicit mapping helper may
map selected domain event types to integration event publications, but it must
keep the publication decision visible in module code.

The first implementation should define transaction ordering carefully:

- how domain events are collected after handler execution;
- how persisted domain event records are staged before the transaction saves;
- when events are cleared or marked collected;
- how failures before or during save affect in-memory event buffers;
- how this behavior composes with validation, inbox handling, outbox staging,
  operation state, and EF transaction behavior.

## Consequences

Bondstone can support a common modular-monolith need without making every
consumer adopt a Bondstone aggregate model.

Provider-neutral collection and storage abstractions keep non-EF providers
possible. EF Core can provide a practical first collector/store over the
change tracker without moving EF dependencies into core.

Pipeline composition keeps domain event persistence explicit, testable, and
module-scoped. It avoids hidden behavior inside `SaveChangesAsync`.

The capability adds another module opt-in and validation surface. It should not
be implemented casually because transaction ordering and clear-on-success
semantics are subtle.

Integration-event mapping remains separate future work and should depend on
the first-class event model.

## Related Decisions

- [0004 Positioning And Service Extraction Path](0004-positioning-and-service-extraction-path.md)
- [0009 EF Core Persistence Entities And Migrations](0009-ef-core-persistence-entities-and-migrations.md)
- [0016 EF Core Persistence Scope](0016-ef-core-persistence-scope.md)
- [0025 Module Command Execution Boundary](0025-module-command-execution-boundary.md)
- [0026 Event Shape Guardrail](0026-event-shape-guardrail.md)
- [0027 Optional EF Core Persistence Mapping](0027-optional-ef-core-persistence-mapping.md)

## Application Notes

- Current contract: Proposed only. Bondstone currently does not provide
  domain event collection, persistence, dispatch, or integration-event mapping.
- Stable docs: Current persistence boundaries are described in
  [docs/architecture/persistence.md](../architecture/persistence.md) and
  [docs/architecture/persistence-ef-core.md](../architecture/persistence-ef-core.md).
  Messaging and event terminology is described in
  [docs/architecture/messaging.md](../architecture/messaging.md). Sequencing is
  tracked in [docs/mvp-plan.md](../mvp-plan.md).
- Agent guidance: Root [AGENTS.md](../../AGENTS.md) requires ADR review before
  public API, package-boundary, persistence, provider, durable behavior, or
  module runtime changes.
- Application evidence: None yet.
- Pending or deferred: Accept or revise this ADR, then apply core
  abstractions, provider implementation, module opt-in, stable docs, and
  tests. Integration-event mapping remains deferred until the event model is
  accepted.

## Verification

Read back the proposed ADR and related stable docs. No executable verification
is relevant for this proposal-only decision.
