# 0006 Testing Strategy

Status: Amended
Application: Partially Applied
Date: 2026-06-04

## Context

Bondstone is a reusable infrastructure library. Its tests need to protect
behavior that matters to consumers: public contracts, package boundaries,
durable persistence behavior, transport behavior, tracing/correlation behavior,
and module/service extraction assumptions.

Some parts of the library are pure logic and should be tested with fast unit
tests. Other parts depend on database semantics, transport behavior,
transaction boundaries, concurrency, and retry behavior. Those are risky to
test only with mocks because the real behavior often lives in provider or
transport integration details.

The test suite should avoid brittle tests that only assert that one method
called another method. Those tests often encode implementation shape without
protecting useful consumer behavior.

## Decision

Use unit tests for relevant logic and behavior that can be verified without
external infrastructure.

Prefer integration tests when persistence, transport, concurrency, retry,
serialization, inbox/outbox, or provider-specific behavior is involved.
Integration tests that require external infrastructure should use
Testcontainers or an equivalent explicit test dependency rather than relying on
ambient developer machine state.

Avoid tests whose only meaningful assertion is that a method called another
method. Interaction assertions are allowed when the interaction itself is the
observable contract, but tests should generally assert outcomes, persisted
state, emitted messages, error behavior, trace/correlation data, or documented
side effects.

Use test doubles for simple collaborators when they keep the test focused.
Use neutral in-repository test fixtures instead of depending on a separate
consumer application or product module.

Keep tests grouped by package or integration boundary so they reveal package
ownership and extraction seams.

Use these test categories:

- `Unit` for fast tests of pure logic, small policies, deterministic mapping,
  validation, and public-contract behavior with no external infrastructure.
- `Application` for fast integration-ish tests that exercise multiple
  in-process components but still avoid external IO.
- `Integration` for tests that require real infrastructure or provider
  behavior, including Testcontainers-backed databases, transports,
  concurrency, serialization, retry, inbox/outbox, or provider-specific
  behavior.

The default repository quality gate runs `Unit` and `Application` tests.
`Integration` tests must have explicit commands because they can require Docker
or another container runtime.

## Consequences

The test suite may be heavier than a pure unit-test suite, especially around
EF Core providers and transports.

Integration tests should provide stronger confidence for the library behavior
that consumers actually depend on.

Some existing tests may be rewritten rather than moved unchanged when they only
protect implementation wiring or depend on consumer-specific modules.

Repository verification has a clear split between fast default checks and
infrastructure-backed integration checks.

## Applied To

- Code:
  - [tests/Bondstone.Tests/Bondstone.Tests.csproj](../../tests/Bondstone.Tests/Bondstone.Tests.csproj)
  - [tests/Bondstone.EntityFrameworkCore.Tests/Bondstone.EntityFrameworkCore.Tests.csproj](../../tests/Bondstone.EntityFrameworkCore.Tests/Bondstone.EntityFrameworkCore.Tests.csproj)
  - [tests/Bondstone.EntityFrameworkCore.Postgres.Tests/Bondstone.EntityFrameworkCore.Postgres.Tests.csproj](../../tests/Bondstone.EntityFrameworkCore.Postgres.Tests/Bondstone.EntityFrameworkCore.Postgres.Tests.csproj)
  - [tests/Bondstone.Transport.Rebus.Tests/Bondstone.Transport.Rebus.Tests.csproj](../../tests/Bondstone.Transport.Rebus.Tests/Bondstone.Transport.Rebus.Tests.csproj)
  - [package.json](../../package.json)
  - [.github/workflows/verify.yml](../../.github/workflows/verify.yml)
  - [AGENTS.md](../../AGENTS.md)
- Stable docs:
  - [docs/testing.md](../testing.md)
  - [docs/README.md](../README.md)
- Agent instructions:
  - [AGENTS.md](../../AGENTS.md)
- Skills: Not applicable.

## Verification

Read back [docs/testing.md](../testing.md),
[docs/README.md](../README.md), and [AGENTS.md](../../AGENTS.md). Ran
`pnpm verify`; the empty test projects build and execute through `dotnet test`.
Real tests, neutral fixtures, and infrastructure-backed integration checks
remain pending.
