# 0013 Worker Boundaries And Transport Adapter Ownership

Status: Proposed
Application: Not Applicable
Date: 2026-06-16

## Context

Bondstone currently ships a hosted outbox worker and thin native-driver
RabbitMQ and Azure Service Bus adapter packages with opt-in receive workers.
The project direction is "library, not framework" and "not a broker runtime."

Workers are useful because Bondstone owns durable local state transitions such
as outbox claiming and dispatch recording. Workers can also become dangerous
if they grow into provider-native infrastructure managers, topology
provisioners, retry engines, subscription stores, or a generic bus host.

Future receive-buffer work would likely introduce another worker over
Bondstone-owned persistence state, so the worker boundary should be explicit
before that feature grows.

## Decision

A Bondstone worker may move records between Bondstone-owned durable states.
It must not manage provider-native infrastructure.

Valid Bondstone worker responsibilities include:

- pending outbox to processing to dispatched, retry scheduled, or terminal
  failed;
- optional future receive-buffer record to processing to processed, retry
  scheduled, or terminal receive failed;
- optional operation expiration from pending/running to failed or cancelled
  when explicitly configured by application policy;
- inspection, health, or metrics surfaces over Bondstone-owned persistence.

Invalid Bondstone worker responsibilities include:

- creating or mutating queues, exchanges, topics, subscriptions, rules, or
  bindings;
- inferring broker topology from module registrations;
- owning native broker retry, dead-letter, prefetch, concurrency, or
  subscription policy;
- managing provider-native monitoring;
- becoming a generic message-bus host.

Transport adapter packages may contain native-driver workers only when those
workers remain explicit opt-in ergonomics around durable envelope plumbing.
Such workers may read a native delivery, call `IDurableEnvelopeReceiver` or a
future durable ingestion boundary, and perform the native success or failure
settlement chosen by package options. They must not own topology or provider
policy.

Application hosts may always bypass adapter workers and implement native
receive loops around `IDurableMessageEnvelopeSerializer` and
`IDurableEnvelopeReceiver` directly.

## Consequences

Bondstone can grow worker ergonomics without crossing into full broker runtime
ownership. Worker design stays compatible with service extraction because the
durable boundary remains the envelope, inbox/outbox state, and module
persistence transaction.

Adapter receive workers must stay boring. If a requested worker feature
requires broker topology storage, delivery-count policy, dead-letter routing,
subscription rule management, or provider-native scheduling, that feature
belongs in application code, native broker configuration, or a separate ADR.

The outbox worker can evolve with better metrics, fairness, or selected-module
options as long as those options operate over Bondstone persistence state and
do not provision or manage broker infrastructure.

## Related Decisions

- Relates to [0005 Transport Adapters And Receive Helpers](0005-transport-adapters-and-receive-helpers.md).
- Narrows [0008 Thin Broker Adapters](0008-thin-broker-adapters.md).
- Relates to [0010 Route-Aware Multi-Transport Dispatch](0010-route-aware-multi-transport-dispatch.md).
- Relates to [0012 Direct Receive Inbox And Durable Receive Buffer](0012-direct-receive-inbox-and-durable-receive-buffer.md).

## Application Notes

- Current contract: the durable outbox worker moves outbox records through
  Bondstone dispatch state; broker adapter receive workers are explicit
  opt-in and hand envelopes to Bondstone receive.
- Stable docs: hosting and messaging architecture docs describe much of this
  boundary today. Proposed follow-up should make the worker boundary explicit
  in hosting docs.
- Agent guidance: no new agent rule is required until accepted and applied.
- Application evidence: current outbox worker composes
  `IDurableOutboxDispatcher`; current RabbitMQ and Service Bus receive workers
  do not provision topology.
- Pending or deferred: selected-module worker options, receive-buffer worker,
  operation-expiration worker, and worker metrics remain future work.

## Verification

ADR draft only. Reviewed hosting and messaging architecture docs plus current
outbox and transport receive worker code paths while producing this proposal.
