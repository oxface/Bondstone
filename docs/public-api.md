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

## Current Scope

This first pass covers all current package projects:

- `Bondstone`
- `Bondstone.Capabilities.DomainEvents`
- `Bondstone.Capabilities.DomainEvents.EntityFrameworkCore`
- `Bondstone.Hosting`
- `Bondstone.Persistence`
- `Bondstone.Persistence.EntityFrameworkCore`
- `Bondstone.Persistence.EntityFrameworkCore.Postgres`
- `Bondstone.Persistence.Postgres`
- `Bondstone.Transport`
- `Bondstone.Transport.Local`
- `Bondstone.Transport.RabbitMq`
- `Bondstone.Transport.ServiceBus`

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
- `IDurableOperationResultReader`
- `IDurablePayloadSerializer`
- `IMessageTypeRegistry`
- `IModuleExecutionContextAccessor`
- `DurableCommandIdentityAttribute`
- `IntegrationEventIdentityAttribute`
- `MessageTypeRegistration`
- `DurableCommandSendResult`
- `DurableCommandSendStatus`
- `DurableEventPublishResult`
- `DurableEventPublishStatus`
- `DurableOperationResult<TResult>`
- `DurableOperationResultDeserializationFailure`
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
- `DurableOperationStatus`
- `DurableMessageEnvelope`
- `MessageTraceContext`
- `MessageKind`
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
- `IDurableOutboxTransport`
- `IDurableOutboxTransportRoute`
- `IDurableOutboxWriter`
- `IDurableInboxHandlerExecutor`
- `IDurableInboxRegistrar`
- `IDurableInboxStore`
- `IDurableOperationStateStore`
- `IDurableOutboxClaimer`
- `IDurableOutboxLeaseRenewer`
- `IDurableOutboxDispatchRecorder`
- `DurableInboxHandlerExecutor`
- `DurableOutboxDispatcher`
- `DurableOutboxFailurePolicy`
- `RoutedDurableOutboxTransport`

Provider/runtime contract:

- `IModuleTransactionFeature`
- `DurableModulePersistenceRegistrationRegistry`
- `DurableModuleOutboxWriterRegistration`
- `DurableModuleInboxHandlerExecutorRegistration`
- `DurableModuleOperationStateStoreRegistration`
- `DurableModuleOutboxDispatcherRegistration`
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

- `EntityFrameworkCorePersistenceScope<TDbContext>`
- `EntityFrameworkCoreDurableOutboxWriter<TDbContext>`
- `EntityFrameworkCoreModuleDurableOutboxWriter<TDbContext>`
- `EntityFrameworkCoreDurableInboxStore<TDbContext>`
- `EntityFrameworkCoreDurableOperationStateStore<TDbContext>`
- `EntityFrameworkCoreModuleDurableOperationStateStore<TDbContext>`
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

## Bondstone.Persistence.Postgres

Normal setup API:

- `BondstonePostgresBuilderExtensions`
- `BondstonePostgresServiceCollectionExtensions`

User application contract:

- `IPostgresModuleSession`

Advanced composition API:

- `PostgresSchema`

Public implementation detail exposed for now:

- `PostgresModuleSession`
- `PostgresDurableOutboxWriter`
- `PostgresModuleDurableOutboxWriter`
- `PostgresDurableInboxRegistrar`
- `PostgresDurableInboxStore`
- `PostgresModuleDurableInboxHandlerExecutor`
- `PostgresDurableOperationStateStore`
- `PostgresModuleDurableOperationStateStore`
- `PostgresDurableOutboxClaimer`
- `PostgresDurableOutboxLeaseRenewer`
- `PostgresDurableOutboxDispatchRecorder`
- `PostgresModuleDurableOutboxDispatcher`

## Bondstone.Hosting

Normal setup API:

- `BondstoneHostingBuilderExtensions`
- `DurableOutboxWorkerOptions`

Advanced composition API:

- `BondstoneHostingServiceCollectionExtensions`

Public implementation detail exposed for now:

- `DurableOutboxWorker`
- `DurableOutboxWorkerOptionsValidator`

## Bondstone.Transport

User application contract:

- `DurableTransportTopologyRouteDiagnostic`
- `DurableMessageTopologyDiagnosticKind`

Provider/runtime contract:

- `IDurableTransportTopologyDiagnosticSource`

## Bondstone.Transport.Local

Normal setup API:

- `BondstoneLocalBuilderExtensions`
- `BondstoneLocalTransportBuilder`
- `BondstoneLocalModuleRouteBuilder`
- `BondstoneLocalEventRouteBuilder`
- `BondstoneLocalQueueBuilder`

## Bondstone.Transport.ServiceBus

Normal setup API:

- `BondstoneServiceBusBuilderExtensions`
  (`BondstoneBuilder.UseServiceBusTransport(...)` overload)
- `BondstoneServiceBusServiceCollectionExtensions`
- `BondstoneServiceBusTransportBuilder`
- `BondstoneServiceBusModuleRouteBuilder`
- `BondstoneServiceBusEventRouteBuilder`
- `BondstoneServiceBusReceiveSourceBuilder`
- `ServiceBusReceiveWorkerOptions`

User application contract:

- `ServiceBusCommandDestinationDiagnostic`
- `ServiceBusCommandDestinationSource`
- `ServiceBusEventDestination`
- `ServiceBusEventDestinationDiagnostic`
- `ServiceBusEventDestinationKind`
- `ServiceBusEventDestinationSource`
- `ServiceBusReceiveSourceDiagnostic`
- `ServiceBusReceiveSourceEventSubscriptionDiagnostic`

Advanced composition API:

- `BondstoneServiceBusBuilderExtensions`
  (`BondstoneOutboxBuilder.UseServiceBusTransport(...)` overload)
- `IServiceBusReceivedMessageDispatcher`
- `IServiceBusReceivedMessageHandler`
- `ServiceBusReceivedMessageMapper`
- `ServiceBusReceiveSource`
- `ServiceBusReceiveSourceKind`
- `ServiceBusTransportMessage`

Provider/runtime contract:

- `IServiceBusMessageSender`
- `IServiceBusOutboxDestinationResolver`
- `IServiceBusOutboxEventDestinationResolver`
- `IServiceBusTopologyDiagnostics`

Public implementation detail exposed for now:

- `BondstoneServiceBusHeaders`
- `ServiceBusDurableMessageEnvelope`
- `ServiceBusDurableOutboxTransport`

## Bondstone.Transport.RabbitMq

Normal setup API:

- `BondstoneRabbitMqBuilderExtensions`
  (`BondstoneBuilder.UseRabbitMqTransport(...)` overload)
- `BondstoneRabbitMqServiceCollectionExtensions`
- `BondstoneRabbitMqTransportBuilder`
- `BondstoneRabbitMqModuleRouteBuilder`
- `BondstoneRabbitMqEventRouteBuilder`
- `BondstoneRabbitMqReceiveQueueBuilder`
- `RabbitMqReceiveWorkerOptions`

User application contract:

- `RabbitMqCommandRoutingDiagnostic`
- `RabbitMqCommandRoutingSource`
- `RabbitMqEventRoutingDiagnostic`
- `RabbitMqEventRoutingSource`
- `RabbitMqPublishDestination`
- `RabbitMqPublishDestinationKind`
- `RabbitMqReceiveQueueDiagnostic`
- `RabbitMqReceiveQueueEventSubscriptionDiagnostic`

Advanced composition API:

- `BondstoneRabbitMqBuilderExtensions`
  (`BondstoneOutboxBuilder.UseRabbitMqTransport(...)` overload)
- `IRabbitMqReceivedMessageDispatcher`
- `IRabbitMqReceivedMessageHandler`
- `RabbitMqReceivedMessageMapper`
- `RabbitMqTransportMessage`

Provider/runtime contract:

- `IRabbitMqMessagePublisher`
- `IRabbitMqOutboxCommandRouteResolver`
- `IRabbitMqOutboxEventRouteResolver`
- `IRabbitMqTopologyDiagnostics`

Public implementation detail exposed for now:

- `BondstoneRabbitMqHeaders`
- `RabbitMqDurableMessageEnvelope`
- `RabbitMqDurableOutboxTransport`

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
