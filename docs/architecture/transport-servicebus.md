# Azure Service Bus Transport

`Bondstone.Transport.ServiceBus` owns Azure Service Bus-specific transport
adapter behavior.

## Current Scope

The current adapter is a Phase 6 proof-oriented outgoing durable outbox
transport. `ServiceBusDurableOutboxTransport` implements
`IDurableOutboxTransport` for claimed outbox records and maps Bondstone
durable envelopes to Azure Service Bus messages through a provider-local
envelope mapper.

Commands send to queues resolved by target module. Integration events publish
to topics resolved by stable event identity. The adapter keeps command queue
topology and event topic topology separate because the durable intent differs:
commands target one module, while events publish facts for zero or more
subscribers.

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

Explicit module queue and event topic routes override conventions. Diagnostic
services report the same resolution results used by dispatch so applications
and tests can explain missing queues or topics without sending messages.

## App-Owned Setup

Bondstone does not create queues, topics, subscriptions, rules, processors,
retry policy, dead-letter policy, credentials, connection strings, or
administrative clients. Applications register and configure the Azure
`ServiceBusClient` and own native Service Bus infrastructure setup.

The adapter sends the Bondstone-owned durable envelope as the message body and
copies durable identity, module metadata, operation id, partition key, and W3C
trace context into Service Bus message properties where appropriate. External
event handoff, unwrapped payloads, CloudEvents, and schema-specific envelopes
remain separate ADR-backed decisions.

## Deferred Work

Receive-side Service Bus processors, command queue listeners, event topic
subscription binding, acknowledgement behavior, retry/dead-letter policy,
broker-backed integration tests, and topology declaration are future slices.
