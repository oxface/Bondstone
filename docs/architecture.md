# Bondstone Architecture

This document is the durable architecture reference for Bondstone. It was
seeded from the surviving legacy architecture artifact during migration on
2026-06-23.

## Product Positioning

Bondstone is a .NET library for durable module boundaries. It supports modular
monoliths first and preserves a path to service extraction when a module needs
independent scalability, deployment, or operational isolation.

Bondstone is not:

- a general-purpose message bus;
- a workflow engine;
- a saga/process-manager framework;
- a broker topology manager;
- a code generator;
- a SaaS application framework.

The core promise is continuity of module contracts, stable message identities,
outbox/inbox behavior, handler patterns, and operation-observation semantics as
modules move from in-process composition to separate services.

## Package Responsibilities

- `Bondstone`: module model, command/event contracts, durable message
  identities, module execution, durable send/publish APIs, domain-event
  contracts, and runtime composition over provider contracts.
- `Bondstone.Persistence`: provider-neutral durable persistence contracts,
  operation state contracts, outbox/inbox abstractions, and inspection surfaces.
- `Bondstone.Persistence.EntityFrameworkCore`: generic EF Core mappings,
  stores, transaction behavior, outbox/inbox persistence, operation state, and
  optional EF-backed domain-event persistence.
- `Bondstone.Persistence.EntityFrameworkCore.Postgres`: PostgreSQL-specific EF
  behavior and provider integration.
- `Bondstone.Hosting`: hosted outbox and durable inbox worker composition.
- `Bondstone.Transport.Local`: explicit local/dev/test transport adapter.
- `Bondstone.Transport.RabbitMq`: thin RabbitMQ native-driver envelope adapter.
- `Bondstone.Transport.ServiceBus`: thin Azure Service Bus native-driver
  envelope adapter.

Package dependency direction must stay layered from core contracts to
provider/runtime implementations. Runtime packages collaborate through public
or package-local contracts, not production `InternalsVisibleTo`.

## Module And Message Boundaries

Modules own durable messaging and persistence declarations: module name,
durable messaging capability, persistence binding, command handlers,
validators, published integration events, and event subscribers.

`IModuleCommandExecutor` is the immediate same-process module command boundary.
It is not a generic mediator. Cross-module state-changing work that must
survive restart or service extraction should use durable commands or
integration events.

`IDurableCommandSender` accepts work, stages a command envelope through the
source module outbox, and returns send metadata. Target handler results are
observed through operation APIs when an operation id is used.

`IIntegrationEvent` is reserved for durable cross-module facts and fans out to
independently identified subscribers. Subscriber identity is durable and must
not be derived from handler CLR type names.

Domain events are module-local facts. They are distinct from integration
events and durable transport messages. Domain events are not automatically
staged in the outbox or dispatched; publishing integration events from domain
events is explicit module code.

## Durable Persistence

The outbox is the source-module ledger for outgoing durable commands and
integration events. Source state and outgoing envelopes commit atomically in
the source module transaction boundary.

The durable inbox is the receive ledger around durable ingestion, claim, retry,
processed, stale, and terminal receive failure state. Native transport
deliveries are parsed into durable envelopes, validated, inserted idempotently,
and settled only after durable ingestion succeeds.

EF Core plus PostgreSQL is the supported production durable persistence path.
Consumers own EF migrations. Bondstone packages provide mappings and provider
helpers, not package-shipped migrations or automatic schema rollout.

## Transport And Hosting

Transport adapters translate between provider-native messages and Bondstone
durable envelopes. Hosts own queues, topics, exchanges, subscriptions, rules,
bindings, credentials, connection lifecycle, worker placement, prefetch,
concurrency, native retry/dead-letter policy, native monitoring, and topology
provisioning.

Bondstone owns durable identities, serialization, binding validation, outbox
and inbox records, operation finalization semantics, and module handler
execution.

The durable worker/listener roles are:

1. Source outbox worker: claims source outbox rows, dispatches envelopes, and
   records outcomes.
2. Transport ingestion listener or worker: converts provider-native deliveries
   into durable inbox rows before native settlement.
3. Durable inbox processing worker: claims durable inbox rows, invokes module
   receive pipelines, and records outcomes.

Cleanup, retention, replay, purge, stale-row mutation, and broker dead-letter
movement remain application-owned unless explicitly accepted in future
architecture.

## Operation Observation

Operation observation answers what is known about accepted durable work. It is
not orchestration, saga state, process-manager state, or durable continuation
state. Wait helpers are convenience APIs for edge callers and tests. Caller
timeout does not write operation state.

## Diagnostics And Compatibility

Diagnostics should be OpenTelemetry-native where possible and avoid
high-cardinality values such as message ids, operation ids, exception messages,
payloads, broker delivery counts, topology details, and dead-letter state.

Public/protected API changes are compatibility-sensitive. Public API baselines
in `tests/Bondstone.PublicApi.Tests` guard packable packages. Refresh baselines
only after reviewing compatibility impact.

## Documentation Ownership

The SpecKit constitution owns governance. `docs/` owns durable architecture,
consumer-facing guidance, and repository-operation docs. GitHub Issues and
Projects own backlog work, real-project findings, cleanup tasks,
prioritization, and completion tracking. Feature specs under `specs/` are
change-scoped deltas and should be archived into the constitution or docs when
their durable knowledge must persist.
