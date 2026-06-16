# 0011 OTel Native Diagnostics And Misconfiguration Reporting

Status: Accepted
Application: Partially Applied
Date: 2026-06-16

## Context

Post-MVP review found that Bondstone has several useful diagnostic surfaces:
trace context on durable envelopes, receive activities, operation result
diagnostic context, deserialization diagnostics, terminal outbox inspection,
inbox inspection, worker logs, and startup/runtime misconfiguration errors.

Individually these surfaces are valuable. Collectively they are scattered and
do not yet tell consumers how to observe Bondstone as one runtime boundary.
Earlier ADRs intentionally removed provider-neutral transport topology
diagnostics because broker topology, retry, dead-letter policy, monitoring,
and settlement behavior are app-owned and provider-native.

Bondstone still needs a coherent diagnostics story because its fixed module
runtime does not expose a public middleware pipeline where applications can
easily insert logging or tracing at every durable boundary.

## Decision

Bondstone should consolidate diagnostics around OpenTelemetry-native signals
and targeted misconfiguration reporting.

The preferred diagnostics model is:

- use `ActivitySource`, tags, and standard trace propagation for durable send,
  publish, outbox dispatch, receive, inbox decision, handler execution, and
  operation finalization boundaries;
- use metrics for durable state transitions and operational counters such as
  outbox claimed, dispatched, retried, terminal failed, stale claim, receive
  handled, receive skipped, receive already received, operation finalized, and
  operation expired;
- use structured logs for exceptional runtime handoffs and worker failures;
- use clear startup and runtime exceptions for missing module persistence,
  missing mappings, missing dispatcher, duplicate registrations, missing
  routes, ambiguous routes, invalid receive binding, and invalid durable
  identity;
- keep transport-infrastructure diagnostics in the native broker or
  application code that owns that infrastructure.

Bondstone should not reintroduce a provider-neutral transport topology
diagnostics package, a topology validation matrix, or a broker diagnostic DSL.
RabbitMQ, Azure Service Bus, Rebus, and other transport runtimes should expose
their own infrastructure diagnostics through their native client libraries,
host logs, and application monitoring.

Diagnostics should be documented in one stable observability guide rather than
spread across package setup examples. Package docs may point to that guide and
list package-specific tags or log event ids when needed.

## Consequences

The diagnostics direction stays aligned with the "library, not framework"
rule. Consumers can plug Bondstone into their existing OpenTelemetry and host
logging setup without adopting a Bondstone-specific monitoring stack.

Diagnostics work should favor a small stable vocabulary over many bespoke
result or diagnostic types. Existing result diagnostics remain useful, but new
diagnostic surface should first ask whether an OTel activity tag, metric, log,
or exception message is enough.

Transport adapter packages can add logs around native settlement handoff, but
those logs must not imply that Bondstone owns broker retry or dead-letter
policy.

## Related Decisions

- Narrows [0005 Transport Adapters And Receive Helpers](0005-transport-adapters-and-receive-helpers.md).
- Relates to [0008 Thin Broker Adapters](0008-thin-broker-adapters.md).
- Relates to [0010 Route-Aware Multi-Transport Dispatch](0010-route-aware-multi-transport-dispatch.md).

## Application Notes

- Current contract: Bondstone diagnostics should stay OpenTelemetry-native,
  with transport-infrastructure diagnostics left to the application and native
  broker tooling.
- Stable docs: [observability.md](../observability.md) documents the current
  diagnostic surfaces and OTel-native direction; setup, architecture, and
  package discovery docs link to it. Testing-specific diagnostics guidance
  remains future work.
- Agent guidance: no new agent rule is required until the stable
  observability guide exists.
- Application evidence: `ModuleReceiveTelemetry` starts receive activities;
  operation state can carry diagnostic context; inspectors expose terminal
  outbox and unprocessed inbox rows.
- Pending or deferred: define the complete stable activity-name, tag, metric,
  log event-id, and misconfiguration-message vocabulary, then expand
  instrumentation beyond the current receive activity and worker logs.

## Verification

Accepted during v2 planning. Read repository ADR guidance and current
architecture, messaging, setup, packaging, and public API docs during the
review that produced this decision. On 2026-06-16, added the stable
observability guide and links from setup, architecture, and package discovery
docs. Application remains partial until the full OTel vocabulary, metrics, log
event ids, and misconfiguration-message conventions are applied.
