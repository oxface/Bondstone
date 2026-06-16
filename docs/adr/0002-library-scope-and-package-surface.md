# 0002 Library Scope And Package Surface

Status: Amended
Application: Applied
Date: 2026-06-16

## Context

After MVP, Bondstone's strongest product fit is not becoming a full message
bus or application framework. Wolverine, Brighter, MassTransit, Rebus, and
workflow engines have broader feature sets and years of ecosystem depth.

Bondstone's useful lane is narrower: durable module boundaries for modular
monoliths, with enough durable messaging and persistence structure that a
module can later move behind a broker or service boundary without rewriting
message contracts and handler patterns.

The package surface grew during MVP exploration. Post-MVP cleanup removed the
direct Dapper PostgreSQL package, Service Bus adapter, provider-neutral
transport diagnostics package, and separate domain event capability packages.

## Decision

Bondstone is a library for durable module boundaries, not a framework or
broker runtime.

The active package set is:

- `Bondstone`;
- `Bondstone.Persistence`;
- `Bondstone.Persistence.EntityFrameworkCore`;
- `Bondstone.Persistence.EntityFrameworkCore.Postgres`;
- `Bondstone.Hosting`;
- `Bondstone.Transport.Local`;
- `Bondstone.Transport.RabbitMq`.

`Bondstone` owns module registration, module execution, command and event
contracts, durable send/publish APIs, receive pipelines, result observation,
and module-local domain event contracts.

`Bondstone.Persistence` owns provider-neutral durable persistence contracts
and records: durable envelopes, trace context, operation state, outbox, inbox,
inspection contracts, expiration/finalization contracts, and passive
registration records.

`Bondstone.Persistence.EntityFrameworkCore` and
`Bondstone.Persistence.EntityFrameworkCore.Postgres` are the supported durable
persistence implementation path.

Transport packages are adapters. They should not pull Bondstone toward owning
broker topology, broker runtime lifecycle, retries, dead-letter policy,
subscription storage, or a provider-neutral transport diagnostics matrix.

Backwards compatibility remains desirable, but it is not allowed to preserve
undercooked public surface that makes the library harder to reason about. A
public API baseline and ADR review are still required for broad public API
changes.

## Consequences

The package set stays small and aligned with the current learning-project and
template use cases.

Non-EF persistence and additional broker adapters can return later only from
real consumer need. They should be added by extending the durable boundary
model, not by prebuilding speculative abstractions.

The public API can continue to shrink where old exploration leaked framework
internals. Normal consumers should see setup builders, module contracts,
handler contracts, durable send/publish, receive helpers, result observation,
and provider/adapter registration APIs first.

## Amendment 2026-06-16 Broker Adapter Removal

After further transport simplification, `Bondstone.Transport.RabbitMq` was
removed from the active package set instead of being kept as the broker proof.
The active package set is now:

- `Bondstone`;
- `Bondstone.Persistence`;
- `Bondstone.Persistence.EntityFrameworkCore`;
- `Bondstone.Persistence.EntityFrameworkCore.Postgres`;
- `Bondstone.Hosting`;
- `Bondstone.Transport.Local`.

Broker integrations are app-owned for the current MVP. Applications can
implement `IDurableEnvelopeDispatcher` for outgoing outbox records, use
`IDurableMessageEnvelopeSerializer` to map durable envelopes to transport
payloads, and call `IDurableEnvelopeReceiver` from their native receive
handler. Reintroducing broker adapter packages requires a new ADR or amendment
based on real usage pressure.

## Amendment 2026-06-16 Thin Broker Adapter Packages

[ADR 0008](0008-thin-broker-adapters.md) reintroduced broker adapter packages
only for thin native-driver envelope integration. The active package set is
now:

- `Bondstone`;
- `Bondstone.Persistence`;
- `Bondstone.Persistence.EntityFrameworkCore`;
- `Bondstone.Persistence.EntityFrameworkCore.Postgres`;
- `Bondstone.Hosting`;
- `Bondstone.Transport.Local`;
- `Bondstone.Transport.RabbitMq`;
- `Bondstone.Transport.ServiceBus`.

`Bondstone.Transport.RabbitMq` and `Bondstone.Transport.ServiceBus` are
adapter packages, not broker runtimes. They may provide native-driver envelope
dispatchers and opt-in receive workers, but applications continue to own
native topology, provisioning, retry, dead-letter, monitoring, and operational
policy.

## Related Decisions

- Supersedes the active package and compatibility posture from the archived
  pre-restart ADR sequence summarized by
  [0001](0001-restart-adr-history-around-current-baseline.md) and pruned by
  [0009](0009-prune-pre-restart-archive-and-planning-notes.md).

## Application Notes

- Current contract: package IDs, dependency direction, package artifact policy,
  versioning, and publishing are documented in
  [docs/packaging.md](../packaging.md).
- Stable docs: [docs/architecture/README.md](../architecture/README.md),
  [docs/package-discovery.md](../package-discovery.md), and
  [docs/public-api.md](../public-api.md).
- Agent guidance: root [AGENTS.md](../../AGENTS.md) requires ADR review before
  public API, package-boundary, provider, transport, release, or compatibility
  changes.
- Application evidence: removed packages are absent from the solution and
  package discovery; active packages are covered by package artifact tests.
  Thin RabbitMQ and Azure Service Bus packages were added by the 2026-06-16
  thin broker adapter amendment.
- Pending or deferred: Rebus remains app-owned; further public API cleanup is
  planned as a cleanup sweep, not as compatibility preservation.

## Verification

Read current packaging, package discovery, public API, and architecture docs.
Package verification is covered by `pnpm backend:pack`.
