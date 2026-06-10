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

## Verification

- `pnpm backend:test:fast`
- `pnpm backend:test:integration`
