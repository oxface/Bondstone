# 0015 Service Extraction Proof Before Broad Bus Features

Status: Proposed
Application: Not Applicable
Date: 2026-06-16

## Context

Bondstone's strongest differentiation is durable modular-monolith semantics:
module names, module-owned persistence, stable durable message identities,
source and target module metadata, inbox/outbox behavior, and handler
patterns that can survive service extraction.

Mature alternatives such as Wolverine and Brighter have broader message-bus,
command-processing, middleware, transport, saga, scheduling, retry, and
operational ecosystems. Bondstone should not try to compete by quickly
rebuilding a bus framework. Its near-term value is simpler adoption for
modular monoliths and a credible path for extracting a module later.

The first real consumer project is expected to be a pure modular monolith.
Service extraction will come later, but proving that path early is important
because it is a core part of the library promise.

## Decision

Bondstone should prioritize modular-monolith semantics and service-extraction
proofs before broad bus features.

Near-term product direction should favor:

- module-owned setup and persistence;
- clear in-process module command execution;
- durable command send and integration event publish across module
  boundaries;
- operation result observation for durable commands;
- local transport for development and samples;
- thin broker adapters as extraction proof, not as a full transport runtime;
- documentation that explains when to use Bondstone and when a mature bus or
  workflow framework is a better choice.

Bondstone should defer broad bus-framework features unless real consumer need
justifies them through ADR review. Deferred features include sagas/workflow
engines, generic middleware pipelines, topology DSLs, provider-neutral
transport diagnostics, subscription storage, receive retry frameworks, and
broker dead-letter orchestration.

Service extraction proof should be implemented through realistic samples and
tests that show one module moving behind a broker while preserving stable
message contracts, operation ids, inbox/outbox behavior, and handler patterns.

## Consequences

Bondstone remains a library for durable module boundaries rather than a small
unfinished bus. This makes adoption easier for simple modular monoliths and
keeps public API growth disciplined.

The project can still learn from Wolverine and Brighter, especially around
operations and documentation, without copying their runtime scope.

Extraction-proof work may require stronger broker sample coverage,
multi-transport routing ergonomics, and production operations docs, but those
should be framed as support for the modular boundary story rather than a move
toward owning application messaging infrastructure.

## Related Decisions

- Narrows [0002 Library Scope And Package Surface](0002-library-scope-and-package-surface.md).
- Relates to [0003 Module Boundaries Runtime And Domain Events](0003-module-boundaries-runtime-and-domain-events.md).
- Relates to [0007 Keep Orchestration App-Owned For Now](0007-keep-orchestration-app-owned-for-now.md).
- Relates to [0008 Thin Broker Adapters](0008-thin-broker-adapters.md).
- Relates to [0010 Route-Aware Multi-Transport Dispatch](0010-route-aware-multi-transport-dispatch.md).

## Application Notes

- Current contract: stable architecture docs already position Bondstone as
  modular-monolith first with service extraction as an evolution path.
- Stable docs: proposed follow-up should sharpen docs that compare Bondstone
  with full bus frameworks and explain extraction scenarios.
- Agent guidance: no new agent rule is required until accepted and applied.
- Application evidence: modular monolith sample and broker-backed extraction
  tests already exist.
- Pending or deferred: improve extraction docs, route-aware multi-transport
  sample when needed, and clear "choose Bondstone vs a bus" guidance.

## Verification

ADR draft only. Reviewed architecture, setup, sample, transport, and package
docs while producing this proposal.
