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
- `Bondstone.Persistence.Postgres` for PostgreSQL non-EF durable module
  persistence.
- `Bondstone.Transport.RabbitMq` or `Bondstone.Transport.ServiceBus` when the
  host dispatches durable outbox records through a direct provider adapter.
- `Bondstone.Transport.Local` when a sample, test, or local development host
  explicitly wants in-process queue routing through the durable receive
  pipelines.

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
record module persistence metadata and register the module-owned outbox, inbox,
operation-state, transaction, and dispatch services used by durable messaging.

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
