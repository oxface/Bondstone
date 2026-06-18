# 0020 Host-Owned Transport With Module-Aware Bindings

Status: Proposed
Application: Not Applicable
Date: 2026-06-18

## Context

Bondstone's v2 durable inbox direction makes receive-side module ownership
more important. Durable inbox ingestion must write to the receiver module's
persistence boundary, and event subscribers have stable module/subscriber
identities.

At the same time, Bondstone must stay a library rather than a transport
framework. Broker topology, deployment, retry, dead-letter policy,
concurrency, monitoring, credentials, and provisioning should remain owned by
the application or host. This is especially important for service extraction:
module code should describe module contracts and durable boundaries, while the
host or extracted service owns how those boundaries connect to infrastructure.

The design discussion also clarified event fanout. Integration event fanout is
broker topology: RabbitMQ exchanges/bindings, Service Bus topics/subscriptions,
or another app-owned transport layout. Bondstone should not create a
subscription store or topology DSL to hide that.

The guiding principle is: transport infrastructure is host-owned; durable
runtime semantics are module-owned. The host decides which native queues,
topics, subscriptions, workers, and transport options exist in a process. A
module owns the contracts, handlers, subscriber identities, persistence
boundary, durable inbox rows, outbox rows, and operation finalization semantics
that make extraction into a separate service tractable later.

## Decision

Transport remains host-owned and provider-native. Bondstone should provide
module-aware bindings, not broker topology ownership.

Module-aware bindings are deployment wiring, not module implementation
metadata. The host or extracted service configures them because the host owns
which transport endpoints are active in that process.

For durable commands, a host should be able to state that a native queue/entity
feeds the durable inbox for one command target module.

For integration events, a host should be able to state that a native
queue/subscription feeds the durable inbox for one event subscriber identity:
event identity, subscriber module, and subscriber identity. Native broker
fanout remains outside Bondstone.

Transport adapter packages may provide small receive-worker registration
helpers for existing native queues/entities. Those helpers may derive durable
receive identity and call the durable inbox ingestion boundary. They must not
declare exchanges, queues, topics, subscriptions, rules, bindings, retry
policies, dead-letter policy, provisioning, provider-neutral topology
validation, or subscription storage.

Local transport may keep explicit local routing helpers for samples and tests,
but must not be presented as production broker topology guidance.

## Consequences

The host controls infrastructure while Bondstone controls durable module
semantics. This keeps module extraction straightforward: when a module becomes
a service, that service host owns its queues/subscriptions and workers while
the module code keeps its contracts, handlers, persistence boundary, and
durable inbox semantics.

This intentionally resembles host-owned transport runtimes in larger bus
frameworks while preserving Bondstone's module-owned durable persistence
boundaries. Per-module durable boundaries do not mean modules provision or own
broker topology.

Consumers must still deploy and configure broker topology themselves. Bondstone
can improve ergonomics by making receive-worker registration read like module
binding rather than broker provisioning.

This decision avoids hidden topology ownership but keeps enough structure for
docs, diagnostics, and tests to explain command queues and event subscriber
queues/subscriptions.

## Related Decisions

- Reaffirms and narrows
  [0008 Thin Broker Adapters](0008-thin-broker-adapters.md).
- Relates to
  [0010 Route-Aware Multi-Transport Dispatch](0010-route-aware-multi-transport-dispatch.md).
- Relates to
  [0017 Single Durable Inbox Incoming Ledger](0017-single-durable-inbox-incoming-ledger.md)
  and
  [0018 V2 Module Execution And Durable Inbox Reset](0018-v2-module-execution-and-durable-inbox-reset.md).

## Application Notes

- Current contract: not binding until accepted. Current RabbitMQ helpers
  already follow much of this shape for durable incoming inbox ingestion.
- Stable docs: when accepted, apply to messaging, hosting, transport-local,
  operations, setup, package-discovery, packaging, and samples docs.
- Agent guidance: no agent instruction change yet.
- Application evidence: RabbitMQ receive workers can ingest command and event
  deliveries into durable incoming inbox boundaries; host-owned broker topology
  is already the documented transport stance.
- Pending or deferred: API naming cleanup, Service Bus parity decision, sample
  topology reset, docs cleanup, and future saga/process-manager design that
  preserves host-owned transport with module-owned saga state.

## Verification

No executable verification. This ADR records a proposed transport boundary
clarification from the 2026-06-18 orchestration discussion.
