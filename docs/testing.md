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

The default quality gate runs `Unit` and `Application` tests. `Integration`
tests are explicit because they may require Docker or another container
runtime.

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
compatibility, inbox deduplication races, outbox claiming, or retry/dead-letter
state transitions, must be an `Integration` test backed by Testcontainers or
an equivalent real provider fixture.

Keep tests grouped by package or integration boundary so they reveal package
ownership and extraction seams.

For transport adapters, prefer a layered shape: use fast in-process transport
tests for the broad behavior matrix, and keep a small number of
Testcontainers-backed transport tests for the real provider handoff contract:
acknowledgement/completion after success, negative acknowledgement or abandon
after failure, and broker-owned dead-letter behavior where the adapter promises
that handoff.

After ADR 0038, receive retry-policy tests should assert Bondstone's provider
boundary rather than expecting Bondstone to own broker retry policy. Fast tests
should cover native mapping, dispatch, and settlement ordering. Provider-backed
integration tests should prove the native handoff that Bondstone performs:
RabbitMQ negative acknowledgement with the configured `requeue` value and
Service Bus abandon/complete behavior. Broker retry schedules, max delivery
count, and dead-letter topology are app/provider configuration and should be
tested only where the adapter explicitly promises that handoff.

For first-class events, keep the same split. Unit and application tests should
cover event route/topic resolution, publish dispatch, subscriber registration,
subscriber inbox-key identity, and diagnostics. Provider-backed delivery,
acknowledgement, retry, dead-letter, or subscription-storage behavior belongs
in explicit `Integration` tests.

Transport topology validation is fast startup behavior. Cover missing command
routes, missing published-event destinations, ambiguous multi-transport route
ownership, missing subscriber bindings, and invalid receive bindings with
`Unit` or `Application` tests unless the assertion depends on a real broker
handoff.

## Verification Surface

Repository verification will need a clear split between fast default checks and
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
- `pnpm backend:test:all` for every discovered test.

`pnpm verify` is kept as an alias for `pnpm check`.

The modular monolith adoption-proof sample has an explicit `Integration`
smoke test under `tests/Bondstone.Samples.Tests`. It is covered by
`pnpm backend:test:integration` and intentionally stays out of the default
fast test filter because it starts Testcontainers PostgreSQL.

The PostgreSQL persistence proof has explicit `Integration` tests under
`tests/Bondstone.Persistence.Postgres.Tests` because the proof depends
on real PostgreSQL schema, transaction, inbox, outbox, and operation-state
behavior.

## Current Status

This testing strategy is accepted and documented. Initial test projects,
category-filtered commands, and CI wiring exist. Current test coverage is
summarized in [mvp-plan.md](mvp-plan.md). Keep this document focused on
testing policy and command entrypoints; keep the tactical list of currently
covered slices in the MVP plan.
