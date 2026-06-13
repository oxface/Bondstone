# Plans

This file captures known pressure points and possible future work. It is not a
roadmap, priority queue, or current operating contract.

When a topic becomes immediate, extract one focused issue note in this folder.
When that issue is resolved, move durable decisions into ADRs, move current
behavior into stable docs, and return any remaining ideas here.

## Immediate

- No immediate issue is extracted.

## Known Pressure Points

- Persistence recovery follow-ups: consider terminal outbox maintenance
  helpers, inbox stale receive recovery helpers, and structured
  diagnostics/reporting only after ADR review.
- Transport provisioning: consider broker provisioning helpers only after real
  deployment needs prove the ownership boundary and ADR review accepts a
  concrete model.
- Domain Events follow-ups: keep EF Core domain event persistence as the first
  provider-owned implementation; keep non-EF PostgreSQL staging
  application-owned until a concrete use case justifies a public provider API;
  consider explicit domain-event-to-integration-event mapping only after
  module-local behavior is proven in real use.
- User pipeline scoping: normal application behavior remains global DI
  registration for now. Consider module-scoped, per-command, or per-subscriber
  behavior registration only after a concrete application concern needs a more
  explicit setup API.
- Package compatibility: plan cleanup of the current public API cleanup
  candidates recorded in [../public-api.md](../public-api.md) through ADR
  0046 compatibility review and release-note treatment before hiding or
  removal. Add an automated public API baseline before stronger compatibility
  promises or broad public-surface cleanup. Keep package README quick paths
  focused on normal setup APIs and link to the inventory when advanced
  composition classification matters.
- Provider storage: consider migration helpers, PostgreSQL payload storage
  choices, or multi-data-source support only when real project needs make
  them concrete.
- External event handoff: consider unwrapped payloads, CloudEvents,
  schema-specific envelopes, or non-JSON payload negotiation only after the
  core EF plus Service Bus/RabbitMQ path is stable.

## Guardrails

- Keep `Bondstone.Persistence.Postgres` as a guardrail for provider-neutral
  durable messaging, transactions, inbox/outbox, and operation state. Do not
  force optional capability parity with EF Core unless a concrete non-EF use
  case needs it.
- Do not treat this file as implementation guidance. Stable docs describe
  current behavior, and ADRs preserve durable decisions.
