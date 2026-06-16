# 0028 Domain Event Persistence Capability

Status: Archived
Application: Not Applicable
Date: 2026-06-10

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

The better capability is narrower: give modules a small Bondstone-owned
contract for module-local domain facts, then let provider packages collect and
optionally persist those facts inside the module transaction when the consumer
opts in.

## Decision

Bondstone will own a small module-local domain event abstraction in core. This
is a module-boundary contract, not a DDD framework and not another durable
message kind.

The first core contract should include:

- `IDomainEvent`, a marker for module-local facts that may extend the neutral
  `IMessage` marker for payload serialization but is not a durable message
  kind;
- `DomainEventIdentityAttribute`, a stable module-local identity for persisted
  domain event records;
- a source/accessor interface for domain objects that expose pending domain
  events and can clear them after successful collection;
- `IDomainEventHandler<TDomainEvent>` or an equivalent local handler contract
  for module-local handlers, constrained to `IDomainEvent`.

Domain events must not be registered in `MessageTypeRegistry`, represented by
`MessageKind`, wrapped in `DurableMessageEnvelope`, written to the outgoing
outbox, registered as module published events, or registered as integration
event subscribers.

Domain event source contracts must not require inheritance from a Bondstone
aggregate base class, an `IAggregateRoot` abstraction, or EF Core entity base
type. Consumer aggregate roots and entities may implement the source/accessor
interface directly.

Domain events are both transient and optionally persistable:

- the `IDomainEvent` instance is the transient in-memory fact raised by module
  domain code;
- provider packages may stage immutable domain event records for auditing,
  local projections, later in-module dispatch, or explicit mapping to
  integration events;
- persisted domain event records are module-local records, not outbox
  messages, not inbox messages, and not transport publications.

The persisted record shape should be narrow and provider-neutral enough for EF
Core first: stable record id, owning module, `DomainEventIdentityAttribute`
name, occurred/captured timestamps, serialized payload, payload metadata, and
trace or causation metadata when available. Domain event identities are local
to the owning module and must not be used as transport topology names.

EF Core is the first provider implementation. EF-backed module persistence
will collect domain events through the `DbContext.ChangeTracker` by looking
for tracked entities that implement the explicit domain event source/accessor
interface. The EF collector will stage configured domain event records in the
same module `DbContext` transaction after the command or integration event
subscriber handler completes and before the transaction-owned
`SaveChangesAsync`.

The EF behavior must compose through the module command and event subscriber
pipeline. It must not hijack `DbContext.SaveChangesAsync`, require consumers to
inherit a custom DbContext base class, scan arbitrary method names, or publish
events from EF interceptors.

Collection and clearing semantics are part of the provider behavior:

- collect pending domain events after application handlers and application
  pipeline behaviors have run;
- stage all accepted domain event records before the module transaction saves;
- clear the source's pending domain events only after staging, save, and
  transaction commit succeed;
- if collection, staging, save, or transaction commit fails, do not mark the
  domain events as durably handled beyond what the consumer's in-memory object
  has already done;
- preserve existing ordering around validation, receive-side inbox handling,
  operation state, outgoing outbox staging, and EF transaction behavior.

`Bondstone.Persistence.Postgres` does not get a domain event staging API in
this decision. Non-EF PostgreSQL modules keep domain event collection and
staging application-owned until a concrete non-EF use case justifies a small
provider-specific contract.

Domain events remain private to the owning module unless module code
explicitly maps selected domain events to integration events. Bondstone must
not automatically publish all domain events as integration events. A later
mapping helper may reduce ceremony, but it must preserve the visible step
where module-local state becomes a durable public `IIntegrationEvent`
published through `IDurableEventPublisher`.

This decision explicitly does not introduce:

- a mediator, generic in-process bus, or public event bus;
- automatic cross-module publishing;
- transport package behavior;
- event sourcing, replay, global ordering, or a global event log;
- non-EF provider staging;
- broad migration helpers;
- automatic discovery from CLR names or handler names;
- public external wire formats for domain events.

## Consequences

Bondstone can support a common modular-monolith need without making every
consumer adopt a Bondstone aggregate model.

The core contract is small public API and must be treated as
compatibility-sensitive. Future code work that changes the names, visibility,
or package placement of `IDomainEvent`, `DomainEventIdentityAttribute`, the
source/accessor contract, the handler contract, or persisted record contracts
must go through ADR language before implementation.

EF Core can provide a practical first collector/store over the change tracker
without moving EF dependencies into core.

Pipeline composition keeps domain event persistence explicit, testable, and
module-scoped. It avoids hidden behavior inside `SaveChangesAsync`.

The capability adds another module opt-in, persisted table shape, mapping
helper, and validation surface. It should not be implemented casually because
transaction ordering and clear-on-success semantics are subtle.

Integration-event mapping remains separate future work and should depend on
the first-class event model and the EF collection proof.

## Amendments

### 2026-06-10: Domain Events Are Not Messaging Contracts

The core domain event contracts live under the `Bondstone.DomainEvents`
namespace and do not extend `IMessage`. Domain events are a DDD/domain-model
concept, not durable messaging or event-delivery contracts.

This narrows the original allowance that `IDomainEvent` may extend the neutral
`IMessage` marker. Persistence providers that collect or serialize domain
events must use the domain event contracts directly instead of routing them
through durable message identity, registry, envelope, topology, inbox, outbox,
or transport abstractions.

### 2026-06-11: EF Capability Activation And Package Boundary

Domain event contracts remain in the existing `Bondstone` core package under
the `Bondstone.DomainEvents` namespace. Do not create a new
`Bondstone.DomainEvents` package in this slice. A separate package can be
reconsidered only when there is a concrete dependency, versioning, or adoption
problem that cannot be solved by the current core contracts plus
provider-owned runtime behavior.

EF Core remains the first provider-owned runtime implementation. Domain event
persistence should activate through a small combination of module opt-in,
provider registration, and DI:

- the module declares EF Core persistence;
- the module explicitly opts into domain event persistence;
- the EF package registers the provider-owned runtime behavior and services;
- the behavior no-ops for modules that are not EF-backed or not opted in.

This does not introduce a public capability-step registry, public named
pipeline slots, a generic domain event runtime package, or a generalized
provider metadata registry. The DE-03 implementation may add the smallest
module-scoped EF opt-in metadata or options needed to make activation
observable and testable, but that metadata should remain specific to EF domain
event persistence unless a broader API decision is accepted.

The EF runtime placement is fixed for DE-03. Collection and staging happen
inside module command execution and module integration event subscriber
execution, inside the EF transaction, after application pipeline behavior and
handler logic, while the module execution context is still active, and before
the EF transaction owner calls `SaveChangesAsync` and commits. Receive inbox
handling and operation-state updates stay in the same transaction boundary.
Pending events are cleared from `IDomainEventSource` instances only after
collection, staging, `SaveChangesAsync`, and transaction commit all succeed.
Collection, staging, save, or commit failure must leave source pending events
uncleared by Bondstone.

### 2026-06-11: Explicit EF Mapping And Non-EF PostgreSQL Boundary

EF domain event persistence remains explicit at both runtime and mapping time.
`UseEntityFrameworkCoreDomainEventPersistence()` opts an EF-backed module into
collection and staging, and the module DbContext must map the record shape
with `ApplyBondstoneDomainEvents()`. `ApplyBondstonePersistence()` remains the
durable EF mapping bundle for outbox, inbox, and operation state only; it must
not implicitly include domain event records.

DE-04 does not reopen non-EF PostgreSQL staging. `Bondstone.Persistence.Postgres`
has a module transaction/session boundary but no EF-style change tracker or
provider-owned pending-domain-event source registry. Library-owned non-EF
staging would require a new public staging API, explicit application calls, or
a provider transaction hook, plus provider contracts, schema and migration
policy, and clear-on-commit semantics. Without a concrete non-EF use case,
domain event staging in `Bondstone.Persistence.Postgres` remains
application-owned.

### 2026-06-11: Domain Event Contracts Move To Their Own Package

ADR 0051 supersedes the earlier package-placement rule that kept domain event
contracts inside the `Bondstone` core package. The small domain event contract
surface now lives in the separate `Bondstone.DomainEvents` package while
keeping the `Bondstone.DomainEvents` namespace.

This does not create a public domain event bus or provider runtime package.
EF Core collection and persistence remain provider-owned behavior in
`Bondstone.Persistence.EntityFrameworkCore`, and non-EF PostgreSQL staging
remains application-owned.

### 2026-06-11: Domain Events Are An Optional Capability Package

ADR 0052 supersedes ADR 0051's `Bondstone.DomainEvents` package placement.
The domain event contract surface now lives in
`Bondstone.Capabilities.DomainEvents` under the
`Bondstone.Capabilities.DomainEvents` namespace.

The EF implementation is no longer part of base EF persistence. EF domain
event collection, record mapping, module opt-in, and pipeline behavior live in
`Bondstone.Capabilities.DomainEvents.EntityFrameworkCore`. The base
`Bondstone.Persistence.EntityFrameworkCore` package must not reference domain
event capability packages.

This keeps domain events as optional Bondstone utility functionality rather
than core durable messaging or persistence semantics.

### 2026-06-16: Domain Events In Core And EF Persistence

ADR 0058 supersedes ADR 0052's capability-package placement. The active
domain event contracts now live in the core `Bondstone` package under the
`Bondstone.DomainEvents` namespace:

- `IDomainEvent`;
- `DomainEventIdentityAttribute`;
- `IDomainEventSource`.

`IDomainEventHandler<TDomainEvent>` is removed from the active public API
until a concrete local-dispatch model is accepted by a future ADR. EF-backed
domain event collection, record mapping, module opt-in, and persistence now
live in `Bondstone.Persistence.EntityFrameworkCore`.

## Related Decisions

- [0004 Positioning And Service Extraction Path](0004-positioning-and-service-extraction-path.md)
- [0009 EF Core Persistence Entities And Migrations](0009-ef-core-persistence-entities-and-migrations.md)
- [0016 EF Core Persistence Scope](0016-ef-core-persistence-scope.md)
- [0025 Module Command Execution Boundary](0025-module-command-execution-boundary.md)
- [0026 Event Shape Guardrail](0026-event-shape-guardrail.md)
- [0027 Optional EF Core Persistence Mapping](0027-optional-ef-core-persistence-mapping.md)
- [0033 First-Class Event Publish/Subscribe Topology](0033-first-class-event-publish-subscribe-topology.md)
- [0051 Package Boundary Split](0051-package-boundary-split.md)
- [0052 Domain Event Capability Package Boundary](0052-domain-event-capability-package-boundary.md)
- [0058 Domain Events In Core And EF Persistence](0058-domain-events-in-core-and-ef-persistence.md)

## Application Notes

- Current contract: Bondstone owns a small module-local `IDomainEvent`
  contract, `DomainEventIdentityAttribute`, and `IDomainEventSource` in the
  core `Bondstone` package under the `Bondstone.DomainEvents` namespace.
  `IDomainEvent` does not extend the messaging `IMessage` marker. EF Core is
  the first provider-backed implementation for optional collection and
  persistence, using `ChangeTracker` plus `IDomainEventSource`. EF domain
  event persistence activates only when EF-backed modules explicitly opt into
  it through `UseEntityFrameworkCoreDomainEventPersistence()` and map the
  record shape with `ApplyBondstoneDomainEvents()`.
  `ApplyBondstonePersistence()` remains the durable EF mapping bundle for
  outbox, inbox, and operation state. Non-EF PostgreSQL domain event staging
  remains application-owned. Domain events stay module-local unless module
  code explicitly maps selected facts to integration events.
- Stable docs: Current domain event direction is described in
  [docs/architecture/messaging.md](../architecture/messaging.md),
  [docs/architecture/persistence.md](../architecture/persistence.md), and
  [docs/architecture/persistence-ef-core.md](../architecture/persistence-ef-core.md).
  The non-EF PostgreSQL boundary is described in
  [docs/architecture/persistence-postgres.md](../architecture/persistence-postgres.md).
  Remaining Domain Events follow-up ideas are tracked in
  [docs/backlog/00-plans.md](../backlog/00-plans.md).
- Agent guidance: Root [AGENTS.md](../../AGENTS.md) requires ADR review before
  public API, package-boundary, persistence, provider, durable behavior, or
  module runtime changes.
- Application evidence: Stable docs and backlog guidance are updated. Domain
  event contracts and contract tests are implemented through `Bondstone`. EF
  collection, explicit EF mapping, persisted record mapping, runtime opt-in
  behavior, and transaction-clear tests are implemented in
  `Bondstone.Persistence.EntityFrameworkCore` and its PostgreSQL test suite.
- Pending or deferred: Non-EF PostgreSQL staging and integration-event mapping
  remain deferred until a later accepted decision narrows or reopens them.

## Verification

2026-06-11 DE-04 amendment: read back this ADR and affected stable
docs/backlog entries. Ran `pnpm format:check`, `pnpm backend:build`, and
`pnpm backend:test:fast`. Did not run `pnpm backend:test:integration` because
the non-EF PostgreSQL SQL/schema contract did not change.

2026-06-11 amendment: read back this ADR and affected stable docs/backlog
entries. Ran `pnpm format:check`.

Read back the accepted ADR and related stable docs. Ran `pnpm format:check`.
