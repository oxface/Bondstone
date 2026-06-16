# Testing

This document records the current Bondstone testing direction.

## Principles

Tests should protect behavior that matters to consumers:

- public contracts;
- package boundaries;
- durable persistence behavior;
- transport behavior;
- tracing and causation behavior;
- module and service extraction assumptions.

Use unit tests for relevant logic and behavior that can be verified without
external infrastructure.

The public API baseline test is a `Unit` test under
`tests/Bondstone.PublicApi.Tests`. It reflects packable package assemblies and
compares their public/protected surface to checked-in baselines. Refresh those
baselines intentionally with `BONDSTONE_UPDATE_PUBLIC_API_BASELINE=1` only
after reviewing the compatibility impact of the API change.

Avoid tests whose only meaningful assertion is that a method called another
method. Interaction assertions are acceptable when the interaction itself is
the observable contract, but tests should generally assert outcomes, persisted
state, emitted messages, error behavior, trace/causation data, or documented
side effects.

Use test doubles for simple collaborators when they keep the test focused.
Use neutral in-repository test fixtures instead of depending on a separate
consumer application or product module.

## Categories

Use these `Category` values consistently:

- `Unit`: fast tests for pure logic, small policies, deterministic mapping,
  validation, and public-contract behavior with no external infrastructure.
- `Application`: fast integration-ish tests that exercise multiple in-process
  components but still avoid external IO such as databases, brokers, caches,
  network calls, or filesystem-coupled behavior.
- `Integration`: tests that require real infrastructure or provider behavior,
  including Testcontainers-backed databases, transports, concurrency,
  serialization, retry, inbox/outbox, or provider-specific behavior.
- `Package`: package artifact tests that inspect produced `.nupkg` files.
  These tests require `pnpm backend:pack` to produce a clean artifact directory
  before they run.

The default quality gate runs `Unit` and `Application` tests. `Integration`
and `Package` tests are explicit because they may require Docker, another
container runtime, or freshly produced package artifacts.

## Integration Tests

Prefer integration tests when persistence, transport, concurrency, retry,
serialization, inbox/outbox, or provider-specific behavior is involved.

Integration tests that require external infrastructure should use
Testcontainers or an equivalent explicit test dependency instead of ambient
developer machine state.

EF Core InMemory tests are allowed only for fast package-local checks of
entity mapping, change-tracker behavior, and "does not call SaveChanges"
boundaries. They are not persistence-semantics tests. Anything that depends on
real database behavior, including PostgreSQL behavior, unique constraints,
transactions, savepoints, locking, indexing, SQL generation, migration
compatibility, inbox deduplication races, outbox claiming, or
retry/terminal-failure state transitions, must be an `Integration` test backed
by Testcontainers or an equivalent real provider fixture.

Keep tests grouped by package or integration boundary so they reveal package
ownership and extraction seams.

For transport adapters, prefer a layered shape: use fast in-process transport
tests for local behavior. Bondstone does not currently ship broker adapter
packages, so broker receive retry, settlement, delivery counts, and
dead-letter topology belong to application or selected transport-library
tests.

For first-class events, keep the same split. Unit and application tests should
cover event destination resolution, publish dispatch, subscriber registration,
and subscriber inbox-key identity. Provider-backed delivery, acknowledgement,
retry, dead-letter, or subscription-storage behavior belongs in explicit
`Integration` tests.

Transport adapter routing is fast behavior. Cover missing command routes,
missing published-event destinations, ambiguous multi-adapter dispatch route
ownership, and missing receive bindings at dispatch or receive time with
`Unit` or `Application` tests unless the assertion depends on a real broker.
handoff.

## Verification Surface

Repository verification is split between fast default checks and
infrastructure-backed integration checks.

The current default verification commands are:

- `pnpm check`
- `pnpm backend:restore`
- `pnpm backend:build`
- `pnpm backend:test`
- `pnpm backend:pack`

Additional test entrypoints are:

- `pnpm backend:test:fast` for `Unit` and `Application` tests.
- `pnpm backend:test:integration` for `Integration` tests.
- `pnpm backend:pack` for package creation and `Package` artifact tests.
- `pnpm backend:test:all` for every discovered test.

`pnpm verify` is kept as an alias for `pnpm check`.

The modular monolith adoption-proof sample has an explicit `Integration`
smoke test under `tests/Bondstone.Samples.Tests`. It is covered by
`pnpm backend:test:integration` and intentionally stays out of the default
fast test filter because it starts Testcontainers PostgreSQL.

The non-EF PostgreSQL, Service Bus, and RabbitMQ package tests were removed
with their packages after MVP. EF/PostgreSQL remains the provider-backed
integration-test path.
