# Local Transport

`Bondstone.Transport.Local` is an explicit in-process transport adapter for
samples, tests, and local development.

It is not a hidden fallback and it is not a broker replacement. Applications
must opt in with `UseLocalTransport`. Local transport is useful infrastructure
for exercising Bondstone's durable loop inside one host; it is not broker
durability and should not be presented as production transport guidance.

## Topology

For basic module-to-module durable command routing, `UseModuleQueueConvention()`
configures complete local command topology. Each target module routes to
`{module}.commands`, and that convention queue accepts the same target module.

```csharp
bondstone.UseLocalTransport(local =>
{
    local.UseModuleQueueConvention();
});
```

Use explicit queue bindings when a command route does not follow the module
queue convention, or when the topology should document the queue name in host
code. Explicit command routes stay explicit: `RouteModule(...).ToQueue(...)`
does not infer that the destination queue may execute that module. Add
`Queue(...).AcceptModule(...)` for every accepted command target module.

```csharp
bondstone.UseLocalTransport(local =>
{
    local.RouteModule("fulfillment").ToQueue("fulfillment.priority.commands");
    local.Queue("fulfillment.priority.commands").AcceptModule("fulfillment");
});
```

Event routes also require explicit subscriber bindings. The queue name may be
configured explicitly, or by event queue convention, but subscriber module and
subscriber identity are part of the durable receive identity and cannot be
inferred from the event type alone. Add `Queue(...).SubscribeEvent(...)` for
each local subscriber that should receive the event:

```csharp
bondstone.UseLocalTransport(local =>
{
    local.RouteEvent("ordering.order-placed.v1").ToQueue("ordering.order-placed");
    local.Queue("ordering.order-placed")
        .SubscribeEvent(
            "ordering.order-placed.v1",
            "fulfillment",
            "fulfillment.order-placed-projection.v1");
});
```

## Receive Semantics

The adapter sends claimed outbox records through Bondstone's provider-neutral
receive pipelines:

- commands call `IModuleCommandReceivePipeline`;
- events call `IModuleEventReceivePipeline` once per configured subscriber.

That means local transport still exercises the durable outbox, receive inbox,
subscriber identity, module execution, and module transaction boundaries. It
uses the same command and event inbox identity described in
[messaging.md](messaging.md): commands are keyed by message id, target module,
and stable handler identity; events are keyed by message id, subscriber module,
and stable subscriber identity. The persistence contract for inbox records is
described in [persistence-core.md](persistence-core.md).

Already processed inbox rows are handled idempotently by the receive pipeline
and do not re-run the handler. Already received but unprocessed inbox rows are
loud failures through `DurableInboxAlreadyReceivedException`; local transport
does not silently skip them, mark them processed, or re-run the handler.
Stale inbox row recovery remains operator-owned or application-owned.

Local transport does not provide broker durability, network isolation, broker
retry policy, dead-letter handling, subscription storage, queue
administration, broker receive persistence, or stale inbox recovery. A failed
local dispatch is an outbox dispatch failure for the sending module's outbox
worker and follows Bondstone's persisted outbox failure policy; it is not
handed to a broker retry or DLQ mechanism.

Use this package when a sample needs to prove Bondstone's durable loop before
choosing a real broker integration. Production broker paths should use
app-owned transport code around `IDurableEnvelopeDispatcher`,
`IDurableMessageEnvelopeSerializer`, and durable inbox ingestion.
