---
baseline_commit: fb42e57
---

# Story 7.4: OpenTelemetry Diagnostics And Stable Setup Codes

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a consumer,
I want stable setup failure codes and low-cardinality diagnostics,
so that common misconfigurations can be detected and documented consistently.

## Acceptance Criteria

1. Given diagnostics are emitted, when OpenTelemetry-native activity, metric, or log surfaces are practical, then they are used.
2. Given diagnostics include dimensions, when reviewed, then message ids, operation ids, exception text, payloads, broker delivery counts, topology details, and dead-letter state are not used as high-cardinality dimensions.
3. Given common setup failures occur, when exceptions or validation results are produced, then stable codes cover missing module persistence, missing EF mappings, missing dispatcher, duplicate durable registrations, invalid durable identities, missing receive binding, and ambiguous dispatch routes.
4. Given stable codes are added, when compatibility is reviewed, then the code representation and migration promise are documented.
5. Given common setup failures are already thrown today, when this story is implemented, then representative failures emit stable code values through a source-compatible exception or validation surface instead of relying only on message text.
6. Given diagnostics are added or changed, when tests inspect emitted data, then they assert stable low-cardinality names and dimensions without matching volatile ids, payloads, or exception text.

## Tasks / Subtasks

- [x] Inventory current diagnostics and setup-failure surfaces before editing. (AC: 1, 2, 3, 5, 6)
  - [x] Read every file listed in "Files To Read Or Update" before changing code; record current state, intended change, and preserved behavior in the dev record.
  - [x] Map each required setup code to the current throw site and existing test: missing module persistence, missing EF mappings, missing dispatcher, duplicate durable registrations, invalid durable identities, missing receive binding, and ambiguous dispatch routes.
  - [x] Inventory activity tags, metric attributes, and log scopes/events for high-cardinality values; distinguish activity tags from metric dimensions explicitly in completion notes.
- [x] Add a stable setup-code representation with source-compatible behavior. (AC: 3, 4, 5)
  - [x] Prefer an additive public contract in the core package, for example a coded Bondstone setup/configuration exception or coded validation result shape; do not break existing exception catch behavior unless compatibility is deliberately reviewed.
  - [x] Define stable string codes as documented constants or enum-like values with lowercase/dot-separated names such as `bondstone.setup.missing_module_persistence`; avoid deriving codes from CLR type names, handler names, exception text, or package implementation class names.
  - [x] Preserve clear human-readable messages, but update representative tests to assert the stable code first and message content only where it remains part of the human diagnostic contract.
  - [x] Document the migration promise: codes are intended to be stable for automation; messages may improve; new codes may be added; existing code names require compatibility review before rename/removal.
- [x] Cover all required common setup failures. (AC: 3, 5)
  - [x] Missing module persistence: `DurableMessagingConfigurationValidator` currently throws when a module uses durable messaging but lacks persistence.
  - [x] Missing EF mappings: EF Core stores/runners currently throw mapping-specific errors from `EntityFrameworkCoreModuleTransactionRunner`, `EntityFrameworkCoreDurableIncomingInboxIngestionStore`, `EntityFrameworkCoreDurableIncomingInboxInspectionStore`, `EntityFrameworkCoreDurableOperationStateStore`, and related EF stores.
  - [x] Missing dispatcher: `BondstoneOutboxConfigurationValidator` currently throws when outbox dispatching has persistence but no envelope dispatcher/transport.
  - [x] Duplicate durable registrations: `ModuleCommandRouteRegistry`, `ModuleEventSubscriberRegistry`, `MessageTypeRegistry`, and `BondstoneModuleRegistry` currently throw for duplicate routes, subscribers, message identities, persistence providers, or contexts.
  - [x] Invalid durable identities: `MessageIdentityMetadata`, `MessageTypeRegistry`, `ModuleCommandRoute`, subscriber identity registration, and domain-event identity checks currently reject missing, blank, wrong-kind, or conflicting identities.
  - [x] Missing receive binding: `RabbitMqReceiveWorker` and `ServiceBusReceiveWorker` currently throw when event ingestion lacks subscriber module/subscriber identity binding; local transport also has missing queue/subscriber binding failures.
  - [x] Ambiguous dispatch routes: `RoutedDurableEnvelopeDispatcher` currently throws when multiple routes match one durable outbox record.
- [x] Keep diagnostics OpenTelemetry-native and low-cardinality. (AC: 1, 2, 6)
  - [x] Continue using `System.Diagnostics.ActivitySource`, `System.Diagnostics.Metrics.Meter`, `ILogger`, and existing log event ids instead of adding a custom diagnostics framework.
  - [x] Metrics must keep low-cardinality attributes only: message kind, source module, target module when present, receiver module, source transport, operation status, and bounded count/status values.
  - [x] Do not add message id, operation id, partition key, payload, exception message/text, broker delivery count, native destination/topology, dead-letter state, claim owner, or handler CLR type as metric attributes.
  - [x] Review existing activity tags in `BondstoneMessagingDiagnostics`, `BondstonePersistenceDiagnostics`, and `IncomingInboxProcessingDiagnostics`; remove or deliberately document any volatile identifiers that remain on activities. Do not put those values on metrics or logs as dimensions.
  - [x] If setup failure logs or activities are added, emit the stable setup code and bounded category only; keep exception details in exception objects/log message text, not dimensions.
- [x] Update docs and public API guidance. (AC: 4)
  - [x] Update `docs/observability.md` to describe stable setup codes as current behavior and keep the low-cardinality diagnostic boundary accurate.
  - [x] Update `docs/public-api.md` if a public/protected exception, code type, constants class, or validation result API is added.
  - [x] Update package README/setup guidance only if the chosen code surface changes normal setup or migration guidance.
  - [x] If public/protected API baselines change, review and update the relevant files under `tests/Bondstone.PublicApi.Tests/Baselines/`.
- [x] Validate and verify. (AC: 1, 2, 3, 4, 5, 6)
  - [x] Run targeted core setup-code tests first, for example `dotnet test tests/Bondstone.Tests/Bondstone.Tests.csproj --configuration Release --filter "FullyQualifiedName~BondstoneBuilderTests|FullyQualifiedName~ModuleCommandRegistrationTests|FullyQualifiedName~ModuleEventRegistrationTests|FullyQualifiedName~RoutedDurableEnvelopeDispatcherTests"`.
  - [x] Run EF mapping setup-code tests if EF exception surfaces change: `dotnet test tests/Bondstone.Persistence.EntityFrameworkCore.Tests/Bondstone.Persistence.EntityFrameworkCore.Tests.csproj --configuration Release --filter "FullyQualifiedName~EntityFrameworkCoreModuleTransactionBehaviorTests|FullyQualifiedName~EntityFrameworkCoreDurableIncomingInboxIngestionStoreTests|FullyQualifiedName~EntityFrameworkCoreDurableIncomingInboxInspectionStoreTests|FullyQualifiedName~EntityFrameworkCoreDurableOperationStateStoreTests"`.
  - [x] Run transport setup-code tests if receive binding or adapter option failures change: targeted RabbitMQ, Service Bus, and Local transport tests.
  - [x] Run diagnostics tests that inspect activities and metrics with `ActivityTestHelper` and `MetricTestHelper`; assert names and bounded attributes, and assert absence of volatile dimensions.
  - [x] Run `pnpm backend:test` after runtime, test, or public docs changes.
  - [x] Run `pnpm backend:pack` and review public API baseline diffs if public/protected APIs change.
  - [x] Run `pnpm check` as the final broad gate when code, public API, package metadata, samples, or broad docs change.

### Review Findings

- [x] [Review][Patch] Document provider-neutral ownership for stable setup-code contracts — `Bondstone.Diagnostics` is implemented in `Bondstone.Persistence`, while the story asks to prefer an additive public contract in the core package and the contract is used by core module, messaging, and configuration failures. `docs/packaging.md` allows `Bondstone` to depend on provider-neutral `Bondstone.Persistence`; keep the contract there for this story and document the ownership rationale.
- [x] [Review][Patch] PostgreSQL incoming-inbox mapping failures still lack setup codes [src/Bondstone.Persistence.EntityFrameworkCore.Postgres/IncomingInbox/PostgreSqlDurableIncomingInboxClaimer.cs:165]
- [x] [Review][Patch] Blank or invalid durable identity values still bypass setup-code exceptions [src/Bondstone/Messaging/Identity/MessageIdentityAttributes.cs:55]
- [x] [Review][Patch] Receive bindings with blank or unregistered subscribers still bypass setup-code exceptions [src/Bondstone.Transport.RabbitMq/RabbitMqReceiveWorker.cs:199]
- [x] [Review][Patch] Outbox missing-persistence validation uses the misleading missing-module-persistence code [src/Bondstone/Configuration/BondstoneOutboxConfigurationValidator.cs:17]
- [x] [Review][Patch] Local transport missing-receive-binding setup-code paths are not covered by tests [src/Bondstone.Transport.Local/Outbox/LocalDurableEnvelopeDispatchRoute.cs:65]

## Dev Notes

Story 7.4 is runtime-first. It must not close as documentation-only unless inventory proves stable setup codes and low-cardinality diagnostics already exist with tests for every required common setup failure. Current inventory shows diagnostics exist, but stable setup codes do not.

### Current State Intelligence

Diagnostics:

- `BondstoneMessagingDiagnostics` owns `ActivitySource` and `Meter` name `Bondstone.Modules`. It emits command send, event publish, module receive, operation finalization, direct receive, operation finalization, and operation expiration diagnostics.
- `BondstonePersistenceDiagnostics` owns `ActivitySource` and `Meter` name `Bondstone.Persistence`. It emits outbox dispatch activities and outbox state-transition counters.
- `IncomingInboxProcessingDiagnostics` reuses `Bondstone.Modules` for durable incoming inbox processing activities and metrics.
- Metric attributes are mostly low-cardinality today: message kind, source module, target module when present, receiver module, source transport, and operation status.
- Activity tags currently include high-cardinality values in several places: message id, operation id, partition key, claim owner, handler identity, and record-level message ids. Story 7.4 must review these tags and either remove/demote volatile identifiers or document why activity-only volatile tags are still acceptable while metrics/log dimensions stay low-cardinality.
- `DurableOutboxDispatcher` and `DurableIncomingInboxDispatcher` set activity error status with exception messages. Do not copy exception messages into metric attributes or stable code values.
- Hosted workers already have stable log event ids: outbox `1001` / `DispatchBatchFailed`, incoming inbox `2001` / `ProcessBatchFailed`, RabbitMQ receive `2001` / `ReceiveFailed`, Service Bus receive `3001` / `ReceiveFailed`.

Setup failures:

- Missing module persistence: `DurableMessagingConfigurationValidator` throws `InvalidOperationException` when durable messaging is enabled without module persistence.
- Missing dispatcher: `BondstoneOutboxConfigurationValidator` throws when outbox dispatching has persistence but no envelope dispatcher/transport.
- Missing EF mappings: EF Core stores and transaction runner throw `InvalidOperationException` naming missing Bondstone mappings and `ApplyBondstone...` helpers.
- Duplicate durable registrations: route, subscriber, message identity, and module persistence registries throw `InvalidOperationException` for conflicts.
- Invalid durable identities: message/domain identity metadata and route constructors currently throw `InvalidOperationException` or `ArgumentException` for missing, blank, wrong-kind, or conflicting durable identities.
- Missing receive binding: RabbitMQ and Service Bus durable incoming event ingestion throw `ArgumentException` when subscriber module/subscriber identity binding is missing.
- Ambiguous dispatch routes: `RoutedDurableEnvelopeDispatcher` throws `InvalidOperationException` when more than one route can send a record.

Existing test helpers:

- `tests/Bondstone.Tests/Diagnostics/ActivityTestHelper.cs` captures activities and tag values.
- `tests/Bondstone.Tests/Diagnostics/MetricTestHelper.cs` captures metric measurements and tags.
- Existing diagnostics tests already assert names/tags for operation, outbox, and incoming inbox metrics in package-local test files; extend those rather than introducing an unrelated telemetry test harness.

### Files To Read Or Update

Read these UPDATE files completely before changing them:

- `src/Bondstone/Messaging/BondstoneMessagingDiagnostics.cs` - module activity/metric names, tags, and counters.
- `src/Bondstone/Modules/Execution/ModuleReceiveTelemetry.cs` - receive activity creation and trace-context behavior.
- `src/Bondstone/Messaging/Sending/DurableCommandSender.cs` - command send activity tags and pending operation setup.
- `src/Bondstone/Messaging/Publishing/DurableEventPublisher.cs` - event publish activity tags and published-event validation.
- `src/Bondstone.Persistence/Persistence/BondstonePersistenceDiagnostics.cs` - outbox activity/metric names, tags, and counters.
- `src/Bondstone.Persistence/Persistence/Outbox/DurableOutboxDispatcher.cs` - outbox activity boundaries, failure reasons, and metrics.
- `src/Bondstone.Persistence/Persistence/Outbox/RoutedDurableEnvelopeDispatcher.cs` - missing and ambiguous dispatch route failures.
- `src/Bondstone/Persistence/IncomingInbox/IncomingInboxProcessingDiagnostics.cs` - incoming inbox processing activity/metric names, tags, and counters.
- `src/Bondstone/Persistence/IncomingInbox/DurableIncomingInboxDispatcher.cs` - incoming inbox activity boundaries, failure reasons, and metrics.
- `src/Bondstone/Configuration/BondstoneOutboxConfigurationValidator.cs` and `DurableMessagingConfigurationValidator.cs` - startup validation failures for missing dispatcher/persistence.
- `src/Bondstone/Configuration/BondstoneBuilder.cs` and `BondstoneConfigurationValidationContext.cs` - validation composition and source-compatible extension point.
- `src/Bondstone/Messaging/Identity/MessageIdentityAttributes.cs` and `MessageTypeRegistry.cs` - invalid/duplicate durable identity failures.
- `src/Bondstone/Modules/Routing/ModuleCommandRoute.cs` and `ModuleCommandRouteRegistry.cs` - handler identity, duplicate route, and missing route diagnostics.
- `src/Bondstone/Modules/Events/ModuleEventSubscriberRegistry.cs` - duplicate/missing subscriber diagnostics.
- `src/Bondstone/Modules/Registration/BondstoneModuleRegistry.cs` - duplicate persistence provider/context diagnostics.
- `src/Bondstone.Persistence.EntityFrameworkCore/Persistence/EntityFrameworkCoreModuleTransactionRunner.cs` and EF store files that call `FindEntityType(...)` - missing EF mapping diagnostics.
- `src/Bondstone.Transport.RabbitMq/RabbitMqReceiveWorker.cs` and `src/Bondstone.Transport.ServiceBus/ServiceBusReceiveWorker.cs` - durable incoming event receive binding diagnostics.
- `src/Bondstone.Transport.Local/Outbox/LocalDurableEnvelopeDispatchRoute.cs` and local topology files - missing local command/event binding diagnostics.
- `docs/observability.md` - current diagnostics and stable-code documentation.
- `docs/public-api.md` - compatibility classification for any new public/protected code surface.

Likely tests to inspect or extend:

- `tests/Bondstone.Tests/Configuration/BondstoneBuilderTests.cs`
- `tests/Bondstone.Tests/Modules/ModuleCommandRegistrationTests.cs`
- `tests/Bondstone.Tests/Modules/ModuleEventRegistrationTests.cs`
- `tests/Bondstone.Tests/Persistence/RoutedDurableEnvelopeDispatcherTests.cs`
- `tests/Bondstone.Tests/Persistence/DurableOutboxDispatcherTests.cs`
- `tests/Bondstone.Tests/Persistence/DurableIncomingInboxDispatcherTests.cs`
- `tests/Bondstone.Persistence.EntityFrameworkCore.Tests/Persistence/EntityFrameworkCoreModuleTransactionBehaviorTests.cs`
- `tests/Bondstone.Persistence.EntityFrameworkCore.Tests/IncomingInbox/EntityFrameworkCoreDurableIncomingInboxIngestionStoreTests.cs`
- `tests/Bondstone.Persistence.EntityFrameworkCore.Tests/IncomingInbox/EntityFrameworkCoreDurableIncomingInboxInspectionStoreTests.cs`
- `tests/Bondstone.Persistence.EntityFrameworkCore.Tests/Operations/EntityFrameworkCoreDurableOperationStateStoreTests.cs`
- `tests/Bondstone.Transport.Local.Tests/LocalDurableEnvelopeDispatcherTests.cs`
- `tests/Bondstone.Transport.RabbitMq.Tests/RabbitMqReceiveWorkerTests.cs`
- `tests/Bondstone.Transport.ServiceBus.Tests/ServiceBusReceiveWorkerTests.cs`
- Public API baselines under `tests/Bondstone.PublicApi.Tests/Baselines/` if public/protected APIs change.

### Architecture Compliance

Follow these non-negotiable rules:

- Bondstone remains a durable module-boundary library, not a generic bus, workflow engine, broker runtime owner, topology manager, broker monitoring stack, or application platform.
- Diagnostics should be OpenTelemetry-native where practical: `ActivitySource`, `Meter`, and `ILogger` are the expected primitives.
- Stable setup codes are for Bondstone-owned misconfiguration surfaces. Do not create provider-neutral broker retry, topology, dead-letter, delivery-count, or native monitoring diagnostics.
- Do not derive durable identities, setup codes, or handler/subscriber identities from CLR type names or handler class names.
- Do not add high-cardinality metric attributes: message ids, operation ids, exception text, payloads, broker delivery counts, topology details, dead-letter state, native destination names, claim owner ids, or partition keys.
- Public/protected API changes are compatibility-sensitive. Additive setup-code APIs need docs, baseline review, and migration notes.
- Do not add `InternalsVisibleTo` for runtime package collaboration.
- Do not add cleanup, replay, purge, DLQ movement, topology ownership, automatic operation failure inference, or default hosted workers as part of diagnostics work.

### Testing Requirements

Use the repository testing policy from `docs/testing.md`:

- Use `Unit` tests for setup-code exception/validation behavior, duplicate registration failures, invalid identity failures, missing dispatcher/persistence validation, routed dispatcher ambiguity, and low-cardinality metric assertions.
- Use `Application` tests for EF Core mapping/change-tracker surfaces that do not need real provider semantics.
- Use `Integration` tests only if the implementation changes PostgreSQL, RabbitMQ, Service Bus, or provider-backed behavior. Stable code wrappers around existing setup failures should usually be fast tests.
- Prefer outcome assertions over message-string matching: stable code value, exception type/source-compatible base type, bounded metric/activity names, absence of volatile tags on metrics, and preservation of existing human-readable message where useful.
- If public/protected APIs change, run public API baseline tests through the package gate and update baselines only after compatibility review.

### Previous Story Intelligence

Story 7.3 established:

- Start with implementation inventory and close documentation-only only when current code and tests already prove every criterion.
- Existing read-only inspection surfaces and worker logs should be reused instead of adding destructive helpers.
- No cleanup, retention, replay, purge, stale-row mutation, broker DLQ movement, topology management, provider monitoring, or automatic operation-failure inference.
- `docs/observability.md` must reflect current emitted activities and metrics.

Story 7.2 established:

- Operation waits and reads are observation only; timeout does not write operation state.
- Public API changes require compatibility review and baseline consideration.
- Durable incoming inbox terminal failure is operational evidence, not automatic operation failure.

Story 7.1 established:

- Transport adapters are thin native-driver envelope adapters.
- Host code owns topology, retry, dead-letter policy, credentials, monitoring, worker placement, and broker behavior.
- Receive workers ingest durable inbox rows before native settlement and do not run handlers during ingestion.

Story 6.3 established:

- EF Core plus PostgreSQL is the supported production durable persistence path.
- Consumers own migrations and schema rollout.
- EF mappings are application-owned setup and must fail clearly when missing.

### Git Intelligence

Recent commits at story creation time:

- `fb42e57 docs: more tightening`
- `8a7090d fix: more observation`
- `e85574a docs: postgres durability`
- `dd17279 fix: sb durable worker`
- `6355dc4 fix: sb worker`

The recent pattern is narrow runtime or docs correction backed by targeted tests. Continue that pattern; do not redesign messaging, persistence, transport, or operation APIs while adding stable codes.

### Latest Technical Information

No dependency upgrade is required for this story. Use repository-pinned versions from `Directory.Build.props`, `Directory.Packages.props`, `global.json`, and `package.json`:

- Target framework `net10.0`; SDK `10.0.108`; nullable enabled; warnings as errors; central package management.
- EF Core and Microsoft.Extensions packages pinned at `10.0.8`.
- `Azure.Messaging.ServiceBus` pinned at `7.20.1`.
- `RabbitMQ.Client` pinned at `7.2.1`.
- xUnit pinned at `2.9.3`; `Microsoft.NET.Test.Sdk` pinned at `18.6.0`.

Official docs checked during story creation:

- OpenTelemetry .NET metrics best practices warn that attribute combinations can create very high cardinality; keep metric attributes bounded and intentional. Source: https://opentelemetry.io/docs/languages/dotnet/metrics/best-practices/
- Microsoft Learn documents `ActivitySource` as the .NET API for creating and starting activities and registering listeners. Source: https://learn.microsoft.com/en-us/dotnet/core/diagnostics/distributed-tracing-instrumentation-walkthroughs
- Microsoft Learn documents `System.Diagnostics.Metrics` for custom .NET metrics. Source: https://learn.microsoft.com/en-us/dotnet/core/diagnostics/metrics-instrumentation

### Project Structure Notes

- No UX files were found under `_bmad-output/planning-artifacts`; UX is not applicable for this library story.
- Stable setup-code contracts likely belong in `src/Bondstone` unless the code surface is provider-specific. Provider-specific EF mapping code should remain in `Bondstone.Persistence.EntityFrameworkCore`.
- Provider-neutral outbox dispatch routing belongs in `src/Bondstone.Persistence`; core module/messaging setup validation belongs in `src/Bondstone`; broker receive binding code belongs in the RabbitMQ and Service Bus packages.
- Consumer-facing diagnostics guidance belongs in `docs/observability.md`; compatibility guidance belongs in `docs/public-api.md`; internal architecture remains in `_bmad-output/planning-artifacts/architecture.md`.
- Generated packages, coverage, temporary sample outputs, and build artifacts should stay out of source.

### Project Context Reference

Project context says Bondstone is a .NET library/framework for durable module boundaries, durable command sending, EF Core backed inbox/outbox persistence, operation observation, and transport adapters. Stable durable identities must be explicit. Transport adapters remain thin; host code owns broker topology, retries, dead-letter policy, credentials, and monitoring. Diagnostics should avoid high-cardinality values. Public/protected API changes are compatibility-sensitive, and EF Core InMemory is not proof of relational durability.

### Open Questions

None blocking. During implementation, decide the final code representation: constants, enum-like values, or a coded exception/validation contract. The chosen shape must be source-compatible, documented, and asserted in tests without depending on message text.

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- 2026-06-21: Activation resolver failed because local `python3` could not import stdlib `json`; workflow customization was resolved manually from base/team/user TOML fallback. No prepend or append steps configured.
- 2026-06-21: Loaded project context, config, sprint status, full epics, full PRD, full architecture, observability/testing docs, previous stories 7.2/7.3, relevant scoped AGENTS files, recent git history, package pins, diagnostics code, setup validators, registries, EF mapping diagnostics, transport receive workers, and representative tests before creating this story.
- 2026-06-21: Inventory before editing mapped setup-code categories to existing validators, registries, EF mapping guards, local/broker receive binding guards, and routed dispatch failures. Existing metrics were already low-cardinality; activities carried volatile message id, operation id, partition key, claim owner, and handler identity tags.
- 2026-06-21: Red phase added stable-code assertions and low-cardinality activity assertions; targeted core tests failed first because `Bondstone.Diagnostics` setup-code API did not exist.
- 2026-06-21: Green phase added setup-code constants and coded exception contracts, wired representative setup throw sites, removed volatile activity tags, and preserved broad catch compatibility through `InvalidOperationException` / `ArgumentException` inheritance.
- 2026-06-21: Public API baseline for `Bondstone.Persistence` was updated after review; diff only added `Bondstone.Diagnostics` setup-code contracts.
- 2026-06-21: Validation passed: targeted core setup/diagnostics tests, targeted EF mapping tests, targeted local/RabbitMQ/Service Bus tests, public API baseline tests, `pnpm backend:build`, `pnpm backend:test`, `pnpm backend:pack`, and `pnpm check`.
- 2026-06-21: Code review patches documented provider-neutral setup-code ownership, added `missing_outbox_persistence`, coded PostgreSQL incoming-inbox mapping failures, coded blank durable identity and receive-binding paths, added local route direct coverage, and revalidated with targeted tests plus `pnpm check`.

### Completion Notes List

- Implemented additive stable setup-code contracts in `Bondstone.Diagnostics`: `BondstoneSetupCodes`, `IBondstoneSetupException`, `BondstoneSetupException`, and `BondstoneSetupArgumentException`.
- Code representation is stable lowercase/dot-separated strings. Codes are explicitly defined constants, not derived from CLR type names, handler names, exception text, package implementation class names, or runtime values.
- Source compatibility is preserved for broad catch behavior: coded setup operation failures derive from `InvalidOperationException`; coded setup argument failures derive from `ArgumentException`. Wrapper messages preserve inner exception chains where existing module builder context is added.
- Required setup-code coverage was wired and tested for missing module persistence, missing EF mappings, missing dispatcher/route, duplicate durable registrations, invalid durable identities, missing receive binding, and ambiguous dispatch routes.
- Activity tags were tightened to remove volatile message id, operation id, partition key, outbox/incoming claim owner, and handler identity tags. Activity tags now retain bounded message kind/type/module/status/count context where useful.
- Metric attributes remain low-cardinality and unchanged in shape: message kind, source/target/receiver module, source transport where present, operation status, and bounded outcome counts/status values. No message ids, operation ids, payloads, exception text, broker delivery counts, topology details, dead-letter state, or claim owner ids are used as metric attributes.
- No setup failure logs or setup-failure activities were added; existing `ILogger` event ids remain unchanged and exception details stay in exception objects/log message text rather than metric dimensions.
- Updated `docs/observability.md`, `docs/public-api.md`, and the `Bondstone.Persistence` public API baseline to document current behavior, code compatibility, and migration promise.
- Review patches preserve `Bondstone.Diagnostics` in `Bondstone.Persistence` as the current provider-neutral setup-code contract package, with ownership rationale documented in package/public API guidance.
- Review patches add `bondstone.setup.missing_outbox_persistence` so outbox-level persistence failures are not reported as missing module persistence.
- Review patches add stable-code coverage for PostgreSQL incoming-inbox mutation mapping guards, blank durable identity values, blank or unregistered receive bindings, and local transport missing-receive-binding route guards.

### File List

- `_bmad-output/implementation-artifacts/7-4-opentelemetry-diagnostics-and-stable-setup-codes.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `docs/observability.md`
- `docs/packaging.md`
- `docs/public-api.md`
- `src/Bondstone.Persistence/Diagnostics/BondstoneSetupArgumentException.cs`
- `src/Bondstone.Persistence/Diagnostics/BondstoneSetupCodes.cs`
- `src/Bondstone.Persistence/Diagnostics/BondstoneSetupException.cs`
- `src/Bondstone.Persistence/Diagnostics/IBondstoneSetupException.cs`
- `src/Bondstone.Persistence/Persistence/BondstonePersistenceDiagnostics.cs`
- `src/Bondstone.Persistence/Persistence/Outbox/DurableOutboxDispatcher.cs`
- `src/Bondstone.Persistence/Persistence/Outbox/RoutedDurableEnvelopeDispatcher.cs`
- `src/Bondstone.Persistence.EntityFrameworkCore/DomainEvents/EntityFrameworkCoreDomainEventModuleBehavior.cs`
- `src/Bondstone.Persistence.EntityFrameworkCore/IncomingInbox/EntityFrameworkCoreDurableIncomingInboxIngestionStore.cs`
- `src/Bondstone.Persistence.EntityFrameworkCore/IncomingInbox/EntityFrameworkCoreDurableIncomingInboxInspectionStore.cs`
- `src/Bondstone.Persistence.EntityFrameworkCore/Operations/EntityFrameworkCoreDurableOperationStateStore.cs`
- `src/Bondstone.Persistence.EntityFrameworkCore/Persistence/EntityFrameworkCoreModuleTransactionRunner.cs`
- `src/Bondstone.Transport.Local/Outbox/LocalDurableEnvelopeDispatchRoute.cs`
- `src/Bondstone.Transport.Local/Properties/AssemblyInfo.cs`
- `src/Bondstone.Transport.RabbitMq/RabbitMqReceiveWorker.cs`
- `src/Bondstone.Transport.ServiceBus/ServiceBusReceiveWorker.cs`
- `src/Bondstone/Configuration/BondstoneOutboxConfigurationValidator.cs`
- `src/Bondstone/Configuration/DurableMessagingConfigurationValidator.cs`
- `src/Bondstone/Messaging/BondstoneMessagingDiagnostics.cs`
- `src/Bondstone/Messaging/Identity/MessageIdentityAttributes.cs`
- `src/Bondstone/Messaging/Identity/MessageTypeRegistry.cs`
- `src/Bondstone/Messaging/Publishing/DurableEventPublisher.cs`
- `src/Bondstone/Messaging/Receiving/DurableEnvelopeReceiver.cs`
- `src/Bondstone/Messaging/Sending/DurableCommandSender.cs`
- `src/Bondstone/Modules/Events/ModuleEventSubscriberRegistry.cs`
- `src/Bondstone/Modules/Events/ModulePublishedEventRegistry.cs`
- `src/Bondstone/Modules/Execution/ModuleReceiveTelemetry.cs`
- `src/Bondstone/Modules/Registration/BondstoneModuleCommandBuilder.cs`
- `src/Bondstone/Modules/Registration/BondstoneModuleEventBuilder.cs`
- `src/Bondstone/Modules/Registration/BondstoneModuleRegistry.cs`
- `src/Bondstone/Modules/Routing/ModuleCommandRoute.cs`
- `src/Bondstone/Modules/Routing/ModuleCommandRouteRegistry.cs`
- `src/Bondstone/Persistence/IncomingInbox/DurableIncomingInboxDispatcher.cs`
- `src/Bondstone/Persistence/IncomingInbox/IncomingInboxProcessingDiagnostics.cs`
- `src/Bondstone/Persistence/Operations/DurableOperationFinalizer.cs`
- `tests/Bondstone.Persistence.EntityFrameworkCore.Tests/DomainEvents/EntityFrameworkCoreDomainEventPersistenceTests.cs`
- `tests/Bondstone.Persistence.EntityFrameworkCore.Tests/IncomingInbox/EntityFrameworkCoreDurableIncomingInboxIngestionStoreTests.cs`
- `tests/Bondstone.Persistence.EntityFrameworkCore.Tests/IncomingInbox/EntityFrameworkCoreDurableIncomingInboxInspectionStoreTests.cs`
- `tests/Bondstone.Persistence.EntityFrameworkCore.Tests/Operations/EntityFrameworkCoreDurableOperationStateStoreTests.cs`
- `tests/Bondstone.Persistence.EntityFrameworkCore.Tests/Persistence/EntityFrameworkCoreModuleTransactionBehaviorTests.cs`
- `tests/Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests/Persistence/PostgreSqlIncomingInboxMutationTests.cs`
- `tests/Bondstone.PublicApi.Tests/Baselines/Bondstone.Persistence.txt`
- `tests/Bondstone.Tests/Configuration/BondstoneBuilderTests.cs`
- `tests/Bondstone.Tests/DomainEvents/DomainEventContractTests.cs`
- `tests/Bondstone.Tests/Messaging/DurableCommandSenderTests.cs`
- `tests/Bondstone.Tests/Messaging/DurableEnvelopeReceiverTests.cs`
- `tests/Bondstone.Tests/Messaging/DurableEventPublisherTests.cs`
- `tests/Bondstone.Tests/Messaging/DurableOperationFinalizerTests.cs`
- `tests/Bondstone.Tests/Messaging/MessageTypeRegistryTests.cs`
- `tests/Bondstone.Tests/Modules/ModuleCommandRegistrationTests.cs`
- `tests/Bondstone.Tests/Modules/ModuleEventRegistrationTests.cs`
- `tests/Bondstone.Tests/Modules/ModuleRegistrationTests.cs`
- `tests/Bondstone.Tests/Persistence/DurableOutboxDispatcherTests.cs`
- `tests/Bondstone.Tests/Persistence/RoutedDurableEnvelopeDispatcherTests.cs`
- `tests/Bondstone.Transport.Local.Tests/LocalDurableEnvelopeDispatcherTests.cs`
- `tests/Bondstone.Transport.RabbitMq.Tests/RabbitMqReceiveWorkerTests.cs`
- `tests/Bondstone.Transport.ServiceBus.Tests/ServiceBusReceiveWorkerTests.cs`

### Change Log

- 2026-06-21: Created story 7.4 with implementation inventory, stable setup-code guidance, diagnostics guardrails, testing requirements, and source references.
- 2026-06-21: Implemented stable setup-code contracts, wired representative setup failures, removed high-cardinality activity tags, updated docs/public API baseline, and verified with targeted and full repository gates.
- 2026-06-21: Applied code-review patches for provider-neutral setup-code ownership, PostgreSQL mapping codes, blank identity/binding codes, outbox persistence code specificity, local route coverage, and final `pnpm check`.

### Story Completion Status

Completed after code review patches.

### References

- `_bmad-output/planning-artifacts/epics.md` - Epic 7 and Story 7.4 acceptance criteria.
- `_bmad-output/planning-artifacts/prds/prd-Bondstone-2026-06-18/prd.md` - FR8.4 and FR8.5.
- `_bmad-output/planning-artifacts/architecture.md` - Diagnostics And Observability, Transport Boundary, Persistence Architecture, Public API And Compatibility, Verification Strategy.
- `_bmad-output/project-context.md` - runtime, code, testing, diagnostics, public API, and workflow guardrails.
- `_bmad-output/implementation-artifacts/7-3-app-owned-worker-operations-and-retention.md` - previous story implementation intelligence.
- `_bmad-output/implementation-artifacts/7-2-operation-observation-cleanup.md` - previous operation-observation intelligence.
- `docs/observability.md` - current activity/metric/log surfaces and not-current stable code behavior.
- `docs/testing.md` - test categories and verification surface.
- `docs/public-api.md` - public API compatibility owner for any new code surface.
- `src/Bondstone/Messaging/BondstoneMessagingDiagnostics.cs` - core activity/metric names and tags.
- `src/Bondstone.Persistence/Persistence/BondstonePersistenceDiagnostics.cs` - outbox activity/metric names and tags.
- `src/Bondstone/Persistence/IncomingInbox/IncomingInboxProcessingDiagnostics.cs` - durable incoming inbox activity/metric names and tags.
- `src/Bondstone/Configuration/DurableMessagingConfigurationValidator.cs` and `BondstoneOutboxConfigurationValidator.cs` - setup validation failures.
- `src/Bondstone.Persistence/Persistence/Outbox/RoutedDurableEnvelopeDispatcher.cs` - missing/ambiguous dispatch route failures.
- `src/Bondstone.Transport.RabbitMq/RabbitMqReceiveWorker.cs` and `src/Bondstone.Transport.ServiceBus/ServiceBusReceiveWorker.cs` - receive binding failures.
- `tests/Bondstone.Tests/Diagnostics/ActivityTestHelper.cs` and `MetricTestHelper.cs` - existing telemetry test helpers.
