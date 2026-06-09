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
  claimed durable message.
- Pending or deferred: Add direct RabbitMQ receive, direct Service Bus
  receive, provider-backed receive tests, persistence package naming cleanup,
  and replacement or supplementation of local sample transport with a
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
