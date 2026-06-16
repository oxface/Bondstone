# Public API Surface

This document records the current public API classification for Bondstone
packages as cleanup work continues. It is an inventory and guidance document,
not a source of new compatibility promises.

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
requires ADR review before implementation when it changes the durable package
or compatibility contract.

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
  `RabbitMqDurableEnvelopeDispatcher` during the intermediate transport
  refactor. The RabbitMQ package was later removed from the active package set.
- The renamed envelope dispatcher contract uses `DispatchAsync(...)` to make
  the neutral handoff about dispatching persisted Bondstone envelopes, not
  owning broker transport runtime.
- `Bondstone.Transport.RabbitMq` was removed from the active package set.
  Broker integration is now app-owned through `IDurableEnvelopeDispatcher`,
  `IDurableMessageEnvelopeSerializer`, and `IDurableEnvelopeReceiver`.
- This is an intentional compatibility-breaking public API cleanup made while
  external usage is still bounded.

Thin broker adapters, 2026-06-16:

- `Bondstone.Transport.RabbitMq` and `Bondstone.Transport.ServiceBus` are
  active thin adapter packages again, but only for native-driver envelope
  dispatch and explicitly registered receive workers.
- The public surface is intentionally limited to builder extensions,
  dispatcher options, receive worker options, and small destination/binding
  values.
- Topology, provisioning, retry, dead-letter, prefetch, concurrency,
  credentials, monitoring, and Rebus integration remain outside Bondstone.

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

Runtime coordination cleanup, 2026-06-16:

- `ModuleRuntimeFeatureCollection` was removed from the public core surface.
  The generic feature bag was broader than the remaining runtime coordination
  need.
- `IModuleTransactionFeature` was removed from `Bondstone.Persistence`.
- `IModuleRuntimeExecutionContext` now exposes the narrow observed-transaction
  callback surface used by provider transaction runners and provider
  post-handler actions. This keeps EF transaction/domain-event coordination
  possible without a generic runtime feature collection.

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

Durable operation handles, 2026-06-16:

- `DurableOperationHandle` is an additive `Bondstone.Persistence` user
  application contract carrying a durable operation id, source module, and
  target module.
- `DurableCommandSendResult.Operation`, `.SourceModule`, and `.TargetModule`
  are additive metadata for durable sends that include an operation id.
- Handle-based `IDurableOperationReader` and `IDurableOperationResultReader`
  overloads are additive convenience paths that query the target module store.

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

V2 public API cleanup start, 2026-06-16:

- The inventory is package-based. Types that appear in one package baseline
  only because a public member references another package's public type should
  be classified where the type is defined.
- Documented normal setup APIs stay public unless a replacement setup path is
  approved. Cleanup should not hide broad setup builders simply because they
  are convenient.
- Provider/runtime contracts may remain public when package collaboration needs
  explicit contracts. Prefer small, named contracts over production
  `InternalsVisibleTo`.
- Public concrete implementation types remain cleanup candidates only when a
  v2 decision can either hide them, replace them with a deliberate contract, or
  document them as advanced composition.
- Receive worker settlement options remain decision candidates. The current
  RabbitMQ `AutoAck` option is provider-native and unsafe for production
  durable receive paths that rely on Bondstone's inbox/outbox transaction, but
  renaming or removing it is a breaking adapter API decision.

Transport receive settlement cleanup, 2026-06-16:

- `RabbitMqReceiveWorkerOptions.AutoAck` was removed. The RabbitMQ receive
  worker now always consumes with manual acknowledgement and acknowledges only
  after Bondstone durable receive succeeds.
- `RabbitMqReceiveWorkerOptions.RequeueOnFailure` remains a bool because it is
  only the native RabbitMQ nack requeue flag. Bondstone still does not own
  broker retry, delivery-count, dead-letter, or requeue policy.
- `ServiceBusReceiveWorkerOptions.ProcessorOptions` remains public as the
  deliberate advanced native-driver escape hatch for the opt-in receive
  worker. `AutoCompleteMessages = true` is rejected during worker registration
  because Bondstone requires manual completion after durable receive succeeds.

Remaining v2 decisions for approval:

- EF Core entity and configuration types: decide which are deliberate mapping
  contracts and which can become internal once mapping helpers cover normal
  use.
- Provider concrete stores and dispatchers: decide whether advanced manual
  composition needs direct construction, or whether public interfaces plus
  service registration helpers are enough.
- Hosting concrete worker types: decide whether direct construction remains an
  advanced scheduler/testing contract or should be hidden behind registration
  helpers.
- `SystemTextJsonDurablePayloadSerializer` and
  `SystemTextJsonDurableMessageEnvelopeSerializer`: decide whether these
  default implementations are deliberate app-facing defaults or only DI
  implementation details.

## Current Scope

This first pass covers all current package projects:

- `Bondstone`
- `Bondstone.Hosting`
- `Bondstone.Persistence`
- `Bondstone.Persistence.EntityFrameworkCore`
- `Bondstone.Persistence.EntityFrameworkCore.Postgres`
- `Bondstone.Transport.Local`
- `Bondstone.Transport.RabbitMq`
- `Bondstone.Transport.ServiceBus`

`Bondstone.Persistence.Postgres`, `Bondstone.Transport`, and
the old broad `Bondstone.Transport.ServiceBus` surface were removed from the
active product surface. The current RabbitMQ and Service Bus packages are thin
adapter packages and are covered by the current default public API baseline.

## Bondstone

Normal setup API:

- `BondstoneServiceCollectionExtensions`
- `BondstoneEnvelopeDispatcherBuilderExtensions`
- `BondstoneBuilder`
- `BondstoneOutboxBuilder`
- `BondstoneModuleBuilderExtensions`
- `BondstoneModuleBuilder`
- `BondstoneModuleCommandBuilder`
- `BondstoneModuleEventBuilder`
- `BondstoneDurablePayloadServiceCollectionExtensions`

Extraction setup API:

- `BondstoneBuilder.RegisterMessage<TMessage>()`
- `BondstoneBuilder.RegisterMessagesFromAssembly(...)`
- `BondstoneBuilder.RegisterMessagesFromAssemblyContaining<TMarker>()`

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
- `IDomainEvent`
- `DomainEventIdentityAttribute`
- `IDomainEventSource`
- `IBondstoneModule`
- `IDurableCommandSender`
- `IDurableEventPublisher`
- `IDurableOperationExpirationProcessor`
- `IDurableOperationFinalizer`
- `IDurableOperationResultReader`
- `IDurableEnvelopeReceiver`
- `IDurablePayloadSerializer`
- `IMessageTypeRegistry`
- `IModuleExecutionContextAccessor`
- `DurableCommandIdentityAttribute`
- `IntegrationEventIdentityAttribute`
- `DurableEnvelopeReceiveBinding`
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
- `ModuleCommandReceiveContext`
- `ModuleEventSubscriberReceiveContext`
- `ModuleExecutionContext`
- `IModuleRuntimeExecutionContext`
- `IModuleTransactionRunner`
- `IModulePostHandlerAction`

Public implementation detail exposed for now:

- `MessageTypeRegistry`
- `SystemTextJsonDurablePayloadSerializer`

## Bondstone.Persistence

User application contract:

- `IDurableOperationReader`
- `DurableOperationHandle`
- `DurableOperationState`
- `DurableOperationDiagnosticContext`
- `DurableOperationStatus`
- `DurableMessageEnvelope`
- `MessageTraceContext`
- `MessageKind`
- `IDurableMessageEnvelopeSerializer`
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
- `DurableOutboxFailurePolicy`
- `RoutedDurableEnvelopeDispatcher`

Provider/runtime contract:

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
- `DurableModulePersistenceRegistrationRegistry`
- `DurableModuleOutboxWriterRegistration`
- `DurableModuleInboxHandlerExecutorRegistration`
- `DurableModuleInboxInspectionStoreRegistration`
- `DurableModuleOperationStateStoreRegistration`
- `DurableModuleOutboxDispatcherRegistration`
- `DurableModuleOutboxInspectionStoreRegistration`

Public implementation detail exposed for now:

- `DurableInboxHandlerExecutor`
- `DurableOutboxDispatcher`
- `SystemTextJsonDurableMessageEnvelopeSerializer`
- `DurableModuleOutboxDispatchAggregator`

## Bondstone.Persistence.EntityFrameworkCore

Normal setup API:

- `BondstoneEntityFrameworkCoreServiceCollectionExtensions`
- `BondstoneEntityFrameworkCoreModuleBuilderExtensions`
- `BondstoneModelBuilderExtensions`
- `BondstoneEntityFrameworkCoreDomainEventModuleBuilderExtensions`
- `BondstoneDomainEventModelBuilderExtensions`

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
- `DomainEventRecordEntity`
- `DomainEventRecordEntityConfiguration`
- `DomainEventRecordEntityConfiguration.Columns`

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
- `RabbitMqEnvelopeDispatcherOptions`
- `RabbitMqEnvelopeDestination`
- `RabbitMqReceiveWorkerOptions`

## Bondstone.Transport.ServiceBus

Normal setup API:

- `BondstoneServiceBusBuilderExtensions`
- `ServiceBusEnvelopeDispatcherOptions`
- `ServiceBusReceiveWorkerOptions`
