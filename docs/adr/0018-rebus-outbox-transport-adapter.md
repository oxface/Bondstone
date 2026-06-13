# 0018 Rebus Outbox Transport Adapter

Status: Superseded
Application: Not Applicable
Date: 2026-06-05

Superseded by
[ADR 0036 Direct Transport Adapters And Rebus Removal](0036-direct-transport-adapters-and-rebus-removal.md).

## Context

Bondstone now has a provider-neutral `IDurableOutboxTransport` boundary and a
plain `DurableOutboxDispatcher` that sends claimed outbox records through that
boundary. The next extraction step is the first transport adapter in
`Bondstone.Transport.Rebus`.

Rebus is a mature service-bus library with its own routing, serialization,
headers, retries, dead-letter handling, and receive pipeline. Bondstone should
not hide those capabilities behind a generic message bus or rebuild Rebus'
runtime model. Bondstone should only bridge its durable outbox envelope into a
Rebus send operation and leave broader Rebus host configuration to consumers.

The first transport adapter must also preserve Bondstone's stable message
identity and tracing metadata without deriving durable identity from CLR type
names.

## Decision

`Bondstone.Transport.Rebus` provides a Rebus implementation of
`IDurableOutboxTransport` for command envelopes.

The adapter sends only `MessageKind.Command` envelopes in this first slice. It
resolves an explicit Rebus destination address from the claimed
`DurableOutboxRecord` and calls Rebus' explicit routing send API. Event
publishing, subscription ownership, receive-side inbox integration, and Rebus
handler discovery remain separate decisions.

The adapter sends a Bondstone-owned Rebus wire envelope that carries the
durable envelope fields: message id, kind, stable message type name, source
module, target module, payload, metadata, created timestamp, durable operation
id, trace context, causation id, and partition key.

The adapter writes Bondstone-specific headers for durable identity and module
metadata. It also writes W3C tracing headers (`traceparent`, `tracestate`, and
`baggage`) when present. Rebus' message id header is set from the Bondstone
message id. Rebus' correlation id header is set from the W3C trace id when a
W3C traceparent is available, otherwise from the durable operation id when
available, otherwise from the message id. Rebus' in-reply-to header is set
from Bondstone causation id when available.

The adapter does not set Rebus' type header to the Bondstone message identity
because Rebus serializers use the type header for CLR type deserialization.
The stable Bondstone identity is carried in Bondstone-specific headers and in
the wire envelope.

## Consequences

Bondstone gets a concrete transport adapter without turning core into a
transport framework.

Consumers retain normal Rebus configuration and can choose transports,
serializers, workers, retries, and endpoint topology directly in Rebus.

Outgoing command delivery remains at-least-once. The dispatcher may send a
message and fail to record the outcome if the claim lease is lost; receiving
systems still need inbox/idempotency protection.

Receive-side Rebus integration is intentionally deferred. Until it exists,
this adapter verifies outgoing transport behavior and header mapping only.

## Related Decisions

- [0003 Package Boundaries And Target Framework](0003-package-boundaries-and-target-framework.md)
- [0017 Outbox Dispatcher Composition](0017-outbox-dispatcher-composition.md)
- [0020 Neutral Hosted Worker Package](0020-neutral-hosted-worker-package.md)

## Application Notes

- Current relevance: Superseded by
  [ADR 0036](0036-direct-transport-adapters-and-rebus-removal.md). This ADR is
  retained only as historical decision trail for the removed Rebus adapter.
- Stable docs: Current transport rules are described in
  [docs/architecture/messaging.md](../architecture/messaging.md) and
  [docs/architecture/persistence-core.md](../architecture/persistence-core.md).
- Agent guidance: Root [AGENTS.md](../../AGENTS.md) requires ADR review before
  broad transport support changes.
- Application evidence: The former Rebus implementation was removed by ADR 0036. Direct transport adapters now carry current transport behavior.
- Pending or deferred: Not applicable after superseding.

## Verification

Read back this ADR and affected stable docs. Ran no-restore build, targeted
Rebus unit tests, fast tests, pack, format check, and diff check.
