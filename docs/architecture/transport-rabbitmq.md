# RabbitMQ Transport

`Bondstone.Transport.RabbitMq` owns RabbitMQ-specific transport adapter
behavior.

## Current Scope

The current adapter is a Phase 6 proof-oriented outgoing durable outbox
transport. `RabbitMqDurableOutboxTransport` implements
`IDurableOutboxTransport` for claimed outbox records and maps Bondstone
durable envelopes to RabbitMQ publish messages through a provider-local
envelope mapper.

Commands publish to a configured command exchange with a routing key resolved
by target module. Integration events publish to event destinations resolved by
stable event identity. Event destinations can be a configured event exchange
with a routing key, or a direct queue publish through RabbitMQ's default
exchange. This keeps RabbitMQ exchange/routing-key/queue vocabulary native
while preserving Bondstone's durable command versus integration event
semantics.

Applications configure topology with the RabbitMQ transport builder:

```csharp
bondstone.UseRabbitMqTransport(rabbitMq =>
{
    rabbitMq
        .UseCommandExchange("bondstone.commands")
        .UseEventExchange("bondstone.events")
        .UseModuleRoutingKeyConvention()
        .RouteEvent("ordering.order.placed.v1")
        .ToRoutingKey("ordering.order.placed.v1");
});
```

Explicit module and event routing keys override conventions. Diagnostic
services report the same resolution results used by dispatch so applications
and tests can explain missing exchanges or routing keys without publishing
messages.

Use `.ToQueue(...)` or `UseEventQueueConvention(...)` when an event should be
sent directly to a queue.

## App-Owned Setup

Bondstone does not declare exchanges, queues, bindings, consumers,
acknowledgement policy, retry policy, dead-letter policy, prefetch, channels,
or connections. Applications register and configure the RabbitMQ
`IConnection` and own native broker topology setup.

The adapter publishes the Bondstone-owned durable envelope as the message body
and copies durable identity, module metadata, operation id, partition key, and
W3C trace context into RabbitMQ properties and headers where appropriate.
External event handoff beyond provider-native queue destinations, unwrapped
payloads, CloudEvents, and schema-specific envelopes remain separate
ADR-backed decisions.

## Deferred Work

Receive-side RabbitMQ consumers, command queue binding, event queue binding,
acknowledgement behavior, retry/dead-letter policy, broker-backed integration
tests, and topology declaration are future slices.
