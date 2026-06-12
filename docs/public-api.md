# Public API Surface

This document records the current public API classification for Bondstone
packages as cleanup work applies ADR 0046. It is an inventory and guidance
document, not a source of new compatibility promises.

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

## Current Scope

This first pass covers the core durable messaging and persistence packages:

- `Bondstone`
- `Bondstone.Persistence`
- `Bondstone.Persistence.EntityFrameworkCore`
- `Bondstone.Persistence.EntityFrameworkCore.Postgres`
- `Bondstone.Persistence.Postgres`

Hosting, transport, and optional capability packages still need the same
classification pass.

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
- `IDurableCommand`
- `IIntegrationEvent`
- `ICommandHandler<TCommand>`
- `IIntegrationEventHandler<TEvent>`
- `ICommandValidator<TCommand>`
- `IModuleCommandPipelineBehavior<TCommand>`
- `IModuleEventSubscriberPipelineBehavior<TEvent>`
- `IBondstoneModule`
- `IDurableCommandSender`
- `IDurableEventPublisher`
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
- `DurablePayloadJsonOptions`
- `ModuleCommandExecutionResult`
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

Cleanup candidate:

- `StringExtensions` is a package utility used broadly inside Bondstone and
  transport packages. Its current public visibility should be reviewed before
  stronger compatibility expectations, because it is not documented as an
  application extension point.

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

## Follow-Up

- Classify `Bondstone.Hosting`, `Bondstone.Transport`,
  `Bondstone.Transport.Local`, `Bondstone.Transport.ServiceBus`,
  `Bondstone.Transport.RabbitMq`, `Bondstone.Capabilities.DomainEvents`, and
  `Bondstone.Capabilities.DomainEvents.EntityFrameworkCore`.
- Decide whether cleanup candidates need a compatibility plan, release-note
  treatment, or a public API baseline before code changes.
- Keep package README files focused on normal setup paths and link here for
  advanced composition classification.
