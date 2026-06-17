# Package Discovery

This guide maps common Bondstone capabilities to NuGet package IDs and the
namespaces that expose the setup APIs most consumers import. For a complete
working host shape, start with the canonical setup path in [setup.md](setup.md).
For package boundaries, dependency direction, target framework, and publishing
policy, including v2 replacement/migration guidance, see
[packaging.md](packaging.md). For production operations and observability, see
[operations.md](operations.md) and [observability.md](observability.md).

## Capability Matrix

| Capability                                                                                                                                            | Package ID                                           | Common namespaces                                                                               | Notes                                                                                                                                                                                                               |
| ----------------------------------------------------------------------------------------------------------------------------------------------------- | ---------------------------------------------------- | ----------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Host composition, module registration, command/event contracts, durable send/publish, module execution, payload serialization, domain event contracts | `Bondstone`                                          | `Bondstone.Configuration`, `Bondstone.Modules`, `Bondstone.Messaging`, `Bondstone.DomainEvents` | The normal entrypoint is `services.AddBondstone(...)`; modules register commands, events, durable messaging, and optional persistence through the builder.                                                          |
| Provider-neutral durable persistence contracts, envelopes, outbox, inbox, operation state, dispatcher and inspection contracts                        | `Bondstone.Persistence`                              | `Bondstone.Persistence`, `Bondstone.Messaging`                                                  | Use directly for custom persistence, custom dispatch composition, terminal outbox inspection, low-level inbox/outbox work, and operation-state reads.                                                               |
| EF Core durable persistence mappings, module transaction behavior, and EF-backed domain event persistence                                             | `Bondstone.Persistence.EntityFrameworkCore`          | `Bondstone.Persistence.EntityFrameworkCore.Persistence`                                         | Provides `ApplyBondstonePersistence(...)`, granular EF mapping helpers, `UseEntityFrameworkCorePersistence<TDbContext>()`, `UseEntityFrameworkCoreDomainEventPersistence()`, and `ApplyBondstoneDomainEvents(...)`. |
| EF Core plus PostgreSQL durable persistence helpers                                                                                                   | `Bondstone.Persistence.EntityFrameworkCore.Postgres` | `Bondstone.Persistence.EntityFrameworkCore.Postgres.Persistence`                                | Preferred EF/PostgreSQL module setup is `module.UsePostgreSqlPersistence<TDbContext>(connectionString, schema: ...)`.                                                                                               |
| Hosted durable outbox worker and default dispatcher registration                                                                                      | `Bondstone.Hosting`                                  | `Bondstone.Hosting.Outbox`                                                                      | Normal hosts use `bondstone.Outbox.UseWorker(...)`; advanced schedulers can use the dispatcher registration separately.                                                                                             |
| Explicit local in-process transport for samples, tests, and local development                                                                         | `Bondstone.Transport.Local`                          | `Bondstone.Transport.Local.Outbox`                                                              | Use `UseLocalTransport(...)` when the host intentionally routes through local queues and Bondstone receive pipelines.                                                                                               |
| Thin RabbitMQ envelope adapter                                                                                                                        | `Bondstone.Transport.RabbitMq`                       | `Bondstone.Transport.RabbitMq`                                                                  | Use `UseRabbitMqDispatcher(...)` and optional `UseRabbitMqReceiveWorker(...)` when the app already owns RabbitMQ topology and client/channel registration.                                                          |
| Thin Azure Service Bus envelope adapter                                                                                                               | `Bondstone.Transport.ServiceBus`                     | `Bondstone.Transport.ServiceBus`                                                                | Use `UseServiceBusDispatcher(...)` and optional `UseServiceBusReceiveWorker(...)` when the app already owns Service Bus topology and client registration.                                                           |

Package IDs match project names. The full package inventory and dependency
direction live in [packaging.md](packaging.md).

## Common Imports

Most applications start with the core setup namespaces:

```csharp
using Bondstone.Configuration; // AddBondstone, BondstoneBuilder
using Bondstone.Modules; // IBondstoneModule, module builders, handlers
using Bondstone.Messaging; // ICommand, IDurableCommand, IIntegrationEvent
using Bondstone.DomainEvents; // IDomainEvent, IDomainEventSource
```

`Bondstone.Messaging` also contains durable message identity attributes,
`IDurableCommandSender`, `IDurableEventPublisher`,
`IDurableOperationResultReader`, durable send/publish results, operation
result types, durable operation handles and state records, durable message
envelopes, and trace context records.

`Bondstone.Modules` contains module registration and execution contracts:
`IBondstoneModule`, `BondstoneModuleBuilder`, `ICommandHandler<TCommand>`,
`ICommandHandler<TCommand, TResult>`,
`IIntegrationEventHandler<TEvent>`, command validators, receive pipelines,
module execution context contracts, and the module command/event execution
result types.

## Core Module And Command APIs

Use these imports when declaring durable commands, integration events, module
registrations, and handlers:

```csharp
using Bondstone.Configuration;
using Bondstone.Messaging;
using Bondstone.Modules;
```

Common setup and registration APIs include:

- `services.AddBondstone(...)` from `Bondstone.Configuration`;
- `bondstone.AddModule(...)` and `bondstone.Module(...)` from
  `Bondstone.Modules`;
- `module.UseDurableMessaging()` from `Bondstone.Modules`;
- `module.Commands.RegisterFromAssemblyContaining<TMarker>()`,
  `module.Commands.RegisterHandler<TCommand, THandler>()`, and
  `module.Commands.RegisterValidator<TCommand, TValidator>()` from
  `Bondstone.Modules`;
- `module.Events.RegisterPublishedEvent<TEvent>()` and
  `module.Events.RegisterSubscriber<TEvent, THandler>(subscriberIdentity)`
  from `Bondstone.Modules`;
- `IDurableCommandSender`, `IDurableEventPublisher`,
  `IDurableOperationReader`, and `IDurableOperationResultReader` from
  `Bondstone.Messaging`.

## Persistence Abstractions

Use `Bondstone.Persistence` when implementing custom persistence, custom
outbox dispatch, low-level inbox handling, or custom transport composition:

```csharp
using Bondstone.Persistence;
using Bondstone.Messaging;
```

Common contracts include `IDurableOutboxWriter`,
`IDurableOutboxDispatcher`, `IDurableOutboxInspector`,
`IDurableOutboxInspectionStore`, `IDurableEnvelopeDispatcher`,
`IDurableEnvelopeDispatchRoute`, `IDurableInboxHandlerExecutor`,
`IDurableInboxInspector`, `IDurableInboxInspectionStore`,
`IDurableInboxRegistrar`, `IDurableInboxStore`,
`IDurableOperationStateStore`, `IDurableOutboxClaimer`,
`IDurableOutboxLeaseRenewer`, `IDurableOutboxDispatchRecorder`,
`IDurableOutboxFailurePolicy`, `DurableInboxMessageKey`,
`DurableInboxRecord`, `DurableOutboxRecord`,
`DurableOutboxDispatchState`, and `DurableInboxAlreadyReceivedException`.

`DurableMessageEnvelope`, `MessageKind`, `MessageTraceContext`,
`DurableOperationState`, `DurableOperationStatus`,
`DurableOperationHandle`, `IDurableOperationReader`, `IDurableOperationFinalizer`,
`IDurableOperationExpirationProcessor`,
`DurableOperationFinalizationResult`, and
`DurableOperationExpirationResult` are in `Bondstone.Messaging`.
`IDurableOperationReader` and `IDurableOperationResultReader` include
module-hinted read overloads for callers that know the result-owning module.

`IDurableOperationExpirationStore` is in `Bondstone.Persistence` for provider
stores that support app-owned operation expiry jobs.

## Broker Integration

Bondstone ships thin RabbitMQ and Azure Service Bus packages for native-driver
envelope plumbing. Those packages do not own topology, provisioning,
subscription storage, retry, dead-letter policy, prefetch/concurrency, or
monitoring.

Normal hosts register one outbound `IDurableEnvelopeDispatcher`. If a host
needs more than one outbound transport, compose one explicit aggregate
dispatcher instead of registering multiple built-in dispatchers and expecting
Bondstone to infer ownership.

Custom or app-owned broker integrations normally use:

- `UseDurableEnvelopeDispatcher<TDispatcher>()` from `Bondstone.Configuration`
  to register the outbound dispatcher and satisfy outbox composition
  validation;
- `IDurableEnvelopeDispatcher` from `Bondstone.Persistence` to publish claimed
  outbox records through the chosen transport;
- `IDurableEnvelopeDispatchRoute` and `RoutedDurableEnvelopeDispatcher` from
  `Bondstone.Persistence` when the app explicitly needs route-aware outbound
  dispatch across more than one transport;
- `IDurableMessageEnvelopeSerializer` from `Bondstone.Messaging` to write and
  read `DurableMessageEnvelope` payloads;
- `IDurableEnvelopeReceiver` from `Bondstone.Messaging` to execute received
  command envelopes or explicitly selected event subscribers through
  Bondstone's inbox and module transaction boundary.

The application or transport library owns broker topology, subscriptions,
native consumers, ack/nack/settlement, retry, dead-letter policy, prefetch,
and monitoring. Bondstone-owned receive, settlement, stale inbox, and terminal
outbox guidance is centralized in [operations.md](operations.md).

The built-in RabbitMQ receive worker consumes with manual acknowledgement and
uses `RequeueOnFailure` only as the native nack requeue flag. The built-in
Azure Service Bus receive worker exposes native `ServiceBusProcessorOptions`
for advanced driver configuration, but requires `AutoCompleteMessages = false`
so Bondstone completes messages after durable receive succeeds.

Rebus remains app-owned guidance rather than a Bondstone package because Rebus
already owns bus routing, handlers, subscriptions, retries, error queues,
serialization, and endpoint lifecycle.

## EF Core Mappings

Use this import for EF Core durable mapping and module persistence helpers:

```csharp
using Bondstone.Persistence.EntityFrameworkCore.Persistence;
```

Common APIs include:

- `modelBuilder.ApplyBondstonePersistence(schema)` for outbox, inbox, and
  operation-state mappings;
- `modelBuilder.ApplyBondstoneOutbox(schema)`;
- `modelBuilder.ApplyBondstoneInbox(schema)`;
- `modelBuilder.ApplyBondstoneOperationState(schema)`;
- `module.UseEntityFrameworkCorePersistence<TDbContext>()` for
  provider-neutral EF module transactions and root EF durable stores;
- `services.AddBondstoneEntityFrameworkCorePersistence<TDbContext>()` for
  advanced non-module EF durable store registration.

Provider-neutral EF entity types live under
`Bondstone.Persistence.EntityFrameworkCore.Outbox`,
`Bondstone.Persistence.EntityFrameworkCore.Inbox`, and
`Bondstone.Persistence.EntityFrameworkCore.Operations`. Normal consumers
usually import only the `.Persistence` namespace and call the mapping helpers.

## PostgreSQL Helpers

For EF-backed PostgreSQL modules, import:

```csharp
using Bondstone.Persistence.EntityFrameworkCore.Postgres.Persistence;
```

Common APIs include:

- `module.UsePostgreSqlPersistence<TDbContext>(connectionString, schema: ...)`
  for module-owned EF/PostgreSQL durable messaging;
- `bondstone.UsePostgreSqlPersistence<TDbContext>(moduleName, connectionString, schema: ...)`
  when binding a named module from the root builder;
- `services.AddBondstonePostgreSqlPersistence<TDbContext>(...)` and
  `services.AddBondstonePostgreSqlModulePersistence<TDbContext>(...)` for
  advanced composition.

The direct non-EF `Bondstone.Persistence.Postgres` package was removed after
MVP. EF Core plus `Bondstone.Persistence.EntityFrameworkCore.Postgres` is the
supported PostgreSQL persistence path.

## Hosting And Outbox Worker

Use this import for normal hosted outbox dispatch:

```csharp
using Bondstone.Hosting.Outbox;
```

Common APIs include:

- `bondstone.Outbox.UseWorker(options => ...)` for the default durable
  dispatcher plus hosted worker;
- `bondstone.Outbox.UseDurableDispatcher()` when a host wants the default
  dispatcher but not the built-in worker;
- `DurableOutboxWorkerOptions` for worker id, batch size, polling interval,
  lease duration, and failure delay;
- `services.AddBondstoneDurableOutboxWorker(...)` and
  `services.AddBondstoneDurableOutboxDispatcher()` for advanced or custom
  scheduler composition.

## Local Transport

Use this import for local in-process queue routing:

```csharp
using Bondstone.Transport.Local.Outbox;
```

Common APIs include:

- `bondstone.UseLocalTransport(local => ...)`;
- `local.UseModuleQueueConvention()` for complete module command topology
  using `{module}.commands`;
- `local.RouteModule(targetModule).ToQueue(queueName)` plus
  `local.Queue(queueName).AcceptModule(targetModule)` for explicit command
  topology;
- `local.RouteEvent(messageTypeName).ToQueue(queueName)` plus
  `local.Queue(queueName).SubscribeEvent(messageTypeName, subscriberModule, subscriberIdentity)`
  for explicit event subscriber topology.

Local transport is explicit routing for samples, tests, and local development.
It exercises the durable outbox and receive inbox semantics through
Bondstone's neutral receive pipelines, but it is not broker durability, a
production fallback, topology management, retry, dead-letter handling, or
durable inbox worker behavior.
See [architecture/transport-local.md](architecture/transport-local.md).

## Domain Events

Use this import for module-local domain event contracts:

```csharp
using Bondstone.DomainEvents;
```

Common contracts include `IDomainEvent`, `DomainEventIdentityAttribute`, and
`IDomainEventSource`. Domain events
are module-local facts; they are not durable transport messages and are not
automatically published as integration events.

For EF-backed domain event persistence, import:

```csharp
using Bondstone.Persistence.EntityFrameworkCore.Persistence;
```

Common APIs include:

- `module.UseEntityFrameworkCoreDomainEventPersistence()` for the EF behavior
  that collects and stages module-local domain event records inside the EF
  module transaction;
- `modelBuilder.ApplyBondstoneDomainEvents(schema)` for the domain event
  record mapping.

The EF bridge is optional and separate from
`ApplyBondstonePersistence(...)`; domain event records are module-local
records, not outbox records.

## Related Docs

- [setup.md](setup.md) is the canonical golden-path setup example.
- [packaging.md](packaging.md) owns package IDs and dependency direction.
- [operations.md](operations.md) describes production receive, outbox, inbox,
  operation-state, migration, retention, and app-owned recovery guidance.
- [observability.md](observability.md) describes current diagnostics and the
  OpenTelemetry-native direction.
- [public-api.md](public-api.md) classifies the current public API surface.
- [architecture/messaging.md](architecture/messaging.md) describes commands,
  integration events, domain events, receive pipelines, and transport
  boundaries.
- [architecture/persistence-core.md](architecture/persistence-core.md)
  describes provider-neutral persistence contracts.
- [architecture/persistence-ef-core.md](architecture/persistence-ef-core.md)
  describes EF Core mappings and transaction behavior.
- [architecture/persistence-postgresql.md](architecture/persistence-postgresql.md)
  describes EF/PostgreSQL provider behavior.
- [architecture/hosting.md](architecture/hosting.md) describes the durable
  outbox worker.
- [architecture/transport-local.md](architecture/transport-local.md)
  describes local transport topology and receive semantics.
