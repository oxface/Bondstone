# 0005 Slow Layered Extraction

Status: Accepted
Application: Partially Applied
Date: 2026-06-03

## Context

Bondstone source currently exists outside this repository. A fast bulk copy
would create a working tree quickly, but it would also import existing design
assumptions without enough pressure to examine package boundaries, public API
shape, test ownership, module/service extraction behavior, and microservice
usefulness.

The extraction is not a compatibility-preservation exercise for the current
consumer repository. Bondstone may break, rename, split, or rewrite current
APIs when that produces a better reusable library. The current consumer can be
adapted later, replaced, or archived.

The extraction itself is an opportunity to find design flaws. Some boundaries
that work inside an application repository may not hold up as reusable NuGet
packages. Some abstractions that work for in-process modular monoliths may not
yet support service extraction or microservice internal durability well enough.

## Decision

Extract Bondstone slowly, project by project and often layer by layer.

Do not bulk-copy all source into this repository as one migration step.

Do not preserve current consumer compatibility as a design constraint. Keep
only the useful concepts, tests, and behavior; change or remove existing APIs
when extraction exposes a better library boundary.

Each extraction step should move or rewrite a coherent slice and should be
small enough to review package boundaries, public surface, dependency
direction, tests, docs, and migration risks.

Prefer this broad extraction order unless a later ADR changes it:

1. Core abstractions and low-dependency primitives.
2. Durable command and messaging contracts.
3. Message identity and registration behavior.
4. EF Core-neutral persistence abstractions and entities.
5. PostgreSQL provider behavior.
6. Rebus transport behavior.
7. Integration tests rewritten around neutral Bondstone fixtures.
8. Samples that validate modular-monolith and service-split usage.

Each step should do at least one of these:

- move a coherent code slice;
- rewrite tests into neutral fixtures;
- create or update stable docs;
- create or update ADRs when design tension appears;
- identify package, new-consumer compatibility, or service-extraction
  questions before moving further.

Avoid destructive edits in the current consumer repository unless they are part
of an explicit migration step. Its current compatibility should not block
Bondstone design.

## Consequences

Extraction will take longer, but each layer can expose reusable-library design
problems earlier.

The repository may temporarily contain accepted docs and ADRs before matching
code exists. ADR `Application` states and stable docs must make pending work
visible.

Microservice and service-extraction claims must be tested against each layer
instead of treated as marketing language.

Some existing code may be rewritten rather than moved unchanged when extraction
reveals an API or dependency flaw.

The current consumer repository may need substantial adaptation or may stop
being the primary consumer after Bondstone packaging and samples mature.

## Application Notes

- Current contract: Bondstone source is extracted slowly by coherent slices,
  and the historical repository is source material rather than a compatibility
  constraint.
- Stable docs: Current extraction rules are described in
  [docs/extraction.md](../extraction.md), with architecture context in
  [docs/architecture/README.md](../architecture/README.md) and current
  persistence context in
  [docs/architecture/persistence.md](../architecture/persistence.md).
- Agent guidance: Root [AGENTS.md](../../AGENTS.md) directs agents to follow
  the slow extraction strategy before moving or rewriting source.
- Application evidence: The first `Bondstone` core slices include stable
  message identity contracts, registry behavior, message trace context, durable
  command send contracts, durable operation read contracts, durable message
  envelopes, persistence-neutral outbox/inbox and operation state store
  contracts, provider-neutral outbox dispatch state, send result semantics, and
  neutral unit tests. EF Core provider-neutral entity mappings have started.
- Pending or deferred: `Send and wait` behavior, trace context and causation
  propagation, envelope content type/header expansion, retry policy, EF Core
  store implementations, PostgreSQL provider behavior, Rebus transport behavior,
  integration tests, and samples remain future extraction work.

## Verification

Read back [docs/extraction.md](../extraction.md),
[docs/architecture/README.md](../architecture/README.md),
[docs/architecture/messaging.md](../architecture/messaging.md),
[docs/architecture/persistence.md](../architecture/persistence.md),
[docs/README.md](../README.md), and [AGENTS.md](../../AGENTS.md). Ran
`pnpm check`; formatting, restore, build, fast `Unit`/`Application` tests, and
packaging pass for the current extracted slices. Further extraction will
continue to use focused build/test/doc checks.
