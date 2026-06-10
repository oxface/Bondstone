# Module Architecture

## Module Ownership

Bondstone modules own their durable messaging and persistence declarations.
Application hosts compose modules, but module implementation assemblies should
own:

- module name;
- durable messaging capability;
- persistence binding;
- command handler registration;
- published integration event registration;
- event subscriber registration.

The preferred public shape is a module-owned `IBondstoneModule` registration
object plus a thin host extension that supplies environment-specific inputs
such as connection strings.

## Host Composition

Hosts use `AddBondstone` to compose modules, persistence providers, transport
adapters, and hosted outbox workers.

```csharp
builder.Services.AddBondstone(bondstone =>
{
    bondstone.AddOrderingModule(connectionString);
    bondstone.AddFulfillmentModule(connectionString);
    bondstone.UseRabbitMqTransport(rabbitMq =>
    {
        rabbitMq.UseCommandExchange("bondstone.commands");
        rabbitMq.UseEventExchange("bondstone.events");
        rabbitMq.UseModuleRoutingKeyConvention();
        rabbitMq.UseEventRoutingKeyConvention();
    });
    bondstone.Outbox.UseWorker();
});
```

Provider-native transport configuration, broker administration, retry,
dead-letter policy, worker settings, credentials, and topology declaration stay
app-owned. Bounded helper ideas are tracked outside current guidance in
[../backlog/04-future-work.md](../backlog/04-future-work.md).

## Durable Messaging Capability

`module.UseDurableMessaging()` marks a module as participating in durable
commands or integration events. Durable messaging modules must also declare
persistence through `module.UsePersistence(...)` or a provider-specific helper
such as `UsePostgreSqlPersistence<TDbContext>()` or
`UsePostgresPersistence(...)`.

Startup validation checks that durable command handlers and event subscribers
belong to registered durable-messaging modules.

## Command Registration

Modules register command handlers through module command routes:

```csharp
module.Commands.RegisterHandler<ReserveInventoryCommand, ReserveInventoryHandler>();
```

The route stores module name, stable message type identity, command CLR type,
handler type, and stable handler identity. Handler identity is part of the
command receive inbox key and must remain stable.

`IModuleCommandExecutor` executes registered typed handlers through system and
application pipeline behaviors. It is not a generic mediator for arbitrary
in-process calls.

## Event Registration

Modules register published integration events and subscribers explicitly:

```csharp
module.Events.RegisterPublishedEvent<OrderPlacedEvent>();
module.Events.RegisterSubscriber<OrderPlacedEvent, RecordOrderPlacedHandler>(
    "fulfillment.order-placed-projection.v1");
```

Published event registration records module-owned publish metadata used by
the durable event publisher and transport topology validation. Subscriber
registration records module, message type, handler type, and stable subscriber
identity used by receive binding validation and per-subscriber inbox keys.

Subscriber identity is consumer-owned durable identity. It should describe the
logical subscriber, not the handler CLR type. Event inbox identity uses
Bondstone message id, subscriber module, and subscriber identity.

`IModuleEventSubscriberExecutor` resolves registered subscribers and executes
typed `IIntegrationEventHandler<TEvent>` handlers through event subscriber
pipeline behaviors.

## Receive Pipeline

Core module receive is provider-neutral:

- `IModuleCommandReceivePipeline` handles command envelopes;
- `IModuleEventReceivePipeline` handles event envelopes for a specific
  subscriber module and subscriber identity.

Transport adapters should parse provider-native messages into
`DurableMessageEnvelope` and call the appropriate receive pipeline inside the
provider acknowledgement boundary. Direct RabbitMQ and Service Bus adapters
provide native message mappers, receive dispatchers, settlement handler
helpers, and opt-in hosted receive workers over configured receive topology.

## Persistence Boundaries

Module-owned persistence behavior composes through pipeline behaviors. For EF
Core modules, command and event subscriber transaction behavior saves handler
state, inbox markers, operation state when applicable, and outgoing outbox
messages in the module persistence boundary.

`Bondstone.Persistence.Postgres` supplies equivalent durable outbox, inbox,
operation-state, and transaction behavior without EF Core for modules that opt
into that PostgreSQL provider. Dapper is an internal implementation helper for
that package, not the app-facing provider identity.

## Service Extraction

Stable module names, stable message identities, durable envelopes, inbox keys,
and module-owned registration are intended to survive service extraction. A
module can move from in-process composition to a separately deployed service
without changing its public durable command/event contracts.
