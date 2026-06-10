# Future Work

This document tracks ideas that are not part of Bondstone's current operating
contract. Move an item into stable docs only after the corresponding decision
is accepted and implemented.

## Compatibility And Packaging

- Evaluate wider target framework support when consumer demand justifies the
  maintenance cost.
- Revisit independent package versioning only if coordinated package versioning
  becomes a release-management problem.
- Consider narrower package splits only with an ADR that preserves the current
  dependency direction.

## Persistence

- Add migration helpers or provider-specific migration conventions.
- Add provider-specific payload storage choices such as PostgreSQL `jsonb`.
- Add stale outbox claim recovery, stale inbox receive recovery, cleanup
  workers, retention workers, or dead-letter maintenance workers.
- Add richer operation-state transitions, including default running, failure,
  cancellation, retry, timeout, polling, and result payload policies.
- Add multi-data-source selection for `Bondstone.Persistence.Postgres`.
- Add optional domain event collection and persistence only as a separate
  module-boundary capability.

## Transport

- Add broker topology declaration helpers for RabbitMQ or Service Bus.
- Add public multi-transport diagnostic report shapes if startup exception
  messages stop being enough.
- Add external event handoff formats such as unwrapped payloads, CloudEvents,
  schema-specific envelopes, or non-JSON payload negotiation.
- Broaden provider-backed integration coverage beyond the current receive
  success, failure handoff, and event fan-out contracts when new adapter
  behavior needs it.

## Hosting

- Add inbox, cleanup, archiving, dead-letter retention, or maintenance workers
  after their core abstractions are stable.
- Consider module-targeted outbox worker registration for selected-module
  dispatch and stronger noisy-neighbor isolation. A future ADR must define the
  option shape, DI registration, validation behavior, failure semantics,
  global-versus-module batch budgeting, parallelism or sequential behavior, and
  test scope before implementation.

## Samples

- Add service-split, broker, or multi-process samples only as bounded sample
  scenarios.
- Use Aspire as the preferred local orchestration host when a sample needs
  multiple processes or local infrastructure.
