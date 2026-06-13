# 0023 Rebus Receive-Side Inbox Integration

Status: Superseded
Application: Not Applicable
Date: 2026-06-05

Superseded by
[ADR 0036 Direct Transport Adapters And Rebus Removal](0036-direct-transport-adapters-and-rebus-removal.md).

## Context

Bondstone now has receive-side persistence primitives:
`IDurableInboxRegistrar` for idempotent registration,
`IDurableInboxHandlerExecutor` for delegate-based handle-once execution, and
`IEntityFrameworkCorePersistenceScope` as an EF-specific transaction
companion. `Bondstone.Transport.Rebus` currently supports only outgoing command
transport by sending a Bondstone-owned `RebusDurableMessageEnvelope`.

The next receive-side decision is how a Rebus handler should turn that wire
envelope back into Bondstone inbox protection without introducing a generic
message bus, Rebus handler discovery, EF Core dependencies in the Rebus
package, or hidden acknowledgement behavior.

Transport acknowledgement is part of the durable contract. In Rebus, a message
handler that completes successfully allows the transport message to be
acknowledged; a thrown exception leaves Rebus retry and dead-letter policy in
control. Bondstone must not acknowledge a receive before the inbox processed
marker and user state have committed.

## Decision

`Bondstone.Transport.Rebus` will provide a receive-side inbox integration for
Bondstone-owned `RebusDurableMessageEnvelope` command messages.

The first receive integration is command-only. Event publish/subscribe,
subscription ownership, event target-module derivation, and event handler
fan-out remain separate decisions.

The Rebus receive adapter maps a `RebusDurableMessageEnvelope` back to
Bondstone's durable envelope shape and derives the inbox key as:

- stable message id from the wire envelope;
- target module from the command envelope's target module;
- explicit caller-supplied handler identity.

Handler identity must be stable text supplied by the consumer or registration
API. Bondstone must not derive it from handler CLR names. The adapter validates
that command envelopes have a target module. Envelopes without a target module
remain unsupported until event receive semantics are accepted.

The adapter composes `IDurableInboxHandlerExecutor`. It supplies a
`DurableInboxRecord` with the derived key and a receive timestamp from
`TimeProvider`. It invokes a caller-supplied handler delegate and a
caller-supplied commit delegate. This keeps `Bondstone.Transport.Rebus`
independent from EF Core and lets consumers use
`IEntityFrameworkCorePersistenceScope.SaveChangesAsync`, their own transaction
boundary, or a future provider-specific helper.

The adapter uses .NET `ActivityContext` parsing for W3C `traceparent` and
`tracestate` values carried by the Bondstone wire envelope. Invalid
`traceparent` values are rejected and are not treated as legacy correlation
identifiers.

Rebus acknowledgement follows these rules:

- `Handled`: complete normally after the handler, processed marker, and commit
  succeed.
- `AlreadyProcessed`: complete normally so duplicates that were already
  handled are acknowledged.
- `AlreadyReceived`: throw a Bondstone Rebus receive exception so Rebus retry
  and dead-letter policy can surface the unresolved unprocessed inbox row
  instead of silently acknowledging it.
- handler, registration, processed-marker, or commit failures: let the
  exception propagate so Rebus retry and dead-letter policy remains in control.

The first integration does not discover handlers, deserialize payloads into
typed command objects, own module identity scopes, start EF Core transactions,
configure Rebus endpoints, define Rebus retry policy, or perform stale receive
recovery. Consumers can call the adapter from a normal Rebus handler while
retaining explicit control of Rebus configuration and domain handler code.

## Consequences

Bondstone gets a receive-side path that matches its outgoing Rebus wire
envelope and existing inbox primitives without turning Rebus into a hidden
Bondstone runtime.

The Rebus package remains transport-focused and does not take dependencies on
EF Core, PostgreSQL, hosting, or consumer domain assemblies.

At-least-once delivery remains explicit. Messages that fail before a successful
commit are retried by Rebus according to the consumer's Rebus policy.

`AlreadyReceived` is intentionally operationally loud. Without an inbox lease
or stale receive recovery model, Bondstone cannot safely run the handler
again. Throwing lets Rebus retry/dead-letter the unresolved message rather
than silently dropping it.

The first receive adapter is lower-level than a full module pipeline. Later
ADRs may add typed handler registration, payload deserialization, module
identity scopes, EF-specific receive helpers, receive retry state, stale
receive recovery, metrics, or event publish/subscribe behavior.

## Amendment 2026-06-09: Topology-Owned Module Endpoint Binding

The Rebus module command receive path now has an app-facing topology binding
above the lower-level receive primitives. Configuring module receive topology
through the Bondstone Rebus transport builder records the accepted local
modules, registers the module command receive pipeline and endpoint
dispatcher, and for the current single-endpoint app-facing shape registers the
matching Rebus `IHandleMessages<RebusDurableMessageEnvelope>` endpoint
handler.

Applications still configure Rebus-native infrastructure: broker transport,
input queue, serializer, worker count, retry, and dead-letter policy.
Bondstone owns the durable module dispatch binding from the configured Rebus
endpoint to `IModuleCommandExecutor`.

The low-level explicit endpoint-handler registration remains available for
tests and advanced composition, including future multiple-endpoint or
multiple-bus shapes that need more explicit ownership than the current fluent
single-endpoint path.

## Related Decisions

- [0014 Inbox Registration Contract](0014-inbox-registration-contract.md)
- [0015 Inbox Handle-Once Orchestration](0015-inbox-handle-once-orchestration.md)
- [0016 EF Core Persistence Scope](0016-ef-core-persistence-scope.md)
- [0018 Rebus Outbox Transport Adapter](0018-rebus-outbox-transport-adapter.md)
- [0021 Fluent Service Composition Guardrails](0021-fluent-service-composition-guardrails.md)

## Application Notes

- Current relevance: Superseded by
  [ADR 0036](0036-direct-transport-adapters-and-rebus-removal.md). This ADR is
  retained only as historical decision trail for the removed Rebus receive
  adapter.
- Stable docs: Current receive-side transport direction is described in
  [docs/architecture/messaging.md](../architecture/messaging.md),
  [docs/architecture/persistence-core.md](../architecture/persistence-core.md),
  and [ADR 0036](0036-direct-transport-adapters-and-rebus-removal.md),
  with current implementation state in [docs/archive/mvp-plan.md](../archive/mvp-plan.md) and
  historical extraction notes in
  [docs/archive/extraction-plan.md](../archive/extraction-plan.md).
- Agent guidance: Root [AGENTS.md](../../AGENTS.md) requires ADR review before
  public API, durable behavior, provider, or transport strategy changes.
- Application evidence: The former Rebus receive adapter was removed by ADR 0036. Provider-neutral receive pipelines and direct transport adapters now
  carry current receive behavior.
- Pending or deferred: Not applicable after superseding.

## Verification

Read back this ADR and affected stable docs. Ran `dotnet restore
Bondstone.slnx --disable-build-servers -p:NuGetAudit=false`, `dotnet build
Bondstone.slnx --configuration Release --no-restore --disable-build-servers`,
fast `Unit|Application` tests, the sample `Integration` smoke test,
`pnpm format:check`, and `git diff --check`.
