# 0014 Production Operations And Lifecycle Guidance

Status: Accepted
Application: Partially Applied
Date: 2026-06-16

## Context

Consumer feedback after public MVP has focused on documentation and bugs, and
architecture review identified operational clarity as the highest near-term
risk. Bondstone deliberately leaves broker infrastructure, retry/dead-letter
policy, topology, and many operator mutations app-owned. That is the right
library boundary, but consumers still need guidance for running the durable
boundary safely.

The current docs describe setup, architecture, testing, package discovery, and
individual durable concepts. They do not yet provide one production operations
guide that connects outbox terminal failures, stale inbox rows, operation
expiration, retention, EF migrations, message contract evolution, adapter
settlement behavior, and observability.

Bondstone targets `net10.0` by deliberate project choice, so framework-target
expansion is not part of this guidance.

## Decision

Bondstone should add stable production operations and lifecycle guidance
before broad feature growth.

The operations guidance should cover:

- terminal outbox inspection, alerting, retention, and application-owned
  replay/reset cautions;
- stale inbox inspection, alerting, and application-owned recovery cautions;
- operation result polling, finalization, expiration, and endpoint policy;
- EF Core migration ownership for Bondstone tables in app-owned DbContexts;
- upgrade notes for Bondstone table-shape changes;
- message contract evolution using stable message identity and explicit new
  identities for breaking payload changes;
- JSON serializer/converter guidance for durable payload compatibility;
- broker adapter settlement behavior and failure handoff;
- retention and archival guidance for outbox, inbox, operation state, and
  optional domain event records;
- observability entrypoints from the OTel-native diagnostics direction.

Bondstone should not run EF migrations for the application. EF-backed
Bondstone tables are mapped in the application's DbContext model, so the
application owns migration generation and application. Bondstone should
document expected migration behavior, table changes, and upgrade notes.

Bondstone should not provide provider-neutral mutation APIs for replay,
reset, purge, stale-inbox recovery, or broker movement unless a later ADR
accepts a bounded safety model. Documentation should explain why these
procedures are application/operator decisions.

## Consequences

This work makes the current library more usable without expanding the runtime
surface too early. It turns "app-owned" from a disclaimer into practical
guidance.

Operations docs may reveal places where small code additions are worthwhile,
such as missing metrics, clearer exception messages, or read-only inspection
filters. Those additions should stay subordinate to the operations model.

The guidance should be kept current with package READMEs and setup examples
because it is consumer-facing product documentation, not an internal planning
note.

## Related Decisions

- Relates to [0002 Library Scope And Package Surface](0002-library-scope-and-package-surface.md).
- Relates to [0004 Persistence Operation State And Results](0004-persistence-operation-state-and-results.md).
- Relates to [0005 Transport Adapters And Receive Helpers](0005-transport-adapters-and-receive-helpers.md).
- Relates to [0011 OTel Native Diagnostics And Misconfiguration Reporting](0011-otel-native-diagnostics-and-misconfiguration-reporting.md).
- Relates to [0012 Direct Receive Inbox And Durable Receive Buffer](0012-direct-receive-inbox-and-durable-receive-buffer.md).

## Application Notes

- Current contract: production operations guidance is centralized in
  [operations.md](../operations.md), with detailed architecture docs remaining
  the deeper runtime contract. V2 replacement and high-level migration
  guidance, including the release checklist, lives in
  [packaging.md](../packaging.md).
- Stable docs: docs README, setup, architecture, package discovery, packaging,
  messaging, hosting, and package README docs link to the operations or
  observability guidance.
- Agent guidance: no new agent rule is required; root and docs AGENTS files
  already route documentation and architecture changes through stable docs and
  ADR review.
- Application evidence: inspectors, finalizer, expiration processor, EF
  mappings, broker adapter receive workers, and public API baselines already
  exist. The operations guide documents direct receive behavior, stale inbox
  ambiguity, broker settlement after successful receive, terminal outbox
  inspection, operation polling/finalization/expiration, EF migration
  ownership, table-shape upgrades, contract evolution, serializer
  compatibility, retention, and Bondstone/app ownership boundaries.
  On 2026-06-17, package README links were added for setup, package discovery,
  operations, observability, and packaging/release guidance. On 2026-06-17,
  focused unit tests covered RabbitMQ receive failure logging and native
  nack/requeue handoff, RabbitMQ ack-after-receive ordering, Service Bus
  processor error logging, and Service Bus complete-after-receive ordering.
  Later on 2026-06-17, durable operations and release docs were cleaned up to
  describe current behavior and non-current surfaces without roadmap language.
- Pending or deferred: finalized observability vocabulary remains follow-up
  work.

## Verification

Accepted during v2 planning. Reviewed setup, architecture, public API,
testing, packaging, and package discovery docs while producing this decision.
On 2026-06-16, added the production operations guide, observability guide, and
related stable-doc links. Application was partially applied because package
README links, adapter failure-handoff follow-up, and finalized observability
vocabulary remained open. On 2026-06-17, package README links and v2
release/migration guidance were added; application remains partially applied
because adapter failure-handoff follow-up and finalized observability
vocabulary remain open. Later on 2026-06-17, focused RabbitMQ and Service Bus
unit tests closed the adapter failure-handoff follow-up for v2; application
remains partially applied only because finalized observability vocabulary
remains open. Later on 2026-06-17, removed roadmap-style wording from durable
operations and packaging docs while keeping the current non-current behavior
warnings.
