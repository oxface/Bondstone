# 0008 Thin Broker Adapters

Status: Accepted
Application: Applied
Date: 2026-06-16

## Context

ADR 0005 removed broker adapter packages because the previous transport
surface drifted into topology DSLs, startup diagnostics, provider-neutral
transport semantics, and receive runtime ownership. After the cleanup, the
remaining Bondstone boundary is clearer: durable envelopes, outbox dispatch,
inbox receive, and operation state stay in Bondstone; native broker topology
and policy stay in the application.

The next consumer need is ergonomic proof for common native broker drivers
without returning to a general bus/runtime. RabbitMQ.Client and
Azure.Messaging.ServiceBus are driver-level packages where a thin bridge can
remain honest. Rebus is already a bus abstraction with its own routing,
handlers, retries, subscriptions, serialization, and endpoint model, so a
Bondstone Rebus package would be abstraction over abstraction before a real
consumer need proves the value.

## Decision

Bondstone may ship thin RabbitMQ and Azure Service Bus adapter packages.

The accepted packages are:

- `Bondstone.Transport.RabbitMq`;
- `Bondstone.Transport.ServiceBus`.

Each package may provide:

- an `IDurableEnvelopeDispatcher` implementation over the native driver;
- opt-in builder extensions that register that dispatcher and mark outbox
  transport composition;
- a native receive worker only when explicitly registered by the host;
- receive worker options that identify an existing native queue/entity and an
  optional event subscriber binding;
- minimal native message metadata that helps operators inspect deliveries.

Each package must not provide:

- topology provisioning;
- queue/exchange/topic/subscription/rule DSLs;
- provider-neutral transport diagnostics;
- provider-neutral retry, dead-letter, or receive-attempt persistence;
- implicit worker registration;
- Rebus integration.

Core `IDurableEnvelopeReceiver` owns message-kind dispatch. Adapter receive
workers pass native message bodies plus optional
`DurableEnvelopeReceiveBinding` into that receiver, and then perform the
native success settlement. Failure settlement remains native and option-based
inside the explicitly registered worker.

## Consequences

The adapter packages make common driver integrations easier without changing
Bondstone's transport model. Applications still own native topology,
credentials, client registration, prefetch/concurrency, retry, dead-letter,
monitoring, and broker provisioning.

Receive workers are acceptable only as opt-in adapter ergonomics. Module
registrations remain domain/runtime declarations, not deployment topology.
Hosts can wrap worker registration in their own module host helpers, but
module implementation code should not own broker queues or subscriptions.

RabbitMQ and Service Bus adapter APIs should stay small. If either adapter
needs topology storage, subscription validation, retry orchestration, or a
provider-neutral diagnostic matrix, that is evidence to stop and create a
separate ADR instead of growing the adapter.

Rebus remains sample/application-owned guidance until a real consumer need
shows a package can add value without fighting Rebus's own bus model.

## Related Decisions

- Amends [0005 Transport Adapters And Receive Helpers](0005-transport-adapters-and-receive-helpers.md).
- Relates to [0002 Library Scope And Package Surface](0002-library-scope-and-package-surface.md).

## Application Notes

- Current contract: thin RabbitMQ and Azure Service Bus packages provide
  native-driver envelope dispatchers and explicitly registered receive workers.
  The receive workers call the shared `IDurableEnvelopeReceiver` helper, which
  owns command/event message-kind dispatch and optional event subscriber
  binding.
- Stable docs: package inventory, package discovery, messaging architecture,
  setup, testing, and public API docs describe the narrow adapter scope.
- Agent guidance: root `AGENTS.md` already requires ADR review before
  changing transport support or package boundaries.
- Application evidence: `Bondstone.Transport.RabbitMq` and
  `Bondstone.Transport.ServiceBus` are active solution packages with fast
  registration tests, Testcontainers-backed adapter integration tests, public
  API baselines, package documentation, setup examples, and sample-level
  extracted-service broker tests. Core receive helpers accept durable
  envelopes and serialized durable envelope payloads.
- Pending or deferred: Rebus adapter package remains deferred. Broker retry,
  settlement exhaustion, delivery counts, and dead-letter topology remain
  app-owned unless a narrower Bondstone-owned behavior is accepted.

## Verification

Verified with:

- `dotnet build Bondstone.slnx --configuration Release --disable-build-servers`
- `BONDSTONE_UPDATE_PUBLIC_API_BASELINE=1 dotnet test tests/Bondstone.PublicApi.Tests/Bondstone.PublicApi.Tests.csproj --configuration Release --no-build --filter "Category=Unit" --disable-build-servers`
- `dotnet test tests/Bondstone.Tests/Bondstone.Tests.csproj --configuration Release --no-build --filter "FullyQualifiedName~DurableEnvelopeReceiverTests" --disable-build-servers`
- `dotnet test tests/Bondstone.Transport.RabbitMq.Tests/Bondstone.Transport.RabbitMq.Tests.csproj --configuration Release --no-build --filter "Category=Unit" --disable-build-servers`
- `dotnet test tests/Bondstone.Transport.ServiceBus.Tests/Bondstone.Transport.ServiceBus.Tests.csproj --configuration Release --no-build --filter "Category=Unit" --disable-build-servers`
- `dotnet test tests/Bondstone.Transport.RabbitMq.Tests/Bondstone.Transport.RabbitMq.Tests.csproj --configuration Release --no-build --filter "Category=Integration" --disable-build-servers`
- `dotnet test tests/Bondstone.Transport.ServiceBus.Tests/Bondstone.Transport.ServiceBus.Tests.csproj --configuration Release --no-build --filter "Category=Integration" --disable-build-servers`
- `dotnet test tests/Bondstone.Samples.Tests/Bondstone.Samples.Tests.csproj --configuration Release --no-build --filter "FullyQualifiedName~ModularMonolithBrokerAdapterSampleTests" --disable-build-servers`
- `pnpm backend:test:integration`
- `pnpm check`
