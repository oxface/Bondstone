# Plans

This file captures known pressure points and possible future work. It is not a
roadmap, priority queue, or current operating contract.

When a topic becomes immediate, extract one focused issue note in this folder.
When that issue is resolved, move durable decisions into ADRs, move current
behavior into stable docs, and return any remaining ideas here.

## Immediate

- Public API and composition cleanup: inventory public package surfaces,
  distinguish normal setup APIs from advanced composition APIs, and avoid
  broad hiding or renaming without ADR 0046 compatibility planning. See
  [01-public-api-and-composition-cleanup.md](01-public-api-and-composition-cleanup.md).

## Known Pressure Points

- Persistence recovery and maintenance: document operator-owned recovery for
  terminal outbox rows, already-received inbox rows, failure text, claim
  leases, and provider receive retry; add helpers only after a concrete
  recovery model is accepted.
- Transport and hosting ergonomics: clarify provider receive helper
  classification, worker isolation needs, explicit provisioning helpers, and
  diagnostics only when real custom receive loops or deployments need them.
- Real project readiness: prepare adoption guidance, package README quick
  paths, and one bounded service-split sample after the core module runtime
  and normal setup APIs settle.
- Domain Events follow-ups: keep EF Core domain event persistence as the first
  provider-owned implementation; keep non-EF PostgreSQL staging
  application-owned until a concrete use case justifies a public provider API;
  consider explicit domain-event-to-integration-event mapping only after
  module-local behavior is proven in real use.
- User pipeline scoping: normal application behavior remains global DI
  registration for now. Consider module-scoped, per-command, or per-subscriber
  behavior registration only after a concrete application concern needs a more
  explicit setup API.
- Package compatibility: consider an automated public API baseline before
  stronger compatibility promises or broad public-surface cleanup.
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
