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
  provider-neutral outbox failure decisions and plain outbox dispatch
  composition;
- provider-neutral EF Core entity mappings, outbox writer, inbox store,
  operation state store, and EF persistence scope;
- PostgreSQL provider registration, duplicate classification, inbox
  registration, outbox claiming, outbox lease renewal, and outbox dispatch
  recording;
- neutral hosted outbox worker composition over `IDurableOutboxDispatcher`;
- Rebus outgoing command transport for claimed outbox records, including
  destination resolution, wire-envelope mapping, and durable header mapping;
- lightweight `AddBondstone` composition builder with outbox capability
  validation for hosted or dispatcher-based processing.

## Verification Surface

Current automated coverage includes:

- neutral unit tests for core messaging and persistence contracts;
- EF Core unit and application tests for mapping, service registration, store
  staging, and persistence-scope validation;
- PostgreSQL Testcontainers integration tests for real schema creation,
  transactions, savepoints, unique constraints, inbox registration, outbox
  claiming, outbox lease renewal, outbox dispatch recording,
  outbox dispatcher composition, EF persistence-scope behavior, and
  schema-aware provider registration;
- hosting unit tests for outbox worker options, hosted worker loop behavior,
  DI registration, and builder guardrails;
- Rebus unit tests for outgoing command transport routing, wire-envelope
  mapping, durable headers, trace headers, unsupported event envelopes,
  destination resolution, and DI registration.
- a cross-package application smoke test for preferred `AddBondstone`
  composition with PostgreSQL persistence, Rebus transport, and the hosted
  outbox worker.

The default quality gate remains `pnpm check`. The current tree passes the
default gate. Integration tests remain separate and should be run for provider
behavior changes.

## Deferred Work

Deferred extraction work includes:

- stale-claim recovery, dead-letter routing, cleanup/maintenance workers, and
  advanced dispatcher or worker configuration;
- inbox handler discovery, receive retry policy, stale receive recovery, and
  transport acknowledgement coordination;
- module identity scopes, domain-event capture, and higher-level transaction
  helpers above the EF persistence scope;
- Rebus receive-side inbox integration and event publish/subscribe behavior;
- provider-specific payload storage such as PostgreSQL `jsonb`, migration
  helpers, broader provider support, samples, and additional integration
  fixtures.
