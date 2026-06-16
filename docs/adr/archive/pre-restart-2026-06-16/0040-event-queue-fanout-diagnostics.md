# 0040 Event Queue Fan-Out Diagnostics

Status: Archived
Application: Not Applicable
Date: 2026-06-10

## Context

ADR 0033 made integration events first-class durable messages with
per-subscriber inbox identity. ADR 0039 added startup topology validation for
durable outbound routes and provider receive bindings.

That validation still left a service-extraction trap. A queue-style event
destination can be valid when one process receives one broker delivery and
fans it out in-process to multiple registered subscribers. The same topology is
wrong for split subscribers spread across multiple broker receive entities:
publishing the event directly to one queue will not deliver independent copies
to each queue or subscription.

Bondstone should catch that mismatch without becoming a broker provisioning
tool and without declaring RabbitMQ bindings, Service Bus subscriptions, rules,
or retry/dead-letter policy.

## Decision

RabbitMQ and Service Bus startup topology validation will validate queue-style
event destinations against provider receive bindings in single-transport
hosts.

When a registered event subscriber has a receive binding and the provider event
route/destination sends directly to a queue:

- all receive bindings for that event in that provider must be on the same
  receive entity as the outbound queue destination;
- multiple subscribers on that same receive entity remain valid in-process
  fan-out for modular monoliths;
- receive bindings spread across multiple queues or subscriptions are a
  startup configuration error;
- receive bindings on a different queue than the direct event destination are
  also a startup configuration error.

RabbitMQ exchange routes and Service Bus topic destinations remain the
preferred split-subscriber topology because applications can bind or subscribe
separate receive entities and own the native broker fan-out setup.

This decision does not introduce broker topology declaration helpers or a
provider-neutral provisioning API.

## Consequences

Applications get earlier feedback when an event route looks like it should
serve multiple split subscribers but actually targets a single competing
consumer queue.

Monolith and local-development topologies can still bind multiple Bondstone
subscribers to one receive entity and rely on Bondstone's per-subscriber inbox
keys.

Validation remains provider-native and startup-only. It cannot prove that a
RabbitMQ exchange has the intended queue bindings or that a Service Bus topic
has the intended subscriptions and rules, because that infrastructure remains
application-owned.

Multi-transport hosts remain bounded by ADR 0039's overvalidation guardrail.
This queue-destination check applies to single-transport hosts.

## Related Decisions

- [0033 First-Class Event Publish/Subscribe Topology](0033-first-class-event-publish-subscribe-topology.md)
- [0038 Provider Retry Recovery And Settlement Boundaries](0038-provider-retry-recovery-and-settlement-boundaries.md)
- [0039 Startup Transport Topology Validation](0039-startup-transport-topology-validation.md)

## Application Notes

- Current contract: RabbitMQ and Service Bus fail startup in single-transport
  hosts when an event is routed directly to a queue but receive bindings for
  that event are missing from that queue, are on another queue, or are spread
  across multiple receive entities. Same-queue in-process fan-out remains
  valid.
- Stable docs: The contract is reflected in
  [docs/architecture/messaging.md](../architecture/messaging.md),
  [docs/architecture/transport-rabbitmq.md](../architecture/transport-rabbitmq.md),
  [docs/architecture/transport-servicebus.md](../architecture/transport-servicebus.md),
  [docs/testing.md](../testing.md), [docs/archive/mvp-plan.md](../archive/mvp-plan.md), and
  [AGENTS.md](../../AGENTS.md).
- Agent guidance: Root AGENTS guidance now treats queue-destination event
  fan-out diagnostics as part of startup topology validation while still
  keeping broker provisioning app-owned.
- Application evidence: RabbitMQ and Service Bus topology validators compare
  queue-style event routes/destinations with configured provider receive
  bindings. Fast tests cover split receive-entity failures and same-queue
  in-process fan-out success for both providers.
- Pending or deferred: Broker topology declaration helpers and any public
  cross-provider diagnostic report object remain separate future decisions.

## Verification

- `dotnet build Bondstone.slnx --configuration Release --no-restore --disable-build-servers`
- `dotnet test Bondstone.slnx --configuration Release --no-build --filter "Category=Unit|Category=Application" --disable-build-servers`
- `pnpm format:check`
- `git diff --check`
