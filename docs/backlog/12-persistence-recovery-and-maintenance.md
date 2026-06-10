# Persistence Recovery And Maintenance

Priority: Medium

Goal: define and implement operational persistence follow-ups that were
deliberately deferred from the current durable loop.

## Scope

- Stale outbox claim recovery beyond lease-based reclaiming.
- Stale inbox receive recovery after `AlreadyReceived` rows.
- Cleanup, retention, archival, or terminal-failure maintenance workers.
- Failure-reason redaction or operator guidance for persisted failure text.
- Richer operation-state transitions such as running, failed, cancelled,
  timeout, polling, retry projection, and result payloads.
- Provider-specific payload storage choices such as PostgreSQL `jsonb`.
- Multi-data-source selection for `Bondstone.Persistence.Postgres`.

## Related ADRs

- [0031 Durable Operation State Integration](../adr/0031-durable-operation-state-integration.md)
- [0041 Outbox Terminal Failure Boundary](../adr/0041-outbox-terminal-failure-boundary.md)
- [0043 Inbox Stale Receive Recovery](../adr/0043-inbox-stale-receive-recovery.md)

## Candidate Work Items

- Create ADRs for any default recovery or maintenance behavior.
- Keep operator/app-owned recovery documented until a Bondstone-owned behavior
  is accepted.
- Add provider-backed integration tests for any new recovery behavior.

## Proposed Slices

1. Operator docs slice: document the current manual/operator-owned recovery
   expectations for terminal outbox rows, stale inbox rows, and persisted
   failure text.
2. Maintenance API slice: decide whether Bondstone should expose cleanup or
   retry helpers before adding hosted maintenance workers.
3. Operation-state slice: define richer state transitions only after deciding
   what real callers need to observe.
4. Provider storage slice: evaluate PostgreSQL-specific payload and
   multi-data-source improvements independently from recovery semantics.

## Implementation Backlog

### PRM-01: Operator Recovery Playbook

Priority: P0

Document the current operational playbook for terminal outbox rows,
already-received inbox rows, persisted failure text, claim leases, and
provider-owned receive retry. This is docs-first because current behavior has
been accepted but real operators still need instructions.

Important files:

- `docs/architecture/persistence-core.md`
- `docs/architecture/persistence-postgresql.md`
- `docs/architecture/persistence-postgres.md`
- `docs/architecture/transport-rabbitmq.md`
- `docs/architecture/transport-servicebus.md`

Verification:

- `pnpm format:check`

### PRM-02: Failure Reason Policy

Priority: P1.

Review failure reason storage across EF PostgreSQL and non-EF PostgreSQL.
Decide whether truncation, redaction guidance, or typed failure categories are
needed before production use.

Candidate files:

- `src/Bondstone/Persistence/Outbox/DurableOutboxDispatcher.cs`
- `src/Bondstone.EntityFrameworkCore.Postgres/Outbox/PostgreSqlDurableOutboxDispatchRecorder.cs`
- `src/Bondstone.Persistence.Postgres/Outbox/PostgresDurableOutboxDispatchRecorder.cs`
- `tests/Bondstone.EntityFrameworkCore.Postgres.Tests/Persistence`
- `tests/Bondstone.Persistence.Postgres.Tests`

Verification:

- `pnpm backend:test:fast`
- `pnpm backend:test:integration` if provider storage behavior changes.

### PRM-03: Inbox Stale Receive Recovery Design

Priority: P1.

Turn ADR 0043 into an implementation decision if real-project validation
shows operator-owned stale receive handling is too rough. Cover inbox lease
semantics, transaction boundaries, idempotency proof, and provider SQL.

Important files:

- `docs/adr/0043-inbox-stale-receive-recovery.md`
- `src/Bondstone/Persistence/Inbox`
- `src/Bondstone.EntityFrameworkCore.Postgres/Inbox`
- `src/Bondstone.Persistence.Postgres/Inbox`

Verification:

- `pnpm backend:test:fast`
- `pnpm backend:test:integration`

### PRM-04: Maintenance Helper Prototype

Priority: P2.

Prototype explicit app-invoked helpers for retrying terminal outbox rows,
marking stale inbox rows, or cleaning completed durable rows. Do not add
always-on hosted maintenance workers until the helper semantics are proven.

Candidate files:

- `src/Bondstone/Persistence`
- `src/Bondstone.EntityFrameworkCore.Postgres`
- `src/Bondstone.Persistence.Postgres`
- provider integration test projects

Verification:

- `pnpm backend:test:fast`
- `pnpm backend:test:integration`

### PRM-05: Operation State Expansion

Priority: P2.

Define richer operation-state semantics only after real callers need them.
Candidate states include running, failed, cancelled, timeout, retry-projected,
and result payload retention.

Important files:

- `docs/adr/0031-durable-operation-state-integration.md`
- `src/Bondstone/Messaging/Operations`
- `src/Bondstone.EntityFrameworkCore/Operations`
- `src/Bondstone.Persistence.Postgres/Operations`
- operation tests across core, EF, and PostgreSQL packages

Verification:

- `pnpm backend:test:fast`
- `pnpm backend:test:integration`

## Verification

- `pnpm backend:test:fast`
- `pnpm backend:test:integration`
