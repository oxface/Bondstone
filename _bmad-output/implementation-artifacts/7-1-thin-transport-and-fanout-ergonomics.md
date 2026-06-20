---
baseline_commit: e85574a
---

# Story 7.1: Thin Transport And Fanout Ergonomics

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a host developer,
I want module-aware transport bindings without Bondstone owning topology,
so that native broker infrastructure remains app-owned.

## Acceptance Criteria

1. Given a transport adapter sends or receives, when it crosses the boundary, then it only translates native messages and Bondstone durable envelopes.
2. Given command dispatch is configured, when one durable envelope is routed, then it maps to exactly one outbound route.
3. Given integration event dispatch is configured, when subscribers exist, then fanout uses provider-native topology owned by the host.
4. Given queues, topics, exchanges, subscriptions, rules, credentials, retry, dead-letter policy, workers, or deployment topology are needed, when setup is reviewed, then the host owns them.
5. Given Bondstone durable semantics are inspected, when transport is involved, then Bondstone owns stable identities, module persistence boundaries, outbox rows, durable inbox rows, command handlers, subscriber handlers, and operation finalization.
6. Given `Bondstone.Transport.Local` is configured, when production guidance is reviewed, then it is explicit local/dev/test infrastructure and never a hidden fallback for missing broker configuration.
7. Given native deliveries are received, when durable ingestion fails, then the adapter does not acknowledge or complete the native delivery as processed.

## Tasks / Subtasks

- [x] Inventory the existing transport boundary before changing code. (AC: 1, 2, 3, 4, 5, 6, 7)
  - [x] Read `src/Bondstone.Transport.RabbitMq/README.md`, `src/Bondstone.Transport.ServiceBus/README.md`, `src/Bondstone.Transport.Local/README.md`, and the scoped package `AGENTS.md` files.
  - [x] Read `docs/setup.md`, `docs/operations.md`, `docs/package-discovery.md`, `docs/testing.md`, and relevant transport sections in `_bmad-output/planning-artifacts/architecture.md`.
  - [x] Read the existing dispatcher, receive-worker, option, topology, and route tests listed in "Files To Read Or Update" before editing.
  - [x] Confirm whether the current implementation already satisfies an AC; do not rewrite working transport code just to make the story look busy.
- [x] Preserve and tighten the thin adapter contract. (AC: 1, 4, 5)
  - [x] Keep RabbitMQ and Service Bus adapters as native-driver envelope adapters over `DurableMessageEnvelope`; do not add broker topology creation, subscription provisioning, retry orchestration, DLQ movement, credential ownership, monitoring ownership, or a generic bus facade.
  - [x] Keep custom/app-owned broker integration routed through `UseDurableEnvelopeDispatcher<TDispatcher>()`, `IDurableEnvelopeDispatcher`, `IDurableMessageEnvelopeSerializer`, and durable incoming inbox ingestion contracts.
  - [x] Ensure docs and package READMEs describe adapters as envelope plumbing that expects host-owned native broker infrastructure.
- [x] Validate exactly-one outbound route behavior. (AC: 2, 3)
  - [x] Reuse `RoutedDurableEnvelopeDispatcher` and `IDurableEnvelopeDispatchRoute` for any route-aware dispatch behavior; do not invent a second router.
  - [x] Ensure command records match exactly one outbound route and ambiguous/missing routes fail loudly with a stable, actionable message.
  - [x] Ensure integration events can fan out only through configured route/subscriber bindings and provider-native host topology; Bondstone must not create or infer broker subscriptions.
  - [x] If built-in RabbitMQ or Service Bus dispatcher docs imply multiple built-in dispatchers can be registered side-by-side, correct them to require one explicit aggregate dispatcher.
- [x] Preserve durable receive settlement ordering. (AC: 7)
  - [x] RabbitMQ receive must use manual acknowledgement and must not ack when deserialization, route/subscriber binding resolution, boundary resolution, ingestion, or save fails.
  - [x] Service Bus receive must require `AutoCompleteMessages = false` and `PeekLock`; it must complete messages only after durable incoming inbox ingestion commits.
  - [x] Duplicate `AlreadyIngested` deliveries may be settled because Bondstone already has durable receive evidence and no handler execution should occur during ingestion.
  - [x] Receive workers must ingest only into the durable incoming inbox; they must not run handlers, finalize operations, stage outgoing outbox rows, or mutate incoming inbox processing outcomes.
- [x] Keep `Bondstone.Transport.Local` explicit and non-production. (AC: 6)
  - [x] Preserve `UseLocalTransport(...)` as an opt-in sample/test/local-development route through provider-neutral receive pipelines.
  - [x] Preserve `UseModuleQueueConvention()` for local command routing and explicit queue subscriber bindings for events.
  - [x] Ensure docs do not present Local transport as a fallback for missing RabbitMQ or Service Bus configuration.
- [x] Fill only real test gaps. (AC: 1, 2, 3, 6, 7)
  - [x] Use `[Trait("Category", "Unit")]` or `[Trait("Category", "Application")]` for routing, option validation, missing/ambiguous route, Local transport, and receive-worker ordering tests that do not require real infrastructure.
  - [x] Use `[Trait("Category", "Integration")]` only when asserting real broker behavior that cannot be proven with current fakes/proxies.
  - [x] Prefer outcome/state assertions: route match count, thrown message, ack/nack/complete ordering, ingestion records, source transport name, receiver module, handler/subscriber identity, and absence of handler execution during ingestion.
- [x] Verify the story outcome.
  - [x] Run targeted transport tests first, for example `dotnet test tests/Bondstone.Transport.Local.Tests/Bondstone.Transport.Local.Tests.csproj --configuration Release --filter "Category=Unit|Category=Application"`.
  - [x] Run targeted RabbitMQ/Service Bus test projects if adapter code changes.
  - [x] Run `pnpm backend:test` after runtime or test changes.
  - [x] Run `pnpm backend:test:integration` if provider-backed broker/sample integration tests change.
  - [x] Run `pnpm backend:pack` and review public API baselines if public/protected transport APIs or package docs materially change.
  - [x] Run `pnpm check` as the final broad gate when code, public API, package metadata, samples, or broad docs change.

### Review Findings

- [x] [Review][Patch] Clarify Service Bus manual completion requirement [src/Bondstone.Transport.ServiceBus/README.md:14]

## Dev Notes

Story 7.1 is a boundary-sharpening story. Much of the desired shape already exists after Epic 6, especially durable incoming inbox ingestion before native settlement. Start by proving and documenting what is already true, then make narrow fixes for gaps.

### Current State Intelligence

Core transport seams:

- `IDurableEnvelopeDispatcher` publishes claimed `DurableOutboxRecord` values through the configured outbound transport.
- `IDurableEnvelopeDispatchRoute` plus `RoutedDurableEnvelopeDispatcher` already implement route-aware dispatch. `RoutedDurableEnvelopeDispatcher` requires exactly one matching route, throws when none match, and throws when multiple routes match.
- `IDurableMessageEnvelopeSerializer` is the durable envelope serialization boundary. Built-in adapters should serialize/deserialize `DurableMessageEnvelope`, not broker-specific DTOs.
- `IDurableEnvelopeReceiver` and `DurableEnvelopeReceiver` route provider-neutral envelopes into `IModuleCommandReceivePipeline` or `IModuleEventReceivePipeline`. Event receive requires explicit subscriber module and subscriber identity.
- `DurableIncomingInboxIngestionBoundary` is the durable receive-ingestion boundary. Built-in broker receive workers should resolve this boundary by receiver module and call `IngestAndSaveAsync(...)` before native settlement.

RabbitMQ current state:

- `RabbitMqEnvelopeDispatcher` publishes serialized envelope bytes to the `IChannel` using `RabbitMqEnvelopeDispatcherOptions.ResolveDestination`.
- `RabbitMqEnvelopeDispatcherOptions` requires a non-null exchange and non-empty routing key. It does not create exchanges, queues, bindings, or retry policy.
- `UseRabbitMqDispatcher(...)` currently replaces the singleton `IDurableEnvelopeDispatcher` and marks the outbox transport as `RabbitMq`.
- `RabbitMqReceiveWorker` consumes with `autoAck: false`, ingests into the durable incoming inbox in `DurableIncomingInboxIngestion` mode, acks only after ingestion succeeds, and nacks on failure using `RequeueOnFailure`.
- `RabbitMqReceiveWorkerOptions.ReceiveCommand()`, `ReceiveEvent(...)`, `IngestCommandToDurableIncomingInbox()`, and `IngestEventToDurableIncomingInbox(...)` all select durable incoming inbox ingestion mode. Event ingestion requires subscriber binding.
- `RabbitMqReceiveWorkerMode.DirectReceive` still exists internally for older direct-receive tests. Do not expand direct receive as the public production path; broker deliveries should enter the durable incoming inbox first.

Azure Service Bus current state:

- `ServiceBusEnvelopeDispatcher` serializes the durable envelope into `ServiceBusMessage.Body`, sets `MessageId`, `Subject`, `ContentType`, `CorrelationId`, and Bondstone application properties, then sends through a `ServiceBusSender`.
- `ServiceBusEnvelopeDispatcherOptions.ResolveEntityName` chooses the native queue/topic entity name; the host owns what that entity means and how it is provisioned.
- `UseServiceBusDispatcher(...)` currently replaces the singleton `IDurableEnvelopeDispatcher` and marks the outbox transport as `ServiceBus`.
- `ServiceBusReceiveWorkerOptions` defaults `AutoCompleteMessages` to false and rejects `AutoCompleteMessages = true` and `ReceiveAndDelete`.
- `ServiceBusReceiveWorker` creates queue or topic/subscription processors, ingests the message body into the durable incoming inbox, then calls `CompleteMessageAsync(...)`.
- On ingestion failure, `ProcessMessageAsync` throws and does not complete the message, allowing provider-native retry/error behavior.

Local transport current state:

- `Bondstone.Transport.Local` is explicit local/dev/test infrastructure, not a broker substitute.
- `LocalDurableEnvelopeDispatchRoute` sends command envelopes through `IDurableEnvelopeReceiver.ReceiveCommandAsync(...)` and event envelopes through `ReceiveEventAsync(...)` for each configured local event subscription.
- `LocalTransportTopology` supports explicit module queue bindings, `UseModuleQueueConvention()` for command queues, explicit event routes, and explicit event subscriber bindings.
- Local event fanout is in-process test/local fanout. It must not be described as production provider topology or broker durability.

Existing test coverage to preserve:

- `LocalDurableEnvelopeDispatcherTests` covers command binding, module queue convention, event fanout to multiple subscribers, missing queue binding, and startup validation with the local convention.
- `RabbitMqBuilderTests` covers dispatcher registration, missing queue validation, default nack-without-requeue, receive worker registration, ingestion mode, source transport naming, and event subscriber binding.
- `RabbitMqReceiveWorkerTests` covers nack on receiver failure, ack after receive, ingestion commit before ack, duplicate `AlreadyIngested` ack without handler execution, receiver module boundary selection, ingestion failure nack, command handler identity, event subscriber identity, and envelope field preservation.
- `ServiceBusBuilderTests` covers dispatcher registration, missing entity validation, manual completion defaults, rejection of auto-complete, rejection of receive-and-delete, processor option copying, and hosted worker registration.
- `ServiceBusReceiveWorkerTests` covers processor error logging, completion after ingestion save, no complete on ingestion failure, duplicate `AlreadyIngested` complete without handler execution, and event subscriber binding.

### Files To Read Or Update

Read these UPDATE files completely before changing them:

- `src/Bondstone.Persistence/Persistence/Contracts/IDurableEnvelopeDispatcher.cs` - outbound envelope dispatcher contract.
- `src/Bondstone.Persistence/Persistence/Contracts/IDurableEnvelopeDispatchRoute.cs` - route-level dispatch contract.
- `src/Bondstone.Persistence/Persistence/Outbox/RoutedDurableEnvelopeDispatcher.cs` - existing exactly-one route guardrail.
- `src/Bondstone/Messaging/Contracts/IDurableEnvelopeReceiver.cs` and `src/Bondstone/Messaging/Receiving/DurableEnvelopeReceiver.cs` - provider-neutral receive bridge.
- `src/Bondstone.Transport.RabbitMq/BondstoneRabbitMqBuilderExtensions.cs` - RabbitMQ setup registration and dispatcher replacement behavior.
- `src/Bondstone.Transport.RabbitMq/RabbitMqEnvelopeDispatcher.cs`, `RabbitMqEnvelopeDispatcherOptions.cs`, and `RabbitMqEnvelopeDestination.cs` - outbound native publish bridge.
- `src/Bondstone.Transport.RabbitMq/RabbitMqReceiveWorker.cs`, `RabbitMqReceiveWorkerOptions.cs`, and `RabbitMqReceiveWorkerRegistration.cs` - durable ingestion before ack.
- `src/Bondstone.Transport.ServiceBus/BondstoneServiceBusBuilderExtensions.cs` - Service Bus setup registration and dispatcher replacement behavior.
- `src/Bondstone.Transport.ServiceBus/ServiceBusEnvelopeDispatcher.cs` and `ServiceBusEnvelopeDispatcherOptions.cs` - outbound native send bridge.
- `src/Bondstone.Transport.ServiceBus/ServiceBusReceiveWorker.cs`, `ServiceBusReceiveWorkerOptions.cs`, and `ServiceBusReceiveWorkerRegistration.cs` - durable ingestion before complete.
- `src/Bondstone.Transport.Local/Outbox/LocalDurableEnvelopeDispatchRoute.cs` and `src/Bondstone.Transport.Local/Outbox/Topology/LocalTransportTopology.cs` - local explicit routing and fanout behavior.
- `docs/setup.md`, `docs/operations.md`, and `docs/package-discovery.md` - consumer-facing transport ownership, receive settlement, and package guidance.
- `src/Bondstone.Transport.RabbitMq/README.md`, `src/Bondstone.Transport.ServiceBus/README.md`, and `src/Bondstone.Transport.Local/README.md` - package-specific scope statements.

Likely tests to inspect or extend:

- `tests/Bondstone.Transport.Local.Tests/LocalDurableEnvelopeDispatcherTests.cs`
- `tests/Bondstone.Transport.Local.Tests/LocalTransportInboxPersistenceTests.cs`
- `tests/Bondstone.Transport.RabbitMq.Tests/RabbitMqBuilderTests.cs`
- `tests/Bondstone.Transport.RabbitMq.Tests/RabbitMqReceiveWorkerTests.cs`
- `tests/Bondstone.Transport.ServiceBus.Tests/ServiceBusBuilderTests.cs`
- `tests/Bondstone.Transport.ServiceBus.Tests/ServiceBusReceiveWorkerTests.cs`
- `tests/Bondstone.Samples.Tests/RabbitMqSampleFixture.cs`
- `tests/Bondstone.Samples.Tests/ServiceBusSampleFixture.cs`
- Public API baselines under `tests/Bondstone.PublicApi.Tests/Baselines/` if public/protected API changes are required.

### Architecture Compliance

Follow these non-negotiable rules:

- Bondstone is a durable module-boundary library, not a generic bus, broker topology manager, workflow engine, saga/process-manager framework, code generator, SaaS framework, application platform, or broker runtime owner.
- Commands, integration events, and domain events remain distinct. Do not create a generic message abstraction that erases command target-module semantics or event fanout semantics.
- Durable command dispatch maps one envelope to exactly one outbound route. If more than one transport can send it, fail and require explicit host routing.
- Integration event fanout is provider-native host topology. Bondstone owns stable event identity and subscriber identity, but the host owns topics, exchanges, queues, subscriptions, bindings, and rules.
- Native broker delivery must not be acknowledged or completed before durable incoming inbox ingestion succeeds.
- Transport adapters must not run module handlers directly in the native receive listener for production broker receive; they ingest first, then `Bondstone.Hosting` durable incoming inbox worker processes rows later.
- `Bondstone.Transport.Local` remains explicit local/dev/test infrastructure. It is not production broker durability and not a fallback if RabbitMQ or Service Bus are not configured.
- Broker retry, delivery counts, prefetch, concurrency, dead-letter policy, credentials, monitoring, connection lifecycle, and topology provisioning stay host-owned or native-driver-owned.
- Cleanup, retention, replay, purge, stale-row mutation, and broker DLQ movement remain application-owned.
- Public/protected API changes are compatibility-sensitive. Inventory affected setup APIs and update public API baselines only after review.
- Do not add `InternalsVisibleTo` for production package collaboration.

### Testing Requirements

Use the repository testing policy from `docs/testing.md`:

- Use `Unit` tests for option validation, route matching, serializer mapping, missing route, ambiguous route, and receive-worker ordering with fakes/proxies.
- Use `Application` tests when multiple in-process Bondstone components are composed without external infrastructure.
- Use `Integration` tests only for real RabbitMQ, Azure Service Bus, PostgreSQL, or sample smoke behavior.
- Test receive settlement by asserting ordering and outcome: no ack/complete before ingestion save starts and finishes; nack/no-complete on ingestion failure; ack/complete for `AlreadyIngested` duplicates without handler execution.
- Test fanout by asserting subscriber module and subscriber identity delivery, not just that a dispatch method was called.
- If changing setup APIs, run public API baseline tests and intentionally refresh baselines only after compatibility review.

High-value gap-fill candidates if the inventory finds a miss:

- A `RoutedDurableEnvelopeDispatcher` test for ambiguous command routes if existing coverage only proves missing routes.
- A docs/test assertion that registering multiple built-in outbound dispatchers does not imply Bondstone can infer transport ownership; hosts must use a single explicit aggregate when more than one outbound transport is needed.
- A Service Bus test that processor options preserve `PeekLock` and manual completion when copied into registration.
- A RabbitMQ test that `BasicAckAsync` is not called when route/subscriber binding resolution fails during durable incoming inbox ingestion.
- A package README/doc sweep that explicitly says Local transport is not a fallback and broker topology remains app-owned.

### Previous Story Intelligence

Story 6.3 completed EF/PostgreSQL production persistence and established:

- Start with an inventory; make runtime changes only for real gaps.
- Keep docs aligned with actual code after Story 6.2 Service Bus durable receive changes.
- Preserve application ownership of migrations, topology, retry, dead-letter policy, retention, and recovery.
- Use targeted tests first, then broader repo scripts.

Story 6.2 completed the durable receive transaction boundary and established:

- Native broker deliveries must be durably ingested before native settlement.
- RabbitMQ and Service Bus receive workers should not execute handlers during ingestion.
- `incoming_inbox_messages` is the operator-facing durable receive ledger; `inbox_messages` remains the direct module-processing idempotency detail.
- Duplicate ingestion can settle safely because durable receive evidence already exists.
- Terminal receive evidence is operational evidence, not automatic operation failure.

Story 6.1 completed source outbox atomicity and established:

- Source state and outgoing outbox rows must commit atomically.
- Terminal outbox evidence should be observable through the intended inspection surface.
- Provider-backed behavior belongs in integration tests only when real provider semantics matter.

### Git Intelligence

Recent commits at story creation time:

- `e85574a docs: postgres durability`
- `dd17279 fix: sb durable worker`
- `6355dc4 fix: sb worker`
- `23bda7f fix: atomicity test`
- `946886c fix: epic 5 done`

The recent pattern is narrow runtime corrections driven by tests, then docs alignment. Follow that pattern; avoid broad transport rewrites.

### Latest Technical Information

No dependency upgrade is required for this story. Use the repository-pinned stack in `Directory.Packages.props` and `Directory.Build.props`:

- Target framework `net10.0`; nullable enabled; warnings as errors; central package management.
- `RabbitMQ.Client` `7.2.1`.
- `Azure.Messaging.ServiceBus` `7.20.1`.
- Testcontainers packages `4.12.0`.
- xUnit `2.9.3`.

Current official docs align with the story direction:

- RabbitMQ .NET client 7.x uses `IChannel`; the official API guide says `IChannel` must not be shared for concurrent publishing without mutual exclusion and shows manual acknowledgement with `BasicAckAsync` after processing. This supports keeping channel lifecycle/concurrency host-owned and preserving explicit ack-after-ingest behavior. Source: https://www.rabbitmq.com/client-libraries/dotnet-api-guide
- NuGet lists `RabbitMQ.Client` `7.2.1` as the official RabbitMQ .NET client package and includes `net8.0` and `.NETStandard 2.0` assets, which is compatible with Bondstone's `net10.0` target. Source: https://www.nuget.org/packages/RabbitMQ.Client/
- Microsoft's Azure Service Bus .NET docs describe `ServiceBusProcessor` as a queue/subscription-scoped callback processor with automatic completion available. Bondstone's worker must keep automatic completion disabled so it can complete only after durable ingestion. Source: https://learn.microsoft.com/en-us/dotnet/api/overview/azure/messaging.servicebus-readme
- `CompleteMessageAsync` can only be performed for messages received in `PeekLock` mode. This supports the existing `ServiceBusReceiveWorkerOptions` guard against `ReceiveAndDelete`. Source: https://learn.microsoft.com/en-us/dotnet/api/azure.messaging.servicebus.servicebusreceiver.completemessageasync

### Project Context Reference

Project context says Bondstone is a .NET library/framework for durable module boundaries, durable command sending, EF Core backed inbox/outbox persistence, operation observation, and transport adapters. Transport adapters are thin native-driver envelope adapters. Broker topology, provisioning, retries, dead-letter policy, prefetch/concurrency, credentials, and monitoring remain host-owned. Native broker delivery must not be acknowledged or completed before durable inbox ingestion succeeds. Local transport is explicit local/dev/test infrastructure only and not production broker durability or a hidden fallback.

### Open Questions

None blocking. If implementation discovers that multi-transport outbound routing needs a public setup helper beyond `RoutedDurableEnvelopeDispatcher`, inventory public API impact first and prefer documentation or explicit host composition over new API.

### Story Completion Status

Ultimate context engine analysis completed - comprehensive developer guide created.

### References

- `_bmad-output/planning-artifacts/epics.md` - Epic 7 and Story 7.1 acceptance criteria
- `_bmad-output/planning-artifacts/prds/prd-Bondstone-2026-06-18/prd.md` - FR3.4, FR5.4, FR7.1-FR7.5, FR8.1
- `_bmad-output/planning-artifacts/architecture.md` - Transport Boundary, Durable Inbox, Receive Pipeline, Hosting And Workers, Verification Strategy
- `_bmad-output/project-context.md` - runtime, code, testing, and workflow guardrails
- `_bmad-output/implementation-artifacts/6-1-source-outbox-atomicity.md` - source outbox proof patterns
- `_bmad-output/implementation-artifacts/6-2-durable-receive-transaction-boundary.md` - durable receive and settlement patterns
- `_bmad-output/implementation-artifacts/6-3-ef-postgresql-production-persistence-and-migrations.md` - docs/code inventory and verification pattern
- `docs/setup.md` - consumer setup, transport package use, and receive-worker examples
- `docs/operations.md` - production ownership, receive semantics, broker settlement
- `docs/package-discovery.md` - package/capability matrix and broker integration guidance
- `docs/testing.md` - transport and integration-test category policy
- `src/Bondstone.Persistence/Persistence/Outbox/RoutedDurableEnvelopeDispatcher.cs` - existing exactly-one route guardrail
- `src/Bondstone.Transport.RabbitMq/RabbitMqReceiveWorker.cs` - durable inbox ingestion before ack
- `src/Bondstone.Transport.ServiceBus/ServiceBusReceiveWorker.cs` - durable inbox ingestion before complete
- `src/Bondstone.Transport.Local/Outbox/LocalDurableEnvelopeDispatchRoute.cs` - local command/event receive bridge
- `tests/Bondstone.Transport.Local.Tests/LocalDurableEnvelopeDispatcherTests.cs` - local routing and fanout tests
- `tests/Bondstone.Transport.RabbitMq.Tests/RabbitMqReceiveWorkerTests.cs` - RabbitMQ settlement and ingestion tests
- `tests/Bondstone.Transport.ServiceBus.Tests/ServiceBusReceiveWorkerTests.cs` - Service Bus settlement and ingestion tests
- RabbitMQ .NET client guide: https://www.rabbitmq.com/client-libraries/dotnet-api-guide
- NuGet RabbitMQ.Client package: https://www.nuget.org/packages/RabbitMQ.Client/
- Azure Service Bus .NET overview: https://learn.microsoft.com/en-us/dotnet/api/overview/azure/messaging.servicebus-readme
- Azure Service Bus `CompleteMessageAsync`: https://learn.microsoft.com/en-us/dotnet/api/azure.messaging.servicebus.servicebusreceiver.completemessageasync

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- 2026-06-20: Resolved workflow customization manually after `python3 _bmad/scripts/resolve_customization.py --skill .agents/skills/bmad-create-story --key workflow` failed because the environment could not import Python's `json` module.
- 2026-06-20: Loaded root/project context, PRD, architecture, epics, sprint status, transport package docs, test guidance, and current transport source/test files before creating the story.
- 2026-06-20: Confirmed `7-1-thin-transport-and-fanout-ergonomics` was backlog and Epic 7 was backlog in sprint status before final update.
- 2026-06-20: Resolved `bmad-dev-story` workflow customization manually after `python3 _bmad/scripts/resolve_customization.py --skill .agents/skills/bmad-dev-story --key workflow` failed because the environment could not import Python's `json` module.
- 2026-06-20: Inventoried transport READMEs, scoped AGENTS files, setup/operations/package-discovery/testing docs, architecture transport sections, and transport dispatcher/receive worker/source/test files before editing.
- 2026-06-20: Added a failing missing-route test for `RoutedDurableEnvelopeDispatcher`, then tightened the missing-route message to require exactly-one adapter ownership.
- 2026-06-20: Ran targeted Local, RabbitMQ, Service Bus, and core fast tests, then `pnpm backend:test`, `pnpm backend:pack`, and final `pnpm check`.

### Completion Notes List

- Preserved the existing thin RabbitMQ, Service Bus, and Local transport implementations after inventory showed they already keep topology, retry, dead-letter policy, credentials, subscriptions, and monitoring host-owned.
- Tightened `RoutedDurableEnvelopeDispatcher` missing-route failures so missing and ambiguous route cases both point hosts back to exactly-one adapter ownership.
- Added unit coverage for the missing-route actionable message without introducing a second router or changing public API.
- Updated the Service Bus package README to state manual `PeekLock` completion after durable incoming inbox ingestion and later processing by the `Bondstone.Hosting` incoming inbox worker.
- Verified no provider-backed integration changes were needed; existing fake/proxy tests cover receive settlement ordering and handler non-execution during ingestion.

### File List

- `_bmad-output/implementation-artifacts/7-1-thin-transport-and-fanout-ergonomics.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `src/Bondstone.Persistence/Persistence/Outbox/RoutedDurableEnvelopeDispatcher.cs`
- `src/Bondstone.Transport.ServiceBus/README.md`
- `tests/Bondstone.Tests/Persistence/RoutedDurableEnvelopeDispatcherTests.cs`

### Change Log

- 2026-06-20: Implemented Story 7.1 thin transport/fanout ergonomics guardrails; added missing-route test/message, aligned Service Bus README receive settlement wording, verified targeted and broad gates.
