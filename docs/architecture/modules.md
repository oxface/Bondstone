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
- future module transaction, inbox, outbox, and operation-state behavior.

Hosts own deployment topology:

- which modules are loaded in a process;
- which modules are local or remote;
- connection strings and environment-specific settings;
- transport adapters and route maps;
- Rebus endpoint names, queue names, retry policy, and workers;
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

## Transport Binding

Transport is host topology. A module can be local-only, local and exposed
through Rebus, or remote and reached through Rebus. That should be decided by
host route configuration rather than module handler registration.

The current Rebus receive pipeline remains a lower-level adapter. A later
slice should bind Rebus host receive topology to `IModuleCommandExecutor` so
applications do not repeat handler delegates or commit delegates per command.

Wolverine provides a useful comparison point: it discovers handlers, applies
local routing automatically for known local handlers, lets explicit routing
override conventions, and configures listening endpoints separately from
handler registration. Bondstone should borrow that separation of concerns,
while keeping routing by stable module and durable message identity rather
than CLR type names.
