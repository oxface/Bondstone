# Architecture

Bondstone is a .NET library for durable module boundaries.

## Positioning

Bondstone supports modular monoliths first, including a first-class path for
splitting a module into a separately deployed service when that module needs
independent scalability, deployment, or operational isolation.

Bondstone should also be usable in microservice setups that require internal
durability, stable message identities, local transactional outbox processing,
receive-side inbox deduplication, and transport adapter integration.

The intended continuity matters: a team should not need to throw away module
contracts, message identities, outbox/inbox behavior, or handler patterns just
because a module moves from in-process composition to a separate service.

## Durable Boundary Principles

- Module boundaries are explicit in code and persistence.
- Durable message identities are stable and not derived from CLR names.
- Source state and outgoing durable messages commit atomically.
- Receive-side handlers are protected by an inbox boundary.
- Transport adapters connect module boundaries without owning domain behavior.
- The same message contracts and handler patterns should survive service
  extraction when practical.
- Service extraction should be a supported evolution path, not a separate
  rewrite story.

## Package Architecture

The initial package split is documented in [packaging.md](packaging.md).
The migration strategy for bringing source into this repository is documented
in [extraction.md](extraction.md).

## Samples And Verification

Samples and tests should eventually include at least one scenario that starts
as a modular monolith and demonstrates the intended service extraction shape.

Sample direction is documented in [samples.md](samples.md). Aspire is the
preferred local sample orchestration host, while exact sample names, broker
support, and deployment packaging remain undecided.

## Application State

This architecture direction is accepted and documented. Source extraction,
samples, and service-split verification remain future application work.
