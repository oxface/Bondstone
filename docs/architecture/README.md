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

## Topic Docs

- [messaging.md](messaging.md) records durable command, message identity, and
  messaging-boundary rules.
- [persistence.md](persistence.md) records persistence-neutral outbox and inbox
  boundary rules.

## Related Docs

The initial package split is documented in [../packaging.md](../packaging.md).
The migration strategy for bringing source into this repository is documented
in [../extraction.md](../extraction.md).
Sample direction is documented in [../samples.md](../samples.md).
