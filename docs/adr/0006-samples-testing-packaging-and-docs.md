# 0006 Samples Testing Packaging And Docs

Status: Accepted
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

## Related Decisions

- Supersedes the active sample, testing, package artifact, and docs governance
  direction from the archived ADR sequence.
- See archived ADRs
  [0006](archive/pre-restart-2026-06-16/0006-testing-strategy.md),
  [0007](archive/pre-restart-2026-06-16/0007-early-sample-application.md),
  [0053](archive/pre-restart-2026-06-16/0053-github-tracked-work-and-current-docs.md),
  and
  [0055](archive/pre-restart-2026-06-16/0055-package-readme-and-xml-doc-artifacts.md)
  for prior context.

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
- Application evidence: package artifact tests assert XML docs; local and
  RabbitMQ sample integration tests cover the current sample path.
- Pending or deferred: a cleanup sweep should remove redundant docs/tests and
  dedupe stale guidance after the architecture pivot settles.

## Verification

Read current docs README, packaging, samples, setup, package discovery, and
testing docs. Current verification entrypoints are documented in
`docs/testing.md` and package artifacts are checked by `pnpm backend:pack`.
