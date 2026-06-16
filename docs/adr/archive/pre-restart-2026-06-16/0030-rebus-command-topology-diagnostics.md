# 0030 Rebus Command Topology Diagnostics

Status: Archived
Application: Not Applicable
Date: 2026-06-08

Superseded by
[ADR 0036 Direct Transport Adapters And Rebus Removal](0036-direct-transport-adapters-and-rebus-removal.md).

## Context

Phase 3 needs a usable durable command loop where applications can understand
why a command can or cannot be routed before the endpoint dispatcher and
listener binding work is complete.

`Bondstone.Transport.Rebus` already supports command destination resolution
from explicit target-module routes, local receive endpoint bindings, and a
module queue naming convention. The dispatch path used that precedence, but it
did not expose a diagnostic result that could describe which source resolved a
destination or why no destination exists. Without a diagnostic surface,
missing outbound topology errors remain late exceptions from dispatch and are
harder to report alongside receive topology validation.

ADR 0026 already established command/event topology diagnostic vocabulary,
including command destinations and route source information. This ADR narrows
that vocabulary into the first concrete Rebus command diagnostic surface.

## Decision

`Bondstone.Transport.Rebus` will expose a small command destination diagnostic
surface for the current Rebus command topology.

The diagnostic surface reports:

- target module;
- diagnostic kind, using `CommandDestination`;
- destination source: explicit route, receive endpoint, module queue
  convention, or missing;
- resolved Rebus destination address when one exists;
- receive endpoint name when the destination came from a receive binding;
- failure reason when no destination can be resolved.

Destination diagnostics and Rebus outbox dispatch must share the same
resolution logic and precedence:

1. explicit `RouteModule(...).ToQueue(...)` or `.ToAddress(...)`;
2. accepted local Rebus receive endpoint binding;
3. module queue convention;
4. missing destination.

The first diagnostic API is Rebus-specific because route source and address
semantics are transport-owned. Core keeps the shared diagnostic vocabulary but
does not introduce a generic topology service in this slice.

The diagnostic surface does not implement endpoint dispatch, listener binding,
event topology diagnostics, route health checks, circuit breaking, or
operation-state integration.

## Consequences

Applications and later Bondstone runtime pieces can ask Rebus how a command
target module would be addressed before dispatching an outbox record.

The dispatch path and diagnostics no longer risk drifting because both use the
same command destination topology object.

The first diagnostic result is intentionally narrow. Future event diagnostics
will need topic/subscription and subscriber-specific vocabulary rather than
reusing command destination source names.

## Related Decisions

- [0018 Rebus Outbox Transport Adapter](0018-rebus-outbox-transport-adapter.md)
- [0021 Fluent Service Composition Guardrails](0021-fluent-service-composition-guardrails.md)
- [0025 Module Command Execution Boundary](0025-module-command-execution-boundary.md)
- [0026 Event Shape Guardrail](0026-event-shape-guardrail.md)

## Application Notes

- Current relevance: Superseded by
  [ADR 0036](0036-direct-transport-adapters-and-rebus-removal.md). This ADR is
  retained only as historical decision trail for the removed Rebus diagnostic
  surface.
- Stable docs: Current command destination diagnostics are described in
  [docs/architecture/messaging.md](../architecture/messaging.md),
  [ADR 0036](0036-direct-transport-adapters-and-rebus-removal.md),
  and [docs/archive/mvp-plan.md](../archive/mvp-plan.md).
- Agent guidance: Root [AGENTS.md](../../AGENTS.md) requires ADR review before
  public API, package-boundary, provider, transport, or durable runtime
  changes.
- Application evidence: The former Rebus diagnostics were removed by ADR 0036.
  Current topology validation is handled by direct transport diagnostics and
  ADRs 0039 and 0040.
- Pending or deferred: Not applicable after superseding.

## Verification

Read back this ADR and affected stable docs. Ran focused Rebus tests,
`git diff --check`, and `pnpm check`.
