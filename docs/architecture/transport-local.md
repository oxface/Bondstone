# Local Transport

`Bondstone.Transport.Local` is an explicit in-process transport adapter for
samples, tests, and local development.

It is not a hidden fallback and it is not a broker replacement. Applications
must opt in with `UseLocalTransport` and declare local queues, command module
bindings, and event subscriber bindings.

```csharp
bondstone.UseLocalTransport(local =>
{
    local.RouteModule("fulfillment").ToQueue("fulfillment.commands");
    local.Queue("fulfillment.commands").AcceptModule("fulfillment");

    local.RouteEvent("ordering.order-placed.v1").ToQueue("ordering.order-placed");
    local.Queue("ordering.order-placed")
        .SubscribeEvent(
            "ordering.order-placed.v1",
            "fulfillment",
            "fulfillment.order-placed-projection.v1");
});
```

The adapter sends claimed outbox records through Bondstone's provider-neutral
receive pipelines:

- commands call `IModuleCommandReceivePipeline`;
- events call `IModuleEventReceivePipeline` once per configured subscriber.

That means local transport still exercises the durable outbox, receive inbox,
subscriber identity, module execution, and module transaction boundaries. It
does not provide broker durability, network isolation, broker retry policy,
dead-letter handling, subscription storage, or queue administration.

Use this package when a sample needs to prove Bondstone's durable loop before
choosing a real broker receive adapter. Production broker paths should use
direct provider adapters such as `Bondstone.Transport.RabbitMq` or
`Bondstone.Transport.ServiceBus`.
