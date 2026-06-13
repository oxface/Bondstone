# 0043 Inbox Stale Receive Recovery

Status: Amended
Application: Applied
Date: 2026-06-10

## Context

Bondstone receive-side idempotency records an inbox row before handler
execution and marks it processed after successful handler execution. If a
message-handler pair is already processed, receive can skip handling. If a row
exists without a processed timestamp, current module receive pipelines treat
the state as operationally loud through `DurableInboxAlreadyReceivedException`.

This is safe because Bondstone has no accepted proof that re-running the
handler is idempotent after a partial or ambiguous receive attempt. However,
the current behavior can strand a message until an operator or application-owned
recovery path handles the stale row.

The low-level `DurableInboxHandlerExecutor` also accepts a commit delegate.
That keeps the primitive flexible for provider-specific transaction boundaries,
but module transaction behaviors often pass a no-op because the outer module
transaction owns the final commit.

## Decision

Bondstone keeps the current loud behavior for already-received but unprocessed
inbox rows. The module command and event receive pipelines must not silently
re-run handlers, skip provider settlement as though the message were handled,
or mark stale rows processed without a successful handler execution in the
current transaction boundary.

Recovery for stale receive rows is operator-owned or application-owned for
now. Applications may inspect their inbox tables, correlate broker/provider
state, and apply their own recovery procedure, but that procedure is outside
Bondstone's default receive semantics and owns its own safety proof and audit
trail. Bondstone should not turn stale inbox rows into a provider-neutral
broker retry or dead-letter abstraction.

Inbox leases, stale-row sweepers, app-owned recovery hooks, maintenance
workers, failed receive states, or row mutation helpers are not accepted in
this decision. Any later recovery feature needs a separate ADR that defines at
least the owner, timeout or lease model, transaction boundary, provider SQL
semantics, allowed mutations, and transport settlement interaction before
implementation.

The low-level `DurableInboxHandlerExecutor` keeps its explicit commit delegate.
It remains a low-level primitive for callers that compose registration, handler
execution, processed-marker staging, and a persistence commit directly. Module
transaction behaviors remain the preferred owner of normal module commit
boundaries; they may pass a no-op commit delegate because their outer
transaction behavior commits handler state, inbox markers, operation state,
and outgoing outbox rows together.

## Consequences

Keeping the current behavior is simple and safe but leaves stale receives as an
operator or application concern. Broker redelivery may continue according to
provider policy, but Bondstone receive pipelines will keep failing loudly until
the stale inbox row is resolved by an app-owned procedure or a later accepted
Bondstone recovery model.

Deferring recovery behavior avoids pretending that timeout-based re-execution
is safe without a lease, owner, transaction, and provider-SQL model. It also
keeps command handler identity and event subscriber identity separate because
recovery is keyed by the existing `DurableInboxMessageKey`.

Retaining the commit delegate avoids a compatibility-sensitive public API
change and keeps the low-level executor useful for root-level or provider-owned
composition. Documentation must make clear that module transaction behaviors,
not the no-op delegate passed by module receive inbox behaviors, own normal
module commits.

## Amendment 2026-06-10: Commit Ownership Cleanup

Follow-up implementation removed the low-level inbox executor commit delegate.
Normal module execution already proved that transaction ownership belongs
outside `DurableInboxHandlerExecutor`: EF Core and non-EF PostgreSQL module
transaction behaviors wrap command and event subscriber execution, then commit
handler state, inbox markers, operation state, and outgoing outbox rows in the
module persistence boundary.

`IDurableInboxHandlerExecutor` now composes inbox registration, the handler
delegate, and processed-marker staging only. A `Handled` result means the
handler ran and the processed marker was staged in the current persistence
context; it does not itself mean the surrounding transaction has durably
committed. Callers that use the low-level executor directly must execute it
inside their chosen transaction or save boundary and commit outside the
executor.

This amendment keeps the stale receive decision unchanged: already-received
but unprocessed rows remain loud, and Bondstone still does not provide default
stale-row recovery.

## Related Decisions

- [0014 Inbox Registration Contract](0014-inbox-registration-contract.md)
- [0015 Inbox Handle-Once Orchestration](0015-inbox-handle-once-orchestration.md)
- [0038 Provider Retry Recovery And Settlement Boundaries](0038-provider-retry-recovery-and-settlement-boundaries.md)

## Application Notes

- Current contract: already-received but unprocessed inbox rows are
  operationally loud in the module receive pipelines. Bondstone does not
  silently re-run handlers or provide default stale-row recovery. The inbox
  executor no longer accepts a commit delegate; transaction and save ownership
  is outside the executor.
- Stable docs: the current rule is reflected in
  [docs/architecture/messaging.md](../architecture/messaging.md),
  [docs/architecture/persistence.md](../architecture/persistence.md),
  [docs/architecture/persistence-core.md](../architecture/persistence-core.md),
  [docs/architecture/persistence-ef-core.md](../architecture/persistence-ef-core.md),
  [docs/architecture/persistence-postgresql.md](../architecture/persistence-postgresql.md),
  [docs/architecture/persistence-postgres.md](../architecture/persistence-postgres.md),
  and [docs/setup.md](../setup.md).
- Agent guidance: root [AGENTS.md](../../AGENTS.md) already requires ADR
  review before broad durable behavior, provider support, transport strategy,
  compatibility, or public API changes. No new agent instruction is needed for
  this narrowed decision.
- Application evidence: `DurableInboxHandlerExecutor` returns
  `AlreadyReceived` without invoking the handler; command and event module
  receive pipelines throw `DurableInboxAlreadyReceivedException` for that
  status; RabbitMQ and Service Bus workers settle only after receive dispatch
  succeeds and hand failures back to provider-native retry/dead-letter policy.
- Pending or deferred: any Bondstone-owned lease, recovery hook, maintenance
  worker, failed receive state, or inbox row mutation helper needs a later ADR
  and implementation.

## Verification

- Read back this ADR and affected stable docs.
- Existing unit and integration tests already cover the current receive flow:
  newly registered rows run handlers and stage processed markers,
  already-processed rows skip handlers, already-received rows skip handlers at
  the executor level, module receive pipelines throw for `AlreadyReceived`,
  PostgreSQL registrars classify already-received and already-processed rows,
  provider transaction behaviors own commit, and transport workers settle only
  after successful dispatch.
- Verified this docs and decision update with:
  - `git diff --check`
  - `pnpm format:check`
  - `pnpm backend:build`
  - `pnpm backend:test:fast`
  - `pnpm backend:test:integration`
  - `dotnet test tests/Bondstone.EntityFrameworkCore.Postgres.Tests/Bondstone.EntityFrameworkCore.Postgres.Tests.csproj --configuration Release --no-build --filter "Category=Integration"`
