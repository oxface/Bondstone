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
[../backlog/14-future-work.md](../backlog/14-future-work.md).

## Durable Messaging Capability

`module.UseDurableMessaging()` marks a module as participating in durable
commands or integration events. Durable messaging modules must also declare
persistence through `module.UsePersistence(...)` or a provider-specific helper
such as `UsePostgreSqlPersistence<TDbContext>()` or
`UsePostgresPersistence(...)`.

The current module registration records persistence with a provider name and,
for EF Core-backed modules, an optional context type. The provider name is the
provider-neutral marker used by startup validation, provider transaction
behaviors, and missing-service diagnostics. The context type exists for EF Core
module transactions and model validation; non-EF providers should keep their
schema, session, connection, and SQL details in provider-owned services rather
than treating a CLR context type as a general module capability requirement.

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

During command execution, Bondstone's system pipeline sets the current module
execution context to the route's module before the application handler runs
and restores the previous context afterward. This is the source-module context
used by durable command sending and event publishing inside the handler.

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

During subscriber execution, Bondstone's system pipeline sets the current
module execution context to the subscriber module before the application
handler runs and restores the previous context afterward. Follow-up durable
commands published from a subscriber therefore use the subscriber module as
their source module.

## Execution Context Limits

The module execution context is ambient and handler-flow scoped. It flows
through normal awaited asynchronous work, but it is not a durable source-module
token that can be captured for arbitrary background work, reused after handler
completion, or supplied manually by HTTP routes and custom hosts.

Application-owned entrypoints that need module command behavior should call
`IModuleCommandExecutor` for a registered module command rather than calling
`IDurableCommandSender` directly outside a module execution context. Explicit
source-module send/publish APIs, module-scoped clients, and mediator-like HTTP
command routing are not part of the current module contract.

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

Module-owned persistence is the preferred durable messaging path. Root-level
non-module persistence services remain available for advanced single-store
composition when no module-owned implementations are registered, but normal
module-boundary setup should use provider-specific module persistence helpers.

## Service Extraction

Stable module names, stable message identities, durable envelopes, inbox keys,
and module-owned registration are intended to survive service extraction. A
module can move from in-process composition to a separately deployed service
without changing its public durable command/event contracts.
