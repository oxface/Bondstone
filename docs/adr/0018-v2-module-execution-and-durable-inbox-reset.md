# 0018 V2 Module Execution And Durable Inbox Reset

Status: Proposed
Application: Not Applicable
Date: 2026-06-18

## Context

The current post-MVP receive model grew through incremental hardening. It now
has a direct receive path with a small inbox idempotency table and an opt-in
durable incoming inbox path with ingestion, claim, lease, retry, and terminal
failure state.

That split preserved early public behavior, but Bondstone has only one real
consumer and v2 can reset from zero. The priority is a clean library model over
compatibility with transitional v1 concepts.

The design discussion also clarified that Bondstone should expose reusable
module execution pipelines. Durable inbox should be one ingress into those
pipelines, not the only way to invoke module behavior. Consumers still need
first-class HTTP ingress, direct same-process command execution through module
contracts, and query execution without pretending every interaction is a
broker delivery.

## Decision

For v2, Bondstone should present one durable receive ledger: the durable inbox.
The old direct receive inbox idempotency model should be removed, collapsed, or
kept only as private transitional implementation until it can be deleted.
Durable broker receive should ingest into the durable inbox and be processed by
the durable inbox worker.

Bondstone should expose three module interaction modes:

1. Direct module command execution through the module command pipeline.
   This is immediate execution inside the target module boundary. It can
   return typed results and can use module persistence, validation,
   transaction runners, domain event persistence, and outgoing outbox staging.
   It is not durable receive and does not make cross-module orchestration
   durable.
2. Module query execution through a separate read pipeline. Queries are direct
   boundary-respecting reads. They are not durable messages, do not write
   inbox/outbox rows, and do not imply local projections.
3. Durable command and event execution through outbox, transport, durable
   inbox ingestion, and durable inbox processing. This is the durable
   cross-boundary path and is asynchronous by design.

Shared `.Contracts` projects remain the normal way for modules and hosts to
share command, query, result, and integration-event contracts. They are not a
recommendation to instantiate handlers directly. Handler direct calls bypass
Bondstone and leave persistence, diagnostics, and consistency to application
code.

HTTP ingress should have two explicit shapes:

- immediate command/query execution through Bondstone module pipelines;
- durable command ingress that records work into the target durable inbox and
  returns operation tracking metadata.

Bondstone may add small Minimal API or controller helpers for those shapes, but
must not require code generation or own the HTTP application model.

## Consequences

The v2 product story becomes smaller and clearer:

- direct command execution is for immediate same-process work;
- queries are for synchronous reads;
- durable inbox is for durable receive;
- outbox plus durable inbox is for durable cross-boundary work.

Consumers get a convenient path for HTTP endpoints and `.Contracts` calls
without being pushed toward raw handler invocation.

Bondstone must update code, docs, samples, public API, and tests to remove
direct-receive-as-durable-path wording. Any sample migrations generated from
the transitional receive model should be reset before v2 publication.

This decision does not add sagas, durable workflows, automatic projections, or
broker topology ownership.

## Related Decisions

- Would supersede or further narrow
  [0017 Single Durable Inbox Incoming Ledger](0017-single-durable-inbox-incoming-ledger.md)
  when accepted.
- Builds on
  [0003 Module Boundaries Runtime And Domain Events](0003-module-boundaries-runtime-and-domain-events.md).
- Relates to
  [0005 Transport Adapters And Receive Helpers](0005-transport-adapters-and-receive-helpers.md)
  and
  [0008 Thin Broker Adapters](0008-thin-broker-adapters.md).

## Application Notes

- Current contract: not binding until accepted. Current stable docs still
  describe direct receive and durable incoming inbox as they exist today.
- Stable docs: when accepted, apply to messaging, modules, hosting,
  persistence-core, persistence-ef-core, operations, setup, package discovery,
  packaging, public API, samples, and testing docs.
- Agent guidance: no agent instruction change yet. If accepted, root and
  architecture guidance already require ADR review for this runtime shift.
- Application evidence: module command execution, durable inbox primitives,
  and RabbitMQ durable inbox ingestion already exist, but the unified v2 model
  is not yet applied.
- Pending or deferred: implementation sweep, sample migration reset, docs
  cleanup, public API cleanup, and final design report.

## Verification

No executable verification. This ADR records a proposed design reset from the
2026-06-18 orchestration discussion.
