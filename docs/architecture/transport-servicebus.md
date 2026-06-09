# Azure Service Bus Transport

`Bondstone.Transport.ServiceBus` owns Azure Service Bus-specific transport
adapter behavior.

## Current Scope

The current adapter is a Phase 6 proof-oriented outgoing durable outbox
transport plus a Phase 6.5 receive dispatcher proof.
`ServiceBusDurableOutboxTransport` implements
`IDurableOutboxTransport` for claimed outbox records and maps Bondstone
durable envelopes to Azure Service Bus messages through a provider-local
envelope mapper.

Commands send to queues resolved by target module. Integration events publish
to event destinations resolved by stable event identity. Event destinations
can be Service Bus topics for broker fan-out or queues when another
infrastructure component owns fan-out. The adapter keeps command queue
topology and event destination topology separate because the durable intent
differs: commands target one module, while events publish facts for zero or
more subscribers.

Applications configure topology with the Service Bus transport builder:

```csharp
bondstone.UseServiceBusTransport(serviceBus =>
{
    serviceBus
        .UseModuleQueueConvention()
        .RouteEvent("ordering.order.placed.v1")
        .ToTopic("ordering.order.placed.v1");
});
```

Use `.ToQueue(...)` when an event should be sent to a queue instead of a topic.
Explicit module queue and event destination routes override conventions.
Diagnostic services report the same resolution results used by dispatch so
applications and tests can explain missing queues, topics, or destinations
without sending messages.

Receive topology is declared with Service Bus receive source vocabulary:

```csharp
bondstone.UseServiceBusTransport(serviceBus =>
{
    serviceBus.ReceiveQueue("fulfillment-commands")
        .AcceptModule("fulfillment");

    serviceBus.ReceiveSubscription("sales-events", "fulfillment")
        .SubscribeEvent(
            "sales.order.submitted.v1",
            "fulfillment",
            "fulfillment.sales-order-projection.v1");
});
```

`IServiceBusReceivedMessageDispatcher` maps a received Service Bus transport
message back to a Bondstone durable envelope. Command messages dispatch
through `IModuleCommandReceivePipeline`; event messages dispatch through
`IModuleEventReceivePipeline` once per subscriber binding on the receive
source. A Service Bus processor should complete the broker message only after
the dispatcher returns.

## App-Owned Setup

Bondstone does not create queues, topics, subscriptions, rules, long-running
processors, retry policy, dead-letter policy, credentials, connection strings,
or administrative clients. Applications register and configure the Azure
`ServiceBusClient` and own native Service Bus infrastructure setup.

The adapter sends the Bondstone-owned durable envelope as the message body and
copies durable identity, module metadata, operation id, partition key, and W3C
trace context into Service Bus message properties where appropriate. External
event handoff, unwrapped payloads, CloudEvents, and schema-specific envelopes
remain separate ADR-backed decisions.

## Deferred Work

Receive-side Service Bus hosted processors, acknowledgement integration,
retry/dead-letter policy handoff, broker-backed integration tests, and
topology declaration are future slices.
