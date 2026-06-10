# Azure Service Bus Transport

`Bondstone.Transport.ServiceBus` owns Azure Service Bus-specific transport
adapter behavior.

## Current Scope

The current adapter covers outgoing durable outbox transport, receive source
topology, receive dispatchers, native message mapping, settlement handler
helpers, and an opt-in hosted receive worker.
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

Service Bus contributes command and event destination diagnostics to
Bondstone's aggregate outbound route ownership validation. Receive source
bindings always validate that accepted modules have durable command handlers
and subscribed event identities match registered Bondstone event subscribers.
In a single-transport host, Service Bus also fails startup when registered
event subscribers have no Service Bus receive binding. Queue-style event
destinations also validate that receive bindings for the event are on the
destination queue. Multiple subscriber bindings on that one queue remain valid
in-process fan-out; split subscribers should use a topic with
application-owned subscriptions and rules. This validation is diagnostic only;
it does not create Service Bus queues, topics, subscriptions, rules, retry
policy, or dead-letter settings.

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

For the opt-in worker, configure only processor behavior in Bondstone:

```csharp
bondstone.UseServiceBusTransport(serviceBus =>
{
    serviceBus.ReceiveSubscription("sales-events", "fulfillment")
        .SubscribeEvent(
            "sales.order.submitted.v1",
            "fulfillment",
            "fulfillment.sales-order-projection.v1");

    serviceBus.UseReceiveWorker(options =>
    {
        options.MaxConcurrentCalls = 4;
        options.MaxAutoLockRenewalDuration = TimeSpan.FromMinutes(5);
    });
});
```

Retry and dead-letter policy remains on the Service Bus queue or subscription,
such as max delivery count and dead-letter settings configured through Azure
infrastructure or native administration.

`IServiceBusReceivedMessageDispatcher` is the lower-level receive processing
service for app-owned Service Bus processors and the hosted worker. It maps a
received Service Bus transport message back to a Bondstone durable envelope.
Command messages dispatch through `IModuleCommandReceivePipeline`; event
messages dispatch through `IModuleEventReceivePipeline` once per subscriber
binding on the receive source. A Service Bus processor should complete the
broker message only after the dispatcher returns.

`ServiceBusReceivedMessageMapper` converts native `ServiceBusReceivedMessage`
instances into `ServiceBusTransportMessage`. This is the adapter boundary for
app-owned processors before they call the dispatcher.

`IServiceBusReceivedMessageHandler` composes native message mapping,
dispatcher execution, and a caller-supplied completion delegate. It completes
only after dispatch succeeds and leaves failures visible to the caller. This
helper is a settlement-ordering convenience over the dispatcher, not a hosted
receive loop.

`UseReceiveWorker(...)` is an opt-in hosted processor helper. It starts
processors for the configured receive queues and topic subscriptions with
`AutoCompleteMessages = false`, dispatches each message through Bondstone,
completes on success, and abandons on failure. Service Bus queues, topics,
subscriptions, rules, dead-letter policy, credentials, and administration
remain application-owned.

## Retry And Recovery Boundary

Bondstone owns persisted outbox retry and terminal failure state for outgoing
messages it has claimed. Service Bus receive retry and dead-letter policy
remains provider-native and application-owned.

On receive dispatch failure, the opt-in worker logs the receive source, message
identity and subject when available, delivery count when available, and the
abandon handoff. It then abandons the message. Service Bus redelivery timing,
max delivery count, lock settings, dead-letter subqueue behavior, and client
retry settings are governed by the application's Service Bus configuration,
not by Bondstone.

## App-Owned Setup

Bondstone does not create queues, topics, subscriptions, rules, long-running
processors, retry policy, dead-letter policy, credentials, connection strings,
or administrative clients. Applications register and configure the Azure
`ServiceBusClient` and own native Service Bus infrastructure setup.

Service Bus deployments that use the hosted worker should provision, at
minimum, the receive queue or topic subscription, the subscription rules needed
for event fan-out, max delivery count, and any dead-letter settings. Bondstone
only completes after successful dispatch or abandons on failure so the
configured Service Bus entity policy can take over.

The adapter sends the Bondstone-owned durable envelope as the message body and
copies durable identity, module metadata, operation id, partition key, and W3C
trace context into Service Bus message properties where appropriate. External
event handoff beyond provider-native Service Bus destinations is outside the
current transport contract.

Service Bus has emulator-backed receive worker integration tests for real queue
delivery, completion after successful command dispatch, abandon/dead-letter
handoff after failed dispatch, and topic subscription fan-out to configured
subscriber identities. Follow-up transport ideas are tracked in
[../backlog/15-future-work.md](../backlog/15-future-work.md).
