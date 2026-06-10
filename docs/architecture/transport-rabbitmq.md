# RabbitMQ Transport

`Bondstone.Transport.RabbitMq` owns RabbitMQ-specific transport adapter
behavior.

## Current Scope

The current adapter covers outgoing durable outbox transport, receive queue
topology, receive dispatchers, native message mapping, settlement handler
helpers, and an opt-in hosted receive worker proof.
`RabbitMqDurableOutboxTransport` implements
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

Receive topology is declared with provider-native queue vocabulary:

```csharp
bondstone.UseRabbitMqTransport(rabbitMq =>
{
    rabbitMq.ReceiveQueue("fulfillment.commands")
        .AcceptModule("fulfillment");

    rabbitMq.ReceiveQueue("sales-events")
        .SubscribeEvent(
            "sales.order.submitted.v1",
            "fulfillment",
            "fulfillment.sales-order-projection.v1");
});
```

For the opt-in worker, configure the native failure handoff explicitly:

```csharp
bondstone.UseRabbitMqTransport(rabbitMq =>
{
    rabbitMq.ReceiveQueue("fulfillment.commands")
        .AcceptModule("fulfillment");

    rabbitMq.UseReceiveWorker(options => options.RequeueOnFailure = false);
});
```

`RequeueOnFailure = false` is the usual choice when the application has
declared DLX or retry-queue topology for the receive queue. Set it to `true`
only when immediate broker redelivery is intentional and safe for that queue.

`IRabbitMqReceivedMessageDispatcher` maps a received RabbitMQ transport
message back to a Bondstone durable envelope. Command messages dispatch through
`IModuleCommandReceivePipeline`; event messages dispatch through
`IModuleEventReceivePipeline` once per subscriber binding on the receive
queue. A RabbitMQ consumer should acknowledge the broker delivery only after
the dispatcher returns.

`RabbitMqReceivedMessageMapper` converts native `BasicDeliverEventArgs`, or a
body plus `IReadOnlyBasicProperties`, into `RabbitMqTransportMessage`. This is
the adapter boundary for app-owned consumers before they call the dispatcher.

`IRabbitMqReceivedMessageHandler` composes native delivery mapping, dispatcher
execution, and a caller-supplied acknowledgement delegate. It acknowledges only
after dispatch succeeds and leaves failures visible to the caller.

`UseReceiveWorker(...)` is an opt-in hosted consumer helper. It consumes the
configured receive queues with `autoAck: false`, dispatches each delivery
through Bondstone, acknowledges on success, and negatively acknowledges on
failure using the configured requeue behavior. RabbitMQ connection,
queue/exchange/binding declaration, dead-letter topology, and retry policy
remain application-owned.

## Retry And Recovery Boundary

Bondstone owns persisted outbox retry and terminal failure state for outgoing
messages it has claimed. RabbitMQ receive retry and dead-letter policy remains
broker-native and application-owned.

On receive dispatch failure, the opt-in worker logs the queue, delivery tag,
message identity and type when available, exchange, routing key, redelivery
flag, and configured `requeue` decision. It then negatively acknowledges the
delivery with that configured `requeue` value. RabbitMQ redelivery, delayed
retry, and dead-letter exchange behavior are governed by the application's
RabbitMQ topology and client configuration, not by Bondstone.

## App-Owned Setup

Bondstone does not declare exchanges, queues, bindings, long-running
consumers, acknowledgement policy, retry policy, dead-letter policy, prefetch,
channels, or connections. Applications register and configure the RabbitMQ
`IConnection` and own native broker topology setup.

An MVP RabbitMQ deployment that uses the hosted worker should provision, at
minimum, the receive queue, any command/event bindings that route messages to
that queue, and an intentional failure path. That failure path can be a DLX,
retry queues, delayed exchange topology, quorum-queue delivery limits, or an
explicit decision to requeue immediately. Bondstone only performs the
configured acknowledgement or negative acknowledgement.

The adapter publishes the Bondstone-owned durable envelope as the message body
and copies durable identity, module metadata, operation id, partition key, and
W3C trace context into RabbitMQ properties and headers where appropriate.
External event handoff beyond provider-native queue destinations, unwrapped
payloads, CloudEvents, and schema-specific envelopes remain separate
ADR-backed decisions.

## Deferred Work

RabbitMQ has initial broker-backed receive worker integration tests for real
queue delivery, acknowledgement after successful command dispatch, and failed
dispatch handoff to application-owned dead-letter topology through negative
acknowledgement with `requeue: false`. It also proves event receive fan-out
from one broker queue delivery to each configured subscriber identity before
acknowledgement. Broker topology declaration remains a future slice.
