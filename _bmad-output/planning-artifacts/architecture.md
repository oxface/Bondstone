---
title: Bondstone BMAD-Native Architecture
status: final
date: 2026-06-18
workflowType: architecture
project_name: Bondstone
inputDocuments:
  - _bmad-output/planning-artifacts/prds/prd-Bondstone-2026-06-18/prd.md
  - README.md
  - AGENTS.md
  - docs/setup.md
  - docs/operations.md
  - docs/observability.md
  - docs/packaging.md
  - docs/public-api.md
  - docs/testing.md
---

# Bondstone BMAD-Native Architecture

## Architecture Role

This document is the durable internal architecture source of truth for
Bondstone implementation work. It absorbs the former internal architecture and
planning content. Consumer-facing package guidance remains in `/docs`.

Agents changing runtime behavior, persistence, hosting, transport adapters,
package boundaries, public API, samples, testing, or repository workflow must
start here and then use the nearest scoped `AGENTS.md`.

## Product Positioning

Bondstone is a .NET library for durable module boundaries. It supports
modular monoliths first and preserves a path to service extraction when a
module needs independent scalability, deployment, or operational isolation.

Bondstone is not:

- a general-purpose message bus;
- a workflow engine;
- a saga/process-manager framework;
- a broker topology manager;
- a code generator;
- a SaaS application framework.

The core architectural promise is that teams can keep module contracts,
stable message identities, outbox/inbox behavior, handler patterns, and
operation-observation semantics as modules move from in-process composition to
separate services.

## Package Architecture

Current package responsibilities:

- `Bondstone`: module model, command/event contracts, durable message
  identities, module execution, durable send/publish APIs, domain-event
  contracts, and runtime composition over provider contracts.
- `Bondstone.Persistence`: provider-neutral durable persistence contracts,
  operation state contracts, outbox/inbox abstractions, and inspection
  surfaces.
- `Bondstone.Persistence.EntityFrameworkCore`: generic EF Core mappings,
  stores, transaction behavior, outbox/inbox persistence, operation state,
  and optional EF-backed domain-event persistence.
- `Bondstone.Persistence.EntityFrameworkCore.Postgres`: PostgreSQL-specific
  EF behavior and provider integration.
- `Bondstone.Hosting`: hosted outbox and durable inbox worker composition.
- `Bondstone.Transport.Local`: explicit local/dev/test transport adapter.
- `Bondstone.Transport.RabbitMq`: thin RabbitMQ native-driver envelope
  adapter.
- `Bondstone.Transport.ServiceBus`: thin Azure Service Bus native-driver
  envelope adapter.

Package dependency direction must stay layered from core contracts to
provider/runtime implementations. Runtime packages collaborate through public
or package-local contracts. Do not add `InternalsVisibleTo` for production
package collaboration.

## Module Ownership

Modules own their durable messaging and persistence declarations:

- module name;
- durable messaging capability;
- persistence binding;
- command handler registration;
- command validator registration;
- published integration event registration;
- event subscriber registration.

The preferred public shape is a module-owned `IBondstoneModule` registration
object plus a thin host extension that supplies environment-specific inputs
such as connection strings.

Hosts use `AddBondstone` to compose modules, persistence providers, local
transport for development/test flows, app-owned broker bridges, and hosted
workers. Provider-native transport configuration, broker administration,
retry, dead-letter policy, worker settings, credentials, and topology
declaration remain app-owned.

## Module Command Pipeline

`IModuleCommandExecutor` is the immediate same-process module command
boundary. It executes registered typed command handlers through Bondstone's
runtime sequence. It is not a generic mediator.

Commands:

- `ICommand` marks module command pipeline execution.
- `ICommand<TResult>` adds typed immediate results.
- `IDurableCommand` marks commands accepted for durable send and receive.
- A durable result command implements both `IDurableCommand` and
  `ICommand<TResult>`.

Command route metadata includes module name, stable message type identity,
command CLR type, handler type, optional result type, and stable handler
identity. Handler identity is part of the command receive inbox key and must
remain stable.

Command execution sequence:

1. provider transaction runner;
2. durable operation completion behavior;
3. receive inbox behavior when a receive context exists;
4. module execution context;
5. module-owned command validation;
6. handler execution;
7. provider post-handler actions.

During execution, Bondstone sets the current module execution context before
the handler runs and restores the previous context afterward. Durable command
sending and event publishing inside the handler use that current module as
the source module.

Cross-module immediate execution from inside a handler is allowed only through
the module command pipeline and only when the caller accepts that it is not
restart-safe distributed orchestration. Cross-module state-changing work that
must survive restart or service extraction should use durable commands or
integration events.

## Module Query Pipeline

The v2 direction includes a separate module query boundary for immediate
same-process reads. Queries should respect module registration and persistence
ownership, but they must not write durable inbox rows, outbox rows, operation
state, domain-event records, or integration events.

Query execution is for HTTP endpoints, application entrypoints, tests, and
explicit `.Contracts` use cases that need a module boundary without durable
message semantics.

## Durable Commands

`IDurableCommandSender` accepts a durable command, a required target module,
and optional metadata such as partition key, durable operation id, trace
context, and causation id. The default sender requires a current module
execution context, uses the executing module as the source module, serializes
the command, and stages a command envelope through the source module outbox.

Durable send accepts work. It does not return the target handler result
directly. Results are observed through operation APIs when the command and
caller use an operation id.

Extracted source services that send remote durable commands must register
remote command contract identity with `BondstoneBuilder.RegisterMessage<T>()`
or `RegisterMessagesFromAssembly(...)`. This registers message identity only;
it does not create a local command route or require a reference to the target
module implementation assembly.

## Integration Events

`IIntegrationEvent` is reserved for durable cross-module facts. Integration
events are not commands: they target no single module and fan out to zero or
more independently identified subscribers.

`IDurableEventPublisher` requires current source-module context, serializes
the event with its stable integration event identity, stages an event outbox
envelope without `TargetModule`, and returns publish metadata. It does not
wait for subscriber results.

Published event registration is module-owned metadata. Subscriber
registration records subscriber module, event type, handler type, and stable
subscriber identity. Subscriber identity is consumer-owned durable identity
and must not be derived from handler CLR type names.

Event-driven orchestration composes commands and events rather than erasing
their distinction. A subscriber can react to an integration event and send
durable commands as additional module work.

## Domain Events

Domain events are module-local facts. They are distinct from integration
events and durable transport messages.

Bondstone core owns:

- `IDomainEvent`;
- `DomainEventIdentityAttribute`;
- `IDomainEventSource`.

Domain events are not automatically staged in the outgoing outbox and are not
automatically dispatched. Publishing a public integration event from a domain
event is explicit module code.

EF Core domain-event persistence is optional and module-local. It activates
only for EF-backed modules that opt in with
`UseEntityFrameworkCoreDomainEventPersistence()` and map records with
`ApplyBondstoneDomainEvents()`. EF collection persists module-local records
and clears pending source events only after the observed EF transaction
commits successfully.

## Durable Outbox

The outbox is the source-module ledger for outgoing durable commands and
integration events. Source state and outgoing envelopes commit atomically in
the source module transaction boundary.

Outbox rows carry stable message identity, source module, target module for
commands, payload, trace/causation metadata, operation id where present,
attempt state, and terminal failure state.

The outbox worker claims due rows, dispatches through host-configured
dispatchers, and records dispatched, retry scheduled, stale, or terminal
failed outcomes. Bondstone owns persisted outbox retry and terminal failure
state. Broker retry, topology, dead-letter policy, and native monitoring are
host-owned.

## Durable Inbox

The durable inbox is the v2 durable receive ledger. It owns durable ingestion,
claim, retry, processed, stale, and terminal receive failure state around
processing attempts.

Receive identity:

- command rows use message id, target module, and stable command handler
  identity;
- event rows use message id, subscriber module, and stable subscriber
  identity.

Native transport deliveries are parsed into `DurableMessageEnvelope`,
validated against message kind, stable message type registration, and command
route or subscriber binding, then inserted into the durable inbox
idempotently. Native broker delivery must settle only after durable ingestion
succeeds.

Durable inbox processing claims due rows and invokes the module command or
event subscriber receive pipeline. Success marks the row processed. Failures
schedule retry or terminal failure according to policy. Terminal durable inbox
failure is operational evidence; it does not automatically write caller-
visible operation `Failed` state.

The smaller direct `inbox_messages` idempotency marker is a transitional
implementation detail during module processing. The durable inbox is the
operator-facing receive outcome and should become the single durable receive
ledger.

## Receive Pipeline

Core exposes provider-neutral receive pipelines over `DurableMessageEnvelope`:

- `IModuleCommandReceivePipeline`;
- `IModuleEventReceivePipeline`.

These pipelines resolve stable identities, deserialize payloads, derive inbox
records, execute the module command or event subscriber executor, and surface
stale receive state. They do not configure broker listeners, discover handlers
from broker messages, own retry/dead-letter policy, or replace native
acknowledgement behavior.

Already processed records are skipped. Already received but unprocessed
markers are operationally loud because Bondstone has no default recovery model
that can prove re-execution is safe. Recovery remains operator-owned or
application-owned unless a future BMAD artifact adds an explicit surface.

## Operation Observation

Operation observation answers "what is known about accepted durable work?"
It is not orchestration, saga state, process-manager state, or durable
continuation state.

Durable ingress or durable send may create or accept an operation id. Target
module durable command processing can complete operation state and result
payloads in the target module transaction.

Operation APIs support:

- accepted-work metadata;
- status reads;
- typed result reads;
- short edge-facing waits;
- expiration processing when configured.

Wait helpers are convenience APIs for edge callers and tests. Caller timeout
does not write operation state. Durable inter-module continuations remain
application-owned until a future BMAD PRD and architecture add a native
process-management feature.

## Transport Boundary

Transport adapters are thin native-driver envelope adapters. They translate
between provider-native messages and Bondstone durable envelopes.

Hosts own:

- queues, topics, exchanges, subscriptions, rules, and bindings;
- credentials, connection lifecycle, worker placement, prefetch, concurrency;
- broker retry and dead-letter policy;
- native monitoring and topology provisioning.

Bondstone owns:

- durable message identity and serialization;
- module receive binding validation;
- durable outbox records and dispatch outcomes;
- durable inbox records and processing outcomes;
- operation finalization semantics;
- module command and event subscriber handlers.

`Bondstone.Transport.Local` is explicit local/dev/test infrastructure. It is
not a production broker, fallback transport, or proof of broker behavior.

## Hosting And Workers

The target topology has three durable worker/listener roles:

1. Source outbox worker: claims source outbox rows, dispatches envelopes, and
   records outbox outcomes.
2. Transport ingestion listener or worker: converts provider-native deliveries
   into durable inbox rows before native settlement.
3. Durable inbox processing worker: claims durable inbox rows, invokes module
   command or event subscriber pipelines, and records receive outcomes.

Direct command execution and query execution are not workers. They are
same-process module pipeline calls.

Cleanup, retention, replay, purge, stale-row mutation, and broker
dead-letter movement remain application-owned. Do not add a default cleanup
worker without a future BMAD PRD and architecture decision.

## Persistence Architecture

EF Core plus PostgreSQL is the supported production durable persistence path.
Generic EF mappings own canonical table, column, index, and constraint names.
PostgreSQL adapts provider-specific behavior.

Consumers own EF migrations. Bondstone packages provide mappings and provider
helpers, not package-shipped migrations or automatic schema rollout.

Module-owned EF persistence must keep source state, outbox rows, inbox
markers, operation state, domain-event records, and incoming durable inbox
rows in the owning module transaction boundary where applicable.

EF Core InMemory is acceptable only for fast mapping/change-tracker
boundaries. PostgreSQL semantics, uniqueness, transactions, savepoints,
locking, SQL generation, inbox/outbox claiming, deduplication races, retry,
and terminal state transitions require integration tests.

## Diagnostics And Observability

Diagnostics should be OpenTelemetry-native where possible. Current activity
sources, tags, metrics, and logs should avoid high-cardinality values such as
message ids, operation ids, exception messages, payloads, broker delivery
counts, topology details, and dead-letter state.

Stable misconfiguration codes are desired for common setup failures:

- missing module persistence;
- missing EF mappings;
- missing dispatcher;
- duplicate module durable registrations;
- invalid durable identities;
- missing receive binding;
- ambiguous dispatch routes.

Diagnostic surfaces must not move provider-native broker monitoring into
Bondstone.

## Public API And Compatibility

Public/protected API changes are compatibility-sensitive. Before hiding,
renaming, removing, or changing parameter names, classify affected APIs as:

- normal setup APIs;
- documented advanced composition APIs;
- public implementation types exposed temporarily;
- accidental or obsolete API surface.

Public API baselines in `tests/Bondstone.PublicApi.Tests` guard packable
packages. Refresh baselines only after reviewing compatibility impact.

Normal setup APIs and documented advanced composition APIs need migration
notes when changed. Public implementation types should be reduced gradually
only when explicit contracts or package-local implementation replace them.

## Documentation Ownership

BMAD planning artifacts own internal source-of-truth content:

- `prd.md`: product requirements, scope, goals, non-goals, success criteria;
- `architecture.md`: internal runtime architecture and package-boundary rules;
- `epics.md`: implementation sequencing and story acceptance criteria;
- `project-context.md`: lean agent-facing rules and verification entrypoints.

`/docs` owns consumer-facing or repository-operation docs:

- setup and package discovery;
- packaging and public API review;
- operations and observability;
- samples and testing;
- repository workflow and GitHub issue guidance.

`/docs` should reference BMAD architecture for internal design details instead
of duplicating durable rules. If a docs file is fully absorbed into BMAD, it
should be deleted and references fixed.

## Verification Strategy

Default commands:

- `pnpm check`;
- `pnpm verify`;
- `pnpm backend:restore`;
- `pnpm backend:build`;
- `pnpm backend:test`;
- `pnpm backend:test:integration`;
- `pnpm backend:pack`.

Testing categories:

- `Unit` and `Application` are fast/default;
- `Integration` requires real infrastructure/provider behavior;
- `Package` inspects freshly packed artifacts.

Documentation-only cleanup should at least run formatting/reference checks
where available. Runtime changes should run targeted tests first, then the
broader package script appropriate to the changed surface.

Runtime implementation reviews must name how messaging, persistence, hosting,
transport, or package API changes preserve Bondstone's durable
module-boundary model. Review notes should call out service-extraction
continuity for stable message identities, handler patterns, stable handler
identities, inbox/outbox semantics, operation observation, module-owned
durability, and host-owned broker topology. They must not imply that
Bondstone has become a generic bus, workflow engine, code generator, SaaS
framework, application platform, or broker runtime owner.

## Explicit Deferred Work

- Native saga/process-manager support.
- Provider-neutral broker topology ownership.
- Default cleanup/retention workers.
- Non-EF production durable persistence providers.
- Automatic domain event dispatch.
- Automatic domain-to-integration-event publication.
- Broker dead-letter management.
- Generic application middleware pipeline.
