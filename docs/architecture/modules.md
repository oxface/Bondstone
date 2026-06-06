# Module Architecture

Bondstone modules are service-shaped units that can run close together inside
a modular monolith or later move behind a transport boundary.

## Ownership Split

Modules own their durable capabilities:

- stable module name;
- module command handlers;
- module command validators;
- message identities for commands they handle;
- module persistence capability;
- durable messaging capability when the module sends or receives durable
  commands;
- future module transaction, inbox, outbox, and operation-state behavior.

Hosts own deployment topology and transport infrastructure:

- which modules are loaded in a process;
- which modules are local or remote;
- connection strings and environment-specific settings;
- transport adapters and target-module address maps;
- Rebus endpoint names, queue names, retry policy, and workers;
- exchange, topic, routing-key, subscription, and listener names for other
  transports;
- process-level hosted services and operational policy.

Module code should not need to know whether another module is local,
remote, or Rebus-backed. It can depend on stable module names and durable
message contracts. The host decides how commands reach the target module.

## Module Registration

`AddBondstone` is the host composition entrypoint. A host can register module
capabilities inline:

```csharp
services.AddBondstone(bondstone =>
{
    bondstone.Module("fulfillment", module =>
    {
        module.UseDurableMessaging();
        module.Commands.RegisterFromAssemblyContaining<ReserveOrderCommand>();
    });
});
```

Or a module can provide its own registration object:

```csharp
public sealed class FulfillmentModule : IBondstoneModule
{
    public string Name => "fulfillment";

    public void Configure(BondstoneModuleBuilder module)
    {
        module.UseDurableMessaging();
        module.Commands.RegisterFromAssemblyContaining<ReserveOrderCommand>();
    }
}
```

The host stitches module packages together:

```csharp
services.AddBondstone(bondstone =>
{
    bondstone.AddModule<FulfillmentModule>();
    bondstone.AddModule<SalesModule>();
});
```

Handlers and validators can also be registered explicitly:

```csharp
module.Commands.RegisterHandler<ReserveOrderCommand, ReserveOrderHandler>();
module.Commands.RegisterValidator<ReserveOrderCommand, ReserveOrderValidator>();
```

## Command Handlers

Module command handlers are direct typed handlers:

```csharp
public sealed record CreateDraftOrderCommand(Guid OrderId) : ICommand;

public sealed class CreateDraftOrderHandler
    : ICommandHandler<CreateDraftOrderCommand>
{
    public ValueTask HandleAsync(
        CreateDraftOrderCommand command,
        CancellationToken ct = default)
    {
        return ValueTask.CompletedTask;
    }
}
```

Durable commands extend the same command pipeline and add stable durable
message identity:

```csharp
[DurableCommandIdentity("fulfillment.order.reserve.v1")]
public sealed record ReserveOrderCommand(Guid OrderId) : IDurableCommand;

public sealed class ReserveOrderHandler
    : ICommandHandler<ReserveOrderCommand>
{
    public ValueTask HandleAsync(
        ReserveOrderCommand command,
        CancellationToken ct = default)
    {
        return ValueTask.CompletedTask;
    }
}
```

Validators are typed and optional:

```csharp
public sealed class ReserveOrderValidator
    : ICommandValidator<ReserveOrderCommand>
{
    public ValueTask ValidateAsync(
        ReserveOrderCommand command,
        CancellationToken ct = default)
    {
        return ValueTask.CompletedTask;
    }
}
```

`RegisterFromAssembly` scans at startup for command handlers, validators, and
durable message identity metadata. Runtime dispatch uses cached route metadata
and closed generic delegates.

## Command Execution

`IModuleCommandExecutor` executes an `ICommand` for a named module. The
current applied pipeline runs registered validators before the direct handler.
Future pipeline behaviors will add module transaction ownership, inbox
handling for durable receives, outbox staging for durable sends,
operation-state updates, trace propagation, and source-module scope for
outgoing durable commands.

`ICommand` should be used for module endpoint and boundary operations that
benefit from the module pipeline. Ordinary helper methods and internal domain
collaboration inside a module should remain direct method calls.

Transport adapters should enter the same command executor rather than owning
module command behavior themselves.

## Durable Messaging Capability

Plain `ICommand` execution is direct module command pipeline execution. It is
appropriate for module-owned endpoints and local application use cases that
need validation, logging, transaction behavior, or other module pipeline
features.

`IDurableCommand` represents asynchronous durable messaging. Durable command
send and receive should use inbox/outbox-backed infrastructure even when the
source and target modules happen to be deployed in the same process. Direct
in-process collaboration can use `.Contracts` references or plain `ICommand`.

The normal application-facing shape should be one module capability such as
`UseDurableMessaging`, not separate inbox/outbox toggles. Durable messaging
implies the module needs the persistence and pipeline pieces required for
durable send and receive: outbox writing, inbox registration/storage, module
transaction behavior, source-module scope for send, and durable receive
orchestration. Advanced APIs may later expose separate inbox, outbox, or
operation-state pieces, but they should not be the common path.

Current core registration records module metadata and durable messaging
capability through `IBondstoneModuleRegistry`. That metadata is groundwork for
later validation and pipeline behavior; it does not yet wire module
persistence, transactions, source-module scope, or receive-side inbox behavior.

## Transport Binding

Transport is host topology. Modules declare durable messaging capability and
command handlers; transport adapters configure infrastructure Bondstone cannot
infer, such as broker connection strings, queue names, endpoint names,
exchange/topic names, routing keys, and subscriptions.

For Rebus, outgoing target-module-to-address mapping is configured through the
host-owned transport builder:

```csharp
bondstone.UseRebusTransport(rebus =>
{
    rebus.RouteModule("fulfillment").ToQueue("fulfillment-commands");
    rebus.RouteModule("billing").ToQueue("billing-commands");

    rebus.ListenOn("sales-commands", listener =>
    {
        listener.AcceptCommandsFor("sales");
    });
});
```

The Rebus listener shape is still future work. A later slice should bind
receive topology to local modules and dispatch accepted commands into
`IModuleCommandExecutor` so applications do not repeat handler delegates or
commit delegates per command. The intended shape is listener binding to local
modules, not a generic route table:

```csharp
bondstone.UseRebusTransport(rebus =>
{
    rebus.ListenOn("sales-commands", listener =>
    {
        listener.AcceptCommandsFor("sales");
    });
});
```

A topic-based transport may expose different topology while keeping the same
module concepts:

```csharp
bondstone.UseRabbitMqTransport(rabbit =>
{
    rabbit.RouteModule("fulfillment")
        .ToExchange("commerce.commands")
        .WithRoutingKey("module.fulfillment.commands");

    rabbit.ListenOnQueue("sales-commands", listener =>
    {
        listener.BindExchange("commerce.commands")
            .WithRoutingKey("module.sales.commands");

        listener.AcceptCommandsFor("sales");
    });
});
```

Wolverine provides a useful comparison point: it discovers handlers, applies
local routing automatically for known local handlers, lets explicit routing
override conventions, and configures listening endpoints separately from
handler registration. Bondstone should borrow that separation of concerns,
while keeping routing by stable module and durable message identity rather
than CLR type names.
