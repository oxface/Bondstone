# 0017 Single Durable Inbox Incoming Ledger

Status: Amended
Application: Partially Applied
Date: 2026-06-17

## Context

ADR 0012 accepted direct receive as the default and later amended the future
receive-side design toward a separate durable receive-buffer ledger in front of
the existing inbox idempotency table. That split preserved the published
direct-receive inbox semantics, but it created two inbound ledgers:

- a receive buffer for durable ingestion, claim, retry, terminal failure, and
  operator inspection;
- an inbox row for handler idempotency and processed state inside the target
  module transaction.

Bondstone has only one current real consumer. Compatibility with the young
receive-buffer abstraction is therefore less important than choosing the
cleanest v2 durable receive model before the first consumer project depends on
it.

The separate-buffer design is also conceptually close to a durable inbox or
incoming-envelope store. Wolverine uses a durable incoming-envelope/inbox model
for persisted incoming messages and worker-driven processing. MassTransit and
Brighter keep lighter inbox state closer to duplicate detection while relying
more heavily on broker redelivery for inbound queueing. Bondstone's service
extraction goals need the stronger Wolverine-style durable incoming ledger more
than a second lightweight idempotency marker.

## Decision

Bondstone should pivot from a separate durable receive buffer plus inbox table
to a single richer durable inbox incoming ledger for buffered receive.

The durable inbox incoming ledger owns the inbound durable delivery lifecycle:

1. native transport delivery is parsed into `DurableMessageEnvelope`;
2. Bondstone records an incoming durable inbox row idempotently;
3. the native broker message can be acknowledged only after durable ingestion
   succeeds;
4. a Bondstone-owned durable inbox worker claims due rows;
5. the worker executes the module command or event receive pipeline;
6. processed, retry, stale, and terminal receive-failure state is recorded on
   the same incoming ledger according to claim and lease rules.

The durable inbox identity is the stable receive binding:

- command rows use message id, target module, and stable command handler
  identity;
- event rows use message id, subscriber module, and stable subscriber identity.

The durable inbox row stores the durable envelope fields structurally:
message id, message kind, message type name, source module, optional target
module, durable operation id, trace context, causation id, partition key,
payload, metadata, and created-at timestamp. It also stores receiver module,
handler or subscriber identity, optional source transport diagnostic name,
ingested timestamp, status, attempt count, optional next-attempt timestamp,
optional processed timestamp, optional failed timestamp, optional failure
reason, optional claim owner, and optional claim lease expiry.

The previous tiny inbox model remains valid for current direct receive, but it
is not the target table for buffered receive. Buffered receive should not write
both a receive-buffer row and a separate inbox idempotency row forever. Before
v2 hardening completes, the provisional `ReceiveBuffer` API and EF mapping
work should be renamed or remodeled into durable inbox/incoming-ledger
concepts, or removed if the slice is replaced wholesale.

Transport adapters remain thin. A transport ingestion listener is still
adapter-owned or application-owned over provider-native delivery and settlement
and calls the Bondstone durable inbox ingestion boundary. Broker topology,
dead-letter policy, native retry, delivery counts, and cleanup policy remain
adapter, broker, or application concerns unless a later ADR accepts broader
ownership.

Operation state remains honest. A terminal receive failure is durable
operational evidence, not automatic operation `Failed` state. Applications may
inspect terminal durable inbox rows and explicitly finalize operations through
application policy.

Direct receive remains available as the simple/default receive path until a
later accepted ADR changes the default. The durable inbox incoming ledger is
the target for service extraction and stronger receive recovery.

## Amendment 2026-06-18 Module-Aware Ingestion Boundary

Durable incoming inbox ingestion is receiver-module aware when module-owned
persistence is configured. The ingestion boundary is resolved from the durable
incoming inbox receiver module, which is the command target module for
commands and the subscriber module for events. Transport adapters still remain
provider-neutral: they resolve the durable receive identity and call the
provider/runtime ingestion boundary, but they do not know EF Core,
PostgreSQL, or DbContext types.

Provider packages may register one durable incoming inbox ingestion boundary
per module. That boundary groups the ingestion store and commit scope so the
row write and commit use the same module persistence boundary. When no
module-owned durable runtime registrations or module ingestion boundaries are
registered, Bondstone may fall back to root-level ingestion store and scope
services for advanced single-store composition. Once module persistence
registrations exist, missing receiver-module ingestion boundaries should fail
loudly rather than silently writing the incoming ledger to an unrelated root
store.

## Consequences

The receive-side product model becomes easier to explain: one incoming durable
ledger owns ingestion, claim, retry, terminal failure, and processed state.
Bondstone avoids making users understand both a receive-buffer table and an
inbox idempotency table for the same buffered message.

The persistence model becomes more substantial than the original tiny inbox.
That cost is intentional: storing the envelope and worker state in the durable
inbox is what allows broker acknowledgement after durable ingestion and
recovery from process failure before handler execution.

The implementation must be careful about transaction ownership. `Processed`
must mean the module receive transaction committed. If the durable inbox row is
in the target module persistence boundary, the processed marker should commit
with handler state, successful command operation completion, and outgoing
outbox rows where possible. If implementation must record outcome after the
module transaction, retry must be safe and must not re-run a committed handler.

The existing receive-buffer records, contracts, EF entity, table name, setup
helper, and docs are provisional and should not be treated as the accepted v2
public surface. They should be renamed or remodeled before the first consumer
project adopts buffered receive.

## Related Decisions

- Supersedes [0012 Direct Receive Inbox And Durable Receive Buffer](0012-direct-receive-inbox-and-durable-receive-buffer.md).
- Narrows [0013 Worker Boundaries And Transport Adapter Ownership](0013-worker-boundaries-and-transport-adapter-ownership.md) by renaming the future Bondstone-owned receive worker around the durable inbox incoming ledger.
- Relates to [0004 Persistence Operation State And Results](0004-persistence-operation-state-and-results.md).
- Relates to [0005 Transport Adapters And Receive Helpers](0005-transport-adapters-and-receive-helpers.md).
- Relates to [0008 Thin Broker Adapters](0008-thin-broker-adapters.md).

## Application Notes

- Current contract: direct receive still uses the existing inbox idempotency
  boundary and treats already-received/unprocessed rows as ambiguous receive
  failures. Durable incoming inbox ingestion resolves a receiver-module
  ingestion boundary when module-owned persistence is configured, with
  root-level fallback only for advanced single-store composition.
- Stable docs: messaging, hosting, persistence-core, persistence-ef-core,
  observability, public-api, packaging, package-discovery, operations, and the
  post-MVP plan describe the durable inbox direction and mark old
  receive-buffer naming as provisional/superseded.
- Agent guidance: root and architecture AGENTS files already route durable
  runtime, persistence, hosting, transport, and public API changes through ADR
  review. No new agent rule is needed.
- Application evidence: provider-neutral durable incoming inbox records,
  ingestion boundary resolution, EF Core ingestion and inspection stores,
  PostgreSQL claim/lease/outcome stores, the hosted incoming inbox processing
  worker, and RabbitMQ durable ingestion handoff are implemented. The EF
  module opt-in registers a module incoming ingestion boundary that uses the
  receiver module's DbContext. RabbitMQ ingestion resolves that boundary after
  building the incoming record and acknowledges only after commit.
- Pending or deferred: define migration, cleanup, retention, and broader
  adapter guidance as real consumer needs require.

## Verification

Reviewed ADR 0012, worker-boundary docs, messaging docs, persistence docs,
operations guidance, public API notes, and post-MVP planning. Compared the
design direction against current Wolverine, MassTransit, and Brighter inbound
durability documentation during design discussion. Updated stable docs to make
the durable inbox incoming ledger the current direction while preserving direct
receive as current behavior.

On 2026-06-18, the module-aware ingestion boundary amendment was applied to
code, stable docs, and public API baselines. Verified with focused RabbitMQ,
EF Core, and core persistence tests, public API baseline refresh, and
`pnpm check`.
