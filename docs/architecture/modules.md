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

Hosts use `AddBondstone` to compose modules, persistence providers, local
transport for dev/test flows, app-owned broker bridges, and hosted outbox
workers.

```csharp
builder.Services.AddBondstone(bondstone =>
{
    bondstone.AddOrderingModule(connectionString);
    bondstone.AddFulfillmentModule(connectionString);
    bondstone.UseLocalTransport(local =>
    {
        local.UseModuleQueueConvention();
        local.RouteEvent("ordering.order-placed.v1")
            .ToQueue("ordering.order-placed");
        local.Queue("ordering.order-placed")
            .SubscribeEvent(
                "ordering.order-placed.v1",
                "fulfillment",
                "fulfillment.order-placed-projection.v1");
    });
    bondstone.Outbox.UseWorker();
});
```

Provider-native transport configuration, broker administration, retry,
dead-letter policy, worker settings, credentials, and topology declaration stay
app-owned. Broker hosts bridge native transport code into Bondstone through
`IDurableEnvelopeDispatcher`, `IDurableMessageEnvelopeSerializer`, and
`IDurableEnvelopeReceiver`.

## Durable Messaging Capability

`module.UseDurableMessaging()` marks a module as participating in durable
commands or integration events. Durable messaging modules must also declare
persistence through `module.UsePersistence(...)` or a provider-specific helper
such as `UsePostgreSqlPersistence<TDbContext>()`.

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
module.Commands.RegisterValidator<ReserveInventoryCommand, ReserveInventoryValidator>();
```

Commands that return immediate application results use the same module command
boundary:

```csharp
module.Commands.RegisterHandler<
    CreateOrderCommand,
    CreateOrderResult,
    CreateOrderHandler>();
```

The route stores module name, stable message type identity, command CLR type,
handler type, optional result type, and stable handler identity. Handler
identity is part of the command receive inbox key and must remain stable.

Command validators registered through `module.Commands` are module-owned
metadata. Bondstone records them by module and command type, then the built-in
validation runtime step creates only validators for the executing module. DI
constructs validator implementation types, but it is not used to select all
validators for a command CLR type globally.

`IModuleCommandExecutor` executes registered typed handlers through the fixed
module runtime sequence. Void commands use `ExecuteAsync`; result commands use
`ExecuteResultAsync` and return a typed
`ModuleCommandExecutionResult<TResult>`. The executor is not a generic
mediator for arbitrary in-process calls.

When a handler is already executing inside a module execution context,
`IModuleCommandExecutor` may execute another command in the same module, but
it must not synchronously execute a different module's command. Cross-module
work should cross the durable boundary through `IDurableCommandSender` or an
explicit integration event instead. This keeps module-local transactions from
committing side effects in another module before the current module's handler
has committed or rolled back.

Command execution is direct internal orchestration over Bondstone/provider
runtime services and the registered handler. Bondstone does not provide a
public application middleware pipeline for logging, authorization, or
auditing. Use ordinary handler code, DI decorators, endpoint filters,
host-specific middleware, or an application framework selected by the
consumer app.

During command execution, Bondstone's direct runtime sets the current module
execution context to the route's module before the handler runs and restores
the previous context afterward. This is the source-module context used by
durable command sending and event publishing inside the handler.

For result commands, the registered handler returns the typed result directly
to local execution. During durable receive, Bondstone serializes the result
payload for operation-state observation before provider post-handler actions
and transaction commit.

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
typed `IIntegrationEventHandler<TEvent>` handlers through the fixed module
runtime sequence.

Subscriber execution uses the same direct internal orchestration shape as
command execution: Bondstone/provider runtime services, then the subscriber
handler.

During subscriber execution, Bondstone's direct runtime sets the current
module execution context to the subscriber module before the handler runs and
restores the previous context afterward. Durable commands sent from a
subscriber therefore use the subscriber module as their source module.

## Application Extension

Bondstone does not expose application pipeline behavior contracts,
module-scoped application behavior registration, public runtime contribution
records, public named runtime slots, or public runtime order constants.

Command validation through `ICommandValidator<TCommand>` remains module-owned
command metadata. Other application concerns around handlers should use
ordinary handler code, DI decorators, endpoint filters, host-specific
middleware, or application frameworks selected by the consumer app. Provider
packages use hidden transaction-runner and post-handler-action contracts only
for package-owned runtime concerns such as EF transactions and EF domain event
persistence.

Command and event subscriber execution contexts expose a narrow
provider/runtime transaction callback surface. Provider transaction runners can
mark that Bondstone observes the current transaction outcome, and post-handler
actions can register lightweight callbacks for observed commit or rollback.
This exists for package-owned runtime coordination such as EF-backed domain
event source clearing. It is not an application extension point and must not
be treated as a durable work boundary.

## Runtime Sequence

Module command execution uses a fixed runtime sequence:

1. provider transaction runner;
2. durable operation completion behavior;
3. receive inbox behavior when a receive context exists;
4. module execution context;
5. command validation;
6. handler;
7. provider post-handler action.

Module event subscriber execution uses a fixed runtime sequence:

1. provider transaction runner;
2. receive inbox behavior when a receive context exists;
3. module execution context;
4. handler;
5. provider post-handler action.

Bondstone does not expose a public capability-step registry, public named
runtime slots, public contribution records, public order constants, or public
application middleware contracts.

Provider packages use small hidden runtime service contracts only where
cross-package runtime collaboration is required. EF Core persistence uses a
transaction runner. EF-backed domain event persistence uses a post-handler
action, activates only for EF-backed modules that call
`UseEntityFrameworkCoreDomainEventPersistence()`, and requires EF persistence
to be declared first. EF Core transaction behavior opens the observed
transaction callback scope; EF domain event persistence uses that scope to
clear pending domain events only after an observed commit. It does not invoke
local domain event handlers or map domain events to integration events.

## Execution Context Limits

The module execution context is ambient and handler-flow scoped. It flows
through normal awaited asynchronous work, but it is not a durable source-module
token that can be captured for arbitrary background work, reused after handler
completion, or supplied manually by HTTP routes and custom hosts.

Application-owned entrypoints that need module command behavior should call
`IModuleCommandExecutor` for a registered module command rather than calling
`IDurableCommandSender` directly outside a module execution context. Use
`ExecuteResultAsync` when the registered module command implements
`ICommand<TResult>`. Explicit source-module send/publish APIs, module-scoped
clients, and mediator-like HTTP command routing are not part of the current
module contract.

## Receive Pipeline

Core module receive is provider-neutral:

- `IModuleCommandReceivePipeline` handles command envelopes;
- `IModuleEventReceivePipeline` handles event envelopes for a specific
  subscriber module and subscriber identity.

App-owned transport readers should parse provider-native messages into
`DurableMessageEnvelope` and call `IDurableEnvelopeReceiver` inside the
provider acknowledgement boundary. Commands route by envelope target module;
events require the app to supply the subscriber module and stable subscriber
identity selected by that native subscription.

## Persistence Boundaries

Module-owned persistence behavior composes through provider runtime services.
For EF Core modules, the transaction runner saves handler state, inbox
markers, operation state when applicable, and outgoing outbox messages in the
module persistence boundary.

Provider transaction runners activate only for modules that declare that
provider's persistence capability. EF Core/PostgreSQL is the supported durable
persistence path in the active product surface.

EF Core domain event persistence belongs inside the same module command and
event subscriber transaction boundary. It collects and stages domain events
after handler logic, while the module execution context is still active, and
before transaction-owned `SaveChangesAsync` and commit. Pending domain events
are cleared only after collection, staging, save, and commit succeed.

Module-owned persistence is the preferred durable messaging path. Root-level
non-module persistence services remain available for advanced single-store
composition when no module-owned implementations are registered, but normal
module-boundary setup should use provider-specific module persistence helpers.

## Service Extraction

Stable module names, stable message identities, durable envelopes, inbox keys,
and module-owned registration are intended to survive service extraction. A
module can move from in-process composition to a separately deployed service
without changing its public durable command/event contracts.
