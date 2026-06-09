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
by target module. Integration events publish to a configured event exchange
with a routing key resolved by stable event identity. This keeps RabbitMQ
exchange/routing-key vocabulary native while preserving Bondstone's durable
command versus integration event semantics.

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

## App-Owned Setup

Bondstone does not declare exchanges, queues, bindings, consumers,
acknowledgement policy, retry policy, dead-letter policy, prefetch, channels,
or connections. Applications register and configure the RabbitMQ
`IConnection` and own native broker topology setup.

The adapter publishes the Bondstone-owned durable envelope as the message body
and copies durable identity, module metadata, operation id, partition key, and
W3C trace context into RabbitMQ properties and headers where appropriate.
External event handoff, direct queue convenience, unwrapped payloads,
CloudEvents, and schema-specific envelopes remain separate ADR-backed
decisions.

## Deferred Work

Receive-side RabbitMQ consumers, command queue binding, event queue binding,
acknowledgement behavior, retry/dead-letter policy, broker-backed integration
tests, direct queue send convenience, and topology declaration are future
slices.
