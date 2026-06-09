# 0036 Direct Transport Adapters And Rebus Removal

Status: Amended
Application: Partially Applied
Date: 2026-06-09

## Context

Phase 5 proved first-class integration events by building on Rebus because the
project already had a Rebus command transport and receive loop. Phase 6 then
added thin outgoing proofs for Azure Service Bus and RabbitMQ to test whether
Bondstone's durable message model could survive provider-native topology
vocabulary.

That proof exposed an important product choice. Keeping Rebus as a supported
adapter means Bondstone adapts another bus abstraction while also defining its
own durable outbox, inbox, module execution, topology diagnostics, and
envelope shape. It duplicates Rebus routing concepts, forces Rebus vocabulary
into samples and docs, and makes Bondstone's direct provider design harder to
see.

Bondstone is not live yet, so there is no compatibility cost to removing Rebus
now. This affects package boundaries, transport support, sample architecture,
docs, tests, and ADR guidance, so it needs a durable decision before broad
implementation.

## Decision

Bondstone will remove `Bondstone.Transport.Rebus` as a supported package,
sample dependency, and test surface.

Direct provider adapters are the transport reference architecture:

- `Bondstone.Transport.ServiceBus` for Azure Service Bus vocabulary;
- `Bondstone.Transport.RabbitMq` for RabbitMQ vocabulary;
- future provider adapters only when they expose durable behavior Bondstone
  wants to own directly.

Bondstone transport packages should adapt broker/client SDKs directly instead
of adapting another message bus library. Provider adapters own the mapping
from Bondstone durable envelopes, command destinations, event publication,
subscriber bindings, diagnostics, and receive acknowledgement semantics into
the provider's native concepts. Applications still own provider-native
connection, credentials, worker, retry, dead-letter, serializer,
administration, subscription, queue, exchange, topic, and binding setup unless
a later ADR accepts a bounded helper.

Rebus remains useful historical design pressure. Existing Rebus ADRs explain
why the outbox, inbox, typed receive, event subscriber, and topology
diagnostic boundaries exist, but their package-support decisions are replaced
by this ADR.

The reusable receive behavior that was implemented inside the Rebus package
should move into provider-neutral Bondstone contracts or provider packages
where appropriate. Bondstone should not keep Rebus-named receive pipelines,
topology validators, telemetry, or envelope mappers after the package is
removed.

Multi-transport support remains an intended design constraint. Bondstone
should allow different durable message routes to use different direct
transport adapters when an application needs that shape. The current
single-transport outbox dispatcher can remain as a short-term proof surface,
but a follow-up slice should introduce an explicit transport selection model
before multi-transport samples are treated as supported guidance.

The modular monolith sample should stop depending on Rebus. During the reset,
the sample can either use a direct provider adapter proof or temporarily narrow
to the durable persistence and module execution pieces that are implemented.
It must not present Rebus as the recommended runtime path.

This ADR does not decide to remove Dapper internally. The PostgreSQL
persistence proof can use Dapper as an implementation detail, but package
naming and public provider identity should be decided in a separate
persistence packaging slice if the public `Dapper` package identity is
renamed.

## Amendment 2026-06-09: Provider-Native Event Destinations

Direct provider adapters should expose event destinations in the provider's
own terms. Events are still source facts with subscriber-owned receive
identity, but publish dispatch does not require every provider route to be a
topic. Service Bus may publish an event to a topic or queue. RabbitMQ may
publish an event to an exchange/routing key or directly to a queue through the
default exchange. Core should keep shared durable-message vocabulary at the
event-destination level and avoid a generic endpoint abstraction until more
direct provider receive work proves one is needed.

## Amendment 2026-06-09: Explicit Local Transport Package

The modular monolith sample should not own a bespoke transport implementation
or register raw local subscription lists. Bondstone will add
`Bondstone.Transport.Local` as an explicit in-process transport adapter for
samples, tests, and local development.

Local transport is configured through Bondstone transport routing vocabulary:
local queues, command module bindings, event routes, and subscriber bindings.
It is not a hidden fallback. An application must opt in with
`UseLocalTransport`, and a claimed durable outbox record is dispatched only
when the local topology explicitly matches the command target module or event
subscriber bindings.

Local transport dispatches through the provider-neutral
`IModuleCommandReceivePipeline` and `IModuleEventReceivePipeline`, so it still
exercises durable outbox claiming, receive inbox behavior, subscriber
identity, module execution, and module transaction boundaries. It does not
provide broker durability, network isolation, retry/dead-letter policy,
subscription storage, queue administration, or production transport guidance.

The direct provider adapters remain the production-oriented transport
direction. Local transport keeps the sample useful while direct RabbitMQ and
Service Bus receive workers are implemented and hardened.

## Amendment 2026-06-09: RabbitMQ Receive Dispatcher Proof

RabbitMQ receive work should start at the provider adapter boundary before
adding long-running broker consumers. `Bondstone.Transport.RabbitMq` will own
RabbitMQ receive queue bindings and a dispatcher that maps a received
Bondstone RabbitMQ transport message back to the neutral durable envelope.

RabbitMQ receive queue topology is configured in provider vocabulary with
`ReceiveQueue(...)`. A queue can accept command target modules and bind event
subscriber identities. Outgoing route topology remains separate: applications
still own exchanges, queues, and broker bindings, while Bondstone records what
a queue is allowed to execute once a provider-native consumer has received a
message from it.

The dispatcher calls `IModuleCommandReceivePipeline` for command envelopes and
`IModuleEventReceivePipeline` once per configured event subscriber. It returns
only after receive processing succeeds. Provider-native consumers should ack
RabbitMQ deliveries only after the dispatcher returns and should let thrown
exceptions flow into the app's RabbitMQ retry/dead-letter policy.

This amendment does not add a hosted RabbitMQ consumer, broker topology
declaration, retry policy, channel lifecycle management, or provider-backed
integration tests. Those remain follow-up slices.

## Amendment 2026-06-09: Service Bus Receive Dispatcher Proof

Service Bus receive work should mirror the RabbitMQ dispatcher proof while
keeping Service Bus receive vocabulary native. `Bondstone.Transport.ServiceBus`
will own receive source bindings and a dispatcher that maps a received
Bondstone Service Bus transport message back to the neutral durable envelope.

Service Bus receive topology is configured through receive sources:

- queues for command processors or direct event queues;
- topic subscriptions for broker fan-out event subscribers.

A receive source can accept command target modules and bind event subscriber
identities. Outgoing command/event destination topology remains separate:
applications still own queues, topics, subscriptions, rules, processors, retry,
dead-letter policy, credentials, and administration.

The dispatcher calls `IModuleCommandReceivePipeline` for command envelopes and
`IModuleEventReceivePipeline` once per configured event subscriber. It returns
only after receive processing succeeds. Provider-native processors should
complete Service Bus messages only after the dispatcher returns and should let
thrown exceptions flow into the app's Service Bus retry/dead-letter policy.

This amendment does not add a hosted Service Bus processor, processor lifecycle
management, broker topology declaration, retry policy, or provider-backed
integration tests. Those remain follow-up slices.

## Amendment 2026-06-09: Native Receive Message Mapping

Direct transport receive should expose a small native-message mapping layer
before Bondstone owns any hosted consumer lifecycle. RabbitMQ and Service Bus
adapters will map provider-native received messages into their package-local
Bondstone transport message shapes:

- RabbitMQ maps `BasicDeliverEventArgs` or body plus
  `IReadOnlyBasicProperties` into `RabbitMqTransportMessage`;
- Service Bus maps `ServiceBusReceivedMessage` into
  `ServiceBusTransportMessage`.

The mapping layer reads the Bondstone durable envelope body and stable message
identity from native message fields first, with Bondstone header/application
property fallback where appropriate. It does not acknowledge, complete,
abandon, dead-letter, retry, or start consumers/processors. Applications
remain responsible for native acknowledgement timing and should settle broker
messages only after the Bondstone receive dispatcher succeeds.

## Amendment 2026-06-09: Receive Settlement Handler Helpers

Direct transport adapters may provide small handler helpers that compose native
message mapping, Bondstone receive dispatch, and caller-supplied settlement
callbacks. These helpers are not hosted consumers or processors. They exist to
make the required ordering explicit:

1. map the native received message into the Bondstone transport message shape;
2. dispatch through the neutral receive pipeline;
3. acknowledge or complete the native broker message only after dispatch
   succeeds.

RabbitMQ provides `IRabbitMqReceivedMessageHandler`, which accepts a queue
name, `BasicDeliverEventArgs`, and a caller-supplied acknowledgement delegate.
Service Bus provides `IServiceBusReceivedMessageHandler`, which accepts a
receive source, `ServiceBusReceivedMessage`, and a caller-supplied completion
delegate. If dispatch throws, the helper must not acknowledge or complete the
message; the exception remains visible to the caller so native retry and
dead-letter policy can apply.

The helpers still do not own RabbitMQ channel lifecycle, Service Bus processor
lifecycle, retry policy, dead-letter behavior, broker topology declaration, or
provider-backed integration tests.

## Consequences

The transport surface becomes easier to reason about: Bondstone owns durable
message semantics and each transport adapter speaks one broker/provider
directly.

Removing Rebus will break existing local tests, sample code, and docs in this
repository. That is acceptable before public product use, but the reset should
be applied quickly enough that the repository does not linger in a half-Rebus
state.

Some receive-side code must be extracted or rebuilt. The reset should prefer
small provider-neutral receive contracts over copying Rebus-shaped APIs into
core.

Service Bus and RabbitMQ need follow-up receive slices before Phase 7 can
claim direct provider parity with the removed Rebus loop. Those slices should
cover command receive, event subscriber receive, acknowledgement semantics,
diagnostics, and provider-backed integration tests.

## Related Decisions

- [0018 Rebus Outbox Transport Adapter](0018-rebus-outbox-transport-adapter.md)
- [0023 Rebus Receive-Side Inbox Integration](0023-rebus-receive-side-inbox-integration.md)
- [0024 Rebus Typed Command Receive Pipeline](0024-rebus-typed-command-receive-pipeline.md)
- [0030 Rebus Command Topology Diagnostics](0030-rebus-command-topology-diagnostics.md)
- [0033 First-Class Event Publish/Subscribe Topology](0033-first-class-event-publish-subscribe-topology.md)
- [0034 Adapter Diversity Proof Transports](0034-adapter-diversity-proof-transports.md)

## Application Notes

- Current contract: Rebus is being removed from the supported package set.
  Direct Azure Service Bus and RabbitMQ adapters are the active transport
  direction. Core provider-neutral receive pipelines now carry reusable
  command and event subscriber receive behavior. Direct adapters use
  provider-native event destination vocabulary rather than assuming events are
  always topics.
- Stable docs: Packaging, architecture, setup, samples, testing, and MVP-plan
  docs no longer present Rebus as current supported transport guidance.
- Agent guidance: Root agent instructions should describe direct provider
  adapters, not Rebus adapter work, as the current transport direction.
- Application evidence: The Rebus package, Rebus tests, Rebus package
  versions, solution references, and sample references have been removed.
  Reusable receive behavior moved to `IModuleCommandReceivePipeline` and
  `IModuleEventReceivePipeline` in core. Composition tests now use the direct
  RabbitMQ adapter and core receive pipeline. The modular monolith sample uses
  explicit `Bondstone.Transport.Local` queue routing over the neutral receive
  pipelines until direct provider receive adapters exist. Service Bus event
  publish topology supports explicit topic destinations, explicit queue
  destinations, topic conventions, and queue conventions. RabbitMQ event
  publish topology supports exchange routing-key destinations and queue
  destinations through the default exchange. Core now provides
  `IDurableOutboxTransportRoute` and `RoutedDurableOutboxTransport`. Direct
  providers and local transport contribute route candidates based on their
  topology, and the router sends only when exactly one provider can send the
  claimed durable message. RabbitMQ now has receive queue topology, receive
  queue diagnostics, and an `IRabbitMqReceivedMessageDispatcher` proof that
  dispatches received Bondstone envelopes through the neutral receive
  pipelines. RabbitMQ also maps native received deliveries into
  `RabbitMqTransportMessage`. Service Bus now has receive source topology for
  queues and topic subscriptions, receive source diagnostics, and an
  `IServiceBusReceivedMessageDispatcher` proof that dispatches received
  Bondstone envelopes through the neutral receive pipelines. Service Bus also
  maps native `ServiceBusReceivedMessage` instances into
  `ServiceBusTransportMessage`.
- Provider receive handler helpers now prove the common settlement ordering:
  native acknowledgement/completion happens only after Bondstone dispatch
  succeeds, and failures propagate without settling the broker message.
- Pending or deferred: Add hosted RabbitMQ consumers, hosted Service Bus
  processors, provider-backed receive tests, persistence package naming
  cleanup, and replacement or supplementation of local sample transport with a
  preferred direct provider receive path.

## Verification

Read back this ADR and affected stable docs. Executable verification for the
applied reset slice:

- `dotnet build Bondstone.slnx --configuration Release --no-restore --disable-build-servers`
- `dotnet test Bondstone.slnx --configuration Release --no-build --filter "Category=Unit|Category=Application" --disable-build-servers`
- `dotnet test tests/Bondstone.Samples.Tests/Bondstone.Samples.Tests.csproj --configuration Release --no-build --filter "Category=Integration" --disable-build-servers`
- `dotnet restore Bondstone.slnx --disable-build-servers -p:NuGetAudit=false`
- `dotnet build Bondstone.slnx --configuration Release --no-restore --disable-build-servers`
- `pnpm format:check`
- `git diff --check`

After event destination and multi-transport selection work:

- `dotnet build Bondstone.slnx --configuration Release --no-restore --disable-build-servers`
- `dotnet test tests/Bondstone.Tests/Bondstone.Tests.csproj --configuration Release --no-build --filter "Category=Unit|Category=Application" --disable-build-servers`
- `dotnet test tests/Bondstone.Composition.Tests/Bondstone.Composition.Tests.csproj --configuration Release --no-build --filter "Category=Unit|Category=Application" --disable-build-servers`
- `dotnet test tests/Bondstone.Transport.ServiceBus.Tests/Bondstone.Transport.ServiceBus.Tests.csproj --configuration Release --no-build --filter "Category=Unit|Category=Application" --disable-build-servers`
- `dotnet test tests/Bondstone.Transport.RabbitMq.Tests/Bondstone.Transport.RabbitMq.Tests.csproj --configuration Release --no-build --filter "Category=Unit|Category=Application" --disable-build-servers`

After explicit local transport package work:

- `dotnet restore Bondstone.slnx --disable-build-servers -p:NuGetAudit=false`
- `dotnet build Bondstone.slnx --configuration Release --no-restore --disable-build-servers`
- `dotnet test tests/Bondstone.Transport.Local.Tests/Bondstone.Transport.Local.Tests.csproj --configuration Release --no-build --filter "Category=Unit|Category=Application" --disable-build-servers`
- `dotnet test tests/Bondstone.Tests/Bondstone.Tests.csproj --configuration Release --no-build --filter "Category=Unit|Category=Application" --disable-build-servers`
- `dotnet test Bondstone.slnx --configuration Release --no-build --filter "Category=Unit|Category=Application" --disable-build-servers`
- `dotnet test tests/Bondstone.Samples.Tests/Bondstone.Samples.Tests.csproj --configuration Release --no-build --filter "Category=Integration" --disable-build-servers`
- `pnpm format:check`
- `git diff --check`

After RabbitMQ receive dispatcher proof:

- `dotnet build Bondstone.slnx --configuration Release --no-restore --disable-build-servers`
- `dotnet test tests/Bondstone.Transport.RabbitMq.Tests/Bondstone.Transport.RabbitMq.Tests.csproj --configuration Release --no-build --filter "Category=Unit|Category=Application" --disable-build-servers`

After Service Bus receive dispatcher proof:

- `dotnet build Bondstone.slnx --configuration Release --no-restore --disable-build-servers`
- `dotnet test tests/Bondstone.Transport.ServiceBus.Tests/Bondstone.Transport.ServiceBus.Tests.csproj --configuration Release --no-build --filter "Category=Unit|Category=Application" --disable-build-servers`

After native receive message mapping:

- `dotnet build Bondstone.slnx --configuration Release --no-restore --disable-build-servers`
- `dotnet test tests/Bondstone.Transport.RabbitMq.Tests/Bondstone.Transport.RabbitMq.Tests.csproj --configuration Release --no-build --filter "Category=Unit|Category=Application" --disable-build-servers`
- `dotnet test tests/Bondstone.Transport.ServiceBus.Tests/Bondstone.Transport.ServiceBus.Tests.csproj --configuration Release --no-build --filter "Category=Unit|Category=Application" --disable-build-servers`

After receive settlement handler helpers:

- `dotnet build Bondstone.slnx --configuration Release --no-restore --disable-build-servers`
- `dotnet test tests/Bondstone.Transport.RabbitMq.Tests/Bondstone.Transport.RabbitMq.Tests.csproj --configuration Release --no-build --filter "Category=Unit|Category=Application" --disable-build-servers`
- `dotnet test tests/Bondstone.Transport.ServiceBus.Tests/Bondstone.Transport.ServiceBus.Tests.csproj --configuration Release --no-build --filter "Category=Unit|Category=Application" --disable-build-servers`
