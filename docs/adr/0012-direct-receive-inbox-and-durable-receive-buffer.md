# 0012 Direct Receive Inbox And Durable Receive Buffer

Status: Superseded
Application: Not Applicable
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

## Amendment 2026-06-17: Durable Receive-Buffer Design Slice

The optional durable receive buffer should be a separate persisted delivery
ledger, not leasing bolted onto the current inbox table. Its identity should
match the durable receive identity that will later be handed to the module
receive pipeline:

- command buffer identity is message id, target module, and stable command
  handler identity;
- event buffer identity is message id, subscriber module, and stable
  subscriber identity.

That identity intentionally mirrors `DurableInboxMessageKey`, but it belongs
to the receive-buffer record rather than replacing the inbox key. The inbox
table remains the handler idempotency ledger inside the target module
transaction. The buffer table records durable delivery processing state before
and around the module receive attempt.

The receive-buffer record should store the durable envelope fields
structurally, following the outbox mapping style rather than an opaque
serialized envelope blob:

- message id, message kind, message type name, source module, optional target
  module, durable operation id, trace context, causation id, partition key,
  payload, metadata, and created-at timestamp from `DurableMessageEnvelope`;
- receive identity fields: receiver module and handler or subscriber identity;
- optional source transport diagnostic name, for operator inspection only;
- ingested/received timestamp for durable ingestion;
- status, attempt count, optional next-attempt timestamp, optional processed
  timestamp, optional failed timestamp, optional failure reason, optional
  claimed-by value, and optional claim lease expiry;
- optional denormalized inspection fields for operation id, message kind,
  message type, and module names when those fields are not already directly
  queryable.

The buffer must not store broker settlement state, provider-native delivery
counts, dead-letter state, or topology metadata as Bondstone-owned contract
fields. Those remain adapter, broker, or application concerns.

Ingestion is the native-delivery to durable-record boundary. A transport
adapter or app-owned native listener should deserialize the native body into
`DurableMessageEnvelope`, validate the message kind and stable message type
registration, validate the command route or event subscriber binding, derive
the receive-buffer identity, and insert the buffer record idempotently. Native
broker settlement happens only after durable ingestion succeeds. Duplicate
native delivery of the same message and binding should return the existing
buffer record where the provider can do that safely. Ingestion should not run
the handler, write inbox records, write operation completion, stage outgoing
outbox rows, or infer terminal operation state. Payload-to-CLR deserialization
for handler execution may remain in the processing boundary so payload
compatibility fixes can be retried through the same durable buffer policy.

Processing is the durable-record to module-receive boundary. A Bondstone-owned
receive-buffer worker may claim due buffered records, call the existing module
command or event receive pipeline, and record one of these outcomes:

- processed when the receive pipeline handles the message or reports
  already processed;
- retry scheduled when the processing attempt fails and the receive-buffer
  failure policy allows another attempt;
- terminal receive failure when the processing attempt exhausts policy or is
  classified as non-retryable.

The receive-buffer status update is provider-owned buffer state and should be
recorded through claim-owner and lease-aware updates, like outbox dispatch
recording. The target module transaction remains the commit owner for handler
state, inbox markers, successful command operation completion, and outgoing
outbox rows. A successful target module transaction followed by a stale or
failed buffer outcome update should be safe to retry because the next claim
will find the processed inbox row and can mark the buffer processed without
re-running the handler.

Failure semantics split by boundary:

- ingestion failure means Bondstone could not durably record the native
  delivery for the selected binding. The adapter must use the native failure
  handoff and must not acknowledge the broker message as ingested;
- processing failure means a claimed buffer record did not reach a processed
  receive outcome in this attempt. It is recorded for retry or terminal
  receive failure according to receive-buffer policy;
- `DurableInboxAlreadyReceivedException` remains an ambiguous target-module
  receive outcome. In buffered receive it should be treated as a processing
  failure and retried or terminally failed by buffer policy, not acknowledged
  as processed;
- operation state remains honest. Buffer terminal failure does not
  automatically write operation `Failed`; applications may inspect the buffer
  evidence and explicitly finalize operations through application policy.

Provider-neutral receive-buffer contracts belong in `Bondstone.Persistence`.
Module-aware ingestion and processing orchestration can live in `Bondstone`
or `Bondstone.Hosting`, depending on whether the type is a runtime service or
a hosted worker. EF Core owns the provider-neutral mapping shape.
PostgreSQL owns SQL concurrency for idempotent ingestion, due-record
claiming, lease-aware renewal, retry scheduling, and terminal failure
recording. RabbitMQ and Azure Service Bus packages may add opt-in adapter
handoff helpers or worker modes that call the ingestion boundary, but they
must not grow into provider-neutral transport packages or broker topology
managers.

Worker ownership follows ADR 0013:

- the outbox worker remains source outbox to transport;
- the transport ingestion listener remains adapter-owned or app-owned over
  native delivery, calling Bondstone ingestion and settling only after durable
  ingestion succeeds;
- the receive-buffer processing worker is Bondstone-owned because it moves
  Bondstone durable records through claim, retry, processed, and terminal
  receive failure state;
- cleanup and retention remain application-owned until a later ADR accepts a
  safe mutation model.

Observability should add OpenTelemetry-native activities and metrics for
Bondstone-owned buffer transitions: ingestion accepted or duplicated, claim,
processed, retry scheduled, terminal receive failed, and stale claim/outcome
updates. Metric attributes should stay low-cardinality, such as module,
message kind, source module, target module when present, status, and optional
transport diagnostic name only when cardinality is controlled by application
configuration. Broker retry, dead-letter, topology, delivery count, and queue
health remain provider-native or application-owned telemetry.

The receive buffer remains opt-in. Direct receive remains the default unless
a later accepted ADR changes that default. Consumers own EF migrations for
new receive-buffer tables and any future table-shape changes. Public API
additions for buffer contracts, worker options, inspectors, and adapter hooks
are compatibility-sensitive and need normal public API review before
implementation.

## Related Decisions

- Narrows [0004 Persistence Operation State And Results](0004-persistence-operation-state-and-results.md).
- Relates to [0005 Transport Adapters And Receive Helpers](0005-transport-adapters-and-receive-helpers.md).
- Relates to [0008 Thin Broker Adapters](0008-thin-broker-adapters.md).
- Superseded by [0017 Single Durable Inbox Incoming Ledger](0017-single-durable-inbox-incoming-ledger.md).

## Application Notes

- Current contract: direct receive uses inbox idempotency and treats
  already-received/unprocessed rows as ambiguous receive failures. The optional
  durable receive-buffer design is accepted as an amended future direction but
  is not runtime behavior yet. ADR 0017 supersedes this separate
  receive-buffer direction with a single durable inbox incoming ledger.
- Stable docs: current behavior and the accepted durable inbox pivot are now
  documented by ADR 0017 and the architecture/operations docs. This ADR
  preserves only the superseded receive-buffer decision trail.
- Agent guidance: root and architecture AGENTS files already route durable
  runtime, persistence, hosting, transport, and public API changes through ADR
  review.
- Application evidence: `DurableInboxHandlerExecutor` runs the handler only
  when registration is new and marks processed after the handler completes;
  EF module transaction behavior commits receive state and handler effects
  together; RabbitMQ and Service Bus adapters settle after receive succeeds;
  operations guidance documents why already-received/unprocessed rows remain
  ambiguous in the direct receive model.
- Pending or deferred: durable receive buffer runtime contracts, EF mappings,
  PostgreSQL claim/lease/recording behavior, worker options, inspection
  contracts, adapter opt-in hooks, retention guidance, migrations, public API
  review, and tests.

## Verification

Accepted during v2 planning. Reviewed current messaging architecture docs and
receive, inbox, EF transaction, RabbitMQ receive worker, and Service Bus
receive worker code paths while producing this decision. Application remains
partial because the direct receive inbox exists today, while the optional
durable receive buffer is still design work. On 2026-06-16, added production
operations guidance for the current direct receive semantics and stale inbox
inspection model. On 2026-06-17, removed roadmap-style receive-buffer wording
from durable docs while keeping the non-current behavior warning. Later on
2026-06-17, amended this ADR with the durable receive-buffer identity,
persistence record, ingestion boundary, processing worker, failure semantics,
package ownership, observability, migration, and compatibility design slice.
