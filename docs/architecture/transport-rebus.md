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

## Deferred Rebus Work

Deferred Rebus work includes receive-side inbox integration, handler
discovery, event publish/subscribe semantics, transport-level integration
tests, and hosted outbox worker registration.
