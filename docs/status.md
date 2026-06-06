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
- Rebus command receive-side inbox adapter for Bondstone wire envelopes,
  explicit handler identity, core inbox handle-once execution, caller-supplied
  commit boundaries, .NET `ActivityContext` traceparent validation,
  already-received failure surfacing, and fluent `AddBondstone` registration;
- typed Rebus command receive pipeline using `IMessageTypeRegistry`,
  `System.Text.Json`, explicit handler identity, receive-pipeline Activity
  creation from accepted W3C parent context, the low-level Rebus inbox
  adapter, and caller-supplied commit delegates;
- Rebus module command receive pipeline groundwork using module command route
  metadata, stable message identity resolution, JSON deserialization, module
  command execution with an explicit durable inbox record, inbox handle-once
  behavior inside the module command pipeline, inbox result return through
  `ModuleCommandExecutionResult`, and already-received failure surfacing;
- host-owned Rebus outgoing topology builder for mapping target modules to
  Rebus destination addresses;
- module command registration and execution in core, including
  `IBondstoneModule`, `ICommand`, `IDurableCommand`, direct typed command
  handlers, typed validators, startup reflection scanning, cached module
  command routes, scoped command execution, module metadata registration,
  durable-messaging capability metadata, module persistence capability
  metadata, module execution context, explicit receive inbox records,
  source-module scoped durable command sending, receive-side inbox system
  behavior, ordered system pipeline behavior, execution results, and validation
  pipeline behavior;
- default outbox-backed `IDurableCommandSender` composition that requires a
  current module execution context, serializes durable command payloads, stages
  `DurableMessageEnvelope` records through `IDurableOutboxWriter`, and uses
  the executing module as the source module;
- module-owned EF Core persistence opt-in through
  `UseEntityFrameworkCorePersistence<TDbContext>` and an EF-specific module
  command transaction behavior that wraps opted-in module command execution in
  `IEntityFrameworkCorePersistenceScope` and calls `SaveChangesAsync`;
- lightweight `AddBondstone` composition builder with outbox capability
  validation for hosted or dispatcher-based processing.

## Accepted Direction

Accepted but not yet implemented direction includes:

- host-owned Rebus endpoint binding to local module sets, replacing manual
  receive endpoint wiring in app-facing receive setup.

## Verification Surface

Current automated coverage includes:

- neutral unit tests for core messaging and persistence contracts;
- neutral unit tests for module command registration, startup scanning,
  validation, route lookup, module execution context, source-module scoped
  durable sending, receive inbox behavior, and direct handler execution;
- EF Core unit and application tests for mapping, service registration, store
  staging, persistence-scope validation, module persistence opt-in, and module
  transaction/save behavior;
- PostgreSQL Testcontainers integration tests for real schema creation,
  transactions, savepoints, unique constraints, inbox registration, outbox
  claiming, outbox lease renewal, outbox dispatch recording,
  outbox dispatcher composition, EF persistence-scope behavior, and
  schema-aware provider registration;
- hosting unit tests for outbox worker options, hosted worker loop behavior,
  DI registration, and builder guardrails;
- Rebus unit tests for outgoing command transport routing, wire-envelope
  mapping, durable headers, trace headers, unsupported event envelopes,
  destination resolution, command receive-side inbox mapping, already-received
  failure behavior, traceparent validation, typed command deserialization,
  Activity creation, registry mismatch failures, and DI registration;
- Rebus in-memory transport integration tests for receive-side `SendLocal`
  delivery through a real Rebus worker, typed receive pipeline execution, queue
  drain behavior, and unknown message identity dead-letter behavior;
- Rebus PostgreSQL transport integration tests for receive-side `SendLocal`
  delivery, typed receive handling, PostgreSQL-backed acknowledgement/queue
  drain behavior, and PostgreSQL-backed dead-letter behavior;
- cross-package application smoke tests for preferred `AddBondstone`
  composition with PostgreSQL persistence, Rebus transport, hosted outbox
  worker, Rebus inbox execution, typed Rebus command receive execution, and
  explicit EF persistence commit boundaries.

The default quality gate remains `pnpm check`. The current tree passes the
default gate. Integration tests remain separate and should be run for provider
behavior changes.

## Deferred Work

Deferred extraction work includes:

- stale-claim recovery, dead-letter routing, cleanup/maintenance workers, and
  advanced dispatcher or worker configuration;
- inbox handler discovery, receive retry policy, stale receive recovery, and
  transport acknowledgement coordination;
- module identity scopes beyond the current source-module execution context,
  domain-event capture, durable-messaging capability validation,
  provider-specific module persistence validation, and broader transaction
  helpers above the EF persistence scope;
- Rebus host-topology endpoint binding to local module sets, broader
  transport-level Rebus module receive tests, and event publish/subscribe
  behavior;
- provider-specific payload storage such as PostgreSQL `jsonb`, migration
  helpers, broader provider support, samples, and additional integration
  fixtures.
