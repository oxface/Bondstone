# Package Discovery

This guide maps common Bondstone capabilities to NuGet package IDs and the
namespaces that expose the setup APIs most consumers import. For a complete
working host shape, start with the canonical setup path in [setup.md](setup.md).
For package boundaries, dependency direction, target framework, and publishing
policy, see [packaging.md](packaging.md).

## Capability Matrix

| Capability                                                                                                                    | Package ID                                                | Common namespaces                                                           | Notes                                                                                                                                                      |
| ----------------------------------------------------------------------------------------------------------------------------- | --------------------------------------------------------- | --------------------------------------------------------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Host composition, module registration, command/event contracts, durable send/publish, module execution, payload serialization | `Bondstone`                                               | `Bondstone.Configuration`, `Bondstone.Modules`, `Bondstone.Messaging`       | The normal entrypoint is `services.AddBondstone(...)`; modules register commands, events, durable messaging, and optional persistence through the builder. |
| Provider-neutral durable persistence contracts, envelopes, outbox, inbox, operation state, dispatcher contracts               | `Bondstone.Persistence`                                   | `Bondstone.Persistence`, `Bondstone.Messaging`                              | Use directly for custom persistence, custom dispatch composition, low-level inbox/outbox work, and operation-state reads.                                  |
| EF Core durable persistence mappings and module transaction behavior                                                          | `Bondstone.Persistence.EntityFrameworkCore`               | `Bondstone.Persistence.EntityFrameworkCore.Persistence`                     | Provides `ApplyBondstonePersistence(...)`, granular EF mapping helpers, and `UseEntityFrameworkCorePersistence<TDbContext>()`.                             |
| EF Core plus PostgreSQL durable persistence helpers                                                                           | `Bondstone.Persistence.EntityFrameworkCore.Postgres`      | `Bondstone.Persistence.EntityFrameworkCore.Postgres.Persistence`            | Preferred EF/PostgreSQL module setup is `module.UsePostgreSqlPersistence<TDbContext>(connectionString, schema: ...)`.                                      |
| Hosted durable outbox worker and default dispatcher registration                                                              | `Bondstone.Hosting`                                       | `Bondstone.Hosting.Outbox`                                                  | Normal hosts use `bondstone.Outbox.UseWorker(...)`; advanced schedulers can use the dispatcher registration separately.                                    |
| Explicit local in-process transport for samples, tests, and local development                                                 | `Bondstone.Transport.Local`                               | `Bondstone.Transport.Local.Outbox`                                          | Use `UseLocalTransport(...)` when the host intentionally routes through local queues and Bondstone receive pipelines.                                      |
| RabbitMQ direct transport adapter                                                                                             | `Bondstone.Transport.RabbitMq`                            | `Bondstone.Transport.RabbitMq.Outbox`, `Bondstone.Transport.RabbitMq.Inbox` | Thin direct adapter and sample broker path with provider-native vocabulary and opt-in receive workers.                                                     |
| Module-local domain event contracts                                                                                           | `Bondstone.Capabilities.DomainEvents`                     | `Bondstone.Capabilities.DomainEvents`                                       | Contains `IDomainEvent`, `DomainEventIdentityAttribute`, `IDomainEventSource`, and `IDomainEventHandler<TDomainEvent>`. It is not a transport bus.         |
| EF Core module-local domain event persistence bridge                                                                          | `Bondstone.Capabilities.DomainEvents.EntityFrameworkCore` | `Bondstone.Capabilities.DomainEvents.EntityFrameworkCore.Persistence`       | Adds `UseEntityFrameworkCoreDomainEventPersistence()` and `ApplyBondstoneDomainEvents(...)` for EF-backed modules.                                         |

Package IDs match project names. The full package inventory and dependency
direction live in [packaging.md](packaging.md).

## Common Imports

Most applications start with the core setup namespaces:

```csharp
using Bondstone.Configuration; // AddBondstone, BondstoneBuilder
using Bondstone.Modules; // IBondstoneModule, module builders, handlers
using Bondstone.Messaging; // ICommand, IDurableCommand, IIntegrationEvent
```

`Bondstone.Messaging` also contains durable message identity attributes,
`IDurableCommandSender`, `IDurableEventPublisher`,
`IDurableOperationResultReader`, durable send/publish results, operation
result types, durable operation state records, durable message envelopes, and
trace context records.

`Bondstone.Modules` contains module registration and execution contracts:
`IBondstoneModule`, `BondstoneModuleBuilder`, `ICommandHandler<TCommand>`,
`ICommandHandler<TCommand, TResult>`,
`IIntegrationEventHandler<TEvent>`, command validators, pipeline behavior
contracts, receive pipelines, module execution context contracts, and the
module command/event execution result types.

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
`IDurableOutboxDispatcher`, `IDurableEnvelopeDispatcher`,
`IDurableEnvelopeDispatchRoute`, `IDurableInboxHandlerExecutor`,
`IDurableInboxRegistrar`, `IDurableInboxStore`,
`IDurableOperationStateStore`, `IDurableOutboxClaimer`,
`IDurableOutboxLeaseRenewer`, `IDurableOutboxDispatchRecorder`,
`IDurableOutboxFailurePolicy`, `IModuleTransactionFeature`,
`DurableInboxMessageKey`, `DurableInboxRecord`, `DurableOutboxRecord`,
`DurableOutboxDispatchState`, and `DurableInboxAlreadyReceivedException`.

`DurableMessageEnvelope`, `MessageKind`, `MessageTraceContext`,
`DurableOperationState`, `DurableOperationStatus`, and
`IDurableOperationReader` are in `Bondstone.Messaging`.

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

Local transport is for samples, tests, and local development. It exercises
the durable outbox and receive inbox semantics through Bondstone's neutral
receive pipelines, but it is not broker durability or a production fallback.
See [architecture/transport-local.md](architecture/transport-local.md).

## Domain Events

Use this import for module-local domain event contracts:

```csharp
using Bondstone.Capabilities.DomainEvents;
```

Common contracts include `IDomainEvent`, `DomainEventIdentityAttribute`,
`IDomainEventSource`, and `IDomainEventHandler<TDomainEvent>`. Domain events
are module-local facts; they are not durable transport messages and are not
automatically published as integration events.

For EF-backed domain event persistence, import:

```csharp
using Bondstone.Capabilities.DomainEvents.EntityFrameworkCore.Persistence;
```

Common APIs include:

- `module.UseEntityFrameworkCoreDomainEventPersistence()` for the EF bridge
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
