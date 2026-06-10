# 0038 Provider Retry Recovery And Settlement Boundaries

Status: Amended
Application: Applied
Date: 2026-06-10

## Context

ADR 0036 removed Rebus and made direct RabbitMQ and Azure Service Bus adapters
the current transport architecture. Phase 6.5 proved outgoing dispatch,
provider-native receive topology, native message mapping, settlement handler
helpers, opt-in receive workers, and broker-backed acknowledgement/dead-letter
handoff tests.

Phase 7 needs to harden those direct providers without accidentally turning
Bondstone into a second broker policy engine. Bondstone already owns persisted
outbox retry and terminal failure state for messages it has persisted and
claimed. The brokers already own receive delivery counts, redelivery timing,
lock renewal, dead-letter subqueues, negative acknowledgement behavior, queue
bindings, subscription rules, and provider retry clients.

The boundary must be explicit before adding more diagnostics or recovery
helpers, because public transport behavior, provider support, and sample
guidance are affected.

## Decision

Bondstone owns retry and recovery policy only for Bondstone-persisted durable
outbox dispatch. The core outbox dispatcher may continue to classify dispatch
exceptions, schedule durable outbox retries, and mark persisted outbox records
as terminal failures through the provider-neutral persistence contracts.

Direct provider receive adapters own settlement ordering and operational
diagnostics, not broker retry policy. Their receive workers and handler helpers
must preserve this order:

1. map the native broker message into the provider transport message shape;
2. dispatch through the neutral module command or event receive pipeline;
3. settle the native broker message only after Bondstone dispatch succeeds.

On receive dispatch failure, direct provider workers should perform only the
bounded provider-native failure action that hands control back to broker or
application policy:

- RabbitMQ uses negative acknowledgement with the configured `requeue` value;
- Service Bus abandons the message so Service Bus delivery-count and
  dead-letter policy remain in control.

Direct provider workers must log enough provider-native context to explain
that handoff, including the receive source, broker message identity when
available, Bondstone message type when available, and the native settlement
action selected by Bondstone. These diagnostics should not require Bondstone to
own broker administration, retry schedules, delivery-count policy, dead-letter
topology, or provider client retry configuration.

Provider topology diagnostics should continue to describe configured durable
routes and receive bindings before dispatch. Multi-transport routing failures
should remain loud when no transport route or multiple routes match a durable
message. A later ADR may introduce a public diagnostic report shape if the
current exception messages and provider-specific diagnostic services are not
enough.

Broker topology declaration helpers are not accepted by this ADR. They remain
deferred until a separate ADR defines the bounded provider behavior and app
ownership model.

## Amendment 2026-06-10: MVP Receive Registration Contract

The MVP receive surface must be simple but usable with native retry and
dead-lettering. Bondstone receive registration should therefore name and
validate the provider-native durable receive entities that Bondstone is allowed
to execute, while leaving the failure infrastructure on those entities
application-owned.

For RabbitMQ, `ReceiveQueue(...)` binds a native queue name to Bondstone module
and subscriber execution, and `UseReceiveWorker(...)` exposes
`RabbitMqReceiveWorkerOptions.RequeueOnFailure`. `requeue: false` is the
preferred handoff for DLX or retry-queue topology that the application
declares. `requeue: true` is available for explicit immediate broker redelivery
but can create a hot redelivery loop if the application has not designed for
it. Bondstone should not declare DLX, retry queues, delayed exchanges, TTL
queues, or queue policies by default.

For Service Bus, `ReceiveQueue(...)` and `ReceiveSubscription(...)` bind native
queues and topic subscriptions to Bondstone module and subscriber execution.
`UseReceiveWorker(...)` exposes native processor safety knobs such as
concurrency and lock renewal duration, but receive retry and dead-letter policy
remain on the Azure Service Bus entity. Applications configure queues and
subscriptions, including max delivery count, dead-letter settings, rules, and
client retry behavior, through Azure infrastructure or provider-native
administration.

This keeps the MVP usable: an application can register Bondstone against the
same queues and subscriptions where native retry/DLQ policy is configured,
while Bondstone only guarantees durable inbox/module execution and correct
settlement ordering.

## Amendment 2026-06-10: Outbox Failure Terminology

Bondstone persistence currently has an outbox status named `DeadLettered`.
That status is a terminal persisted outbox failure for a Bondstone-owned row
that could not be dispatched after the configured outbox attempts. It is not a
broker dead-letter queue, does not create broker DLQ messages, and does not
mean Bondstone owns receive-side dead-letter policy.

Stable docs and agent instructions should prefer "terminal outbox failure" or
"persisted outbox terminal failure state" when describing Bondstone ownership.
Use "dead-letter" for provider-native broker behavior or when referring to
the existing `DeadLettered` storage status explicitly.

## Consequences

Phase 7 can improve RabbitMQ and Service Bus recovery diagnostics without
adding a Bondstone-owned receive retry policy.

Applications stay responsible for RabbitMQ dead-letter exchanges, requeue
policy, delayed retry infrastructure, exchange/queue/binding declaration, and
client connection settings.

Applications stay responsible for Service Bus queues, topics, subscriptions,
rules, max delivery count, lock and retry settings, dead-letter policy,
credentials, and administrative clients.

Bondstone samples should keep transport selection explicit and should not
teach users that Bondstone creates or governs broker retry/dead-letter
infrastructure.

Receive-side stale inbox recovery remains outside this decision. Already
received but unprocessed inbox rows are still operationally loud until a later
ADR accepts stale receive recovery semantics.

## Related Decisions

- [0013 Outbox Dispatch Lifecycle Contract](0013-outbox-dispatch-lifecycle-contract.md)
- [0015 Inbox Handle-Once Orchestration](0015-inbox-handle-once-orchestration.md)
- [0033 First-Class Event Publish/Subscribe Topology](0033-first-class-event-publish-subscribe-topology.md)
- [0034 Adapter Diversity Proof Transports](0034-adapter-diversity-proof-transports.md)
- [0036 Direct Transport Adapters And Rebus Removal](0036-direct-transport-adapters-and-rebus-removal.md)

## Application Notes

- Current contract: Bondstone owns persisted outbox retry and terminal failure
  state for outbox dispatch. Direct provider receive adapters own settlement
  ordering and diagnostics, while broker retry/dead-letter policy stays
  provider-native and application-owned. MVP receive registration names the
  native receive entities and exposes only bounded worker options, such as
  RabbitMQ failure requeue and Service Bus processor concurrency/lock renewal.
- Stable docs: The rule is reflected in
  [docs/architecture/README.md](../architecture/README.md),
  [docs/architecture/messaging.md](../architecture/messaging.md),
  [docs/architecture/transport-rabbitmq.md](../architecture/transport-rabbitmq.md),
  [docs/architecture/transport-servicebus.md](../architecture/transport-servicebus.md),
  [docs/testing.md](../testing.md), and [docs/archive/mvp-plan.md](../archive/mvp-plan.md).
- Agent guidance: Root [AGENTS.md](../../AGENTS.md) keeps broker
  administration, retry/dead-letter policy, credentials, and native clients
  app-owned.
- Application evidence: RabbitMQ and Service Bus receive workers log
  provider-native receive source, broker identity, Bondstone type metadata
  when available, and the failure settlement action before handing failed
  dispatch back to broker policy.
- Pending or deferred: Broker topology declaration helpers, a public
  cross-provider operational diagnostic report shape, and stale inbox receive
  recovery remain separate future decisions.

## Verification

- Read back this ADR and affected stable docs.
- For the receive worker diagnostic code slice:
  `dotnet build Bondstone.slnx --configuration Release --no-restore --disable-build-servers`
- For the MVP receive registration amendment:
  - `pnpm format:check`
  - `git diff --check`
