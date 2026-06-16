# Post-MVP Architecture Review Plan

Date: 2026-06-16

## Purpose

This plan prioritizes the architecture work identified after Bondstone's
public MVP publication and initial consumer feedback.

The plan is intentionally a planning handoff, not a durable architecture
contract. Accept or revise the related ADRs before broad implementation. Move
implementation tasks into GitHub Issues or GitHub Projects.

## Proposed ADRs

- [0011 OTel Native Diagnostics And Misconfiguration Reporting](../adr/0011-otel-native-diagnostics-and-misconfiguration-reporting.md)
- [0012 Direct Receive Inbox And Durable Receive Buffer](../adr/0012-direct-receive-inbox-and-durable-receive-buffer.md)
- [0013 Worker Boundaries And Transport Adapter Ownership](../adr/0013-worker-boundaries-and-transport-adapter-ownership.md)
- [0014 Production Operations And Lifecycle Guidance](../adr/0014-production-operations-and-lifecycle-guidance.md)
- [0015 Service Extraction Proof Before Broad Bus Features](../adr/0015-service-extraction-proof-before-broad-bus-features.md)
- [0016 Non-Throwing Operation Wait Ergonomics](../adr/0016-non-throwing-operation-wait-ergonomics.md)

## Priority Order

### P0: Documentation Correction

Status: Started.

- Remove stale docs that say broker adapter packages still need to be
  reintroduced.
- Keep setup, package discovery, packaging, architecture, and package README
  guidance aligned around the current thin RabbitMQ and Azure Service Bus
  adapters.

### P1: Production Operations And Observability

Related ADRs: 0011, 0014.

This is the highest-impact post-MVP work because current consumer feedback is
documentation-heavy and the library already exposes enough durable behavior
to need operational guidance.

Deliverables:

- add a stable observability guide;
- define activity names, tags, metrics, log event ids, and misconfiguration
  message conventions;
- add or refine OTel instrumentation around send, publish, outbox dispatch,
  receive, inbox decisions, handler execution, and operation finalization;
- add a stable production operations guide for outbox terminal failures,
  stale inbox rows, operation expiry/finalization, retention, EF migrations,
  contract evolution, adapter settlement behavior, and troubleshooting;
- link package READMEs and setup docs to the operations and observability
  guidance.

### P2: Receive Semantics And Inbox Recovery Design

Related ADR: 0012.

Keep the current direct receive inbox as the default idempotency boundary.
Design, but do not rush, an optional durable receive buffer for service
extraction and stronger operational recovery.

Deliverables:

- document the current direct receive failure matrix;
- document why already-received/unprocessed inbox rows are ambiguous;
- decide whether the durable receive buffer should be accepted now or remain
  deferred;
- if accepted, design persistence records, state transitions, leases, retry
  attempts, terminal receive failure, worker options, retention, and tests.

### P3: Worker Boundary And Adapter Scope

Related ADR: 0013.

Keep workers simple and focused on Bondstone-owned durable states. Keep
transport adapters thin and explicit.

Deliverables:

- update hosting docs with worker boundary rules;
- add stronger warnings or defaults around unsafe receive options such as
  RabbitMQ auto-ack;
- document how host-owned native receive loops can bypass adapter workers;
- decide whether operation expiration should get an optional hosted worker.

### P4: Service Extraction Proof

Related ADRs: 0015, 0010.

Protect Bondstone's differentiation: durable modular-monolith semantics with
a credible extraction path.

Deliverables:

- improve docs explaining Bondstone versus full bus frameworks;
- document module extraction scenarios and limits;
- keep local modular monolith sample as the fast adoption path;
- keep RabbitMQ and Service Bus sample tests as extraction proofs;
- add route-aware multi-transport ergonomics and a two-transport sample only
  when real demand appears.

### P5: Compatibility And Lifecycle Polish

Related ADRs: 0011, 0014, 0015.

Deliverables:

- write message contract evolution guidance;
- write EF table migration and package upgrade guidance;
- write retention guidance for outbox, inbox, operation state, and optional
  domain event records;
- consider a non-throwing durable operation wait API after endpoint ergonomics
  are better understood;
- keep `net10.0` as the intentional target framework unless a later ADR
  changes platform strategy.

## Deliberately Deferred

- generic application middleware pipeline;
- topology DSLs or provisioning;
- provider-neutral transport diagnostics;
- subscription storage;
- broker retry/dead-letter orchestration;
- saga or workflow engine features;
- non-EF persistence provider work without real consumer demand;
- broad multi-transport routing ergonomics before a concrete use case.

## Open Questions

- Should the durable receive buffer be accepted before the first extraction
  project starts, or designed now and implemented after the pure modular
  monolith validates the default receive path?
- Should RabbitMQ `AutoAck` remain an option, become explicitly documented as
  unsafe for Bondstone durability, or be removed in a compatibility-sensitive
  release?
- Should operation expiration get a built-in hosted worker, or remain an
  application-scheduled processor for now?
- Which operational queries should be documented as canonical examples for
  PostgreSQL-backed EF Core stores?

## Verification

Plan-only change. Read root and ADR guidance, current architecture docs,
setup docs, testing docs, package/public API docs, and the current receive,
outbox, EF transaction, and transport adapter code paths during the
post-MVP review.
