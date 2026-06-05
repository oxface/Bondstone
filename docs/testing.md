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

Samples may become end-to-end and smoke-test targets when the sample
application exists.

## Application State

This testing strategy is accepted and documented. Initial test projects,
category-filtered commands, and CI wiring exist. The current neutral `Unit`
tests cover core message identity registry behavior, durable command send
result semantics, message trace context, durable operation state/status
semantics, durable message envelope validation, and persistence record and
dispatch-state validation. EF Core unit tests cover entity-to-core mapping and
provider-neutral model metadata, plus fast `Application` tests for EF
change-tracker staging boundaries. Infrastructure-backed checks have started
with PostgreSQL Testcontainers tests for EF Core schema creation, transaction
commit/rollback, inbox unique-constraint behavior, and PostgreSQL registration
helper behavior. These tests also cover PostgreSQL primary-key constraint
names, inbox processed timestamps, operation-state updates, savepoint rollback
after duplicate inbox inserts, outbox claim lease columns,
`FOR UPDATE SKIP LOCKED` outbox row selection, and public PostgreSQL outbox
claiming for due rows, scheduled pending rows, locked-row skipping, expired
lease reclaim, active lease exclusion, validation, and schema-aware service
registration. They also cover PostgreSQL outbox dispatch success, retry
scheduling, dead-letter outcomes, stale-claimant rejection, and expired-lease
rejection. Public PostgreSQL inbox registration tests cover newly registered,
already received, already processed, transaction-safe duplicate registration,
and schema-aware registration outcomes. Broader neutral fixtures remain future
application work.
