---
baseline_commit: 4bbd7f6
---

# Story 5.4: Durable Message Kind And Domain Event Boundaries

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As an application developer,
I want commands, integration events, and domain events to stay distinct,
so that durable behavior remains explicit and restart-safe.

## Acceptance Criteria

1. Given a durable command is sent, when it is serialized, then it carries a command identity and target module rather than an event fanout contract.
2. Given an integration event is published, when it is dispatched, then it has no single target module and can fan out to zero or more stable subscribers.
3. Given a domain event is raised, when EF-backed domain-event persistence is enabled, then it remains module-local and is not automatically staged as an outbox integration event.
4. Given a domain event should become public, when integration publication is required, then explicit module code maps it to an integration event.
5. Given a story or API proposes a generic bus abstraction, when reviewed, then it is rejected unless PRD and architecture are updated first.

## Tasks / Subtasks

- [x] Inventory current durable message-kind and domain-event behavior before changing code. (AC: 1, 2, 3, 4, 5)
  - [x] Review `DurableMessageEnvelope`, `MessageKind`, `MessageTypeRegistry`, message identity attributes, command send, event publish, command receive, event receive, and EF domain-event persistence.
  - [x] Confirm existing tests already cover command target-module requirements, event no-target requirements, command/event identity mismatch failures, subscriber identity metadata, and domain-event marker isolation.
  - [x] Record in completion notes whether implementation only adds/strengthens tests or also changes runtime behavior.
- [x] Preserve command envelopes as single-target durable work. (AC: 1)
  - [x] Keep `IDurableCommand` distinct from `IIntegrationEvent`; do not introduce a common public "bus message" or generic durable-message abstraction for application contracts.
  - [x] Keep `DurableCommandSender` creating `DurableMessageEnvelope` with `MessageKind.Command`, source module from current execution context, required `TargetModule`, stable command message identity, and accepted-work metadata.
  - [x] Preserve `DurableMessageEnvelope` validation that commands require `TargetModule`.
  - [x] Preserve receive-side command validation that command receive accepts `MessageKind.Command` only and resolves a command registration whose `Kind` is `Command`.
- [x] Preserve integration events as durable facts with subscriber fanout. (AC: 2)
  - [x] Keep `IDurableEventPublisher` creating `DurableMessageEnvelope` with `MessageKind.Event`, source module from current execution context, no `TargetModule`, stable integration-event identity, and publish metadata.
  - [x] Preserve `DurableMessageEnvelope` validation that events must not specify `TargetModule`.
  - [x] Keep published-event registration module-owned and subscriber registration consumer-owned with explicit stable `SubscriberIdentity`.
  - [x] Preserve receive-side event validation that event receive accepts `MessageKind.Event` only and uses subscriber module plus subscriber identity binding.
- [x] Preserve domain events as module-local facts. (AC: 3, 4)
  - [x] Keep `IDomainEvent` outside `IMessage`, `IDurableCommand`, and `IIntegrationEvent`.
  - [x] Keep `DomainEventIdentityAttribute` separate from durable command and integration-event identity attributes.
  - [x] Keep EF domain-event persistence opt-in via `UseEntityFrameworkCoreDomainEventPersistence()` and mapped via `ApplyBondstoneDomainEvents()`.
  - [x] Keep EF collection writing `DomainEventRecordEntity` records in the module persistence boundary and clearing pending domain events only after the observed transaction commits.
  - [x] Add or strengthen coverage proving EF-backed domain-event persistence does not write an outbox integration-event envelope unless handler/subscriber code explicitly calls `IDurableEventPublisher`.
  - [x] If explicit publication is demonstrated in tests or docs, model it as application/module code that maps a domain event to a separate `IIntegrationEvent`; do not add automatic dispatch or automatic domain-to-integration publication.
- [x] Add focused regression tests. (AC: 1, 2, 3, 4, 5)
  - [x] Use `tests/Bondstone.Tests` `Unit` tests for message identity and envelope boundary rules.
  - [x] Use `tests/Bondstone.Tests` `Unit` tests for event subscriber identity and receive binding behavior if existing coverage is incomplete.
  - [x] Use `tests/Bondstone.Persistence.EntityFrameworkCore.Tests` `Application` tests for EF domain-event persistence isolation and no automatic outbox write.
  - [x] Preserve existing `tests/Bondstone.Samples.Tests` expectations that domain-event records and outbox rows are different evidence surfaces.
  - [x] If any public/protected surface changes, update `docs/public-api.md` classification and public API baselines intentionally.
- [x] Update docs only where implementation changes user-facing guidance. (AC: 3, 4, 5)
  - [x] Update `docs/setup.md`, `docs/package-discovery.md`, `docs/operations.md`, or package READMEs only if the story changes setup, package discovery, or consumer guidance.
  - [x] Keep docs clear that domain events are local facts and public integration events require explicit module code.
  - [x] Do not duplicate BMAD architecture rules into consumer docs; link or summarize only what consumers need.
- [x] Verify the story outcome.
  - [x] Run targeted tests first, for example `dotnet test tests/Bondstone.Tests/Bondstone.Tests.csproj --configuration Release --filter "FullyQualifiedName~DurableMessageEnvelopeTests|FullyQualifiedName~MessageTypeRegistryTests|FullyQualifiedName~DurableEventPublisherTests|FullyQualifiedName~ModuleEventRegistrationTests|FullyQualifiedName~ModuleReceivePipelineTests"`.
  - [x] Run EF domain-event tests, for example `dotnet test tests/Bondstone.Persistence.EntityFrameworkCore.Tests/Bondstone.Persistence.EntityFrameworkCore.Tests.csproj --configuration Release --filter "FullyQualifiedName~EntityFrameworkCoreDomainEventPersistenceTests"`.
  - [x] Run `pnpm backend:test` after runtime changes.
  - [x] Run `pnpm backend:pack` and public API baseline verification if public/protected package surface changes.
  - [x] Run `pnpm check` as the final broad gate when code, public API, package metadata, samples, or broad docs change.

### Review Findings

- [x] [Review][Patch] Add focused story-local AC4 evidence before completion — Added focused EF application coverage demonstrating explicit module code maps local domain-event state to a separate integration event through `IDurableEventPublisher`, while the domain-event record remains distinct from the outbox event.

## Dev Notes

Story 5.4 is a boundary-preservation story. The current implementation already has command/event/domain-event separation in several places, so start by strengthening behavioral evidence and only change runtime code where a test exposes a real gap. Do not rework persistence, transport, hosting, or operation observation as part of this story.

### Current State Intelligence

Durable message kind separation already exists:

- `src/Bondstone.Persistence/Messaging/Identity/MessageKind.cs` defines only `Command` and `Event`.
- `src/Bondstone.Persistence/Messaging/Identity/DurableMessageEnvelope.cs` requires commands to have `TargetModule` and rejects target modules for events.
- `src/Bondstone/Messaging/Identity/MessageTypeRegistry.cs` registers durable commands as `MessageKind.Command`, integration events as `MessageKind.Event`, and rejects types implementing both `IDurableCommand` and `IIntegrationEvent`.
- `src/Bondstone/Messaging/Identity/MessageIdentityAttributes.cs` keeps `DurableCommandIdentityAttribute` and `IntegrationEventIdentityAttribute` separate and rejects using the wrong attribute for each kind.
- `src/Bondstone/Messaging/Sending/DurableCommandSender.cs` stages command envelopes with a target module and returns `DurableCommandSendResult`, not target handler results.
- `src/Bondstone/Messaging/Publishing/DurableEventPublisher.cs` requires a source module execution context, checks the source module registered the published event, stages event envelopes with `targetModule: null`, and returns `DurableEventPublishResult`.
- `src/Bondstone/Messaging/Receiving/DurableEnvelopeReceiver.cs` routes by `MessageKind`: command receive uses the command pipeline; event receive requires `DurableEnvelopeReceiveBinding` with subscriber module and subscriber identity.
- `src/Bondstone/Modules/Execution/ModuleCommandReceivePipeline.cs` accepts command envelopes only and validates the resolved message registration kind is `Command`.
- `src/Bondstone/Modules/Execution/ModuleEventReceivePipeline.cs` accepts event envelopes only and validates the resolved message registration kind is `Event`.

Event subscriber fanout metadata already exists:

- `src/Bondstone/Modules/Registration/BondstoneModuleEventBuilder.cs` registers published integration events and subscriber handlers under module-owned configuration.
- `src/Bondstone/Modules/Events/ModuleEventSubscriberRegistration.cs` requires `IIntegrationEvent`, requires a `MessageKind.Event` registration, stores `ModuleName`, `MessageTypeName`, `HandlerType`, and explicit `SubscriberIdentity`.
- `src/Bondstone/Modules/Events/ModuleEventSubscriberRegistry.cs` keys subscribers by subscriber module, message type, and subscriber identity. It does not derive subscriber identity from handler CLR type names.

Domain-event isolation already exists:

- `src/Bondstone/DomainEvents/IDomainEvent.cs` is a marker for module-local facts and does not implement `IMessage`.
- `src/Bondstone/DomainEvents/DomainEventIdentityAttribute.cs` is separate from durable message identity attributes.
- `src/Bondstone/DomainEvents/IDomainEventSource.cs` exposes pending local domain events plus explicit clearing.
- `src/Bondstone.Persistence.EntityFrameworkCore/Persistence/BondstoneEntityFrameworkCoreDomainEventModuleBuilderExtensions.cs` makes EF domain-event persistence an opt-in module behavior and requires EF Core module persistence first.
- `src/Bondstone.Persistence.EntityFrameworkCore/DomainEvents/EntityFrameworkCoreDomainEventModuleBehavior.cs` collects domain events as a post-handler action, writes records through the module `DbContext`, validates `ApplyBondstoneDomainEvents()`, and clears pending source events only through `OnTransactionCommitted` when the execution context observes transaction outcome.
- `src/Bondstone.Persistence.EntityFrameworkCore/DomainEvents/EntityFrameworkCoreDomainEventCollector.cs` serializes pending domain events into `DomainEventRecordEntity`; it does not call `IDurableEventPublisher` or `IDurableOutboxWriter`.
- `src/Bondstone.Persistence.EntityFrameworkCore/DomainEvents/DomainEventRecordEntity.cs` stores module-local domain-event records. It is not a durable outbox row and does not carry `MessageKind`.

Likely current coverage:

- `tests/Bondstone.Tests/Messaging/DurableMessageEnvelopeTests.cs` covers command target-module requirement and event no-target requirement.
- `tests/Bondstone.Tests/Messaging/MessageTypeRegistryTests.cs` covers command/event kind registration, wrong identity attributes, assembly registration with explicit identities, and ambiguous command+event types.
- `tests/Bondstone.Tests/Messaging/DurableCommandSenderTests.cs` covers command send staging with current module as source and target module as target.
- `tests/Bondstone.Tests/Messaging/DurableEventPublisherTests.cs` covers event publish staging with no target module and missing published-event registration failure.
- `tests/Bondstone.Tests/Modules/ModuleEventRegistrationTests.cs` covers subscriber identity metadata.
- `tests/Bondstone.Tests/Modules/ModuleEventSubscriberExecutionTests.cs` and `ModuleReceivePipelineTests.cs` cover subscriber inbox identity and event receive execution.
- `tests/Bondstone.Tests/DomainEvents/DomainEventContractTests.cs` covers `IDomainEvent` not being `IMessage`, `IDurableCommand`, or `IIntegrationEvent`, and proves the message registry does not treat domain events as durable messages.
- `tests/Bondstone.Persistence.EntityFrameworkCore.Tests/DomainEvents/EntityFrameworkCoreDomainEventPersistenceTests.cs` covers opt-in EF domain-event persistence, no collection when not opted in, missing mapping diagnostics, failure behavior, and subscriber-raised domain events.

### Architecture Compliance

Follow these non-negotiable rules:

- Bondstone remains a durable module-boundary library/framework, not a generic bus, workflow engine, saga/process-manager framework, code generator, SaaS framework, application platform, broker topology manager, or broker runtime owner.
- Commands, integration events, and domain events are distinct. Do not collapse them into one abstraction or a generic public bus API.
- Durable command send accepts work and returns metadata. Results are observed through operation APIs, not returned from send.
- Integration events are durable cross-module facts. They target no single module and may fan out to zero or more stable subscribers through host-owned topology.
- Domain events are module-local facts. They are not integration events, transport messages, outbox rows, or automatically published public events.
- Event-driven orchestration composes explicit commands and integration events. It does not erase the command/event distinction.
- Durable message identities, handler identities, subscriber identities, and domain-event identities must be stable and explicit. Do not derive durable identities from CLR type names.
- Transport adapters remain thin native-driver envelope adapters. Fanout/topology, broker retry, dead-letter, credentials, and monitoring remain host-owned.
- Public/protected API changes are compatibility-sensitive. Inventory and update baselines only after reviewing the surface.
- Do not add production `InternalsVisibleTo`; runtime packages collaborate through explicit contracts or package-local implementation.

### Files To Read Or Update

Read these UPDATE files before modifying behavior:

- `src/Bondstone.Persistence/Messaging/Identity/DurableMessageEnvelope.cs` - central envelope validation for command target module and event no-target shape.
- `src/Bondstone.Persistence/Messaging/Identity/MessageKind.cs` - durable envelope kind enum; do not add a domain-event kind for this story.
- `src/Bondstone/Messaging/Identity/MessageTypeRegistry.cs` and `MessageIdentityAttributes.cs` - stable durable identity registration and command/event kind validation.
- `src/Bondstone/Messaging/Sending/DurableCommandSender.cs` - command outbox staging and accepted-work metadata.
- `src/Bondstone/Messaging/Publishing/DurableEventPublisher.cs` - integration event outbox staging and published-event registration validation.
- `src/Bondstone/Messaging/Receiving/DurableEnvelopeReceiver.cs` - command vs event receive routing and binding requirements.
- `src/Bondstone/Modules/Execution/ModuleCommandReceivePipeline.cs` and `ModuleEventReceivePipeline.cs` - kind-specific receive validation and inbox keys.
- `src/Bondstone/Modules/Registration/BondstoneModuleEventBuilder.cs`, `src/Bondstone/Modules/Events/ModuleEventSubscriberRegistration.cs`, and `ModuleEventSubscriberRegistry.cs` - published-event and subscriber registration behavior.
- `src/Bondstone/DomainEvents/IDomainEvent.cs`, `DomainEventIdentityAttribute.cs`, and `IDomainEventSource.cs` - module-local domain-event contracts.
- `src/Bondstone.Persistence.EntityFrameworkCore/Persistence/BondstoneEntityFrameworkCoreDomainEventModuleBuilderExtensions.cs` - EF domain-event persistence opt-in.
- `src/Bondstone.Persistence.EntityFrameworkCore/DomainEvents/EntityFrameworkCoreDomainEventCollector.cs`, `EntityFrameworkCoreDomainEventModuleBehavior.cs`, and `DomainEventRecordEntity.cs` - domain-event collection, persistence, and clearing behavior.
- `src/Bondstone.Persistence.EntityFrameworkCore/Outbox/OutboxMessageEntity.cs` and `src/Bondstone.Persistence/Persistence/Outbox/DurableOutboxRecord.cs` - outbox row mapping for command/event envelopes; keep distinct from domain-event records.

Likely test files:

- `tests/Bondstone.Tests/Messaging/DurableMessageEnvelopeTests.cs`
- `tests/Bondstone.Tests/Messaging/MessageTypeRegistryTests.cs`
- `tests/Bondstone.Tests/Messaging/DurableCommandSenderTests.cs`
- `tests/Bondstone.Tests/Messaging/DurableEventPublisherTests.cs`
- `tests/Bondstone.Tests/Modules/ModuleEventRegistrationTests.cs`
- `tests/Bondstone.Tests/Modules/ModuleEventSubscriberExecutionTests.cs`
- `tests/Bondstone.Tests/Modules/ModuleReceivePipelineTests.cs`
- `tests/Bondstone.Tests/DomainEvents/DomainEventContractTests.cs`
- `tests/Bondstone.Persistence.EntityFrameworkCore.Tests/DomainEvents/EntityFrameworkCoreDomainEventPersistenceTests.cs`
- `tests/Bondstone.Persistence.EntityFrameworkCore.Tests/Outbox/OutboxMessageEntityTests.cs`
- `tests/Bondstone.Samples.Tests/ModularMonolithSampleTests.cs` and `ModularMonolithBrokerAdapterSampleTests.cs` only if sample evidence is affected.

Do not edit unrelated transport adapter topology, hosting workers, durable incoming inbox processing, package metadata, samples, or generated artifacts unless implementation proves they are directly affected.

### Testing Requirements

Use the repository testing policy from `docs/testing.md`:

- `Unit` tests for pure contract and envelope rules, message registry behavior, route/receive kind validation, subscriber identity metadata, and domain-event marker behavior.
- `Application` tests for EF domain-event persistence behavior using in-repository test fixtures and EF InMemory only for change-tracker/mapping/no-write boundaries.
- `Integration` tests only if behavior depends on PostgreSQL transactions, uniqueness, locking, retry, broker settlement, or real provider semantics. EF InMemory is not proof of relational durability.
- `Package` tests only if public/protected APIs change and package artifacts are inspected.

High-value tests to add or preserve:

- Command envelopes round-trip through EF outbox mapping with `MessageKind.Command` and required `TargetModule`.
- Event envelopes round-trip through EF outbox mapping with `MessageKind.Event` and null `TargetModule`.
- `DurableEnvelopeReceiver.ReceiveAsync` rejects event envelopes without subscriber binding and rejects command/event kind mismatches.
- Event subscriber registration preserves explicit `SubscriberIdentity` independent of handler CLR type.
- Domain events cannot be registered in `MessageTypeRegistry` and do not implement durable message contracts.
- EF domain-event persistence records `DomainEventRecordEntity` but does not stage any `OutboxMessageEntity` when no explicit `IDurableEventPublisher.PublishAsync` call happens.
- Explicit domain-to-integration publication, if covered, is implemented as handler code that maps local domain event state to a separate `IIntegrationEvent` and calls `IDurableEventPublisher`.

### Previous Story Intelligence

Story 5.3 completed immediate command and query boundaries in commit `4bbd7f6 fix: added query capability`.

Carry these learnings forward:

- Query execution deliberately does not set the command/subscriber module execution context. Keep durable send/publish requiring command or subscriber execution context; do not let query handlers become a durable write path.
- `IModuleCommandExecutor` remains the immediate command boundary and is not a generic mediator.
- Durable command send returns `DurableCommandSendResult` metadata, including optional operation handle, and does not return target handler results.
- Cross-module immediate state-changing command execution from inside a handler remains guarded; restart-safe cross-module state changes use durable commands or integration events.
- Stable diagnostic assertions should use durable substrings rather than brittle full exception messages.
- Public query contracts were added and classified in `docs/public-api.md`; any new public surface in this story needs the same compatibility treatment.

Story 5.2 established module-owned durable messaging registration:

- Modules own durable messaging capability, persistence binding, command handlers, published integration events, and subscriber handlers.
- Remote contracts remain identity-only through `BondstoneBuilder.RegisterMessage<TMessage>()` and assembly registration. Identity-only registration must not imply a local route, handler, subscriber, module binding, or receive binding.
- No story should regress explicit durable identity handling or make CLR names durable identities.

Story 5.1 established the product-boundary evidence pattern:

- Runtime changes must name how they preserve durable module boundaries, service-extraction continuity, stable message/handler/subscriber identities, inbox/outbox semantics, operation observation, module-owned durability, and host-owned transport topology.
- Reject scope expansion into a generic bus, workflow engine, code generator, SaaS framework, application platform, or broker runtime owner unless PRD and architecture are updated first.

### Git Intelligence

Recent commits:

- `4bbd7f6 fix: added query capability`
- `ecd150a fix: durable messages registration`
- `9dd9da7 docs: readme and agents doc refine`
- `9bc4667 docs: metadata fix`
- `bb2c102 docs: bmad native docs`

The current branch appears clean before this story file is created. The latest runtime commit is directly relevant: build on its query read-only boundary and avoid broad runtime churn.

### Latest Technical Information

External research does not require a dependency upgrade for this story. Use the repository-pinned stack from project context: `net10.0`, .NET SDK `10.0.108`, EF Core `10.0.8`, Npgsql `10.0.3`, and `Npgsql.EntityFrameworkCore.PostgreSQL` `10.0.2`.

Current official guidance relevant to this story:

- Microsoft DI guidance continues to center setup around registering services in `IServiceCollection` and resolving them from `IServiceProvider`. Keep any new runtime/service registration inside the existing `AddBondstone` and module-owned builder model.
- ASP.NET Core DI docs continue to show grouped `Add{GroupName}` extension methods for related framework features. Keep composition consistent with `AddBondstone`, `UseEntityFrameworkCorePersistence`, and `UseEntityFrameworkCoreDomainEventPersistence`.
- EF Core interceptors can modify or suppress EF operations, but this story should not introduce interceptor-based automatic domain-event dispatch. Existing module transaction/post-handler behavior is narrower and matches the architecture.
- Microsoft domain-event guidance discusses dispatch timing tradeoffs, but Bondstone architecture deliberately chooses module-local persistence and explicit integration publication, not automatic domain-to-integration publication.

Sources:

- https://learn.microsoft.com/en-us/dotnet/core/extensions/dependency-injection/overview
- https://learn.microsoft.com/en-us/aspnet/core/fundamentals/dependency-injection?view=aspnetcore-10.0
- https://learn.microsoft.com/en-us/ef/core/logging-events-diagnostics/interceptors
- https://learn.microsoft.com/en-us/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/domain-events-design-implementation

### Project Context Reference

Project context says Bondstone is a library/framework for durable module boundaries, not a product app, SaaS framework, workflow engine, or general-purpose bus. It also says commands, integration events, and domain events are distinct; durable command send returns metadata while results are observed through operation APIs; durable identities are stable and explicit; `IDurableCommandSender` and `IDurableEventPublisher` require current module execution context; transport adapters are thin native-driver envelope adapters; and runtime packages collaborate through explicit contracts or package-local implementation rather than production `InternalsVisibleTo`.

### Open Questions

None blocking. One design choice should be explicit during implementation: whether the story is a pure regression-test slice or whether any discovered runtime gap needs a narrow code change. Prefer tests-only if current behavior already satisfies the acceptance criteria.

### Story Completion Status

Ultimate context engine analysis completed - comprehensive developer guide created.

### References

- `_bmad-output/planning-artifacts/epics.md` - Epic 5 and Story 5.4 acceptance criteria
- `_bmad-output/planning-artifacts/prds/prd-Bondstone-2026-06-18/prd.md` - FR3.2, FR4.4, FR5.1, FR5.5, FR9, FR10, and NFR3
- `_bmad-output/planning-artifacts/architecture.md` - Durable Commands, Integration Events, Domain Events, Durable Outbox, Durable Inbox, Receive Pipeline, Public API And Compatibility, Verification Strategy
- `_bmad-output/project-context.md` - runtime, code, testing, and workflow guardrails
- `docs/testing.md` - test categories and verification surface
- `docs/public-api.md` - public API classification and recent query-boundary note
- `_bmad-output/implementation-artifacts/5-1-product-boundary-and-extraction-guardrail.md` - product-boundary evidence pattern
- `_bmad-output/implementation-artifacts/5-2-module-registration-and-host-composition.md` - module-owned durable registration learnings
- `_bmad-output/implementation-artifacts/5-3-immediate-command-and-query-boundaries.md` - previous story implementation notes and query boundary learnings

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- 2026-06-19: Inventory confirmed runtime already preserves command/event/domain-event separation; no runtime behavior changes were required.
- 2026-06-19: `dotnet test tests/Bondstone.Persistence.EntityFrameworkCore.Tests/Bondstone.Persistence.EntityFrameworkCore.Tests.csproj --configuration Release --filter "FullyQualifiedName~EntityFrameworkCoreDomainEventPersistenceTests.ModuleCommands_WhenEfBackedDurableModuleOptsIn_DoesNotStageOutboxForDomainEvents"` passed.
- 2026-06-19: `dotnet test tests/Bondstone.Persistence.EntityFrameworkCore.Tests/Bondstone.Persistence.EntityFrameworkCore.Tests.csproj --configuration Release --filter "FullyQualifiedName~EntityFrameworkCoreDomainEventPersistenceTests|FullyQualifiedName~OutboxMessageEntityTests"` passed.
- 2026-06-19: `dotnet test tests/Bondstone.Tests/Bondstone.Tests.csproj --configuration Release --filter "FullyQualifiedName~DurableMessageEnvelopeTests|FullyQualifiedName~MessageTypeRegistryTests|FullyQualifiedName~DurableCommandSenderTests|FullyQualifiedName~DurableEventPublisherTests|FullyQualifiedName~ModuleEventRegistrationTests|FullyQualifiedName~ModuleReceivePipelineTests|FullyQualifiedName~DurableEnvelopeReceiverTests|FullyQualifiedName~DomainEventContractTests"` passed.
- 2026-06-19: `pnpm backend:test` passed.
- 2026-06-19: `pnpm check` passed.
- 2026-06-19: Code review patch added AC4 story-local evidence in `ModuleCommands_WhenDomainEventIsExplicitlyPublishedAsIntegrationEvent_StagesSeparateOutboxEvent`.
- 2026-06-19: `dotnet test tests/Bondstone.Persistence.EntityFrameworkCore.Tests/Bondstone.Persistence.EntityFrameworkCore.Tests.csproj --configuration Release --filter "FullyQualifiedName~EntityFrameworkCoreDomainEventPersistenceTests|FullyQualifiedName~OutboxMessageEntityTests"` passed after review patch.
- 2026-06-19: `pnpm backend:test` passed after review patch.

### Completion Notes List

- Implementation strengthened regression evidence only; no runtime behavior, public API, package metadata, samples, or consumer docs changed.
- Added EF application coverage proving an EF-backed durable module that raises a domain event writes `DomainEventRecordEntity` but does not stage any `OutboxMessageEntity` unless explicit publisher code is invoked.
- Added EF application coverage proving explicit module code can map local domain-event state to a separate `IIntegrationEvent` and stage it through `IDurableEventPublisher` without collapsing domain events into durable messages.
- Added EF outbox mapping coverage proving integration event envelopes round-trip with `MessageKind.Event` and no `TargetModule`.
- Inventory confirmed existing tests cover command target-module requirements, event no-target requirements, command/event identity mismatch failures, subscriber identity metadata, receive binding behavior, and domain-event marker isolation.

### File List

- `_bmad-output/implementation-artifacts/5-4-durable-message-kind-and-domain-event-boundaries.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `tests/Bondstone.Persistence.EntityFrameworkCore.Tests/DomainEvents/EntityFrameworkCoreDomainEventPersistenceTests.cs`
- `tests/Bondstone.Persistence.EntityFrameworkCore.Tests/Outbox/OutboxMessageEntityTests.cs`

### Change Log

- 2026-06-19: Strengthened durable message-kind and domain-event boundary regression coverage; no runtime behavior changes required.
