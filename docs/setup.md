# Setup

This document shows the current host setup shape for Bondstone.

## Packages

Install the packages needed for the host:

- `Bondstone` for core module, command, integration-event, domain-event, and
  module execution contracts.
- `Bondstone.Hosting` when the host runs the durable outbox worker.
- `Bondstone.Persistence` for provider-neutral durable envelopes, inbox,
  outbox, operation state, and persistence contracts used by custom
  persistence or dispatch composition.
- `Bondstone.Persistence.EntityFrameworkCore` for EF Core durable persistence
  mappings, module transaction behavior, and optional EF-backed domain event
  persistence.
- `Bondstone.Persistence.EntityFrameworkCore.Postgres` for PostgreSQL EF Core duplicate
  classification and provider registration.
- `Bondstone.Transport.RabbitMq` when the host dispatches durable outbox
  records through the remaining direct broker adapter.
- `Bondstone.Transport.Local` when a sample, test, or local development host
  explicitly wants in-process queue routing through the durable receive
  pipelines.

## Adoption Path

For a normal PostgreSQL-backed host, start with:

- `Bondstone` for module registration, durable send/publish contracts, and
  module receive pipelines;
- `Bondstone.Hosting` for the durable outbox worker;
- `Bondstone.Persistence.EntityFrameworkCore` and
  `Bondstone.Persistence.EntityFrameworkCore.Postgres` for EF-backed module
  persistence;
- `Bondstone.Transport.RabbitMq` when messages leave the process through the
  remaining direct broker adapter.

Domain event contracts are in the core `Bondstone` package. EF-backed
module-local domain event persistence uses
`Bondstone.Persistence.EntityFrameworkCore` and remains explicit opt-in.

Use the provider transport extensions on `AddBondstone` for ordinary hosts.
Those extensions register outbox dispatch and receive helpers. Broker
topology, consumers, retry, and dead-letter policy remain application-owned.
Lower-level persistence, receive, dispatcher, and outbox transport types remain
available for advanced composition and tests, but they are not the quick-start
path.

After composing a host, use the modular monolith sample as the adoption proof
and [testing.md](testing.md) for verification entrypoints. The default quality
gate is `pnpm check`; infrastructure-backed sample and provider coverage runs
through `pnpm backend:test:integration`.

## Golden Path: EF/Postgres Local Modular Monolith

For a modular monolith that uses module-owned EF Core `DbContext` types,
PostgreSQL persistence, local in-process transport, and the hosted outbox
worker, use this package set in the projects that call the corresponding APIs:

```xml
<ItemGroup>
  <PackageReference Include="Bondstone" />
  <PackageReference Include="Bondstone.Hosting" />
  <PackageReference Include="Bondstone.Persistence.EntityFrameworkCore" />
  <PackageReference Include="Bondstone.Persistence.EntityFrameworkCore.Postgres" />
  <PackageReference Include="Bondstone.Transport.Local" />
</ItemGroup>
```

Contracts projects that define durable commands or integration events need
`Bondstone`. Module implementation projects that configure EF persistence need
the EF Core and PostgreSQL packages. Host projects that configure local
transport and the hosted worker need `Bondstone.Transport.Local` and
`Bondstone.Hosting`. See [packaging.md](packaging.md) for the full package
matrix and dependency direction, and
[package-discovery.md](package-discovery.md) for package and namespace
discovery by capability.

Common namespaces for this path are:

- `Bondstone.Configuration` for `AddBondstone` and `BondstoneBuilder`.
- `Bondstone.Modules` for `IBondstoneModule`, `BondstoneModuleBuilder`,
  command handlers, event handlers, and module registration.
- `Bondstone.Messaging` for `ICommand`, `IDurableCommand`,
  `IIntegrationEvent`, `DurableCommandIdentityAttribute`,
  `IntegrationEventIdentityAttribute`, `IDurableCommandSender`, and
  `IDurableEventPublisher`.
- `Bondstone.DomainEvents` for optional `IDomainEvent` and
  `IDomainEventSource`.
- `Bondstone.Persistence.EntityFrameworkCore.Persistence` for
  `ApplyBondstonePersistence`, `ApplyBondstoneOutbox`,
  `ApplyBondstoneInbox`, `ApplyBondstoneOperationState`, and
  `UseEntityFrameworkCorePersistence`, and optional
  `UseEntityFrameworkCoreDomainEventPersistence` and
  `ApplyBondstoneDomainEvents`.
- `Bondstone.Persistence.EntityFrameworkCore.Postgres.Persistence` for
  `UsePostgreSqlPersistence`.
- `Bondstone.Transport.Local.Outbox` for `UseLocalTransport` and local queue
  topology.
- `Bondstone.Hosting.Outbox` for `UseWorker` and
  `DurableOutboxWorkerOptions`.

Host composition wires modules, local transport, and the hosted outbox worker:

```csharp
using Bondstone.Configuration;
using Bondstone.Hosting.Outbox;
using Bondstone.Transport.Local.Outbox;
using MyApp.Fulfillment;
using MyApp.Fulfillment.Contracts;
using MyApp.Ordering;
using MyApp.Ordering.Contracts;

string connectionString = builder.Configuration.GetConnectionString("App")!;

builder.Services.AddBondstone(bondstone =>
{
    bondstone.AddOrderingModule(connectionString);
    bondstone.AddFulfillmentModule(connectionString);

    bondstone.UseLocalTransport(local =>
    {
        local.UseModuleQueueConvention();

        local.RouteEvent(OrderingIntegrationEvents.OrderPlaced)
            .ToQueue("ordering.order-placed");
        local.Queue("ordering.order-placed")
            .SubscribeEvent(
                OrderingIntegrationEvents.OrderPlaced,
                FulfillmentModule.ModuleName,
                "fulfillment.order-placed-projection.v1");
    });

    bondstone.Outbox.UseWorker(options =>
    {
        options.WorkerId = "myapp-outbox-worker";
        options.BatchSize = 25;
        options.PollingInterval = TimeSpan.FromSeconds(1);
    });
});
```

`UseWorker(...)` registers the default durable dispatcher and the hosted
outbox worker. Use `bondstone.Outbox.UseDurableDispatcher()` only for advanced
manual dispatcher composition where the host does not want the built-in worker.

`Bondstone.Transport.Local` is explicit local development and sample
infrastructure. `local.UseModuleQueueConvention()` is complete command
topology for local module-to-module durable command dispatch: target module
`fulfillment` routes to `fulfillment.commands`, and that queue accepts the
same module. Event routes still need explicit subscriber bindings because
subscriber module and subscriber identity are durable receive identity.

Module assemblies should expose a thin host extension and a module-owned
`IBondstoneModule` registration object:

```csharp
using Bondstone.Configuration;
using Bondstone.Modules;
using Bondstone.Persistence.EntityFrameworkCore.Postgres.Persistence;
using MyApp.Fulfillment.Contracts;
using MyApp.Ordering.Contracts;

namespace MyApp.Fulfillment;

public static class FulfillmentModuleRegistration
{
    public static BondstoneBuilder AddFulfillmentModule(
        this BondstoneBuilder bondstone,
        string connectionString)
    {
        return bondstone.AddModule(
            new FulfillmentBondstoneModule(connectionString));
    }
}

public sealed class FulfillmentBondstoneModule(string connectionString)
    : IBondstoneModule
{
    public string Name => FulfillmentModule.ModuleName;

    public void Configure(BondstoneModuleBuilder module)
    {
        module.UseDurableMessaging();
        module.UsePostgreSqlPersistence<FulfillmentDbContext>(
            connectionString,
            schema: FulfillmentModule.ModuleName);

        module.Commands.RegisterFromAssemblyContaining<ReserveInventoryHandler>();
        module.Events.RegisterPublishedEvent<InventoryReservedEvent>();
        module.Events.RegisterSubscriber<OrderPlacedEvent, RecordOrderPlacedHandler>(
            "fulfillment.order-placed-projection.v1");
    }
}
```

Assembly scanning registers concrete `ICommandHandler<TCommand>` and
`ICommandHandler<TCommand, TResult>` implementations in the marker assembly.
Use explicit registration when the module needs a visible handler identity or
does not want assembly scanning:

```csharp
module.Commands.RegisterHandler<ReserveInventoryCommand, ReserveInventoryHandler>();
```

Durable command contracts use stable message identity:

```csharp
using Bondstone.Messaging;

namespace MyApp.Fulfillment.Contracts;

[DurableCommandIdentity("fulfillment.inventory.reserve.v1")]
public sealed record ReserveInventoryCommand(
    Guid OrderId,
    string Sku,
    int Quantity)
    : IDurableCommand,
        ICommand<ReserveInventoryResult>;

public sealed record ReserveInventoryResult(
    Guid ReservationId,
    Guid OrderId,
    string Sku,
    int Quantity);
```

Handlers are ordinary module services. Bondstone's module transaction behavior
commits handler state, inbox markers, operation state, and outgoing outbox rows
for modules that declare durable EF persistence:

```csharp
using Bondstone.Modules;
using MyApp.Fulfillment.Contracts;

namespace MyApp.Fulfillment;

public sealed class ReserveInventoryHandler(FulfillmentDbContext dbContext)
    : ICommandHandler<ReserveInventoryCommand, ReserveInventoryResult>
{
    public ValueTask<ReserveInventoryResult> HandleAsync(
        ReserveInventoryCommand command,
        CancellationToken ct = default)
    {
        var reservation = new FulfillmentReservation(
            Guid.NewGuid(),
            command.OrderId,
            command.Sku,
            command.Quantity);
        dbContext.Reservations.Add(reservation);

        return ValueTask.FromResult(new ReserveInventoryResult(
            reservation.Id,
            command.OrderId,
            command.Sku,
            command.Quantity));
    }
}
```

EF-backed module `DbContext` types map both application tables and the
Bondstone durable tables used by the module:

```csharp
using Bondstone.Persistence.EntityFrameworkCore.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MyApp.Fulfillment;

public sealed class FulfillmentDbContext(
    DbContextOptions<FulfillmentDbContext> options)
    : DbContext(options)
{
    public DbSet<FulfillmentReservation> Reservations => Set<FulfillmentReservation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<FulfillmentReservation>(entity =>
        {
            entity.ToTable("reservations", FulfillmentModule.ModuleName);
            entity.HasKey(reservation => reservation.Id);
            entity.Property(reservation => reservation.Sku).IsRequired();
        });

        modelBuilder.ApplyBondstonePersistence(FulfillmentModule.ModuleName);
    }
}
```

When the module also persists module-local domain events, opt in at module
registration and map the domain event record shape explicitly:

```csharp
using Bondstone.Persistence.EntityFrameworkCore.Persistence;
using Microsoft.EntityFrameworkCore;

public void Configure(BondstoneModuleBuilder module)
{
    module.UseDurableMessaging();
    module.UsePostgreSqlPersistence<FulfillmentDbContext>(
        connectionString,
        schema: FulfillmentModule.ModuleName);
    module.UseEntityFrameworkCoreDomainEventPersistence();
}

protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.ApplyBondstonePersistence(FulfillmentModule.ModuleName);
    modelBuilder.ApplyBondstoneDomainEvents(FulfillmentModule.ModuleName);
}
```

`ApplyBondstonePersistence(...)` maps the durable outbox, inbox, and operation
state tables. `ApplyBondstoneDomainEvents(...)` is separate because domain
events are optional module-local records, not transport messages or outgoing
outbox records. Use the granular `ApplyBondstoneOutbox(...)`,
`ApplyBondstoneInbox(...)`, and `ApplyBondstoneOperationState(...)` helpers
only when a `DbContext` intentionally maps a smaller durable surface.

Hosts and migrators have different responsibilities:

- The runtime host composes `AddBondstone`, module registrations, local or
  provider transport, application entrypoints, and the hosted outbox worker.
- An EF migrator or design-time factory composes the module `DbContext`
  provider options, schema names, application entities, and the same
  `ApplyBondstone...` mappings. It does not need local transport, broker
  transport, receive workers, or `bondstone.Outbox.UseWorker(...)`.
- If a migrator executable calls module registration to reuse provider setup,
  compose modules and persistence only. Omit transport setup and hosted
  workers so migration generation and application startup do not dispatch
  durable messages.
- Migrations are application-owned. Generate and apply them per module
  `DbContext`, for example:

```bash
dotnet ef migrations add InitialFulfillment \
  --context FulfillmentDbContext \
  --project src/MyApp.Fulfillment \
  --startup-project src/MyApp.Migrator
```

Applications own module names, domain model tables, EF migration history,
PostgreSQL schemas or databases, connection strings, local or broker topology
names, stable durable message identities, stable command handler and event
subscriber identities, and operational policies around broker retry,
dead-lettering, schema deployment, and inbox/outbox maintenance.

Bondstone provides the `AddBondstone` composition guardrails, module execution
context, command and event subscriber pipelines, durable payload
serialization, EF mapping helpers for Bondstone tables, module-bound
PostgreSQL durable infrastructure, local transport dispatch through the
provider-neutral receive pipelines, and the hosted outbox polling loop.

For deeper behavior and limitations, see [architecture/modules.md](architecture/modules.md),
[architecture/persistence-ef-core.md](architecture/persistence-ef-core.md),
[architecture/persistence-postgresql.md](architecture/persistence-postgresql.md),
[architecture/transport-local.md](architecture/transport-local.md), and
[architecture/hosting.md](architecture/hosting.md).

## Host Composition

Hosts compose modules, persistence, transport adapters, and hosted workers
through `AddBondstone`. Provider connection, credentials, retry, dead-letter,
topology declaration, and administration remain app-owned or provider-native.

RabbitMQ example:

```csharp
using Bondstone.Configuration;
using Bondstone.Hosting.Outbox;
using Bondstone.Transport.RabbitMq.Outbox;
using RabbitMQ.Client;

string connectionString = builder.Configuration.GetConnectionString("App")!;
IConnection rabbitMqConnection = await new ConnectionFactory
{
    Uri = new Uri(builder.Configuration.GetConnectionString("RabbitMq")!),
}.CreateConnectionAsync();

builder.Services.AddBondstoneRabbitMqConnection(rabbitMqConnection);

builder.Services.AddBondstone(bondstone =>
{
    bondstone.AddOrderingModule(connectionString);
    bondstone.AddFulfillmentModule(connectionString);
    bondstone.AddBillingModule(connectionString);

    bondstone.UseRabbitMqTransport(rabbitMq =>
    {
        rabbitMq.DispatchCommandsTo(envelope =>
            envelope.TargetModule == "fulfillment"
                ? new RabbitMqPublishDestination(
                    "bondstone.commands",
                    "fulfillment.commands")
                : null);
        rabbitMq.ReceiveQueue("fulfillment.commands")
            .AcceptModule("fulfillment");

        rabbitMq.DispatchEventsTo(envelope =>
            envelope.MessageTypeName == "ordering.order-placed.v1"
                ? RabbitMqPublishDestination.ForQueue("ordering.order-placed")
                : null);
        rabbitMq.ReceiveQueue("ordering.order-placed")
            .SubscribeEvent(
                "ordering.order-placed.v1",
                "fulfillment",
                "fulfillment.order-placed-projection.v1")
            .SubscribeEvent(
                "ordering.order-placed.v1",
                "billing",
                "billing.order-invoice-projection.v1");

        rabbitMq.UseReceiveWorker(options =>
        {
            options.RequeueOnFailure = false;
        });
    });

    bondstone.Outbox.UseWorker(options =>
    {
        options.WorkerId = "app-outbox-worker";
        options.BatchSize = 25;
        options.PollingInterval = TimeSpan.FromSeconds(1);
    });
});
```

RabbitMQ outbound destination functions are outbound-only. They resolve where
commands and events are published, but hosts that receive commands still need
explicit receive bindings such as `ReceiveQueue(...).AcceptModule(...)`.
`Bondstone.Transport.Local` is the local-development exception: its
`UseModuleQueueConvention()` configures complete in-process command topology
because there is no broker queue or binding to provision.

Use provider transport extensions on the main `BondstoneBuilder` for normal
host setup. The lower-level `bondstone.Outbox.UseRabbitMqTransport(...)`
overload is an advanced composition API for manual envelope dispatcher
registration.

When more than one direct transport is registered, Bondstone routes each
claimed outbox record through the provider whose route reports a destination
for that message. If no provider or more than one provider matches, dispatch
fails instead of guessing.

Bondstone owns retry and terminal failure for outgoing persisted outbox
records. Current outbox status semantics are described in
[architecture/persistence-core.md](architecture/persistence-core.md); they are
separate from provider-native receive DLQs. Operators can inspect terminal
outbox rows through `IDurableOutboxInspector`:

```csharp
IReadOnlyList<DurableOutboxRecord> failedRows = await inspector
    .FindTerminalFailedAsync(
        moduleName: FulfillmentModule.ModuleName,
        maxCount: 50,
        failedAtOrBeforeUtc: DateTimeOffset.UtcNow.AddMinutes(-5),
        ct);
```

Inspection is read-only. Resetting a terminal row, replaying a message,
purging old rows, archiving them, or issuing a compensating command remains an
application/operator runbook decision because the application must prove
whether the downstream side effect already happened.

## Module Registration

Register modules through module-owned `IBondstoneModule` objects or thin host
extensions that create those objects. Module assemblies should own their
durable messaging capability, persistence binding, command handler
registration, published integration event registration, and event subscriber
registration.

```csharp
builder.Services.AddBondstone(bondstone =>
{
    bondstone.AddOrderingModule(connectionString);
    bondstone.AddFulfillmentModule(connectionString);
});
```

Durable messaging modules must also declare persistence:

```csharp
public sealed class FulfillmentBondstoneModule(string connectionString)
    : IBondstoneModule
{
    public string Name => FulfillmentModule.ModuleName;

    public void Configure(BondstoneModuleBuilder module)
    {
        module.UseDurableMessaging();
        module.UsePostgreSqlPersistence<FulfillmentDbContext>(
            connectionString,
            schema: FulfillmentModule.ModuleName);
        module.Commands.RegisterFromAssemblyContaining<ReserveInventoryHandler>();
        module.Events.RegisterPublishedEvent<InventoryReservedEvent>();
        module.Events.RegisterSubscriber<OrderPlacedEvent, RecordOrderPlacedHandler>(
            "fulfillment.order-placed-projection.v1");
    }
}
```

EF-backed module `DbContext` models must map the durable tables they use with
`ApplyBondstonePersistence()` or the granular `ApplyBondstoneOutbox()`,
`ApplyBondstoneInbox()`, and `ApplyBondstoneOperationState()` helpers.

Provider-specific module helpers are the preferred setup path because they
record module persistence metadata and register the module-owned runtime
factories in Bondstone runtime registries, plus transaction behavior and
dispatch services used by durable messaging.

The direct non-EF `Bondstone.Persistence.Postgres` package was removed after
MVP. New PostgreSQL modules should use EF Core persistence unless an ADR
reintroduces a non-EF provider for a real consumer need.

## Application Pipeline Behaviors

Application-owned command and event subscriber behavior is registered with
ordinary DI. Use `IModuleCommandPipelineBehavior<TCommand>` for command
handler concerns and `IModuleEventSubscriberPipelineBehavior<TEvent>` for
subscriber concerns.

```csharp
builder.Services.AddScoped<
    IModuleCommandPipelineBehavior<ReserveInventoryCommand>,
    ReserveInventoryAuthorizationBehavior>();

builder.Services.AddScoped<
    IModuleEventSubscriberPipelineBehavior<OrderPlacedEvent>,
    OrderPlacedAuditBehavior>();
```

These behaviors run after selected Bondstone/provider runtime contributions
and inside the module execution context when command or subscriber execution
establishes one. Passive pipeline contribution records are advanced
provider/runtime composition contracts stored in Bondstone runtime metadata,
not the normal application extension path. Bondstone does not provide
module-scoped application behavior registration; prefer ordinary DI
registration.

## Result-Returning Module Commands

Template or application code that already has result-producing command/request
handlers should move module transaction use cases onto Bondstone
`ICommand<TResult>` commands when the workflow needs Bondstone module behavior:
validation, module execution context, provider transaction behavior, durable
send/publish access, inbox/outbox participation, or operation-state updates.

```csharp
[DurableCommandIdentity("ordering.order.create.v1")]
public sealed record CreateOrderCommand(Guid CustomerId)
    : IDurableCommand, ICommand<CreateOrderResult>;

public sealed record CreateOrderResult(Guid OrderId);

public sealed class CreateOrderHandler
    : ICommandHandler<CreateOrderCommand, CreateOrderResult>
{
    public ValueTask<CreateOrderResult> HandleAsync(
        CreateOrderCommand command,
        CancellationToken ct = default)
    {
        Guid orderId = Guid.NewGuid();
        return ValueTask.FromResult(new CreateOrderResult(orderId));
    }
}
```

Register the handler on the owning module and execute it from HTTP endpoints,
schedulers, setup flows, or other app-owned entrypoints through
`IModuleCommandExecutor.ExecuteResultAsync`:

```csharp
module.Commands.RegisterHandler<
    CreateOrderCommand,
    CreateOrderResult,
    CreateOrderHandler>();

ModuleCommandExecutionResult<CreateOrderResult> result =
    await moduleCommandExecutor.ExecuteResultAsync(
        OrderingModule.ModuleName,
        new CreateOrderCommand(customerId),
        ct);

CreateOrderResult created = result.Result;
```

Keep app-owned request/handler or service abstractions for workflows that do
not need Bondstone module behavior, or as thin application-facing ports that
delegate into a registered Bondstone command. Do not keep a parallel
module-command framework for result-producing module transactions.

`ExecuteResultAsync` is the local, in-process path. It runs the command through
the owning module command pipeline and returns
`ModuleCommandExecutionResult<TResult>`, which carries the handler result plus
execution metadata such as receive inbox handling when the executor is called
from a receive pipeline.

When the same workflow must cross a durable boundary, keep the same logical
command contract and add durable identity through `IDurableCommand`. Durable
send is not RPC: it accepts work and returns send metadata, not
`CreateOrderResult` directly. Supply or create a durable operation id when the
caller needs to observe the committed result, record that id in the
application response or workflow state, then read or wait for it explicitly
through `IDurableOperationResultReader`.

```csharp
Guid durableOperationId = Guid.NewGuid();

DurableCommandSendResult sendResult = await durableCommandSender.SendAsync(
    new CreateOrderCommand(customerId),
    targetModule: OrderingModule.ModuleName,
    partitionKey: customerId.ToString("D"),
    durableOperationId: durableOperationId,
    ct: ct);

DurableOperationResult<CreateOrderResult> durableResult =
    await durableOperationResultReader.WaitForResultAsync<CreateOrderResult>(
        sendResult.DurableOperationId!.Value,
        timeout: TimeSpan.FromSeconds(30),
        pollInterval: TimeSpan.FromMilliseconds(250),
        ct: ct);

if (durableResult is { IsCompleted: true, HasResult: true, Result: { } order })
{
    Guid orderId = order.OrderId;
}
```

Use `GetResultAsync<TResult>()` when an API endpoint should read the current
operation state once. Use `WaitForResultAsync<TResult>()` only where an
explicit, timeout-bounded wait is acceptable for the caller. Applications still
own endpoint policy, timeout choice, polling cadence, and what to do with
unknown, pending, failed, or cancelled operation state.

When application policy has enough evidence that a workflow should stop
polling, use `IDurableOperationFinalizer` to mark the operation terminal in
the module that owns the operation-state store:

```csharp
DurableOperationFinalizationResult finalization =
    await durableOperationFinalizer.MarkFailedAsync(
        OrderingModule.ModuleName,
        durableOperationId,
        "Order creation expired before completion.",
        ct: ct);

if (!finalization.WasFinalized)
{
    DurableOperationStatus existingStatus = finalization.Status;
}
```

Use this for explicit application timeout, expiry, cancellation, or
administrative policy. Do not treat outbox terminal dispatch failure, inbox
idempotency, broker retry, or dead-letter behavior as automatic operation
failure unless application or provider-specific code has made that terminal
outcome explicit.

For a recurring expiry job, calculate the cutoff in application code and call
`IDurableOperationExpirationProcessor` for each module whose operation-state
store should be scanned:

```csharp
DateTimeOffset expiresBeforeUtc = timeProvider.GetUtcNow()
    .Subtract(TimeSpan.FromMinutes(30));

DurableOperationExpirationResult expiration =
    await durableOperationExpirationProcessor.MarkExpiredAsync(
        OrderingModule.ModuleName,
        expiresBeforeUtc,
        DurableOperationStatus.Failed,
        "Order creation expired before completion.",
        maxCount: 100,
        ct: ct);
```

Bondstone does not register a hosted expiry worker by default. Applications
own the schedule, cutoff calculation, module list, terminal status choice,
reason text, and alerting around `FinalizedCount`.

For result polling, prefer switching on `DurableOperationResult<TResult>.State`
when the caller needs to explain why a value is not available:

```csharp
return durableResult.State switch
{
    DurableOperationResultState.CompletedWithResult => Results.Ok(durableResult.Result),
    DurableOperationResultState.CompletedWithoutResult => Results.NoContent(),
    DurableOperationResultState.Pending or DurableOperationResultState.Running => Results.Accepted(),
    DurableOperationResultState.Failed => Results.Problem(durableResult.FailureReason),
    DurableOperationResultState.Cancelled => Results.StatusCode(StatusCodes.Status409Conflict),
    DurableOperationResultState.Unknown => Results.NotFound(),
    DurableOperationResultState.ResultDeserializationFailed => Results.Problem(
        durableResult.DeserializationFailure?.Message),
    _ => Results.Problem(),
};
```

The existing flags keep their narrow meanings: `IsKnown` means operation state
exists, `IsCompleted` means the stored status is `Completed`, `IsTerminal`
means `Completed`, `Failed`, or `Cancelled`, and `HasResult` means the result
payload was successfully deserialized as `TResult`. A completed operation can
therefore have `HasResult == false` when no payload was stored or when payload
deserialization failed.

Deserialization diagnostics include the operation id and requested result
type. When the stored operation state carries diagnostic context, result-reader
diagnostics also include the module name, durable message type name, and
handler identity. Old operation rows or schemas that have not added the
nullable diagnostic columns still work; those diagnostics fall back to the
operation id and requested result type.

Bondstone intentionally keeps the in-process and durable result paths
separate. If an application exposes one business-facing service method for
"create an order," that method should still choose either local
`ExecuteResultAsync` or durable `SendAsync` plus operation-result observation
based on the workflow boundary instead of hiding durable transport as a
synchronous request/response call.

The durable operation id ties the send and result lookup together:

- The caller supplies or records the operation id when sending durable work.
- `IDurableCommandSender.SendAsync` returns `DurableCommandSendResult` with the
  send id, accepted status, and the supplied `DurableOperationId`.
- When the sending module has operation-state persistence, Bondstone stages
  the operation as `Pending` if the operation is not already known.
- When the target module receives a durable `IDurableCommand` that also
  implements `ICommand<TResult>`, the receive pipeline executes the result
  handler through the normal module command pipeline.
- After the handler and surrounding pipeline succeed, Bondstone stores
  `Completed` plus the serialized result payload inside the target module
  receive transaction.
- Application policy may explicitly write `Failed` or `Cancelled` through
  `IDurableOperationFinalizer` when the operation should stop being observed
  as pending.
- Application expiry jobs may use `IDurableOperationExpirationProcessor` to
  query stale pending/running states from provider stores and finalize them
  through the same finalizer.
- `IDurableOperationResultReader` uses the same operation id to find operation
  state and deserialize the completed result payload.
- When the caller knows the result-owning module, pass that module name to
  `IDurableOperationResultReader` so Bondstone queries only that module's
  operation-state store instead of aggregating across every configured module.

Durable command payload serialization uses `IDurablePayloadSerializer`. The
default serializer and the built-in durable operation result payload serializer
use Bondstone's durable payload JSON options, configured through
`ConfigureBondstoneDurablePayloadJson(...)`. Application code should configure
that JSON surface for command/result DTO converters instead of introducing a
separate result serializer abstraction.

## Sending Commands

Durable commands are sent from inside a module execution context. The default
sender stages a command envelope in the current module outbox and uses the
current module as the source module. The intended caller is a module command
handler, event subscriber, or other work already executing through Bondstone's
module command/subscriber pipeline.

```csharp
await durableCommandSender.SendAsync(
    new ReserveInventoryCommand(orderId, sku, quantity),
    targetModule: FulfillmentModule.ModuleName,
    partitionKey: orderId.ToString("D"),
    durableOperationId: operationId,
    ct: ct);
```

The send result means the message was accepted into the source module outbox.
It does not mean the target module has handled it.

HTTP endpoints, schedulers, and custom app-owned entrypoints that need module
command behavior should execute a registered module command through
`IModuleCommandExecutor`; durable sends from that handler then use the normal
module execution context. Bondstone does not currently provide a public
source-module override for `IDurableCommandSender`.

## Publishing Events

Integration events are explicit durable facts. Publishing stages an event
envelope in the source module outbox. Event envelopes do not have
`TargetModule`; subscribers own their stable subscriber identities and inbox
keys. Publishing also requires the current module execution context, and the
publishing module must have registered the event through
`RegisterPublishedEvent`.

```csharp
await durableEventPublisher.PublishAsync(
    new OrderPlacedEvent(orderId, sku, quantity),
    partitionKey: orderId.ToString("D"),
    ct: ct);
```

## Receive Direction

Core exposes provider-neutral module receive pipelines over
`DurableMessageEnvelope`:

- `IModuleCommandReceivePipeline`
- `IModuleEventReceivePipeline`

Direct provider receive adapters should parse their provider-native message
body into the neutral durable envelope, acknowledge only after the receive
pipeline completes, and let failures flow to provider-native retry and
dead-letter policy. RabbitMQ exposes `IRabbitMqReceivedMessageDispatcher` and
opt-in hosted receive helpers. Bondstone's receive responsibility is the
native settlement handoff; broker retry schedules, delivery counts, and DLQ
settings remain provider/app-owned.

If a receive attempt finds an inbox row that was already received but not
processed, Bondstone fails the module receive with
`DurableInboxAlreadyReceivedException` instead of re-running the handler. The
provider worker then uses its normal failure handoff, such as RabbitMQ negative
acknowledgement. Recovery of the stale inbox row is an operator or application
procedure. Operators can inspect unprocessed inbox rows through
`IDurableInboxInspector`:

```csharp
IReadOnlyList<DurableInboxRecord> staleReceives = await inboxInspector
    .FindUnprocessedAsync(
        moduleName: FulfillmentModule.ModuleName,
        maxCount: 50,
        receivedAtOrBeforeUtc: DateTimeOffset.UtcNow.AddMinutes(-5),
        ct);
```

Inspection is read-only. Marking rows processed, deleting rows, re-running a
handler, moving broker messages, or issuing a compensating action remains an
application/operator runbook decision because the application must prove what
happened during the ambiguous receive attempt.

The RabbitMQ package also exposes `RabbitMqReceivedMessageMapper` for
app-owned consumers before they call the Bondstone dispatcher.

RabbitMQ also exposes `IRabbitMqReceivedMessageHandler`. This helper calls the
mapper and dispatcher, then invokes a caller-supplied acknowledge delegate only
after dispatch succeeds. It still does not start hosted consumers.

For hosts that want Bondstone to run the native receive loop, RabbitMQ exposes
an opt-in `UseReceiveWorker(...)` helper on its transport builder. This helper
runs consumers for configured receive bindings, but broker entities,
credentials, bindings, retry policy, and dead-letter setup remain
application-owned.

The current sample uses explicit `Bondstone.Transport.Local` queue routing by
default and also exposes `AddModularMonolithSampleWithRabbitMq(...)` for one
preferred direct-provider sample path. The RabbitMQ sample path keeps
connection and topology setup app-owned.
