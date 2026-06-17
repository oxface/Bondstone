# 0012 Direct Receive Inbox And Durable Receive Buffer

Status: Accepted
Application: Partially Applied
Date: 2026-06-16

## Context

Bondstone's current receive path is a classic idempotent broker receive model:
the native broker delivers a durable envelope, Bondstone executes module
receive inside the module persistence transaction, handler state, inbox
processed marker, operation state, and outgoing outbox rows commit together,
and the transport adapter acknowledges or completes the native message after
Bondstone receive succeeds.

This model cannot atomically commit the application database and native broker
settlement without distributed transactions. It relies on inbox idempotency:
if the database commit succeeds but broker settlement fails, redelivery finds
the processed inbox row and skips the handler.

The current inbox is not a true receive retry model. It records received and
processed state for a message-handler or message-subscriber identity. When a
row exists with no processed timestamp, Bondstone treats it as operationally
ambiguous and fails loudly because it cannot prove whether handler side
effects happened before the process failed.

An alternative model was deferred during MVP: split receive into durable
stages, such as transport-to-inbox ingestion and inbox-to-handler processing.
That model may be more operationally complete, but it makes Bondstone own a
durable receive queue with claims, leases, retry attempts, terminal receive
failure state, retention, and operator guidance.

## Decision

Bondstone should keep the current direct receive inbox as the default receive
idempotency boundary.

The current inbox should not be removed. It is central to durable module
boundaries, duplicate protection, redelivery safety after broker settlement
failure, and service-extraction continuity.

Bondstone should not bolt leasing onto the current direct receive path as a
small fix. Inbox leasing is meaningful only with a broader receive retry model
that defines claim ownership, attempt count, lease expiry, stale recovery,
terminal receive failure, retention, and operator behavior.

The accepted future design direction to investigate is an optional durable
receive buffer:

1. native transport delivery is parsed into a durable envelope;
2. Bondstone persists a receive-buffer or inbox-delivery record;
3. the native broker message can be acknowledged after that durable ingestion
   succeeds;
4. a Bondstone inbox processing worker claims buffered deliveries and executes
   module handlers through the existing module receive pipeline;
5. handler state, outgoing outbox rows, operation state, and processed receive
   state still commit in the target module persistence boundary.

The durable receive buffer should be optional and should not replace the
current direct receive path unless a later accepted ADR changes the default.

## Consequences

The default receive model remains simple and aligned with thin transport
adapters. It preserves the current redelivery/idempotency safety story without
making Bondstone a full receive queue runtime.

The stale unprocessed inbox row remains an operationally loud condition in the
direct receive model. Bondstone should improve inspection, observability, and
runbook guidance before adding mutation or retry APIs.

The durable receive buffer becomes a high-priority design item for service
extraction and higher operational confidence, but it is not a small patch. It
requires persistence shape, worker options, tests, failure semantics,
retention policy, and documentation.

## Related Decisions

- Narrows [0004 Persistence Operation State And Results](0004-persistence-operation-state-and-results.md).
- Relates to [0005 Transport Adapters And Receive Helpers](0005-transport-adapters-and-receive-helpers.md).
- Relates to [0008 Thin Broker Adapters](0008-thin-broker-adapters.md).

## Application Notes

- Current contract: direct receive uses inbox idempotency and treats
  already-received/unprocessed rows as ambiguous receive failures.
- Stable docs: current behavior is documented in messaging and persistence
  architecture docs and [operations.md](../operations.md). Stable docs state
  that the receive buffer is not current behavior and leave design intent in
  this ADR and after-v2 planning.
- Agent guidance: no new agent rule is required until a receive-buffer ADR is
  accepted.
- Application evidence: `DurableInboxHandlerExecutor` runs the handler only
  when registration is new and marks processed after the handler completes;
  EF module transaction behavior commits receive state and handler effects
  together; RabbitMQ and Service Bus adapters settle after receive succeeds;
  operations guidance documents why already-received/unprocessed rows remain
  ambiguous in the direct receive model.
- Pending or deferred: durable receive buffer persistence records, leases,
  retry policy, terminal receive failure state, worker options, retention, and
  tests.

## Verification

Accepted during v2 planning. Reviewed current messaging architecture docs and
receive, inbox, EF transaction, RabbitMQ receive worker, and Service Bus
receive worker code paths while producing this decision. Application remains
partial because the direct receive inbox exists today, while the optional
durable receive buffer is still design work. On 2026-06-16, added production
operations guidance for the current direct receive semantics and stale inbox
inspection model. On 2026-06-17, removed roadmap-style receive-buffer wording
from durable docs while keeping the non-current behavior warning.
