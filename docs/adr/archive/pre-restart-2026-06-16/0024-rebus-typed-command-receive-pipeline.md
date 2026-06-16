# 0024 Rebus Typed Command Receive Pipeline

Status: Archived
Application: Not Applicable
Date: 2026-06-05

Superseded by
[ADR 0036 Direct Transport Adapters And Rebus Removal](0036-direct-transport-adapters-and-rebus-removal.md).

## Context

ADR 0023 introduced the first Rebus receive-side inbox adapter. It handles a
Bondstone `RebusDurableMessageEnvelope`, derives an inbox key from message id,
target module, and explicit handler identity, composes the core
`IDurableInboxHandlerExecutor`, and leaves handler delegates and commit
delegates to the caller.

That low-level adapter proves the durable receive boundary, but application
handlers should not need to manually inspect wire envelopes, resolve message
types, deserialize JSON payloads, start telemetry activities, and assemble the
same inbox/commit delegates for every command. A typed receive pipeline needs
to sit above the low-level adapter without turning Bondstone into a generic
mediator or hiding Rebus host configuration.

This affects public API shape, transport behavior, handler identity,
deserialization, trace propagation, and commit-boundary expectations, so it
needs an ADR before implementation.

## Decision

`Bondstone.Transport.Rebus` will provide a typed command receive pipeline over
the low-level `IRebusDurableInboxHandlerExecutor`.

The first typed pipeline is command-only. It handles
`RebusDurableMessageEnvelope` messages whose `MessageKind` is `Command`.
Event publish/subscribe, event handler fan-out, and event subscription
ownership remain separate decisions.

The typed pipeline will use `IMessageTypeRegistry` to resolve the envelope's
stable `MessageTypeName` to a CLR type. The resolved type must be registered
as a durable command. Missing registrations, kind mismatches, unsupported
types, and payload deserialization failures are receive failures and should
flow as exceptions so Rebus retry and dead-letter policy remains in control.

Payload deserialization belongs to the Rebus transport package as an explicit
adapter concern, but it must not rely on Rebus' CLR type header for Bondstone
identity. The pipeline will deserialize the envelope payload into the resolved
command CLR type from the Bondstone message registry. The first
implementation should use `System.Text.Json` and accept injectable
`JsonSerializerOptions` or a small Bondstone-owned serializer abstraction only
if tests show options injection is insufficient.

Typed handler identity must be explicit stable text supplied at registration.
Bondstone must not derive durable inbox handler identity from handler CLR
names. Registration helpers may use a caller-provided handler identity string
with a typed command handler delegate.

The typed pipeline composes:

- Rebus wire envelope input;
- `IMessageTypeRegistry` lookup;
- payload deserialization to a registered command CLR type;
- .NET/OTel `Activity` creation with `ActivityKind.Consumer` using the
  accepted W3C parent context from the envelope when present;
- `IRebusDurableInboxHandlerExecutor` for inbox protection and Rebus
  acknowledgement behavior;
- caller-supplied typed handler delegate;
- caller-supplied commit delegate.

The pipeline owns Activity creation and tagging because it is the first layer
that knows the transport operation, typed message identity, handler identity,
and handler invocation boundary. The low-level inbox adapter continues to
validate and carry trace context but does not start Activities.

Activity tags should start small and stable: transport name, message id,
message type name, source module, target module, and handler identity. Broader
semantic-convention alignment can be refined later without changing the
durable handler contract.

The typed pipeline does not discover assemblies automatically, configure
Rebus endpoints, start EF Core transactions, define Rebus retry policy,
perform stale receive recovery, introduce receive retry state, infer module
identity scopes, publish events, or create a generic mediator for ordinary
in-process calls.

## Consequences

Application code can register typed command handlers without manually
deserializing Bondstone wire envelopes or duplicating inbox/telemetry
composition.

Stable message identity remains Bondstone-owned and registry-driven. Rebus'
CLR type header remains separate from Bondstone durable identity.

Handler identity stays durable and explicit, which avoids accidental inbox-key
changes from CLR renames or refactors.

Telemetry becomes owned by the receive pipeline rather than the low-level
adapter. This allows future OpenTelemetry conventions to be applied around the
actual typed handler invocation.

The pipeline remains deliberately smaller than a module runtime. Stale
receive recovery, receive retry state, module identity scopes, EF-specific
receive helpers, event publish/subscribe, transport-level integration tests,
and samples remain future work.

## Related Decisions

- [0004 Positioning And Service Extraction Path](0004-positioning-and-service-extraction-path.md)
- [0015 Inbox Handle-Once Orchestration](0015-inbox-handle-once-orchestration.md)
- [0016 EF Core Persistence Scope](0016-ef-core-persistence-scope.md)
- [0018 Rebus Outbox Transport Adapter](0018-rebus-outbox-transport-adapter.md)
- [0023 Rebus Receive-Side Inbox Integration](0023-rebus-receive-side-inbox-integration.md)

## Application Notes

- Current relevance: Superseded by
  [ADR 0036](0036-direct-transport-adapters-and-rebus-removal.md). This ADR is
  retained only as historical decision trail for the removed typed Rebus
  receive pipeline.
- Stable docs: Current receive-side transport direction is described in
  [docs/architecture/messaging.md](../architecture/messaging.md) and
  [ADR 0036](0036-direct-transport-adapters-and-rebus-removal.md),
  with current implementation state in [docs/archive/mvp-plan.md](../archive/mvp-plan.md) and
  historical extraction notes in
  [docs/archive/extraction-plan.md](../archive/extraction-plan.md).
- Agent guidance: Root [AGENTS.md](../../AGENTS.md) requires ADR review before
  public API, durable behavior, provider, or transport strategy changes.
- Application evidence: The former typed Rebus receive pipeline was removed by
  ADR 0036. Provider-neutral receive pipelines and direct transport adapters
  now carry current receive behavior.
- Pending or deferred: Not applicable after superseding.

## Verification

Read back this ADR and affected stable docs. Ran targeted Rebus tests after
implementation, targeted composition tests after adding application smoke
coverage, and `pnpm check` after applying the full slice.
