# 0029 Durable Payload Serialization Boundary

Status: Accepted
Application: Applied
Date: 2026-06-07

## Context

Bondstone durable messages already cross several runtime boundaries: core
command send, core event publish, provider-neutral outbox storage, Rebus
command dispatch, Rebus command receive, and future event receive. The current
implementation serializes command and event payloads with direct
`System.Text.Json` calls in more than one place.

That direct usage is acceptable as early scaffolding, but it should not harden
into separate command-only, event-only, or transport-specific serialization
surfaces. Commands and integration events both store durable payload text in
the same envelope field and both resolve stable message identity through
Bondstone's message registry. Consumers need one place to configure JSON
options and converters for all durable payloads.

Rebus also has its own message serializer and CLR type header behavior. That
serializer is transport infrastructure and should remain configured through
Rebus. Bondstone's durable payload format is separate: it is the application
message payload stored in outbox/inbox records and carried inside the
Bondstone wire envelope. The durable payload must be identified by
Bondstone's `MessageKind` and stable `MessageTypeName`, not by transport CLR
type headers.

This affects public API shape, serializer behavior, durable payload
compatibility, command/event parity, and transport boundary ownership, so it
needs an ADR before more send and receive code hardens direct
`System.Text.Json` usage.

## Decision

Bondstone should define one durable payload serialization boundary for
commands and integration events.

The boundary should be core-owned and transport-neutral. The intended public
shape is a small Bondstone serializer abstraction, such as
`IDurablePayloadSerializer`, plus a single JSON configuration surface for the
default implementation. Command send, command receive, event publish, and
future event receive must use that same boundary.

The first supported durable payload format should remain JSON implemented with
`System.Text.Json` and web defaults. Consumers should be able to configure the
default JSON implementation with durable-payload-specific
`JsonSerializerOptions`, including converters. Bondstone should not ask
applications to register a raw application-wide `JsonSerializerOptions`
instance whose ownership is ambiguous across ASP.NET Core, Rebus, and other
libraries.

Serialization should produce the `DurableMessageEnvelope.Payload` text stored
by persistence providers and copied into transport wire envelopes.
Deserialization should occur only after Bondstone has resolved the envelope's
stable `MessageTypeName` and `MessageKind` through `IMessageTypeRegistry`.
The resolved CLR type is the target type for payload deserialization.

The durable payload serializer must be message-kind neutral. It must support
`IDurableCommand` and `IIntegrationEvent` payloads through the same
configuration and behavior. Command/event differences belong to routing,
target module, fan-out, subscriber identity, and receive orchestration, not to
separate payload serializers.

Transport adapters may serialize their transport-specific envelope however
the provider expects, but they should not own the Bondstone durable payload
format. `Bondstone.Transport.Rebus` should keep Rebus infrastructure
configuration provider-native, including Rebus' own serializer, transport,
workers, retry policy, dead-letter policy, and subscription storage. Rebus
CLR type headers must not become Bondstone durable identity.

The durable payload should not embed Bondstone identity in JSON type metadata.
Stable identity remains in envelope fields and Bondstone headers:
`MessageKind`, `MessageTypeName`, message id, source module, target module for
commands, and future subscriber metadata for events. Payload JSON should be
the consumer message body, not a second envelope.

Stored payload compatibility is a consumer-visible durable contract.
Changing JSON options, converters, naming policies, enum representation, date
formats, or payload CLR shape can make already-stored outbox or inbox payloads
unreadable. Bondstone should document that consumers own compatibility for
their durable message schemas. Incompatible payload schema changes should use
a new stable message identity version, such as a new `.v2` identity, or a
consumer-provided serializer/converter strategy that can read both shapes.

The first serializer implementation should not introduce content-type
negotiation, non-JSON payload formats, schema registries, payload encryption,
compression, or automatic stored-payload migrations. Those remain future
decisions if concrete provider, transport, or adoption scenarios justify
them. The serializer abstraction should leave room for those decisions without
requiring them in Phase 1.

## Consequences

Consumers get one durable JSON configuration surface instead of separate
command sender, event publisher, and Rebus receive options.

Command and event payload behavior stays symmetrical before event receive and
fan-out are implemented.

Core owns the durable payload contract, while Rebus and future transport
adapters continue to own their provider-native infrastructure serializers and
runtime configuration.

Receive pipelines must resolve Bondstone stable identity before
deserializing. This keeps transport CLR type headers out of durable identity
and avoids making CLR refactors part of the message contract.

JSON options become compatibility-sensitive. Tests should cover at least one
custom converter or option flowing through command send, command receive, and
event publish. Future event receive should use the same tests and
configuration surface when implemented.

Non-JSON payloads and content-type metadata remain deferred. If those become
necessary, a later ADR should define how content type is stored, how older
payloads are interpreted, and how providers and transports expose the
additional metadata.

## Related Decisions

- [0018 Rebus Outbox Transport Adapter](0018-rebus-outbox-transport-adapter.md)
- [0024 Rebus Typed Command Receive Pipeline](0024-rebus-typed-command-receive-pipeline.md)
- [0025 Module Command Execution Boundary](0025-module-command-execution-boundary.md)
- [0026 Event Shape Guardrail](0026-event-shape-guardrail.md)

## Application Notes

- Current contract: Accepted. Current command send, event publish, module
  command receive, module event receive, and direct transport receive paths use
  the core `IDurablePayloadSerializer` boundary. The default implementation
  uses System.Text.Json with durable-payload-specific
  `DurablePayloadJsonOptions`.
- Stable docs: The need for a shared durable payload serialization boundary is
  described in [docs/architecture/messaging.md](../architecture/messaging.md),
  [docs/architecture/transport-rabbitmq.md](../architecture/transport-rabbitmq.md),
  and [docs/architecture/transport-servicebus.md](../architecture/transport-servicebus.md).
  Sequencing is tracked in [docs/archive/mvp-plan.md](../archive/mvp-plan.md).
- Agent guidance: Root [AGENTS.md](../../AGENTS.md) requires ADR review before
  public API, compatibility, serializer behavior, durable behavior, provider,
  or transport boundary changes.
- Application evidence: Core serializer contracts, default System.Text.Json
  serializer, durable JSON configuration extension, command sender, event
  publisher, provider-neutral command/event receive pipelines, direct
  transport receive dispatchers, and focused custom-converter tests are
  applied.
- Pending or deferred: None for the durable payload serializer boundary.
  Content type, non-JSON payloads, schema registries, payload encryption,
  compression, and stored-payload migration remain separate future decisions.

## Verification

Read back the accepted ADR and related stable docs. Ran focused command sender,
event publisher, command receive, and module receive
tests. Ran `pnpm check`, `git diff --check`, and stale-reference scans for ADR
0029, durable payload serialization, phase terminology, and transport CLR type
header language.
