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
        rabbitMq.DispatchCommandsTo(envelope =>
            new RabbitMqPublishDestination(
                "bondstone.commands",
                $"{envelope.TargetModule}.commands"));
        rabbitMq.DispatchEventsTo(envelope =>
            new RabbitMqPublishDestination(
                "bondstone.events",
                envelope.MessageTypeName));
    });
    bondstone.Outbox.UseWorker();
});
```

Provider-native transport configuration, broker administration, retry,
dead-letter policy, worker settings, credentials, and topology declaration stay
app-owned.

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

`IModuleCommandExecutor` executes registered typed handlers through selected
runtime contributions and application pipeline behaviors. Void commands use
`ExecuteAsync`; result commands use `ExecuteResultAsync` and return a typed
`ModuleCommandExecutionResult<TResult>`. The executor is not a generic
mediator for arbitrary in-process calls.

When a handler is already executing inside a module execution context,
`IModuleCommandExecutor` may execute another command in the same module, but
it must not synchronously execute a different module's command. Cross-module
work should cross the durable boundary through `IDurableCommandSender` or an
explicit integration event instead. This keeps module-local transactions from
committing side effects in another module before the current module's handler
has committed or rolled back.

Command execution is assembled by an internal runtime planner. The current
plan selects ordered system and capability contribution records for the
executing module first, creates only those runtime behaviors, then runs
application behavior steps in DI registration order, then the registered
handler. Normal user extension should use
`IModuleCommandPipelineBehavior<TCommand>` for application concerns such as
validation, logging, authorization, and auditing. Runtime contributions are
advanced provider/runtime composition.

During command execution, Bondstone's system pipeline sets the current module
execution context to the route's module before the application handler runs
and restores the previous context afterward. This is the source-module context
used by durable command sending and event publishing inside the handler.

For result commands, existing `IModuleCommandPipelineBehavior<TCommand>`
behaviors still wrap the handler in the same order. Result-specific behavior
that inspects or transforms `TResult` is not part of the current public
pipeline contract.

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

Subscriber execution uses the same internal runtime planning shape as command
execution: ordered system and capability contribution records selected for the
executing module, application behavior steps in DI registration order, then
the subscriber handler. Normal subscriber extension should use
`IModuleEventSubscriberPipelineBehavior<TEvent>` for application-owned
concerns. Runtime contributions are advanced provider/runtime composition.

During subscriber execution, Bondstone's system pipeline sets the current
module execution context to the subscriber module before the application
handler runs and restores the previous context afterward. Durable commands sent
from a subscriber therefore use the subscriber module as their source module.

## Application Pipeline Behaviors

Normal applications extend command execution by registering
`IModuleCommandPipelineBehavior<TCommand>` in DI. They extend integration event
subscriber execution by registering
`IModuleEventSubscriberPipelineBehavior<TEvent>` in DI. These behaviors are
the supported extension points for logging, authorization,
auditing, metrics enrichment, and other application-owned concerns around a
registered handler. Command validation through `ICommandValidator<TCommand>`
is module-owned command metadata rather than a global application pipeline
behavior.

```csharp
services.AddScoped<
    IModuleCommandPipelineBehavior<ReserveInventoryCommand>,
    ReserveInventoryAuthorizationBehavior>();

services.AddScoped<
    IModuleEventSubscriberPipelineBehavior<OrderPlacedEvent>,
    OrderPlacedAuditBehavior>();
```

Application behaviors run after selected Bondstone/provider runtime
contributions and before the handler. When the normal module execution-context
runtime behavior applies, application behaviors and handlers run inside the
current module context, so durable sends and publishes use the executing module
as their source module.

Provider, runtime, and capability packages use passive module pipeline
contribution records for advanced composition. Core contributions are stored
in Bondstone runtime metadata during `AddBondstone`; provider and capability
module setup APIs attach module-specific contributions as part of module
registration. A contribution declares its system or capability kind, order,
module applicability, and behavior factory. The planner filters contributions
by module metadata before creating behavior instances, so DI remains the
object factory rather than the contribution store or module/provider selector.
Selected runtime contributions for a module pipeline must use unique names
and unique numeric order values across system and capability steps.
When a provider uses a factory-based contribution instead of a concrete
behavior type, duplicate registration equivalence is based on delegate identity
for the behavior factory and applicability predicate. Provider packages should
prefer the behavior-type contribution constructor when the behavior has a
stable concrete type.
Bondstone does not currently provide module-scoped application
behavior registration. Ordinary DI registration is the supported model.

Command and event subscriber execution contexts also carry a
`ModulePipelineFeatureCollection`. This is an advanced provider/runtime
coordination surface, not the normal application extension path. System
behaviors can push typed features for the current execution and other system
behaviors can read the nearest active feature without relying on scoped mutable
state. Features are stored under the exact pushed contract type, so provider
coordination should use shared interfaces rather than concrete implementation
types. Feature scopes are per module execution and must be disposed in reverse
order across the whole collection. Nested module command or event subscriber
executions create independent execution contexts and do not inherit active
features from the caller's pipeline; cross-execution feature inheritance is not
part of the current contract.

## Optional Capability Runtime

Optional capabilities do not currently have a public capability-step registry
or public named pipeline slots. Provider and runtime packages contribute
ordered system or capability pipeline records through Bondstone setup and
module registration, not by adding executable behavior implementations to DI.
Those records are passive metadata plus factories; the runtime planner selects
them from module registration metadata before executable behavior is created.
Module-specific contribution applicability is validated at startup for every
module that carries the contribution, even when the module has no command
handler or event subscriber yet. Capability opt-ins that need executable
pipeline behavior should therefore only attach contributions after the module
metadata required by the contribution is present. Future handlerless or purely
declarative capability markers should use separate metadata rather than
non-applicable executable pipeline contributions.

Domain event runtime behavior follows that model. The EF Core persistence
bridge activates only for EF-backed modules that call
`UseEntityFrameworkCoreDomainEventPersistence()`. The EF opt-in is EF-owned
module metadata, not a broad capability registry. EF Core transaction behavior
publishes a provider-neutral transaction feature into the current execution;
EF domain event persistence consumes that feature to clear pending domain
events only after an observed commit. The bridge contributes capability
pipeline records so its planner step is distinct from Bondstone core system
behavior, while still being ordered before application behavior. The bridge
does not invoke `IDomainEventHandler<TDomainEvent>` services or map domain
events to integration events. Normal user extension remains application
pipeline behavior; pipeline contributions and feature participation are
advanced provider/runtime/capability composition.

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

Transport adapters should parse provider-native messages into
`DurableMessageEnvelope` and call the appropriate receive pipeline inside the
provider acknowledgement boundary. The active RabbitMQ adapter provides native
message mappers, receive dispatchers, settlement handler helpers, and opt-in
hosted receive workers over configured receive topology.

## Persistence Boundaries

Module-owned persistence behavior composes through pipeline behaviors. For EF
Core modules, command and event subscriber transaction behavior saves handler
state, inbox markers, operation state when applicable, and outgoing outbox
messages in the module persistence boundary.

Provider transaction behaviors are registered through passive module pipeline
contributions when modules opt into a provider and activate only for modules
that declare that provider's persistence capability. EF Core/PostgreSQL is the
supported durable persistence path in the active product surface.

EF Core domain event persistence belongs inside the same module command and
event subscriber transaction boundary. It collects and stages domain events
after application behavior and handler logic, while the module execution
context is still active, and before transaction-owned `SaveChangesAsync` and
commit. Pending domain events are cleared only after collection, staging,
save, and commit succeed.

Module-owned persistence is the preferred durable messaging path. Root-level
non-module persistence services remain available for advanced single-store
composition when no module-owned implementations are registered, but normal
module-boundary setup should use provider-specific module persistence helpers.

## Service Extraction

Stable module names, stable message identities, durable envelopes, inbox keys,
and module-owned registration are intended to survive service extraction. A
module can move from in-process composition to a separately deployed service
without changing its public durable command/event contracts.
