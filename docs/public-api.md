# Public API Surface

This document records the current public API classification for Bondstone
packages as cleanup work applies ADR 0046. It is an inventory and guidance
document, not a source of new compatibility promises.

## Automated Baseline

`tests/Bondstone.PublicApi.Tests` uses `PublicApiGenerator` to record the
current public/protected API surface for all packable Bondstone packages. The
default fast test gate fails when that generated API text changes without an
intentional baseline update.

Refresh baselines with:

```bash
BONDSTONE_UPDATE_PUBLIC_API_BASELINE=1 dotnet test tests/Bondstone.PublicApi.Tests/Bondstone.PublicApi.Tests.csproj --configuration Release
```

Review the resulting baseline diff before merging. A baseline update records
that a public API changed; it does not by itself approve breaking changes,
replace ADR review, or replace release-note treatment.

## Classification Labels

- Normal setup API: the preferred path for application host and module setup.
- User application contract: interfaces, attributes, result types, records,
  options, and metadata that normal application code implements, consumes, or
  stores.
- Advanced composition API: lower-level APIs for tests, custom schedulers,
  app-owned receive or dispatch loops, and manual service composition.
- Provider/runtime contract: APIs used by Bondstone runtime, provider
  packages, transport adapters, and optional capability packages.
- Public implementation detail exposed for now: concrete implementation
  classes that are public because of early extraction, direct tests, or
  advanced composition. They are not the preferred user setup path and should
  not grow new public surface casually.
- Cleanup candidate: a public type whose name, visibility, or package
  placement should be reviewed before stronger compatibility expectations.

Public API removal, visibility reduction, renaming, or broad contract movement
remains compatibility-sensitive and requires the ADR 0046 planning path before
implementation.

## Decision Notes

Final decision slice, 2026-06-12:

- `Bondstone.Utility.StringExtensions` is no longer public. String
  normalization helpers are package-local implementation details.
- `BondstoneLocalServiceCollectionExtensions` is no longer public. Local
  transport setup stays on `UseLocalTransport`; service registration plumbing
  is package-local implementation code.
- No other public implementation detail is clearly obsolete or misleading
  enough to promote to cleanup candidate now. The broad concrete
  implementation surface should remain classified and documented rather than
  churned.
- The current automated public API baseline should run before stronger
  compatibility promises or broad public-surface reduction.

Issue 26 result diagnostics, 2026-06-14:

- `DurableOperationResultState` and
  `DurableOperationResultDeserializationFailure` are additive user application
  contracts for explaining why a durable operation result is not available.
- `DurableOperationResult<TResult>.State` and `.DeserializationFailure` are
  additive properties. The existing constructor and the meanings of
  `IsKnown`, `IsCompleted`, `IsTerminal`, and `HasResult` are preserved.
- The public API baseline change is intentional and compatibility-sensitive,
  but it does not remove or rename existing public members.

Issue 28 result diagnostic context, 2026-06-15:

- `DurableOperationDiagnosticContext` is an additive user application
  contract for the optional module name, durable message type name, and
  handler identity associated with an operation state.
- `DurableOperationState`, `DurableOperationResult<TResult>`, and
  `DurableOperationResultDeserializationFailure` expose an optional
  `DiagnosticContext` property so result diagnostics can name the module,
  durable message type, and handler when stored.
- The constructor changes are additive optional parameters. Existing callers
  and old operation rows continue to work with null diagnostic context.

Post-MVP transport simplification, 2026-06-16:

- `IDurableOutboxTransport`, `IDurableOutboxTransportRoute`,
  `RoutedDurableOutboxTransport`, and `RabbitMqDurableOutboxTransport` were
  renamed to `IDurableEnvelopeDispatcher`,
  `IDurableEnvelopeDispatchRoute`, `RoutedDurableEnvelopeDispatcher`, and
  `RabbitMqDurableEnvelopeDispatcher`.
- The renamed envelope dispatcher contract uses `DispatchAsync(...)` to make
  the neutral handoff about dispatching persisted Bondstone envelopes, not
  owning broker transport runtime.
- This is an intentional compatibility-breaking public API cleanup under ADR
  0056 while external usage is still bounded.

Operation finalization, 2026-06-16:

- `IDurableOperationFinalizer` is an additive user application contract for
  marking explicit `Failed` or `Cancelled` operation outcomes in a named
  module's operation-state store.
- `DurableOperationFinalizationResult` is an additive result type that reports
  the resulting operation state and whether the finalizer wrote a new terminal
  state.
- The API deliberately requires a module name and does not infer operation
  failure from outbox, inbox, retry, or broker dead-letter state.

Operation expiry processing, 2026-06-16:

- `IDurableOperationExpirationProcessor` is an additive user application
  contract for running an app-owned expiry pass against one module's
  operation-state store.
- `DurableOperationExpirationResult` is an additive result type that reports
  the module, cutoff, requested terminal status, and per-operation
  finalization results.
- `IDurableOperationExpirationStore` is an additive provider/runtime contract
  for operation-state stores that can query stale `Pending` or `Running`
  operation states.

Terminal outbox inspection, 2026-06-16:

- `IDurableOutboxInspector` is an additive user application contract for
  reading `TerminalFailed` outbox records from a named module's persistence
  boundary.
- `IDurableOutboxInspectionStore` is an additive provider/runtime contract for
  querying terminal outbox rows with optional source-module and failed-at
  cutoff filters.
- `DurableModuleOutboxInspectionStoreRegistration` is an additive advanced
  module runtime registration type for provider packages.
- The API is intentionally read-only; it does not add reset, replay, purge, or
  archival mutation contracts.

Inbox inspection, 2026-06-16:

- `IDurableInboxInspector` is an additive user application contract for
  reading unprocessed inbox records from a named module's persistence
  boundary.
- `IDurableInboxInspectionStore` is an additive provider/runtime contract for
  querying unprocessed inbox rows with optional module and received-at cutoff
  filters.
- `DurableModuleInboxInspectionStoreRegistration` is an additive advanced
  module runtime registration type for provider packages.
- The API is intentionally read-only; it does not add row mutation, handler
  replay, broker action, purge, or archival contracts.

Module-hinted operation reads, 2026-06-16:

- `IDurableOperationReader.GetStateAsync(Guid, string, CancellationToken)` is
  an additive module-hinted read overload. The default module-aware reader uses
  it to query one named module operation-state store.
- `IDurableOperationResultReader.GetResultAsync<TResult>(Guid, string,
CancellationToken)` and
  `IDurableOperationResultReader.WaitForResultAsync<TResult>(Guid, string,
TimeSpan, TimeSpan?, CancellationToken)` are additive module-hinted result
  observation overloads.
- Existing operation-id-only reads remain the global aggregate compatibility
  path.

Public API curation, 2026-06-16:

- The current persistence inspection contracts are intentionally split between
  app-facing inspectors and provider-side stores. `IDurableOutboxInspector`
  and `IDurableInboxInspector` are user application contracts;
  `IDurableOutboxInspectionStore` and `IDurableInboxInspectionStore` are
  provider/runtime contracts.
- `DurableModuleOutboxInspectionStoreRegistration` and
  `DurableModuleInboxInspectionStoreRegistration` remain public because
  provider packages need to register module-owned stores through explicit
  contracts. They are hidden from normal IntelliSense and documented as
  provider/runtime registration hooks, not normal app setup APIs.
- No additional public implementation type is promoted to cleanup candidate in
  this slice. The next reduction should remove an entire obsolete capability
  or package surface, not rename individual concrete types for tidiness.

## Current Scope

This first pass covers all current package projects:

- `Bondstone`
- `Bondstone.Capabilities.DomainEvents`
- `Bondstone.Capabilities.DomainEvents.EntityFrameworkCore`
- `Bondstone.Hosting`
- `Bondstone.Persistence`
- `Bondstone.Persistence.EntityFrameworkCore`
- `Bondstone.Persistence.EntityFrameworkCore.Postgres`
- `Bondstone.Transport.Local`
- `Bondstone.Transport.RabbitMq`

`Bondstone.Persistence.Postgres`, `Bondstone.Transport`, and
`Bondstone.Transport.ServiceBus` were removed from the active product surface
and are not covered by the current default public API baseline.

## Bondstone

Normal setup API:

- `BondstoneServiceCollectionExtensions`
- `BondstoneBuilder`
- `BondstoneOutboxBuilder`
- `BondstoneModuleBuilderExtensions`
- `BondstoneModuleBuilder`
- `BondstoneModuleCommandBuilder`
- `BondstoneModuleEventBuilder`
- `BondstoneDurablePayloadServiceCollectionExtensions`

User application contract:

- `IMessage`
- `ICommand`
- `ICommand<TResult>`
- `IDurableCommand`
- `IIntegrationEvent`
- `ICommandHandler<TCommand>`
- `ICommandHandler<TCommand, TResult>`
- `IIntegrationEventHandler<TEvent>`
- `ICommandValidator<TCommand>`
- `IModuleCommandPipelineBehavior<TCommand>`
- `IModuleEventSubscriberPipelineBehavior<TEvent>`
- `IBondstoneModule`
- `IDurableCommandSender`
- `IDurableEventPublisher`
- `IDurableOperationExpirationProcessor`
- `IDurableOperationFinalizer`
- `IDurableOperationResultReader`
- `IDurablePayloadSerializer`
- `IMessageTypeRegistry`
- `IModuleExecutionContextAccessor`
- `DurableCommandIdentityAttribute`
- `IntegrationEventIdentityAttribute`
- `DurableOperationDiagnosticContext`
- `MessageTypeRegistration`
- `DurableCommandSendResult`
- `DurableCommandSendStatus`
- `DurableEventPublishResult`
- `DurableEventPublishStatus`
- `DurableOperationResult<TResult>`
- `DurableOperationResultDeserializationFailure`
- `DurableOperationExpirationResult`
- `DurableOperationFinalizationResult`
- `DurableOperationResultState`
- `DurablePayloadJsonOptions`
- `ModuleCommandExecutionResult`
- `ModuleCommandExecutionResult<TResult>`
- `ModuleEventSubscriberExecutionResult`

Advanced composition API:

- `IModuleCommandExecutor`
- `IModuleEventSubscriberExecutor`
- `IModuleCommandReceivePipeline`
- `IModuleEventReceivePipeline`
- `IBondstoneModuleRegistry`
- `IModuleCommandRouteRegistry`
- `IModulePublishedEventRegistry`
- `IModuleEventSubscriberRegistry`
- `BondstoneConfigurationValidationContext`
- `IBondstoneConfigurationValidator`
- `DurableModulePersistenceServiceCollectionExtensions`

Provider/runtime contract:

- `BondstoneModuleRegistration`
- `ModuleCommandRoute`
- `ModulePublishedEventRegistration`
- `ModuleEventSubscriberRegistration`
- `ModuleCommandPipelineContribution`
- `ModuleEventSubscriberPipelineContribution`
- `ModuleCommandExecutionContext`
- `ModuleEventSubscriberExecutionContext`
- `ModuleCommandReceiveContext`
- `ModuleEventSubscriberReceiveContext`
- `ModuleExecutionContext`
- `IModulePipelineExecutionContext`
- `ModulePipelineFeatureCollection`
- `ModulePipelineStepKind`
- `ModuleCommandSystemPipelineOrder`
- `ModuleEventSubscriberSystemPipelineOrder`

Public implementation detail exposed for now:

- `MessageTypeRegistry`
- `SystemTextJsonDurablePayloadSerializer`

## Bondstone.Persistence

User application contract:

- `IDurableOperationReader`
- `DurableOperationState`
- `DurableOperationDiagnosticContext`
- `DurableOperationStatus`
- `DurableMessageEnvelope`
- `MessageTraceContext`
- `MessageKind`
- `IDurableOutboxInspector`
- `IDurableInboxInspector`
- `DurableInboxAlreadyReceivedException`
- `DurableInboxHandleResult`
- `DurableInboxHandleStatus`
- `DurableInboxMessageKey`
- `DurableInboxRecord`
- `DurableInboxRegistrationResult`
- `DurableInboxRegistrationStatus`
- `DurableOutboxDispatchResult`
- `DurableOutboxDispatchState`
- `DurableOutboxFailureDecision`
- `DurableOutboxFailureDecisionKind`
- `DurableOutboxRecord`
- `DurableOutboxStatus`

Advanced composition API:

- `IDurableOutboxDispatcher`
- `IDurableOutboxFailurePolicy`
- `IDurableEnvelopeDispatcher`
- `IDurableEnvelopeDispatchRoute`
- `IDurableOutboxWriter`
- `IDurableInboxHandlerExecutor`
- `IDurableInboxRegistrar`
- `IDurableInboxStore`
- `IDurableOperationExpirationStore`
- `IDurableOperationStateStore`
- `IDurableOutboxClaimer`
- `IDurableOutboxLeaseRenewer`
- `IDurableOutboxDispatchRecorder`
- `IDurableOutboxInspectionStore`
- `IDurableInboxInspectionStore`
- `DurableInboxHandlerExecutor`
- `DurableOutboxDispatcher`
- `DurableOutboxFailurePolicy`
- `RoutedDurableEnvelopeDispatcher`

Provider/runtime contract:

- `IModuleTransactionFeature`
- `DurableModulePersistenceRegistrationRegistry`
- `DurableModuleOutboxWriterRegistration`
- `DurableModuleInboxHandlerExecutorRegistration`
- `DurableModuleInboxInspectionStoreRegistration`
- `DurableModuleOperationStateStoreRegistration`
- `DurableModuleOutboxDispatcherRegistration`
- `DurableModuleOutboxInspectionStoreRegistration`
- `DurableModuleOutboxDispatchAggregator`

## Bondstone.Persistence.EntityFrameworkCore

Normal setup API:

- `BondstoneEntityFrameworkCoreServiceCollectionExtensions`
- `BondstoneEntityFrameworkCoreModuleBuilderExtensions`
- `BondstoneModelBuilderExtensions`

Advanced composition API:

- `IEntityFrameworkCorePersistenceScope`

Provider/runtime contract:

- `EntityFrameworkCoreModulePersistence`

Public implementation detail exposed for now:

- `EntityFrameworkCoreDurableInboxInspectionStore<TDbContext>`
- `EntityFrameworkCorePersistenceScope<TDbContext>`
- `EntityFrameworkCoreDurableOutboxWriter<TDbContext>`
- `EntityFrameworkCoreModuleDurableOutboxWriter<TDbContext>`
- `EntityFrameworkCoreDurableInboxStore<TDbContext>`
- `EntityFrameworkCoreDurableOperationStateStore<TDbContext>`
- `EntityFrameworkCoreModuleDurableOperationStateStore<TDbContext>`
- `EntityFrameworkCoreDurableOutboxInspectionStore<TDbContext>`
- `OutboxMessageEntity`
- `OutboxMessageEntityConfiguration`
- `OutboxMessageEntityConfiguration.Columns`
- `InboxMessageEntity`
- `InboxMessageEntityConfiguration`
- `InboxMessageEntityConfiguration.Columns`
- `OperationStateEntity`
- `OperationStateEntityConfiguration`

## Bondstone.Persistence.EntityFrameworkCore.Postgres

Normal setup API:

- `BondstonePostgreSqlBuilderExtensions`
- `BondstonePostgreSqlServiceCollectionExtensions`

Advanced composition API:

- `PostgreSqlPersistenceExceptionClassifier`

Public implementation detail exposed for now:

- `PostgreSqlDurableInboxRegistrar<TDbContext>`
- `PostgreSqlModuleDurableInboxHandlerExecutor<TDbContext>`
- `PostgreSqlDurableOutboxClaimer<TDbContext>`
- `PostgreSqlDurableOutboxLeaseRenewer<TDbContext>`
- `PostgreSqlDurableOutboxDispatchRecorder<TDbContext>`
- `PostgreSqlModuleDurableOutboxDispatcher<TDbContext>`

## Bondstone.Hosting

Normal setup API:

- `BondstoneHostingBuilderExtensions`
- `DurableOutboxWorkerOptions`

Advanced composition API:

- `BondstoneHostingServiceCollectionExtensions`

Public implementation detail exposed for now:

- `DurableOutboxWorker`
- `DurableOutboxWorkerOptionsValidator`

## Bondstone.Transport.Local

Normal setup API:

- `BondstoneLocalBuilderExtensions`
- `BondstoneLocalTransportBuilder`
- `BondstoneLocalModuleRouteBuilder`
- `BondstoneLocalEventRouteBuilder`
- `BondstoneLocalQueueBuilder`

## Bondstone.Transport.RabbitMq

Normal setup API:

- `BondstoneRabbitMqBuilderExtensions`
  (`BondstoneBuilder.UseRabbitMqTransport(...)` overload)
- `BondstoneRabbitMqServiceCollectionExtensions`
- `BondstoneRabbitMqTransportBuilder`
- `BondstoneRabbitMqReceiveQueueBuilder`
- `RabbitMqReceiveWorkerOptions`

User application contract:

- `RabbitMqPublishDestination`
- `RabbitMqPublishDestinationKind`

Advanced composition API:

- `BondstoneRabbitMqBuilderExtensions`
  (`BondstoneOutboxBuilder.UseRabbitMqTransport(...)` overload)
- `IRabbitMqReceivedMessageDispatcher`
- `IRabbitMqReceivedMessageHandler`
- `RabbitMqReceivedMessageMapper`
- `RabbitMqTransportMessage`

Provider/runtime contract:

- `IRabbitMqMessagePublisher`

Public implementation detail exposed for now:

- `BondstoneRabbitMqHeaders`
- `RabbitMqDurableMessageEnvelope`
- `RabbitMqDurableEnvelopeDispatcher`

## Bondstone.Capabilities.DomainEvents

User application contract:

- `IDomainEvent`
- `DomainEventIdentityAttribute`
- `IDomainEventSource`
- `IDomainEventHandler<TDomainEvent>` is a local handler contract. Bondstone
  does not dispatch it automatically in the current runtime.

## Bondstone.Capabilities.DomainEvents.EntityFrameworkCore

Normal setup API:

- `BondstoneEntityFrameworkCoreDomainEventModuleBuilderExtensions`
- `BondstoneDomainEventModelBuilderExtensions`

Public implementation detail exposed for now:

- `DomainEventRecordEntity`
- `DomainEventRecordEntityConfiguration`
- `DomainEventRecordEntityConfiguration.Columns`
