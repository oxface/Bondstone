# 0004 Positioning And Service Extraction Path

Status: Accepted
Application: Partially Applied
Date: 2026-06-03

## Context

Bondstone starts from modular-monolith infrastructure: explicit module
boundaries, in-process command handling, EF Core backed module persistence,
stable message identities, durable outbox/inbox behavior, and transport
adapters.

The library should remain useful when a modular monolith grows and one module
needs independent scalability, deployment, or operational isolation. It should
also be usable in microservice setups that need internal durability, even when
they did not start as a modular monolith.

The important design pressure is continuity. A team should not need to throw
away module contracts, message identities, outbox/inbox behavior, or handler
patterns just because a module moves from in-process composition to a separate
service.

## Decision

Position Bondstone as a library for durable module boundaries.

Bondstone should support modular monoliths first, including a first-class path
for splitting a module into a separately deployed service when that module
needs independent scalability or deployment.

Bondstone should also be usable in microservice setups that require internal
durability, stable message identities, local transactional outbox processing,
receive-side inbox deduplication, and transport adapter integration.

Design documentation, samples, tests, and future APIs should preserve these
principles:

- Module boundaries are explicit in code and persistence.
- Durable message identities are stable and not derived from CLR names.
- Source state and outgoing durable messages commit atomically.
- Receive-side handlers are protected by an inbox boundary.
- Transport adapters connect module boundaries without owning domain behavior.
- The same message contracts and handler patterns should survive service
  extraction when practical.
- Service extraction should be a supported evolution path, not a separate
  rewrite story.

## Consequences

Bondstone should avoid becoming only an in-process mediator abstraction or only
a transport wrapper.

Samples and tests should eventually include at least one scenario that starts as
a modular monolith and demonstrates the intended service extraction shape.

Public API changes need to consider both in-process modular-monolith usage and
separately deployed service usage.

Some features that would be convenient in only one deployment style may be
rejected or isolated behind adapters to keep the extraction path coherent.

This ADR does not decide exact sample names, orchestration technology, broker
support, or deployment packaging. Later
[ADR 0007](0007-early-sample-application.md) accepts Aspire as the preferred
local sample orchestration host.

## Applied To

- Code: Pending. Source extraction has not started yet.
- Stable docs:
  - [docs/architecture.md](../architecture.md)
  - [docs/README.md](../README.md)
- Agent instructions:
  - [AGENTS.md](../../AGENTS.md)
- Skills: Not applicable.

## Verification

Read back [docs/architecture.md](../architecture.md),
[docs/README.md](../README.md), and [AGENTS.md](../../AGENTS.md). Executable
sample or integration tests that exercise modular-monolith and service-split
usage remain pending.
