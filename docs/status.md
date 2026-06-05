# Current Status

This document summarizes the current extraction and verification state. Keep
durable strategy in the architecture, extraction, packaging, and testing docs;
keep tactical backlog details in [extraction-plan.md](extraction-plan.md).

## Applied Surface

Current implemented surface includes:

- core message identity, message type registration, trace context, durable
  command send result, durable operation read, and durable envelope contracts;
- core persistence records and boundaries for outbox, inbox, operation state,
  outbox claiming, outbox lease renewal, outbox dispatch recording, inbox
  registration, and delegate-based inbox handle-once execution, plus
  provider-neutral outbox failure decisions;
- provider-neutral EF Core entity mappings, outbox writer, inbox store,
  operation state store, and EF persistence scope;
- PostgreSQL provider registration, duplicate classification, inbox
  registration, outbox claiming, outbox lease renewal, and outbox dispatch
  recording.

## Verification Surface

Current automated coverage includes:

- neutral unit tests for core messaging and persistence contracts;
- EF Core unit and application tests for mapping, service registration, store
  staging, and persistence-scope validation;
- PostgreSQL Testcontainers integration tests for real schema creation,
  transactions, savepoints, unique constraints, inbox registration, outbox
  claiming, outbox lease renewal, outbox dispatch recording,
  EF persistence-scope behavior, and schema-aware provider registration.

The default quality gate remains `pnpm check`. In this environment, fresh
restore has been timing out around the PostgreSQL project, so recent slices
have been verified with no-restore build/test/pack commands and
`pnpm backend:test:integration` against already restored assets.

## Deferred Work

Deferred extraction work includes:

- outbox dispatcher loop and transport send implementation;
- stale-claim recovery, dead-letter routing, dispatcher configuration, and
  hosted worker registration;
- inbox handler discovery, receive retry policy, stale receive recovery, and
  transport acknowledgement coordination;
- module identity scopes, domain-event capture, and higher-level transaction
  helpers above the EF persistence scope;
- transport adapters, starting with Rebus after the dispatcher boundary is
  stable;
- provider-specific payload storage such as PostgreSQL `jsonb`, migration
  helpers, broader provider support, samples, and additional integration
  fixtures.
