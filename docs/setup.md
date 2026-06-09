# Setup

This document shows the current host setup shape for Bondstone.

## Packages

Install the packages needed for the host:

- `Bondstone` for core module, command, event, inbox/outbox, and durable
  message contracts.
- `Bondstone.Hosting` when the host runs the durable outbox worker.
- `Bondstone.EntityFrameworkCore` for EF Core durable persistence mappings and
  module transaction behavior.
- `Bondstone.EntityFrameworkCore.Postgres` for PostgreSQL EF Core duplicate
  classification and provider registration.
- `Bondstone.Persistence.Dapper.Postgres` for the PostgreSQL non-EF
  persistence proof.
- `Bondstone.Transport.RabbitMq` or `Bondstone.Transport.ServiceBus` when the
  host dispatches durable outbox records through a direct provider adapter.
- `Bondstone.Transport.Local` when a sample, test, or local development host
  explicitly wants in-process queue routing through the durable receive
  pipelines.

## Minimal Durable Outbox Host

The current direct transport packages implement outgoing durable outbox
dispatch. They keep provider connection, credentials, retry, dead-letter,
topology declaration, workers, and administration app-owned or provider-native.

RabbitMQ example:

```csharp
using Bondstone.Configuration;
using Bondstone.EntityFrameworkCore.Postgres.Persistence;
using Bondstone.Hosting.Outbox;
using Bondstone.Transport.RabbitMq.Outbox;
using RabbitMQ.Client;

builder.Services.AddBondstoneRabbitMqConnection(rabbitMqConnection);

builder.Services.AddBondstone(bondstone =>
{
    bondstone.UsePostgreSqlPersistence<AppDbContext>(
        builder.Configuration.GetConnectionString("App")!);

    bondstone.UseRabbitMqTransport(rabbitMq =>
    {
        rabbitMq.UseCommandExchange("bondstone.commands");
        rabbitMq.UseEventExchange("bondstone.events");
        rabbitMq.UseModuleRoutingKeyConvention();
        rabbitMq.UseEventRoutingKeyConvention();
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

When more than one direct transport is registered, Bondstone routes each
claimed outbox record through the provider whose topology matches that
message. If no provider or more than one provider matches, dispatch fails
instead of guessing.

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

## Sending Commands

Durable commands are sent from inside a module execution context. The default
sender stages a command envelope in the current module outbox and uses the
current module as the source module.

```csharp
await durableCommandSender.SendAsync(
    new ReserveInventoryCommand(orderId, sku, quantity),
    targetModule: FulfillmentModule.ModuleName,
    durableOperationId: operationId,
    ct);
```

The send result means the message was accepted into the source module outbox.
It does not mean the target module has handled it.

## Publishing Events

Integration events are explicit durable facts. Publishing stages an event
envelope in the source module outbox. Event envelopes do not have
`TargetModule`; subscribers own their stable subscriber identities and inbox
keys.

```csharp
await durableEventPublisher.PublishAsync(
    new OrderPlacedEvent(orderId, sku, quantity),
    partitionKey: orderId.ToString("D"),
    ct: ct);
```

## Receive Direction

Core now exposes provider-neutral module receive pipelines over
`DurableMessageEnvelope`:

- `IModuleCommandReceivePipeline`
- `IModuleEventReceivePipeline`

Direct provider receive adapters should parse their provider-native message
body into the neutral durable envelope, acknowledge only after the receive
pipeline completes, and let failures flow to provider-native retry and
dead-letter policy. RabbitMQ now has a receive queue dispatcher proof through
`IRabbitMqReceivedMessageDispatcher`, and Service Bus has a receive source
dispatcher proof through `IServiceBusReceivedMessageDispatcher`. Hosted
RabbitMQ consumers and hosted Service Bus processors are still planned
follow-up slices. Do not document app-facing broker receive setup as complete
until those adapters exist.

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
own hosted consumer or processor lifecycle.

The current sample uses explicit `Bondstone.Transport.Local` queue routing to
exercise the durable loop without presenting local transport as a production
broker adapter.
