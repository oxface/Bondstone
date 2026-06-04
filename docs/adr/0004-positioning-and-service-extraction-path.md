# 0004 Positioning And Service Extraction Path

Status: Amended
Application: Partially Applied
Date: 2026-06-04

## Context

Bondstone starts from historical modular-monolith infrastructure that included
explicit module boundaries, in-process command handling, EF Core backed module
persistence, stable message identities, durable outbox/inbox behavior, and
transport adapters.

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

## Amendments

### 2026-06-04: Durable Commands Are Not General In-Process Commands

`IDurableCommand` is reserved for asynchronous commands accepted for durable
outbox delivery. Direct in-process module calls can use consumer-owned
`.Contracts` references without Bondstone mediation.

Bondstone should not import or recreate a general in-process command bus or
generic mediator/message-bus abstraction unless a later sample or ADR shows
that such an abstraction protects a durable boundary concern such as outbox
persistence, inbox handling, tracing, or service-extraction continuity.

Generic in-process message buses often hide call graphs, weaken
discoverability, add reflection or dispatch overhead, and provide little value
when normal typed `.Contracts` references are sufficient.

Durable message identity strings remain free-form for compatibility with
existing systems and consumer naming policies. Docs, tests, and samples should
prefer lowercase dotted identities with an explicit version suffix, such as
`{module}.{aggregate}.{message}.v{major}`.

## Consequences

Bondstone should avoid becoming only an in-process mediator abstraction or only
a transport wrapper.

Samples and tests should eventually include at least one scenario that starts as
a modular monolith and demonstrates the intended service extraction shape.

Public API changes need to consider both in-process modular-monolith usage and
separately deployed service usage.

Some features that would be convenient in only one deployment style may be
rejected or isolated behind adapters to keep the extraction path coherent.

General in-process command dispatch and generic mediator/message-bus APIs
remain outside the current extraction target. This keeps package API pressure
focused on durable messaging and module boundary continuity instead of
replacing ordinary project references between module contract packages.

This ADR does not decide exact sample names, orchestration technology, broker
support, or deployment packaging. Later
[ADR 0007](0007-early-sample-application.md) accepts Aspire as the preferred
local sample orchestration host.

## Application Notes

- Current contract: Bondstone is positioned as a library for durable module
  boundaries that supports modular monoliths first and preserves a path to
  service extraction and microservice internal durability. Durable commands
  are asynchronous outbox-delivered messages, not a replacement for ordinary
  in-process `.Contracts` calls or generic mediator/message-bus dispatch.
- Stable docs: Current architecture positioning is described in
  [docs/architecture/README.md](../architecture/README.md), with messaging
  rules in [docs/architecture/messaging.md](../architecture/messaging.md) and
  persistence rules in
  [docs/architecture/persistence.md](../architecture/persistence.md).
- Agent guidance: Root [AGENTS.md](../../AGENTS.md) requires ADR review before
  broad changes to public API, provider support, transport support, or durable
  behavior.
- Application evidence: The first extracted core slices include stable message
  identity contracts, durable command markers, registry behavior, message trace
  context, durable command send contracts, durable operation read contracts,
  durable message envelopes, persistence-neutral outbox/inbox and operation
  state store contracts, provider-neutral outbox dispatch state, and send
  result semantics. EF Core provider-neutral entity mappings have started.
- Pending or deferred: Samples and integration tests that exercise
  modular-monolith and service-split usage remain pending.

## Verification

Read back [docs/architecture/README.md](../architecture/README.md),
[docs/architecture/messaging.md](../architecture/messaging.md),
[docs/architecture/persistence.md](../architecture/persistence.md),
[docs/extraction.md](../extraction.md), [docs/README.md](../README.md), and
[AGENTS.md](../../AGENTS.md). Ran `pnpm check` for the current extracted core
slices. Executable sample or integration tests that exercise modular-monolith
and service-split usage remain pending.
