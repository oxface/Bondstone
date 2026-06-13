# 0034 Adapter Diversity Proof Transports

Status: Amended
Application: Applied
Date: 2026-06-09

## Context

Phase 5 completed the current first-class event loop with explicit
integration event publishing, subscriber execution, Rebus topic/subscription
topology, per-subscriber inbox behavior, EF subscriber transactions, event
diagnostics, transport-backed Rebus tests, and a modular monolith sample
proof.

Bondstone should not harden its transport API around Rebus vocabulary alone.
ADR 0033 explicitly called for adapter-diversity proof slices using Azure
Service Bus and RabbitMQ after the first-class event loop had enough shape.
Those slices affect public package boundaries, provider support, transport
topology vocabulary, test infrastructure, and sample direction, so they need
a durable decision before broad implementation.

## Decision

Bondstone will add thin proof-oriented transport adapter packages:

- `Bondstone.Transport.ServiceBus`
- `Bondstone.Transport.RabbitMq`

The first slice for each package is outgoing durable outbox dispatch only.
Each adapter implements `IDurableOutboxTransport` for claimed
`DurableOutboxRecord` values, maps Bondstone durable envelopes to a
transport-native message body and headers, and exposes provider-native
topology builders and diagnostics for commands and integration events.

The Service Bus adapter uses Azure Service Bus vocabulary:

- commands send to queues by target module;
- integration events publish to topics by stable event identity;
- future receive work will bind module command handlers to queues and event
  subscribers to topic subscriptions;
- queue, topic, subscription, rule, processor, retry, dead-letter, connection,
  credential, and administration setup remain application-owned or
  provider-native.

The RabbitMQ adapter uses RabbitMQ vocabulary:

- commands publish to an exchange with a routing key resolved by target
  module, with direct queue delivery as an optional convenience if the client
  surface supports it cleanly;
- integration events publish to an exchange with a routing key resolved by
  stable event identity;
- future receive work will bind queues to exchanges/routing keys for command
  modules and event subscribers;
- exchange, queue, binding, connection, channel, consumer, acknowledgement,
  retry, dead-letter, prefetch, and topology declaration remain
  application-owned or provider-native.

Both proof adapters keep the Bondstone durable message envelope as the
internal transport payload. External event handoff or unwrapped/CloudEvents
payload formats remain separate decisions. The proof adapters should not add
a generic cross-provider endpoint abstraction in core. If common concepts
emerge across Rebus, Service Bus, and RabbitMQ, record narrow follow-up
slices before extracting shared abstractions.

Adapter package dependencies stay one-way: the new transport packages depend
on `Bondstone` and their provider client libraries only. They do not depend
on Rebus, EF Core, PostgreSQL, hosting, samples, or each other.

Testing stays layered. Initial adapter proof tests use fast test doubles for
client calls, mapping, topology diagnostics, and service registration.
Provider-backed integration tests are explicit later slices because they need
real broker infrastructure and sharper decisions about receive lifecycle,
subscriptions, acknowledgement, retry, and dead-letter behavior.

The modular monolith sample may be tightened with additional explicit
integration events to stress event vocabulary, but it remains a Rebus sample
until a later sample or test fixture intentionally demonstrates a different
transport. Do not turn the sample into a broker matrix.

## Amendment 2026-06-09: Event Destinations

Direct provider adapters should use event destination vocabulary when the
provider supports more than topics for event publication. Service Bus event
routes can publish to a topic for broker fan-out or to a queue when another
piece of infrastructure owns fan-out. RabbitMQ event routes can publish
through an exchange/routing key or directly to a queue through the default
exchange. These are explicit provider-native choices, not a generic core
endpoint abstraction.

## Consequences

Bondstone gets early design pressure from Service Bus and RabbitMQ without
claiming full production support for receive workers, broker administration,
or provider-backed reliability semantics.

Provider-native vocabulary will duplicate some concepts across adapters. That
duplication is intentional during proof work and should be consolidated only
after multiple adapters expose real repeated complexity.

Outgoing dispatch can be implemented and verified quickly. Receive-side
adapters, broker-backed tests, subscription lifecycle, acknowledgement
policy, retry/dead-letter behavior, and topology declaration remain future
slices.

## Related Decisions

- [0004 Positioning And Service Extraction Path](0004-positioning-and-service-extraction-path.md)
- [0018 Rebus Outbox Transport Adapter](0018-rebus-outbox-transport-adapter.md)
- [0026 Event Shape Guardrail](0026-event-shape-guardrail.md)
- [0029 Durable Payload Serialization Boundary](0029-durable-payload-serialization-boundary.md)
- [0033 First-Class Event Publish/Subscribe Topology](0033-first-class-event-publish-subscribe-topology.md)

## Application Notes

- 2026-06-09 reset: The Rebus sample/reference-adapter portions of this ADR
  have been superseded by
  [ADR 0036](0036-direct-transport-adapters-and-rebus-removal.md). Direct
  Service Bus and RabbitMQ adapters are now the active transport direction.
- Current contract: Phase 6 adapter-diversity proof includes outgoing durable
  outbox transports, provider-native receive topology, direct receive
  dispatchers, settlement helpers, opt-in hosted receive workers, and
  provider-backed receive tests for Service Bus and RabbitMQ. Each adapter
  keeps provider-native topology vocabulary and app-owned broker setup. Event
  publication routes to provider-native destinations: Service Bus topic or
  queue, and RabbitMQ exchange/routing-key or queue.
- Stable docs: Package names and proof scope are reflected in
  [docs/packaging.md](../packaging.md),
  [docs/architecture/README.md](../architecture/README.md),
  [docs/architecture/transport-servicebus.md](../architecture/transport-servicebus.md),
  [docs/architecture/transport-rabbitmq.md](../architecture/transport-rabbitmq.md),
  [docs/archive/mvp-plan.md](../archive/mvp-plan.md), and this ADR. Provider-specific
  architecture docs now describe the outgoing proof scope and deferred receive
  work.
- Agent guidance: Root [AGENTS.md](../../AGENTS.md) directs adapter-diversity
  proof work to stay provider-native, app-owned, and slice-based.
- Application evidence: Package scaffolds, outgoing dispatch implementations,
  diagnostics, service registration, receive topology, receive dispatchers,
  settlement helpers, opt-in hosted receive workers, focused fast tests, and
  provider-backed receive tests are applied for
  `Bondstone.Transport.ServiceBus` and `Bondstone.Transport.RabbitMq`. The
  modular monolith sample includes explicit integration events and a preferred
  RabbitMQ path without becoming a broker matrix.
- Pending or deferred: None for the adapter-diversity proof decision. Broker
  topology declaration, external event wire formats, any public cross-provider
  diagnostic report object, and deeper provider reliability matrices remain
  separate future decisions. The non-EF persistence proof is handled by
  [ADR 0035](0035-postgresql-dapper-persistence-proof.md).

## Verification

Read back this ADR and affected stable docs before implementation.

Executable verification for the first applied slice:

- `dotnet restore Bondstone.slnx --disable-build-servers -p:NuGetAudit=false`
- `dotnet build Bondstone.slnx --configuration Release --no-restore --disable-build-servers`
- `dotnet test tests/Bondstone.Transport.ServiceBus.Tests/Bondstone.Transport.ServiceBus.Tests.csproj --configuration Release --no-build --filter "Category=Unit|Category=Application" --disable-build-servers`
- `dotnet test tests/Bondstone.Transport.RabbitMq.Tests/Bondstone.Transport.RabbitMq.Tests.csproj --configuration Release --no-build --filter "Category=Unit|Category=Application" --disable-build-servers`
- `dotnet test tests/Bondstone.Samples.Tests/Bondstone.Samples.Tests.csproj --configuration Release --no-build --filter "Category=Integration" --disable-build-servers`
- `dotnet test Bondstone.slnx --configuration Release --no-build --filter "Category=Unit|Category=Application" --disable-build-servers`
- `pnpm format:check`
- `git diff --check`

After the event destination amendment, focused verification was rerun with:

- `dotnet build Bondstone.slnx --configuration Release --no-restore --disable-build-servers`
- `dotnet test tests/Bondstone.Transport.ServiceBus.Tests/Bondstone.Transport.ServiceBus.Tests.csproj --configuration Release --no-build --filter "Category=Unit|Category=Application" --disable-build-servers`
- `dotnet test tests/Bondstone.Transport.RabbitMq.Tests/Bondstone.Transport.RabbitMq.Tests.csproj --configuration Release --no-build --filter "Category=Unit|Category=Application" --disable-build-servers`
