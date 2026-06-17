# 0006 Samples Testing Packaging And Docs

Status: Amended
Application: Applied
Date: 2026-06-16

## Context

Bondstone is public, but still mostly used by the maintainer's templates and
learning projects. The repository therefore needs strong local verification
and honest samples more than broad framework ceremony.

Consumer feedback after publication found missing XML API documentation in
NuGet packages and local transport behavior that needed to prove inbox
semantics through the real receive pipeline.

The documentation model also matters now that ADR history has restarted:
stable docs should describe current behavior, while ADRs explain durable
decisions.

## Decision

Keep one small modular monolith sample as the adoption proof. It should
exercise:

- module-owned EF/PostgreSQL persistence;
- separate module schemas;
- durable command send and receive;
- result-returning durable command observation through operation handles;
- integration event publish/subscribe;
- receive-side inbox idempotency;
- outbox dispatch evidence;
- EF-backed module-local domain event persistence;
- local transport as the dev/test path;
- one RabbitMQ path as the direct broker proof.

Do not turn the sample into a broker matrix, product UI, deployment guide, or
feature showcase.

Testing categories remain:

- `Unit` for fast deterministic behavior;
- `Application` for fast in-process composition behavior;
- `Integration` for Testcontainers-backed database and broker behavior;
- `Package` for `.nupkg` artifact inspection.

The default fast gate runs `Unit` and `Application`. Integration and package
tests are explicit.

Every packable package should include README, symbols, and XML API docs.
`pnpm backend:pack` packs the active solution and runs package artifact tests.

Stable docs describe current behavior. ADRs describe why durable decisions
exist. Archived ADRs are not the active operating contract.

## Consequences

The sample remains useful pressure on the library without becoming the
product.

Package consumers get better IntelliSense and package metadata.

The test suite can stay fast by default while still offering explicit
provider-backed confidence for EF/PostgreSQL and RabbitMQ.

Future documentation cleanup should dedupe repeated setup/architecture text
and keep one owner for each durable rule.

## Amendment 2026-06-16 Remove Broker Sample Proof

After the broker adapter package was removed from the active product surface,
the modular monolith sample no longer carries a RabbitMQ registration path or
RabbitMQ Testcontainers smoke test. The current sample proves the durable loop
through EF/PostgreSQL plus explicit local transport. Broker-boundary examples
should stay in app-owned sample code until a real adapter package is
reintroduced by ADR.

Provider-backed confidence currently means EF/PostgreSQL integration coverage.
Broker provider-backed tests are deferred with broker adapter packages.

## Amendment 2026-06-16 Thin Broker Adapter Packages

[ADR 0008](0008-thin-broker-adapters.md) adds thin RabbitMQ and Azure Service
Bus adapter packages without restoring a broker matrix sample. The modular
monolith sample remains focused on EF/PostgreSQL and local transport, while
setup docs show the thin adapter registration shape.

The broker adapter packages include fast unit/application tests for
registration and option validation plus Testcontainers-backed integration tests
for native-driver dispatcher publish and opt-in receive-worker handoff into
Bondstone's receiver boundary. The modular monolith sample tests also prove
RabbitMQ and Azure Service Bus adapter-backed extracted-service flows with
durable operation result observation.

## Related Decisions

- Supersedes the active sample, testing, package artifact, and docs governance
  direction from the pre-restart ADR sequence summarized by
  [0001](0001-restart-adr-history-around-current-baseline.md) and pruned by
  [0009](0009-prune-pre-restart-archive-and-planning-notes.md).

## Application Notes

- Current contract: samples, tests, package artifacts, and documentation
  ownership are documented in [docs/samples.md](../samples.md),
  [docs/testing.md](../testing.md), [docs/packaging.md](../packaging.md), and
  [docs/README.md](../README.md).
- Stable docs: setup guidance lives in [docs/setup.md](../setup.md); package
  discovery lives in [docs/package-discovery.md](../package-discovery.md).
- Agent guidance: root [AGENTS.md](../../AGENTS.md) points agents to docs,
  ADRs, testing, samples, packaging, and GitHub workflow docs before changing
  those areas.
- Application evidence: package artifact tests assert XML docs; the local
  modular monolith sample integration test covers the current sample path; and
  RabbitMQ/Service Bus adapter and sample integration tests prove the thin
  broker handoff. On 2026-06-17, stable docs were swept for roadmap-style
  wording so durable docs describe current behavior while ADRs and plans keep
  decision trail and after-v2 handoff.
- Pending or deferred: no v2 documentation cleanup remains in this ADR.

## Verification

Read current docs README, packaging, samples, setup, package discovery, and
testing docs. Current verification entrypoints are documented in
`docs/testing.md` and package artifacts are checked by `pnpm backend:pack`.
On 2026-06-17, formatted changed Markdown after the durable-doc cleanup.
