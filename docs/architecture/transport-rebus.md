# Rebus Transport

`Bondstone.Transport.Rebus` owns Rebus-specific transport adapter behavior.

## Outgoing Commands

`RebusDurableOutboxTransport` implements `IDurableOutboxTransport` for claimed
command outbox records. It sends one command through Rebus' explicit routing
API after resolving a destination address from the claimed
`DurableOutboxRecord`.

The first adapter supports `MessageKind.Command` dispatch only. Core can stage
`MessageKind.Event` envelopes through `IDurableEventPublisher`, but Rebus
event publish/subscribe semantics remain deferred because Rebus topic
ownership, subscription storage, and module event topology need their own
transport decision.

`RebusModuleDestinationResolver` maps Bondstone target modules to Rebus
destination addresses. This keeps module identity separate from endpoint
addresses while allowing consumers to choose their own Rebus topology.

Outgoing Rebus topology is host-owned and adapter-specific. Applications can
configure conventional module queue names through the `UseRebusTransport`
builder. Explicit target-module routes remain available as overrides for
legacy names, extracted modules, or other non-conventional topology.

The older outbox-specific dictionary registration remains available for now.
Receive topology is also host-owned and adapter-specific. The same
`UseRebusTransport` builder can bind a Rebus receive endpoint name to local
modules accepted by the process:

```csharp
bondstone.UseRebusTransport(rebus =>
{
    rebus
        .UseModuleQueueConvention()
        .ReceiveModule("fulfillment");
});
```

Receive endpoint bindings are recorded in
`IRebusModuleReceiveEndpointRegistry`, and configuring one registers the
module command receive pipeline. A module may be accepted by only one Rebus
receive endpoint in a host; duplicate matching registrations are idempotent.
With the default convention, module `fulfillment` maps to Rebus endpoint
`fulfillment-commands`. A custom naming convention can be provided with
`UseModuleQueueConvention(moduleName => ...)`. The convention can route
outgoing commands to any target module by name, including modules extracted to
another service. Accepted receive modules also provide outgoing command
destinations for the same target modules.

Receive bindings configured through `UseRebusTransport` are validated during
`AddBondstone` composition. Each accepted module must be registered in the
host, must use durable messaging, and must have at least one durable command
handler. Missing modules, non-durable modules, and receive bindings with no
durable handlers fail with endpoint and module names in the error. Outgoing-only
explicit routes and module queue conventions remain valid for remote modules
that are not registered locally.

Explicit `RouteModule(...).ToQueue(...)` or `.ToAddress(...)` calls override
any destination derived from receive bindings or conventions. Destination
resolution order is explicit route, receive binding, then module queue
convention. Bondstone should not require a generic module-to-module route table
for ordinary durable command delivery. Modules declare durable messaging
capability and command handlers; the Rebus adapter supplies queue names,
endpoint names, storage, retry/dead-letter policy, and listener binding.

Rebus infrastructure setup remains Rebus-native and outside Bondstone's
topology builder. Applications still configure the broker transport,
connection string, serializer, worker count, retry/dead-letter policy, and
input queue through Rebus' own configuration APIs.

The default operational shape should be one command receive queue per module
that needs independent ownership, scaling, retry policy, or service-extraction
headroom. A receive endpoint may accept multiple local modules when those
modules deploy, scale, fail, and recover together, but a general catch-all
inbox queue should be treated as a small-host or development convenience, not
the durable default. The database inbox can still be shared because inbox keys
include module and handler identity; the transport queue is the operational
backlog and scale boundary.

Commands should use Rebus queues. Topic or subscription topology is reserved
for future event publish/subscribe work, where each subscriber needs its own
copy of an event.

Future event support should use Rebus publish/subscribe vocabulary instead of
command routing vocabulary. Durable event publish topology may map an
integration event identity to a Rebus topic or equivalent publish subject.
Durable event subscription topology may bind a subscriber module and stable
subscriber identity to a Rebus endpoint/subscription. Bondstone topology
metadata should describe those durable message relationships; applications
still configure Rebus-native subscription storage, broker transport,
connection string, serializer, worker count, retry/dead-letter policy, and
input queue through Rebus.

Subscriber inbox identity is per subscriber. A future Rebus event receive
pipeline should derive event inbox keys from Bondstone message id, subscriber
module, and stable subscriber identity, not from command target module.
Each subscription's copy can then be retried, dead-lettered, and marked
processed independently.

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
command CLR type through Bondstone's shared `IDurablePayloadSerializer`,
starts a .NET/OTel consumer `Activity` from the accepted W3C parent context
when present, and invokes a caller-registered typed command handler delegate.

The typed pipeline is available through low-level
`AddBondstoneRebusTypedCommandReceivePipeline` registration and the preferred
fluent `AddBondstone` path with `UseRebusTypedCommandReceivePipeline`.

Handler identity remains explicit stable text supplied at registration. It
must not be derived from handler CLR names. The typed pipeline still uses
caller-supplied commit delegates so EF Core, PostgreSQL, and consumer unit of
work ownership stay outside the Rebus transport package.

## Current Low-Level Receive Wiring Shape

Applications can wire receive-side commands with a normal Rebus handler while
Bondstone keeps durable identity, inbox protection, and commit behavior
explicit.

This lower-level shape registers the receive pipeline, persistence, Rebus host
configuration, stable message identities, and a normal Rebus handler. The
handler passes the wire envelope, durable handler identity, typed handler
delegate, and commit delegate to `IRebusTypedCommandReceivePipeline`.

This shape is intentionally explicit and works for one or a few handlers, but
it is a low-level primitive rather than the preferred app-facing receive path.
The user-facing setup example in [../setup.md](../setup.md) therefore shows
the outgoing durable command path and avoids presenting this temporary receive
wiring as the main application pattern.

The preferred receive shape is host topology binding from Rebus to module
command routes. Modules register command handlers and validators without
depending on Rebus; the host decides which local modules are exposed through
Rebus endpoints and which remote modules are reached through Rebus routing.
Rebus receive dispatches wire envelopes into `IModuleCommandExecutor` through
the module command receive pipeline instead of asking application code to pass
per-command handler and commit delegates.

Current groundwork adds a Rebus module command receive pipeline that resolves a
wire envelope through Bondstone message identity and module command route
metadata, deserializes payloads through Bondstone's shared
`IDurablePayloadSerializer`, passes the durable inbox record into
`IModuleCommandExecutor`, and reads the inbox result from
`ModuleCommandExecutionResult`. This removes per-command handler delegates
from the receive primitive.

Host-owned receive endpoint topology can now record which local modules a
Rebus endpoint accepts. Actual Rebus worker/listener binding to that topology
is still future work.

## Deferred Rebus Work

Deferred Rebus work includes event publish/subscribe semantics, actual Rebus
worker/listener binding to configured module receive endpoints, endpoint
dispatcher APIs, command topology diagnostics for outbound destination
resolution, route or destination circuit breaking, stale-claim recovery
sweeps, dead-letter routing, receive retry state, stale receive recovery, and
worker metrics. These remain hosting, persistence, or future receive-pipeline
decisions unless a later ADR accepts a transport-specific policy.
