# 0004 Persistence Operation State And Results

Status: Accepted
Application: Applied
Date: 2026-06-16

## Context

Bondstone's clearest value after MVP is durable persistence around module
boundaries: transactional outbox, receive-side inbox idempotency, module-owned
EF persistence, and operation-state result observation.

Result-returning commands are product-critical for real modular monolith
flows. HTTP endpoints and app-owned orchestrators often need a local module
command result, while durable cross-module commands need accepted delivery and
later committed result observation.

Operation state must remain honest. It should not pretend to be a transport
retry ledger, broker dead-letter monitor, or full workflow engine.

## Decision

EF Core with PostgreSQL durability semantics is the supported durable
persistence path.

Provider-specific persistence owns concurrency where it matters:

- outbox claiming;
- claim ownership and leases;
- lease renewal;
- retry and terminal dispatch-failure recording;
- inbox unique-key idempotency;
- operation state storage;
- transaction boundaries that commit handler state, inbox markers, outbox
  rows, operation outcomes, and optional domain event records together.

Outbox rows are the source-side dispatch ledger. Inbox rows are the
receive-side idempotency ledger. Operation state is the caller-visible
workflow/result read model.

Operation ownership is:

- source module persistence writes `Pending` as the durable send acceptance
  receipt when a caller supplies an operation id;
- target module persistence writes `Completed`, `Failed`, or `Cancelled` as
  the committed command outcome;
- completed durable result commands may store a serialized result payload in
  target module operation state.

`IDurableOperationFinalizer` is the application-facing API for explicit
terminal non-success outcomes. `IDurableOperationExpirationProcessor` provides
an app-owned expiry pass over stale `Pending` or `Running` candidates, but
Bondstone does not schedule an expiry worker automatically.

`DurableOperationHandle` is the preferred result observation handle for
durable commands that carry an operation id. It contains the durable operation
id, source module, and target module. Handle-based reads query the target
module store. Operation-id-only reads remain as the global aggregate fallback.

Polling through `IDurableOperationResultReader` is the initial supported
observation model. It is acceptable for current production-scale use when
callers apply explicit timeouts and operation finalization policy.

## Consequences

Result-returning commands remain first-class without making durable sends look
like synchronous request/response calls.

Operation state stays understandable and scalable enough for current needs.
The handle path avoids scanning every module store for common result
observation.

Bondstone intentionally does not infer operation failure from outbox terminal
dispatch failure, broker dead-letter behavior, transient handler exceptions,
or already-received unprocessed inbox rows. Those ledgers mean different
things and require explicit app/operator policy before writing caller-visible
failure.

## Related Decisions

- Supersedes the active operation-state and result-command direction from the
  pre-restart ADR sequence summarized by
  [0001](0001-restart-adr-history-around-current-baseline.md) and pruned by
  [0009](0009-prune-pre-restart-archive-and-planning-notes.md).

## Application Notes

- Current contract: operation state and result observation are documented in
  [docs/architecture/messaging.md](../architecture/messaging.md),
  [docs/architecture/persistence-core.md](../architecture/persistence-core.md),
  and [docs/setup.md](../setup.md).
- Stable docs: EF/PostgreSQL persistence is documented in
  [docs/architecture/persistence.md](../architecture/persistence.md),
  [docs/architecture/persistence-ef-core.md](../architecture/persistence-ef-core.md),
  and [docs/architecture/persistence-postgresql.md](../architecture/persistence-postgresql.md).
- Agent guidance: root [AGENTS.md](../../AGENTS.md) requires ADR review before
  durable behavior, provider behavior, public API, or migration-policy
  changes.
- Application evidence: durable operation handles, handle-based readers,
  operation finalization, expiration processing, EF/PostgreSQL stores, and
  sample handle usage are applied.
- Pending or deferred: hosted expiry workers, provider-specific bulk expiry
  mutation, provider-specific dead-letter handoff helpers, and operation
  locator tables are not part of the current baseline.

## Verification

Read current messaging, persistence, EF persistence, PostgreSQL persistence,
setup, and sample docs. Behavior is covered by operation reader/result reader
tests, EF/PostgreSQL tests, public API tests, and the local modular monolith
sample integration test.
