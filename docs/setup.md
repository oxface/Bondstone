# Setup

This document shows the current host setup shape for Bondstone.

## Packages

Install the packages needed for the host:

- `Bondstone` for core module, command, integration-event, and module
  execution contracts.
- `Bondstone.Capabilities.DomainEvents` when domain model types expose
  module-local domain events.
- `Bondstone.Capabilities.DomainEvents.EntityFrameworkCore` when EF-backed
  modules opt into domain event collection and persistence.
- `Bondstone.Hosting` when the host runs the durable outbox worker.
- `Bondstone.Persistence` for provider-neutral durable envelopes, inbox,
  outbox, operation state, and persistence contracts used by custom
  persistence or dispatch composition.
- `Bondstone.Persistence.EntityFrameworkCore` for EF Core durable persistence mappings and
  module transaction behavior.
- `Bondstone.Persistence.EntityFrameworkCore.Postgres` for PostgreSQL EF Core duplicate
  classification and provider registration.
- `Bondstone.Persistence.Postgres` for PostgreSQL non-EF durable module
  persistence.
- `Bondstone.Transport` for provider-neutral topology diagnostics used by
  custom transport adapters.
- `Bondstone.Transport.RabbitMq` or `Bondstone.Transport.ServiceBus` when the
  host dispatches durable outbox records through a direct provider adapter.
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
- `Bondstone.Persistence.Postgres` only for modules that intentionally avoid
  EF Core while still using PostgreSQL durable persistence;
- one direct transport adapter, either `Bondstone.Transport.RabbitMq` or
  `Bondstone.Transport.ServiceBus`, when messages leave the process through a
  broker.

Add optional capability packages only when the module uses that capability.
For example, EF-backed module-local domain event persistence uses
`Bondstone.Capabilities.DomainEvents` and
`Bondstone.Capabilities.DomainEvents.EntityFrameworkCore`.

Use the provider transport extensions on `AddBondstone` for ordinary hosts.
Those extensions register provider topology validation and diagnostics as well
as outbox dispatch. Lower-level persistence, receive, dispatcher, and outbox
transport types remain available for advanced composition and tests, but they
are not the quick-start path.

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

Add these only when a module uses EF-backed module-local domain event
persistence:

```xml
<ItemGroup>
  <PackageReference Include="Bondstone.Capabilities.DomainEvents" />
  <PackageReference Include="Bondstone.Capabilities.DomainEvents.EntityFrameworkCore" />
</ItemGroup>
```

Contracts projects that define durable commands or integration events need
`Bondstone`. Module implementation projects that configure EF persistence need
the EF Core and PostgreSQL packages. Host projects that configure local
transport and the hosted worker need `Bondstone.Transport.Local` and
`Bondstone.Hosting`. See [packaging.md](packaging.md) for the full package
matrix and dependency direction.

Common namespaces for this path are:

- `Bondstone.Configuration` for `AddBondstone` and `BondstoneBuilder`.
- `Bondstone.Modules` for `IBondstoneModule`, `BondstoneModuleBuilder`,
  command handlers, event handlers, and module registration.
- `Bondstone.Messaging` for `ICommand`, `IDurableCommand`,
  `IIntegrationEvent`, `DurableCommandIdentityAttribute`,
  `IntegrationEventIdentityAttribute`, `IDurableCommandSender`, and
  `IDurableEventPublisher`.
- `Bondstone.Persistence.EntityFrameworkCore.Persistence` for
  `ApplyBondstonePersistence`, `ApplyBondstoneOutbox`,
  `ApplyBondstoneInbox`, `ApplyBondstoneOperationState`, and
  `UseEntityFrameworkCorePersistence`.
- `Bondstone.Persistence.EntityFrameworkCore.Postgres.Persistence` for
  `UsePostgreSqlPersistence`.
- `Bondstone.Capabilities.DomainEvents` for optional `IDomainEvent` and
  `IDomainEventSource`.
- `Bondstone.Capabilities.DomainEvents.EntityFrameworkCore.Persistence` for
  optional `UseEntityFrameworkCoreDomainEventPersistence` and
  `ApplyBondstoneDomainEvents`.
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
    : IDurableCommand;
```

Handlers are ordinary module services. Bondstone's module transaction behavior
commits handler state, inbox markers, operation state, and outgoing outbox rows
for modules that declare durable EF persistence:

```csharp
using Bondstone.Modules;
using MyApp.Fulfillment.Contracts;

namespace MyApp.Fulfillment;

public sealed class ReserveInventoryHandler(FulfillmentDbContext dbContext)
    : ICommandHandler<ReserveInventoryCommand>
{
    public ValueTask HandleAsync(
        ReserveInventoryCommand command,
        CancellationToken ct = default)
    {
        dbContext.Reservations.Add(new FulfillmentReservation(
            command.OrderId,
            command.Sku,
            command.Quantity));

        return ValueTask.CompletedTask;
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
using Bondstone.Capabilities.DomainEvents.EntityFrameworkCore.Persistence;
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
        rabbitMq.UseCommandExchange("bondstone.commands");

        rabbitMq.RouteModule("fulfillment")
            .ToRoutingKey("fulfillment.commands");
        rabbitMq.ReceiveQueue("fulfillment.commands")
            .AcceptModule("fulfillment");

        rabbitMq.RouteEvent("ordering.order-placed.v1")
            .ToQueue("ordering.order-placed");
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

Azure Service Bus uses the same Bondstone composition shape with
`UseServiceBusTransport`, queues for commands, and topic or queue
destinations for integration events.

Provider transport route conventions are outbound-only. RabbitMQ
`UseModuleRoutingKeyConvention()` and Service Bus `UseModuleQueueConvention()`
resolve where commands are sent, but hosts that receive commands still need
explicit receive bindings such as `ReceiveQueue(...).AcceptModule(...)`.
`Bondstone.Transport.Local` is the local-development exception: its
`UseModuleQueueConvention()` configures complete in-process command topology
because there is no broker queue or binding to provision.

Use provider transport extensions on the main `BondstoneBuilder` for normal
host setup. The lower-level `bondstone.Outbox.UseRabbitMqTransport(...)` and
`bondstone.Outbox.UseServiceBusTransport(...)` overloads are advanced
composition APIs for manual outbox transport registration; they do not add the
provider configuration validators and topology diagnostic sources that the
normal setup path adds.

When more than one direct transport is registered, Bondstone routes each
claimed outbox record through the provider whose topology matches that
message. If no provider or more than one provider matches, dispatch fails
instead of guessing.

Bondstone owns retry and terminal failure for outgoing persisted outbox
records. Current outbox status semantics are described in
[architecture/persistence-core.md](architecture/persistence-core.md); they are
separate from provider-native receive DLQs.

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

Modules that do not use EF Core can use `Bondstone.Persistence.Postgres`:

```csharp
public sealed class BillingBondstoneModule(string connectionString)
    : IBondstoneModule
{
    public string Name => BillingModule.ModuleName;

    public void Configure(BondstoneModuleBuilder module)
    {
        module.UseDurableMessaging();
        module.UsePostgresPersistence(
            connectionString,
            schema: BillingModule.ModuleName);
        module.Events.RegisterSubscriber<OrderPlacedEvent, RecordBillingInvoiceHandler>(
            "billing.order-invoice-projection.v1");
    }
}
```

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
public sealed record CreateOrderCommand(Guid CustomerId)
    : ICommand<CreateOrderResult>;

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
```

Keep app-owned request/handler or service abstractions for workflows that do
not need Bondstone module behavior, or as thin application-facing ports that
delegate into a registered Bondstone command. Do not keep a parallel
module-command framework for result-producing module transactions.

When the same workflow must cross a durable boundary, the command can also
implement `IDurableCommand`. Durable send still returns accepted-work metadata,
not `CreateOrderResult` directly. Supply or create a durable operation id when
the caller needs to observe the committed result, then read or wait for it
through `IDurableOperationResultReader`.

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
    durableOperationId: operationId,
    ct);
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
dead-letter policy. RabbitMQ exposes `IRabbitMqReceivedMessageDispatcher`, and
Service Bus exposes `IServiceBusReceivedMessageDispatcher`. Both providers
also expose opt-in hosted receive helpers. Bondstone's receive responsibility
is the native settlement handoff; broker retry schedules, delivery counts, and
DLQ settings remain provider/app-owned.

If a receive attempt finds an inbox row that was already received but not
processed, Bondstone fails the module receive with
`DurableInboxAlreadyReceivedException` instead of re-running the handler. The
provider worker then uses its normal failure handoff, such as RabbitMQ negative
acknowledgement or Service Bus abandon. Recovery of the stale inbox row is an
operator or application procedure.

Provider packages also expose native receive message mappers:

- `RabbitMqReceivedMessageMapper` for RabbitMQ deliveries;
- `ServiceBusReceivedMessageMapper` for Service Bus received messages.

Use those mappers inside app-owned consumers/processors before calling the
Bondstone dispatcher.

Provider packages also expose small handler helpers:

- `IRabbitMqReceivedMessageHandler`
- `IServiceBusReceivedMessageHandler`

These helpers call the mapper and dispatcher, then invoke a caller-supplied
acknowledge/complete delegate only after dispatch succeeds. They still do not
start hosted consumers or processors.

For hosts that want Bondstone to run the native receive loop, RabbitMQ and
Service Bus expose opt-in `UseReceiveWorker(...)` helpers on their transport
builders. These helpers run consumers/processors for configured receive
topology, but broker entities, credentials, bindings, retry policy, and
dead-letter setup remain application-owned.

The current sample uses explicit `Bondstone.Transport.Local` queue routing by
default and also exposes `AddModularMonolithSampleWithRabbitMq(...)` for one
preferred direct-provider sample path. The RabbitMQ sample path keeps
connection and topology setup app-owned.
