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
        module.Commands.RegisterFromAssemblyContaining<CreateOrderCommand>();
    });

    bondstone.UsePostgreSqlPersistence<AppDbContext>(connectionString);
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

Configure Rebus itself through the normal Rebus host APIs for the application's
chosen transport, serializer, endpoint, retry, and worker settings before
building the service provider. Bondstone's Rebus package supplies the durable
outbox transport adapter and module topology mapping; it does not own the
whole Rebus host.

The current receive-side Rebus typed pipeline is still a low-level primitive
that requires explicit handler identity and commit delegates. The preferred
app-facing receive topology is now configured through the Rebus transport
builder, but actual Rebus listener binding to that topology remains deferred,
so this setup page intentionally shows the outgoing durable command path only.
