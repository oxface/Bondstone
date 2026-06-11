# Domain Events

Priority: High

Goal: add explicit module-local domain event behavior before validating
Bondstone on a real project.

Dependency: resolved by intentionally narrowing
[09-module-pipeline-and-capability-runtime.md](09-module-pipeline-and-capability-runtime.md)
before DE-03 adds runtime pipeline behavior.

## Why Now

Domain events are already a repeated pressure point in the architecture docs
and ADR history. Bondstone currently says they are module-local/private and
does not collect, persist, dispatch, or publish them automatically. That is a
safe boundary, but real modules often need a transactional way to record local
domain facts and optionally map selected facts to durable integration events.

This should be addressed before real-project adoption so aggregate and handler
patterns do not drift around an unstated domain-event convention.

## Scope

- Define the module-local domain event model without turning domain events into
  public integration events by default.
- Decide whether Bondstone needs:
  - a marker interface or neutral domain event record shape;
  - aggregate/domain object collection conventions;
  - EF Core collection through `ChangeTracker`;
  - non-EF/PostgreSQL explicit staging APIs;
  - optional persistence of domain event records;
  - explicit mapping from selected domain events to integration event
    publications.
- Keep integration event publishing explicit unless a later ADR accepts a
  mapping helper.
- Keep domain event behavior out of transport packages.

## Related ADRs

- [0026 Event Shape Guardrail](../adr/0026-event-shape-guardrail.md)
- [0028 Domain Event Persistence Capability](../adr/0028-domain-event-persistence-capability.md)
- [0033 First-Class Event Publish Subscribe Topology](../adr/0033-first-class-event-publish-subscribe-topology.md)

## Accepted Direction

DE-01 resolved on 2026-06-10 by accepting ADR 0028 in place. Bondstone will own
a small module-local domain event contract in core. Domain events are
transient module-local facts, and provider packages may optionally persist
module-local domain event records. Domain events are not integration events,
outbox messages, transport events, or public topology subjects.

The module pipeline and capability contribution decisions are resolved. DE-02
added core contracts without runtime behavior. DE-03 can add EF Core
collection/persistence behavior without introducing a public capability-step
registry or new package boundary:

- DE-02 adds core `IDomainEvent`, `DomainEventIdentityAttribute`, explicit
  source/accessor, and local handler contracts.
- DE-03 adds a narrow EF module opt-in plus EF Core `ChangeTracker`
  collection and optional module-local persistence records through
  provider-owned module transaction behavior.

Non-EF PostgreSQL staging stays application-owned for now. Mapping selected
domain events to integration events remains an explicit later slice and must
not become automatic publication.

## Deliverables

- ADR 0028 accepted in place with the module-local domain event contract.
- Stable docs updated for accepted domain-event behavior and implementation
  boundary.
- Core domain-event contracts.
- EF Core collection and optional persistence behavior.
- Tests proving domain events remain module-local unless explicitly mapped to
  integration events.

## Slices

1. Decision slice: resolved by ADR 0028 on 2026-06-10.
2. Core slice: add the smallest domain-event abstractions needed by handlers
   and aggregates without introducing transport publishing.
3. EF Core slice: collect and clear domain events transactionally when module
   persistence is EF-backed.
4. PostgreSQL slice: deferred; non-EF persistence stays application-owned until
   a concrete use case justifies provider APIs.
5. Mapping slice: add explicit domain-event-to-integration-event mapping only
   after the module-local boundary is tested.

## Implementation Backlog

### DE-01: Supersede Or Amend ADR 0028

Priority: P0
Status: Resolved 2026-06-10

Resolved by accepting ADR 0028 in place. ADR 0028 now defines the minimum
module-local contract before code work: Bondstone owns core domain event
contracts and local identities, domain events are transient facts with
optional provider-backed module-local persistence records, EF Core collection
uses `ChangeTracker` entries that implement an explicit source/accessor
contract, non-EF PostgreSQL staging remains application-owned, and
integration-event mapping is explicit future work.

Important files:

- `docs/adr/0028-domain-event-persistence-capability.md`
- `docs/architecture/messaging.md`
- `docs/architecture/persistence.md`
- `docs/architecture/persistence-ef-core.md`

Verification:

- `pnpm format:check`

### DE-02: Add Core Module-Local Domain Event Contracts

Priority: P0 if DE-01 accepts a Bondstone-owned contract.
Status: Resolved 2026-06-10
Dependency: none after the 2026-06-10 first pipeline cleanup slice. DE-02 must
not add runtime pipeline behavior.

Add the smallest core abstractions needed by aggregates and local handlers.
Keep the contracts module-local and do not publish them as integration events.

Accepted guidance:

- add an `IDomainEvent` marker in core;
- add `DomainEventIdentityAttribute` for stable module-local persisted record
  identity;
- add an explicit domain event source/accessor contract for aggregate roots or
  entities to expose pending events and clear them after successful
  collection;
- add an `IDomainEventHandler<TDomainEvent>`-style local handler contract
  constrained to `IDomainEvent`;
- place the contracts under `Bondstone.DomainEvents` because domain events
  are a DDD/domain-model concept rather than durable messaging or event
  delivery;
- keep the contracts independent from EF Core, transport packages, and durable
  message topology;
- do not register domain events in `MessageTypeRegistry`, add a `MessageKind`,
  wrap domain events in `DurableMessageEnvelope`, or treat local identities as
  transport topology names;
- do not add a Bondstone aggregate base class, generic mediator, generic bus,
  automatic discovery, outbox publication, or cross-module subscription.

Candidate files:

- `src/Bondstone/DomainEvents`
- `tests/Bondstone.Tests/DomainEvents`

Verification:

- `pnpm backend:build`
- `pnpm backend:test:fast`

Resolved by adding the core module-local contracts in `Bondstone`:
`IDomainEvent`, `DomainEventIdentityAttribute`, `IDomainEventSource`, and
`IDomainEventHandler<TDomainEvent>` under `Bondstone.DomainEvents`.
`IDomainEvent` does not extend the messaging `IMessage` marker. Domain events
are not registered in `MessageTypeRegistry`, do not add a `MessageKind`, and
are not wrapped in `DurableMessageEnvelope`. No runtime collection,
persistence, publication, discovery, outbox/inbox, EF Core, transport, or
integration-event mapping behavior was added in this slice.

### DE-03: Add EF Core Collection And Clearing

Priority: P0 if DE-01 accepts EF-backed collection.
Status: Resolved 2026-06-11
Dependency: [09-module-pipeline-and-capability-runtime.md](09-module-pipeline-and-capability-runtime.md)
MPC-05 and MPC-06 are resolved; keep the implementation inside the accepted
provider-owned capability model.

Collect domain events through EF Core module persistence in the same handler
transaction, then clear them after successful staging, save, and transaction
commit according to ADR 0028. Keep transaction ownership aligned with existing
module transaction behaviors.

Accepted guidance:

- collect through `DbContext.ChangeTracker` entries whose entities implement
  the explicit domain event source/accessor contract;
- add only the smallest EF-owned module opt-in metadata or options needed to
  activate domain event persistence for selected EF-backed modules;
- do not add a public capability-step registry, public named pipeline slots,
  generalized provider metadata registry, or separate
  `Bondstone.DomainEvents` package;
- stage immutable module-local domain event records in the same module
  `DbContext` transaction as handler state, inbox markers, operation-state
  updates where applicable, and outgoing outbox rows;
- include stable record id, owning module, `DomainEventIdentityAttribute`
  name, timestamps, serialized payload, payload metadata, and trace or
  causation metadata when available;
- place collection/staging inside the EF transaction, after application
  behavior and handler logic, while the module execution context is active,
  and before transaction-owned `SaveChangesAsync` and commit;
- clear source pending events only after successful collection, staging, save,
  and transaction commit;
- do not intercept `SaveChangesAsync`, require a custom DbContext base class,
  scan arbitrary method names, or publish domain events automatically;
- document and test failure behavior so collection, staging, save, or commit
  failures do not mark domain events as durably handled.

Candidate files:

- `src/Bondstone.EntityFrameworkCore/Persistence`
- `src/Bondstone.EntityFrameworkCore/Mapping`
- `tests/Bondstone.EntityFrameworkCore.Tests/Persistence`
- `tests/Bondstone.EntityFrameworkCore.Tests/Mapping`

Verification:

- `pnpm backend:test:fast`
- `pnpm backend:test:integration` if provider-backed SQL behavior changes.

Resolved by adding EF-owned domain event persistence in
`Bondstone.EntityFrameworkCore`. Modules opt in with
`UseEntityFrameworkCoreDomainEventPersistence()` after declaring EF module
persistence. DbContexts map records with `ApplyBondstoneDomainEvents()` or the
full `ApplyBondstonePersistence()` helper. Runtime collection is an EF system
behavior that runs inside command and event subscriber execution after
application behavior and handler logic while the module execution context is
active. It stages `DomainEventRecordEntity` rows from tracked
`IDomainEventSource` entities before the EF transaction runner saves and
commits, then clears sources only after the transaction scope succeeds.

Tests cover EF-backed opt-in activation, no-op behavior for modules that are
not opted in or not EF-backed, collection from `IDomainEventSource`, clearing
after successful save/transaction completion, no clear on collection or save
failure, and both command and event subscriber paths.

Deferred from this slice: automatic integration-event mapping/publication,
local domain-event handler dispatch, non-EF PostgreSQL staging, broad
capability pipeline APIs, and provider-backed SQL migration or concurrency
coverage beyond the provider-neutral EF mapping.

### DE-04: Decide Non-EF PostgreSQL Staging

Priority: P2.

ADR 0028 keeps `Bondstone.Persistence.Postgres` domain event staging
application-owned for now. Reopen this only when a concrete non-EF use case
needs library-owned collection or staging APIs.

Candidate files:

- `src/Bondstone.Persistence.Postgres`
- `tests/Bondstone.Persistence.Postgres.Tests`
- `docs/architecture/persistence-postgres.md`

Verification:

- `pnpm backend:test:fast`
- `pnpm backend:test:integration` if SQL behavior changes.

### DE-05: Explicit Mapping To Integration Events

Priority: P1 after DE-02 and DE-03.

Add optional mapping only if real modules need selected domain events to stage
durable integration events after DE-02 and DE-03 prove the module-local
boundary. Mapping must stay explicit and must not turn every domain event into
a public event automatically.

Candidate files:

- `src/Bondstone/Messaging/Publishing`
- `src/Bondstone.EntityFrameworkCore/Persistence`
- `tests/Bondstone.Tests/Messaging`

Verification:

- `pnpm backend:build`
- `pnpm backend:test:fast`

## Verification

- `pnpm backend:test:fast`
- `pnpm backend:test:integration` if provider persistence behavior changes.
