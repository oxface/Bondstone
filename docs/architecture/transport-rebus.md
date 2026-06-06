# Rebus Transport

`Bondstone.Transport.Rebus` owns Rebus-specific transport adapter behavior.

## Outgoing Commands

`RebusDurableOutboxTransport` implements `IDurableOutboxTransport` for claimed
command outbox records. It sends one command through Rebus' explicit routing
API after resolving a destination address from the claimed
`DurableOutboxRecord`.

The first adapter supports `MessageKind.Command` only. Event publish/subscribe
semantics remain deferred because Rebus topic ownership, subscription storage,
and module event topology need their own transport decision.

`RebusModuleDestinationResolver` maps Bondstone target modules to Rebus
destination addresses. This keeps module identity separate from endpoint
addresses while allowing consumers to choose their own Rebus topology.

Outgoing Rebus topology is host-owned and adapter-specific. Applications can
configure target-module destinations with:

```csharp
bondstone.UseRebusTransport(rebus =>
{
    rebus.RouteModule("fulfillment").ToQueue("fulfillment-commands");
});
```

The older outbox-specific dictionary registration remains available for now.
Future receive topology should also stay host-owned and adapter-specific:
bind a receive endpoint to local modules accepted by the process. Bondstone
should not require a generic module-to-module route table for ordinary durable
command delivery. Modules declare durable messaging capability and command
handlers; the Rebus adapter supplies queue names, endpoint names, storage,
retry/dead-letter policy, and listener binding.

## Wire Envelope And Headers

The adapter sends a Bondstone-owned `RebusDurableMessageEnvelope` as the Rebus
message body. The wire envelope carries the durable message id, kind, stable
message type name, source module, target module, payload, metadata, created
timestamp, durable operation id, trace context, causation id, and partition
key.

Bondstone-specific Rebus headers carry durable identity and module metadata.
The adapter also writes W3C `traceparent`, `tracestate`, and `baggage` headers
when the durable envelope carries trace context.

Rebus' message id header is set from the Bondstone message id. Rebus'
correlation id header is set from the W3C trace id when available, otherwise
from the durable operation id when available, otherwise from the message id.
Rebus' in-reply-to header is set from Bondstone causation id when available.

The adapter does not set Rebus' type header to the Bondstone message identity.
Rebus serializers use that header for CLR type deserialization; Bondstone's
stable message identity is carried separately.

## Hosted Outbox Worker

Reusable hosted worker composition lives in `Bondstone.Hosting`, not in the
Rebus transport package. Rebus provides the `IDurableOutboxTransport`
implementation; the neutral hosting worker calls `IDurableOutboxDispatcher`
and sends through whichever transport adapter the application registered.

## Receive-Side Inbox

`IRebusDurableInboxHandlerExecutor` is the first Rebus receive-side inbox
adapter. It supports command envelopes carried as `RebusDurableMessageEnvelope`.

The adapter maps the wire envelope back to Bondstone's durable envelope
shape and derives the inbox key from message id, command target module, and an
explicit caller-supplied handler identity. Handler identity must be stable
consumer-owned text; it must not be derived from handler CLR names.

The receive adapter is available through low-level
`AddBondstoneRebusInbox` registration and the preferred fluent
`AddBondstone` path with `UseRebusInbox`. The fluent method registers the
receive adapter only; broader inbox capability validation remains deferred.

The adapter composes `IDurableInboxHandlerExecutor` with caller-supplied
handler and commit delegates. This keeps `Bondstone.Transport.Rebus`
independent from EF Core, PostgreSQL, hosting, and consumer domain assemblies.
Consumers that use EF Core can pass an EF persistence-scope commit delegate;
other providers can supply their own commit boundary.

The adapter accepts W3C trace context from the Bondstone wire envelope only
when `traceparent` parses through .NET `ActivityContext`. Invalid
`traceparent` values are rejected instead of being treated as loose
correlation identifiers.

Rebus acknowledgement follows normal Rebus handler completion semantics:
handled and already-processed inbox results complete normally; handler,
registration, processed-marker, commit, or unresolved already-received results
throw so Rebus retry and dead-letter policy remains in control.

## Typed Command Receive Pipeline

`IRebusTypedCommandReceivePipeline` sits above
`IRebusDurableInboxHandlerExecutor`.

The pipeline resolves the wire envelope's stable message type name through
`IMessageTypeRegistry`, deserializes the payload into the registered durable
command CLR type with `System.Text.Json`, starts a .NET/OTel consumer
`Activity` from the accepted W3C parent context when present, and invokes a
caller-registered typed command handler delegate.

The typed pipeline is available through low-level
`AddBondstoneRebusTypedCommandReceivePipeline` registration and the preferred
fluent `AddBondstone` path with `UseRebusTypedCommandReceivePipeline`.

Handler identity remains explicit stable text supplied at registration. It
must not be derived from handler CLR names. The typed pipeline still uses
caller-supplied commit delegates so EF Core, PostgreSQL, and consumer unit of
work ownership stay outside the Rebus transport package.

## Current Low-Level Receive Wiring Sketch

Applications can wire receive-side commands with a normal Rebus handler while
Bondstone keeps durable identity, inbox protection, and commit behavior
explicit.

Register the receive pipeline, persistence, and message identities during host
setup:

```csharp
services.AddSingleton<IMessageTypeRegistry>(serviceProvider =>
{
    var registry = new MessageTypeRegistry();
    registry.Register<ReserveOrderCommand>("fulfillment.order.reserve.v1");
    return registry;
});

services.AddBondstone(bondstone =>
{
    bondstone.UsePostgreSqlPersistence<ApplicationDbContext>(connectionString);
    bondstone.UseRebusTypedCommandReceivePipeline();
});

services.AddRebus(
    configure => configure
        .Transport(transport => transport.UsePostgreSql(
            rebusConnectionString,
            "rebus_messages",
            "fulfillment-receive",
            null,
            "public"))
        .Serialization(serializer => serializer.UseSystemTextJson()));

services.AddRebusHandler<ReserveOrderRebusHandler>();
```

The Rebus handler stays small but still repeats the durable handler identity
and commit boundary:

```csharp
public sealed class ReserveOrderRebusHandler(
    IRebusTypedCommandReceivePipeline receivePipeline,
    ReserveOrderHandler handler,
    IEntityFrameworkCorePersistenceScope persistenceScope)
    : IHandleMessages<RebusDurableMessageEnvelope>
{
    public async Task Handle(RebusDurableMessageEnvelope envelope)
    {
        await receivePipeline.HandleOnceAsync<ReserveOrderCommand>(
            envelope,
            "fulfillment.reserve-order.v1",
            handler.HandleAsync,
            persistenceScope.SaveChangesAsync);
    }
}
```

This shape is intentionally explicit and works for one or a few handlers. It
is now a low-level primitive rather than the preferred future app-facing
receive API.

The preferred next receive shape should be host topology binding from Rebus to
module command routes. Modules register command handlers and validators
without depending on Rebus; the host decides which local modules are exposed
through Rebus endpoints and which remote modules are reached through Rebus
routing. Rebus receive should eventually dispatch accepted wire envelopes into
`IModuleCommandExecutor` instead of asking application code to pass per-command
handler and commit delegates.

## Deferred Rebus Work

Deferred Rebus work includes event publish/subscribe semantics, host-owned
receive topology binding to module command routes, and validation that durable
receive modules have durable messaging enabled. Route or destination circuit
breaking, stale-claim recovery sweeps, dead-letter routing, receive retry
state, stale receive recovery, and worker metrics are hosting, persistence, or
future receive-pipeline decisions unless a later ADR accepts a
transport-specific policy.
