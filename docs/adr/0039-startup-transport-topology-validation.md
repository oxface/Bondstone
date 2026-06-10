# 0039 Startup Transport Topology Validation

Status: Amended
Application: Applied
Date: 2026-06-10

## Context

ADR 0033 made integration events first-class durable messages with explicit
publish routes and per-subscriber receive inbox identity. ADR 0036 made direct
provider adapters the current transport architecture. ADR 0038 clarified that
Bondstone should describe and validate durable message topology without owning
broker retry/dead-letter policy or broker administration.

Phase 7 now needs earlier feedback for miswired durable topology. Without
startup validation, applications can register command handlers, event
publishers, event subscribers, command receive queues, and event receive
bindings that do not line up until the first outbox dispatch or broker
delivery fails.

The validation must not become provisioning. RabbitMQ exchanges, queues,
bindings, DLX/retry topology, and Service Bus queues, topics, subscriptions,
rules, and retry/dead-letter settings remain app-owned and provider-native.

## Decision

Bondstone direct transport adapters should provide startup topology validation
over the durable message topology they are configured to own.

Provider validation should use provider-native diagnostic vocabulary and the
same route and receive binding metadata used by dispatch. The validation may
fail fast when:

- a registered durable command route has no provider command destination in a
  single-transport host;
- a registered published integration event has no provider event destination
  in a single-transport host;
- a registered event subscriber has no provider receive binding in a
  single-transport host;
- a provider receive command binding accepts a module with no registered
  durable command handler;
- a provider receive event binding names a subscriber that is not registered
  by the module.

Multi-transport hosts must not be overvalidated by provider-local validators.
Provider-local validation can always reject invalid receive bindings declared
by that provider, because those bindings explicitly name Bondstone execution
the provider is allowed to invoke. Missing outbound route validation should be
bounded to the single-transport case until a later cross-provider diagnostic
model can report aggregate route coverage and ambiguous route ownership.

Topology validation and reporting remain separate from broker topology
creation. This ADR does not accept automatic exchange, queue, binding, topic,
subscription, rule, retry, or dead-letter provisioning.

## Amendment 2026-06-10: Aggregate Outbound Route Ownership

Provider-local validators are not enough for hosts that configure multiple
Bondstone transports. Multi-transport hosts need startup validation that counts
route ownership across all configured transport diagnostic sources before a
durable outbox record reaches `RoutedDurableOutboxTransport`.

Bondstone core should therefore aggregate provider route diagnostics for:

- registered durable command handler target modules;
- registered published integration events.

For each command target module or published event, exactly one configured
transport diagnostic source should report an outbound route. Zero matching
routes and multiple matching routes are startup configuration errors when
diagnostic sources are available.

This aggregate validation remains limited to Bondstone outbound durable
message ownership. Provider receive binding validation stays provider-local,
and split-service subscriber fan-out mismatch reporting remains deferred until
a later diagnostic model can represent deployment shape.

## Consequences

Applications get earlier, clearer errors for common durable topology mistakes.

Provider diagnostics remain provider-specific and can continue to use
RabbitMQ queue/exchange/routing-key vocabulary and Service Bus
queue/topic/subscription vocabulary.

Receive-only or mixed-provider services are not forced by a provider-local
validator to define outbound routes that another provider or service owns.

Fan-out mismatch reporting for split subscriber deployment and broker topology
declaration helpers remain future ADR-backed work.

## Related Decisions

- [0033 First-Class Event Publish/Subscribe Topology](0033-first-class-event-publish-subscribe-topology.md)
- [0036 Direct Transport Adapters And Rebus Removal](0036-direct-transport-adapters-and-rebus-removal.md)
- [0038 Provider Retry Recovery And Settlement Boundaries](0038-provider-retry-recovery-and-settlement-boundaries.md)
- [0040 Event Queue Fan-Out Diagnostics](0040-event-queue-fanout-diagnostics.md)

## Application Notes

- Current contract: RabbitMQ and Service Bus startup validation should check
  their configured receive bindings against registered Bondstone command
  handlers and event subscribers. Core aggregates configured transport
  diagnostic sources to validate that registered durable command target
  modules and published events have exactly one outbound transport route,
  while keeping broker provisioning app-owned.
- Stable docs: The contract is reflected in
  [docs/architecture/messaging.md](../architecture/messaging.md),
  [docs/architecture/modules.md](../architecture/modules.md),
  [docs/architecture/transport-rabbitmq.md](../architecture/transport-rabbitmq.md),
  [docs/architecture/transport-servicebus.md](../architecture/transport-servicebus.md),
  [docs/testing.md](../testing.md), and [docs/mvp-plan.md](../mvp-plan.md).
- Agent guidance: Root AGENTS guidance already requires ADRs before broad
  transport behavior changes and now references this validation boundary.
- Application evidence: RabbitMQ and Service Bus transport validators run from
  the full `BondstoneBuilder` transport extensions. Local, RabbitMQ, and
  Service Bus contribute transport diagnostic sources for aggregate outbound
  route ownership validation. Core records module-owned published-event
  metadata so validators can distinguish events published by the current app
  from events that are only subscribed, and the durable event publisher
  requires the source module to have declared the published event.
- Pending or deferred: Broker topology declaration helpers and any public
  report object beyond startup validation and existing provider diagnostics.
  Queue-destination event fan-out diagnostics are now covered by ADR 0040.

## Verification

- `dotnet build Bondstone.slnx --configuration Release --no-restore --disable-build-servers`
- Focused fast tests for:
  - `tests/Bondstone.Tests`
  - `tests/Bondstone.Transport.RabbitMq.Tests`
  - `tests/Bondstone.Transport.ServiceBus.Tests`
- `dotnet test Bondstone.slnx --configuration Release --no-build --filter "Category=Unit|Category=Application" --disable-build-servers`
- `pnpm format:check`
- `git diff --check`
