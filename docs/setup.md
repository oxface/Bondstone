# Library Setup

This document is the single home for library-user setup examples. Keep
architecture docs focused on contracts and decisions; update this page when the
preferred application wiring changes.

## Packages

Install only the packages needed by the host:

- `Bondstone` for core messaging, persistence contracts, module registration,
  and command execution.
- `Bondstone.EntityFrameworkCore` when the host maps Bondstone persistence into
  an EF Core `DbContext`.
- `Bondstone.EntityFrameworkCore.Postgres` when the host uses PostgreSQL-backed
  stores and outbox claiming.
- `Bondstone.Transport.Rebus` when the host sends durable commands through
  Rebus.
- `Bondstone.Hosting` when the host runs the durable outbox worker.

## Current App Wiring Shape

The preferred setup path is the fluent `AddBondstone` builder. The Bondstone
portion of a host that maps persistence into a consumer-owned `DbContext`,
registers a module, routes outgoing durable commands to Rebus, and starts the
outbox worker looks like this:

```csharp
using Bondstone.Configuration;
using Bondstone.EntityFrameworkCore.Persistence;
using Bondstone.EntityFrameworkCore.Postgres.Persistence;
using Bondstone.Hosting.Outbox;
using Bondstone.Messaging;
using Bondstone.Modules;
using Bondstone.Transport.Rebus.Outbox;
using Microsoft.EntityFrameworkCore;

builder.Services.AddBondstone(bondstone =>
{
    bondstone.Module("orders", module =>
    {
        module.UseDurableMessaging();
        module.UsePostgreSqlPersistence<AppDbContext>(connectionString);
        module.Commands.RegisterFromAssemblyContaining<CreateOrderCommand>();
    });

    bondstone.UseRebusTransport(rebus =>
        rebus.UseModuleQueueConvention());
    bondstone.Outbox.UseWorker(options =>
    {
        options.WorkerId = "orders-api-1";
        options.BatchSize = 100;
    });
});

public sealed class AppDbContext(DbContextOptions<AppDbContext> options)
    : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyBondstonePersistence();
    }
}

[DurableCommandIdentity("orders.order.create.v1")]
public sealed record CreateOrderCommand(string OrderId) : IDurableCommand;

public sealed class CreateOrderHandler : ICommandHandler<CreateOrderCommand>
{
    public ValueTask HandleAsync(
        CreateOrderCommand command,
        CancellationToken ct = default)
    {
        return ValueTask.CompletedTask;
    }
}
```

Inline `Module(...)` registration is fine for small apps and tests. For a
module-owned assembly, prefer a module-provided registration object and keep a
thin host extension for environment-specific values:

```csharp
public sealed class OrdersBondstoneModule(string connectionString)
    : IBondstoneModule
{
    public string Name => "orders";

    public void Configure(BondstoneModuleBuilder module)
    {
        module.UseDurableMessaging();
        module.UsePostgreSqlPersistence<OrdersDbContext>(
            connectionString,
            schema: "orders");
        module.Commands.RegisterFromAssemblyContaining<CreateOrderHandler>();
    }
}

public static class OrdersModuleRegistration
{
    public static BondstoneBuilder AddOrdersModule(
        this BondstoneBuilder bondstone,
        string connectionString)
    {
        return bondstone.AddModule(new OrdersBondstoneModule(connectionString));
    }
}

builder.Services.AddBondstone(bondstone =>
{
    bondstone.AddOrdersModule(connectionString);
});
```

`ApplyBondstonePersistence` maps the full current Bondstone durable persistence
shape. Modules that use `UseDurableMessaging` with EF persistence currently
need outbox and inbox mappings. Hosts that only need selected durable pieces
can map them explicitly:

```csharp
modelBuilder.ApplyBondstoneOutbox();
modelBuilder.ApplyBondstoneInbox();
modelBuilder.ApplyBondstoneOperationState();
```

For a module that only wants EF-backed module command transactions, omit
`UseDurableMessaging` and map only the module's own tables:

```csharp
builder.Services.AddBondstone(bondstone =>
{
    bondstone.Module("catalog", module =>
    {
        module.UseEntityFrameworkCorePersistence<CatalogDbContext>();
        module.Commands.RegisterFromAssemblyContaining<UpdateCatalogItemCommand>();
    });
});

public sealed class CatalogDbContext(DbContextOptions<CatalogDbContext> options)
    : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CatalogItem>();
    }
}
```

For durable messaging with a granular model, map at least outbox and inbox:

```csharp
builder.Services.AddBondstone(bondstone =>
{
    bondstone.Module("orders", module =>
    {
        module.UseDurableMessaging();
        module.UseEntityFrameworkCorePersistence<OrdersDbContext>();
        module.Commands.RegisterFromAssemblyContaining<CreateOrderCommand>();
    });
});

public sealed class OrdersDbContext(DbContextOptions<OrdersDbContext> options)
    : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Order>();
        modelBuilder.ApplyBondstoneOutbox();
        modelBuilder.ApplyBondstoneInbox();
    }
}
```

Configure Rebus itself through the normal Rebus host APIs for the application's
chosen transport, serializer, endpoint, retry, and worker settings before
building the service provider. Bondstone's Rebus package supplies the durable
outbox transport adapter and module topology mapping; it does not own the
whole Rebus host.

The preferred app-facing receive topology is configured through the Rebus
transport builder. The receive endpoint name must match the Rebus input queue
configured through Rebus native transport setup:

```csharp
builder.Services.AddRebus(configure => configure
    .Transport(transport => transport.UseInMemoryTransport(
        rebusNetwork,
        "fulfillment-commands"))
    .Serialization(serializer => serializer.UseSystemTextJson()));

builder.Services.AddBondstone(bondstone =>
{
    bondstone
        .AddOrderingModule(connectionString)
        .AddFulfillmentModule(connectionString);

    bondstone.UseRebusTransport(rebus =>
    {
        rebus
            .UseModuleQueueConvention()
            .ReceiveModule("fulfillment");
    });
    bondstone.Outbox.UseWorker();
});
```

This keeps Rebus broker, serializer, input queue, retry, dead-letter, and
worker policy Rebus-native while Bondstone owns durable module dispatch,
message identity, inbox handling, module command execution, and the Rebus
handler binding needed for the configured module receive endpoint.

For integration event publish dispatch, configure Rebus event topics on the
same Bondstone Rebus transport builder. Bondstone dispatches through the
application-owned Rebus bus, using Rebus routing for commands and Rebus topics
for events. The first Phase 5 slice is publish-side only: claimed event outbox
records can be published to topics, while subscriber execution and
subscription binding remain follow-up work.

```csharp
builder.Services.AddBondstone(bondstone =>
{
    bondstone.AddOrderingModule(connectionString);

    bondstone.UseRebusTransport(rebus =>
    {
        rebus.UseEventTopicConvention();
        rebus.RouteEvent("ordering.order.submitted.v1")
            .ToTopic("ordering-events");
    });
    bondstone.Outbox.UseWorker();
});
```

In the modular-monolith shape, each module should normally declare its own EF
`DbContext`, expose module-owned Bondstone registration, and bind that context
to PostgreSQL durable persistence. Durable sends write to the source module
outbox; durable receives write handler state, inbox markers, operation
completion, and any outgoing outbox messages through the target module
transaction.
