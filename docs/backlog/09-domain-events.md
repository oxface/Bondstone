# Domain Events

Priority: High

Goal: add explicit module-local domain event behavior before validating
Bondstone on a real project.

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

## Candidate Deliverables

- Update or supersede ADR 0028 if the accepted domain-event design changes.
- Stable docs for current domain-event behavior.
- Core domain-event contracts if accepted.
- EF Core behavior if accepted.
- Non-EF PostgreSQL behavior if accepted.
- Tests proving domain events remain module-local unless explicitly mapped to
  integration events.

## Proposed Slices

1. Decision slice: resolve whether domain events are only a documented
   application convention or a Bondstone-owned module-local contract.
2. Core slice: if accepted, add the smallest domain-event abstractions needed
   by handlers and aggregates without introducing transport publishing.
3. EF Core slice: collect and clear domain events transactionally when module
   persistence is EF-backed.
4. PostgreSQL slice: decide whether non-EF persistence needs explicit staging
   APIs or should stay application-owned.
5. Mapping slice: add explicit domain-event-to-integration-event mapping only
   after the module-local boundary is tested.

## Verification

- `pnpm backend:test:fast`
- `pnpm backend:test:integration` if provider persistence behavior changes.
