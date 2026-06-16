# 0047 Explicit Deploy Provisioning Helpers

Status: Archived
Application: Not Applicable
Date: 2026-06-10

## Context

Current Bondstone docs keep production schema migration and broker topology
ownership app/provider-native. Bondstone validates configured durable topology
but does not create PostgreSQL schemas, RabbitMQ exchanges/queues/bindings, or
Service Bus queues/topics/subscriptions/rules as a default host behavior.

Some users may still want explicit helper APIs for local development, tests,
samples, or tightly controlled deployment environments. Existing proof helpers
such as PostgreSQL table creation are explicit and not silently run during
normal provider registration.

## Decision

Decide whether Bondstone should support explicit opt-in deploy/provisioning
helpers for persistence and transport infrastructure.

The candidate direction is:

- Keep default provider and transport registration validation-only.
- Allow explicit opt-in helpers for local/test/sample or bounded deployment
  use cases.
- Keep production migration and broker administration app-owned unless a later
  accepted ADR narrows that boundary.
- Avoid hidden provisioning from `AddBondstone` or transport registration.

## Consequences

Explicit helpers can improve developer ergonomics and sample setup without
making Bondstone own production infrastructure.

Provisioning APIs increase maintenance scope and can blur the app-owned
infrastructure boundary if not named and documented carefully.

Provider-specific helpers should remain provider-native and should not become
a generic broker administration abstraction.

## Related Decisions

- [0035 PostgreSQL Dapper Persistence Proof](0035-postgresql-dapper-persistence-proof.md)
- [0036 Direct Transport Adapters And Rebus Removal](0036-direct-transport-adapters-and-rebus-removal.md)
- [0038 Provider Retry Recovery And Settlement Boundaries](0038-provider-retry-recovery-and-settlement-boundaries.md)
- [0039 Startup Transport Topology Validation](0039-startup-transport-topology-validation.md)

## Application Notes

- Current contract: proposed only; no binding change yet.
- Stable docs: if accepted, update setup, persistence provider docs, transport
  provider docs, and samples guidance.
- Agent guidance: if accepted, update architecture direction only if helper
  ownership changes current provider/app boundaries.
- Application evidence: current code has explicit PostgreSQL schema proof
  helpers and no default broker/schema provisioning during host registration.
- Pending or deferred: decide helper scope, naming, and which provider-native
  resources are in or out.

## Verification

No executable verification yet; this is a proposed decision draft.
