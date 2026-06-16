# 0005 Transport Adapters And Receive Helpers

Status: Amended
Application: Applied
Date: 2026-06-16

## Context

During MVP, Bondstone transport code grew toward broker runtime ownership:
topology DSLs, startup topology diagnostics, receive workers, validation
matrices, and route ownership diagnostics. That made the library feel
overcooked in transport while still undercooked compared with mature bus
frameworks.

Post-MVP simplification changed the target. Bondstone should own durable
envelopes, outbox dispatch recording, receive pipelines, inbox idempotency,
and provider-backed persistence semantics. Apps should own broker topology and
runtime.

## Decision

Bondstone is not a transport runtime.

Bondstone owns:

- durable envelope creation and serialization;
- persisted outbox claiming, claim leases, retry scheduling, and terminal
  dispatch-failure state;
- small outbound envelope dispatcher contracts;
- provider-native envelope mapping helpers for supported adapters;
- receive pipeline execution over `DurableMessageEnvelope`;
- inbox idempotency and module transaction participation.

Applications own:

- queues, exchanges, topics, subscriptions, rules, bindings, and provisioning;
- native consumers/processors and their lifecycle;
- broker retry, dead-letter, prefetch, concurrency, and monitoring policy;
- native acknowledgement, completion, negative acknowledgement, or settlement
  after Bondstone receive succeeds or fails.

`Bondstone.Transport.Local` is an explicit local queue adapter for samples,
tests, and local development. It is not a hidden fallback and not production
broker durability. It should exercise the same outbox and receive pipelines as
real adapters.

`Bondstone.Transport.RabbitMq` remains the active direct broker adapter only
while it stays thin: outbound publishing through app-supplied destination
functions, native envelope mapping, receive dispatch helpers, settlement
helpers, and minimal opt-in receive workers for sample/proof ergonomics.
Broker topology remains app-owned.

No provider-neutral transport diagnostics package or topology abstraction is
part of the active surface.

## Consequences

The transport story is simpler and more honest. Consumer apps can use native
RabbitMQ.Client, Rebus, Azure Service Bus processors, or another transport
library around Bondstone's envelope and receive helpers.

Bondstone still provides value on both send and receive: it stages durable
outbox rows, claims and dispatches them, records dispatch outcomes, maps
envelopes, executes module receive pipelines, persists inbox rows, and commits
handler state with inbox/operation/outbox changes through provider
transactions.

Future broker adapters must stay small or move to samples. If an adapter needs
to own topology, subscription storage, retries, dead-letter orchestration, or
workflow semantics, that belongs in the app or a mature bus/workflow library.

## Amendment 2026-06-16 Remove Broker Adapter Package

Further simplification removed `Bondstone.Transport.RabbitMq` from the active
package set. The current MVP keeps only `Bondstone.Transport.Local` as an
explicit local adapter for samples, tests, and local development.

Broker integrations are app-owned. Bondstone provides the durable boundaries:

- `IDurableEnvelopeDispatcher` for outgoing outbox records;
- `IDurableMessageEnvelopeSerializer` for durable envelope payloads;
- `IDurableEnvelopeReceiver` for inbound command and event receive execution.

Applications or selected transport libraries own native topology,
subscriptions, consumers, acknowledgement/settlement, retry, dead-letter
policy, prefetch, concurrency, and monitoring. Event receive remains explicit:
the app supplies the subscriber module and stable subscriber identity selected
by its native subscription.

## Related Decisions

- Supersedes the active transport direction from the archived ADR sequence.
- See archived ADRs
  [0036](archive/pre-restart-2026-06-16/0036-direct-transport-adapters-and-rebus-removal.md),
  [0038](archive/pre-restart-2026-06-16/0038-provider-retry-recovery-and-settlement-boundaries.md),
  [0039](archive/pre-restart-2026-06-16/0039-startup-transport-topology-validation.md),
  and
  [0056](archive/pre-restart-2026-06-16/0056-post-mvp-communication-and-transport-simplification.md)
  for prior context.

## Application Notes

- Current contract: local transport behavior is documented in
  [docs/architecture/transport-local.md](../architecture/transport-local.md).
  App-owned broker integration is documented in
  [docs/architecture/messaging.md](../architecture/messaging.md),
  [docs/setup.md](../setup.md), and
  [docs/package-discovery.md](../package-discovery.md).
- Stable docs: messaging receive-pipeline behavior is documented in
  [docs/architecture/messaging.md](../architecture/messaging.md).
- Agent guidance: root [AGENTS.md](../../AGENTS.md) requires ADR review before
  transport support, provider behavior, package-boundary, public API, or
  compatibility changes.
- Application evidence: provider-neutral transport diagnostics, old topology
  ownership, and the RabbitMQ adapter package were removed. Local transport is
  covered by the modular monolith sample integration tests.
- Pending or deferred: future broker adapter packages are deferred until a
  real consumer need justifies adding ergonomic wrappers around the app-owned
  broker boundary.

## Verification

Read current architecture, messaging, transport-local, sample, package
discovery, packaging, and testing docs. Behavior is covered by local transport
tests and the local modular monolith sample integration test.
