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
replace architecture review, or replace release-note treatment.

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
requires SpecKit constitution/architecture review before implementation when
it changes the durable package or compatibility contract.

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
- `Bondstone.Transport.RabbitMq` was later returned as a thin adapter package.
  Broker integration remains app-owned around `IDurableEnvelopeDispatcher`,
  `IDurableMessageEnvelopeSerializer`, and durable inbox ingestion.
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

Non-throwing operation wait, 2026-06-17:

- `DurableOperationWaitResult<TResult>` is an additive user application
  contract that separates caller wait outcome from durable operation result
  state.
- `IDurableOperationResultReader.TryWaitForResultAsync<TResult>(...)`
  overloads are additive non-throwing wait APIs for operation-id-only,
  module-hinted, and handle-based observation.
- The API reports whether a terminal result was observed before caller timeout
  and returns the latest `DurableOperationResult<TResult>`. Timeout remains
  caller patience and does not write `Failed`, `Cancelled`, or any other
  operation state.

Immediate query boundary, 2026-06-19:

- `IQuery<TResult>`, `IQueryHandler<TQuery, TResult>`,
  `IModuleQueryExecutor`, `IModuleQueryRouteRegistry`, `ModuleQueryRoute`,
  and `BondstoneModuleQueryBuilder` are additive user application and
  advanced composition contracts for immediate read-only module queries.
- `BondstoneModuleBuilder.Queries` is an additive normal setup API. Query
  routes are module-owned and do not register durable message identities.
- Query execution deliberately does not set the command module execution
  context, so durable send/publish APIs continue to require command or
  subscriber execution context.

Durable incoming inbox provider-neutral contracts, 2026-06-18:

- `DurableIncomingInboxKey`, `DurableIncomingInboxRecord`,
  `DurableIncomingInboxState`, `DurableIncomingInboxStatus`,
  `DurableIncomingInboxIngestionResult`,
  `DurableIncomingInboxIngestionStatus`,
  `DurableIncomingInboxFailureDecision`, and
  `DurableIncomingInboxFailureDecisionKind` are provider-neutral records and
  values for the durable inbox incoming ledger. The names deliberately avoid
  colliding with the lower-level receive idempotency `DurableInboxRecord`.
- `IDurableIncomingInboxIngestionStore`, `IDurableIncomingInboxClaimer`,
  `IDurableIncomingInboxLeaseRenewer`,
  `IDurableIncomingInboxOutcomeRecorder`, and
  `IDurableIncomingInboxInspectionStore` are provider/runtime contracts for
  ingestion, claim, lease renewal, outcome recording, and inspection of the
  incoming ledger. Inspection supports broad status reads plus stale
  processing claim and terminal receive-failure queries for operations. This
  provider/runtime inspection contract is the current read-only durable receive
  evidence surface; no separate app-facing incoming inbox inspector is added in
  this release because the existing contract is public, registered by EF Core
  persistence, filterable, and non-mutating.
- `DurableIncomingInboxIngestionBoundary` and
  `IDurableIncomingInboxIngestionBoundaryResolver` are additive
  provider/runtime contracts for resolving the ingestion store and commit
  scope by receiver module before broker acknowledgement. The boundary exposes
  the paired store/scope and the common ingest-and-save operation used by
  adapters.
- `IDurableIncomingInboxDispatcher`,
  `DurableIncomingInboxDispatcher`,
  `IDurableIncomingInboxFailurePolicy`,
  `DurableIncomingInboxFailurePolicy`,
  `DurableIncomingInboxProcessingOptions`, and
  `DurableIncomingInboxProcessingResult` are host-callable processing APIs for
  the incoming ledger. They claim due rows, invoke the
  existing module receive pipelines, and record processed, retry, terminal
  failure, or stale outcomes through the incoming inbox store contracts.
- The PostgreSQL provider supplies runtime stores for incoming inbox claim,
  lease renewal, outcome recording, and module-owned processing dispatch.
  Bondstone does not provide operation failure inference or cleanup mutation
  APIs. Processing outcome is recorded after module receive returns, so a
  crash between the module receive commit and incoming-ledger outcome
  recording relies on the existing receive idempotency row during retry.

Durable incoming inbox EF Core mapping, 2026-06-18:

- `ApplyBondstonePersistence(...)` maps the durable incoming inbox table along
  with outbox, receive idempotency inbox, and operation state. This is an
  intentional v2 table-shape change so normal durable EF mapping can host
  durable receive.
- `ApplyBondstoneIncomingInbox(...)` remains a granular EF Core setup API for
  hosts that map selected Bondstone tables explicitly.
- `IncomingInboxMessageEntity` and
  `IncomingInboxMessageEntityConfiguration` expose the accepted durable inbox
  incoming ledger table shape and map `incoming_inbox_messages` by default.
- `EntityFrameworkCoreDurableIncomingInboxIngestionStore<TDbContext>` and
  `EntityFrameworkCoreDurableIncomingInboxInspectionStore<TDbContext>` are
  additive provider/runtime implementations for idempotent EF ingestion and
  read-only EF inspection. They are registered by
  `AddBondstoneEntityFrameworkCorePersistence<TDbContext>`.
- `DurableModuleIncomingInboxIngestionBoundaryRegistration` is an additive
  advanced module runtime registration type for provider packages. The EF
  module persistence opt-ins register one boundary per module so ingestion
  uses the receiver module's `DbContext`.
- PostgreSQL-specific incoming inbox mutation stores live in
  `Bondstone.Persistence.EntityFrameworkCore.Postgres`. Bondstone does not
  provide operation failure inference or cleanup mutation APIs.

Durable incoming inbox worker, 2026-06-18:

- `Bondstone.Hosting.IncomingInbox` is an additive normal setup namespace for
  the incoming inbox processing worker.
- `UseDurableIncomingInboxWorker(...)` and
  `AddBondstoneDurableIncomingInboxWorker(...)` are additive setup APIs over
  the existing `IDurableIncomingInboxDispatcher`.
- `DurableIncomingInboxWorkerOptions` is a public options type for worker id,
  batch size, polling interval, lease duration, failure delay, max attempts,
  and retry delays. The concrete worker and options validator remain internal
  DI implementation details.
- Bondstone does not provide provider-neutral transport ingestion workers,
  selected-module or source-transport worker filters, operation failure
  inference, or cleanup mutation APIs.
- `DurableModuleIncomingInboxDispatcherRegistration`,
  `DurableModulePersistenceRegistrationRegistry`, and
  `DurableModuleIncomingInboxDispatcherAggregator` are advanced
  provider/runtime composition types hidden from normal IntelliSense. They let
  provider packages aggregate module-owned durable incoming inbox processors
  behind the app-facing `IDurableIncomingInboxDispatcher`.

Stable setup codes, 2026-06-21:

- `Bondstone.Diagnostics.BondstoneSetupCodes` is an additive user application
  contract for stable setup-code string constants covering common
  Bondstone-owned misconfiguration failures. The contract currently lives in
  `Bondstone.Persistence` as provider-neutral runtime diagnostics because both
  core composition and provider-neutral persistence surfaces emit setup-code
  exceptions, and `Bondstone` may depend on that neutral contract package.
- `IBondstoneSetupException` is an additive diagnostic contract implemented by
  coded setup exceptions through the `SetupCode` property.
- `BondstoneSetupException` and `BondstoneSetupArgumentException` are additive
  exception types that derive from `InvalidOperationException` and
  `ArgumentException` respectively, preserving broad catch behavior while
  exposing machine-readable setup codes.
- Setup code names are intended to be stable for automation. Human-readable
  messages may improve, new codes may be added, and existing code names
  require compatibility review before rename or removal.

Story 8.1 validation, 2026-06-21:

- The packable project set, public API test assembly list, and checked-in
  baseline files all cover the same eight active packages:
  `Bondstone`, `Bondstone.Hosting`, `Bondstone.Persistence`,
  `Bondstone.Persistence.EntityFrameworkCore`,
  `Bondstone.Persistence.EntityFrameworkCore.Postgres`,
  `Bondstone.Transport.Local`, `Bondstone.Transport.RabbitMq`, and
  `Bondstone.Transport.ServiceBus`.
- Production package references still follow the dependency direction in
  [packaging.md](packaging.md). `InternalsVisibleTo` declarations under
  `src/**/Properties/AssemblyInfo.cs` target only package test assemblies or
  composition tests, not production package collaboration.
- The package-by-package classification below has been reconciled with the
  current baselines for query APIs, setup-code diagnostics, durable incoming
  inbox contracts, incoming inbox hosting, and EF incoming inbox mapping
  surfaces.
- No public/protected API shape changed in this validation slice, so no
  baseline refresh, replacement contract, migration note, or release-note entry
  is required here.

RabbitMQ durable incoming inbox ingestion handoff, 2026-06-18:

- `RabbitMqReceiveWorkerOptions.ReceiveCommand()` and `ReceiveEvent(...)`
  now select durable incoming inbox ingestion. This is an intentional v2
  behavior change away from the old direct broker-to-handler receive path.
- `RabbitMqReceiveWorkerOptions.IngestCommandToDurableIncomingInbox()` and
  `IngestEventToDurableIncomingInbox(...)` remain aliases for hosts that
  prefer to name the ingestion boundary directly.
- `RabbitMqReceiveWorkerOptions.SourceTransportName` is additive diagnostic
  metadata stored on durable incoming inbox rows. When omitted, RabbitMQ uses
  `rabbitmq:{QueueName}`.
- `IDurableIncomingInboxIngestionPersistenceScope` is an additive
  provider/runtime contract for committing staged incoming inbox ingestion
  before native broker settlement. The EF Core package registers an adapter
  over its existing EF persistence scope.
- The worker resolves `IDurableIncomingInboxIngestionBoundaryResolver` after
  creating the durable incoming inbox record, so module-owned persistence
  writes through the receiver module boundary while advanced single-store
  hosts can still use the root store/scope fallback.
- The ingestion mode does not execute handlers, complete operation state,
  stage outgoing outbox rows, or replace the `Bondstone.Hosting` incoming
  inbox processing worker.

Azure Service Bus durable incoming inbox ingestion handoff, 2026-06-19:

- `ServiceBusReceiveWorkerOptions.ReceiveCommand()` and `ReceiveEvent(...)`
  select durable incoming inbox ingestion for queue or subscription receive
  workers.
- The worker rejects `AutoCompleteMessages = true` and `ReceiveAndDelete`
  mode so native completion happens only after durable incoming inbox
  ingestion succeeds.
- The worker resolves the receiver module's durable incoming inbox ingestion
  boundary, persists the incoming ledger row, and completes the native message
  only after that persistence boundary succeeds.
- The ingestion mode does not execute handlers, complete operation state,
  stage outgoing outbox rows, mutate incoming inbox processing outcomes, or
  replace the `Bondstone.Hosting` incoming inbox processing worker.

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
  because Bondstone requires manual completion after durable incoming inbox
  ingestion succeeds. `ReceiveAndDelete` mode is rejected for the same reason.

V2 public API cleanup continuation, 2026-06-16:

- `DurableOutboxWorker` and `DurableOutboxWorkerOptionsValidator` are no
  longer public. Hosted worker construction and options validation are DI
  implementation details behind `UseWorker(...)` and
  `AddBondstoneDurableOutboxWorker(...)`.
- `DurableOutboxWorkerOptions` remains public because normal host setup
  configures it.
- EF Core entity and configuration types remain public for now as deliberate
  EF mapping and provider-runtime contracts. The mapping helpers are still the
  normal setup API, but the mapped CLR types, table names, column names,
  constraint names, and shared limits are also consumed by the PostgreSQL
  provider and can support migration or inspection-oriented advanced use.
- `SystemTextJsonDurablePayloadSerializer` remains public as the default
  durable payload serializer for normal setup and advanced manual composition.
  Applications normally configure it through `DurablePayloadJsonOptions` and
  `ConfigureBondstoneDurablePayloadJson(...)`, but direct construction remains
  useful for custom send/receive pipelines and tests.
- `SystemTextJsonDurableMessageEnvelopeSerializer` remains public as the
  default durable envelope serializer for app-owned or adapter-owned broker
  integration. Applications normally consume
  `IDurableMessageEnvelopeSerializer` from DI, but direct construction is a
  useful advanced composition path for native broker payload tests and custom
  transport code.
- PostgreSQL concrete SQL components are no longer public. The inbox
  registrar, module inbox executor, outbox claimer, outbox lease renewer,
  outbox dispatch recorder, and module outbox dispatcher are provider package
  implementation details behind public setup helpers and provider-neutral
  contracts.

Final v2 public API decision check, 2026-06-17:

- The remaining public implementation-detail sections were
  `MessageTypeRegistry`, the provider-neutral concrete persistence helpers,
  and the EF Core concrete stores/scope. They are now classified as deliberate
  normal defaults, advanced composition APIs, or provider/runtime concrete APIs
  instead of unresolved implementation-detail exposure.
- `MessageTypeRegistry` remains public for v2 as the default mutable message
  identity registry used by setup and as a useful direct-test/manual
  composition helper. Normal applications should depend on
  `IMessageTypeRegistry`.
- `DurableInboxHandlerExecutor`, `DurableOutboxDispatcher`,
  `DurableOutboxFailurePolicy`, `DurableModuleOutboxDispatchAggregator`, and
  `RoutedDurableEnvelopeDispatcher` remain public for v2. They are the shared
  concrete helpers used by Hosting, Local transport, PostgreSQL EF module
  composition, custom schedulers, and app-owned broker composition. Hiding
  them would require new public factory or service-registration contracts
  across packages, which is larger than an obvious cleanup.
- The EF Core concrete stores and `EntityFrameworkCorePersistenceScope` remain
  public for v2 as provider/runtime concrete APIs and advanced composition
  defaults. The EF and PostgreSQL setup helpers construct them across package
  boundaries, and direct construction remains useful for custom persistence
  composition, tests, and app-owned inspection/migration tooling. The
  provider-neutral interfaces remain the preferred dependency surface.
- No remaining public concrete type in this final slice is an obvious hide-now
  candidate.

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
- `BondstoneModuleQueryBuilder`
- `BondstoneDurablePayloadServiceCollectionExtensions`

Extraction setup API:

- `BondstoneBuilder.RegisterMessage<TMessage>()`
- `BondstoneBuilder.RegisterMessagesFromAssembly(...)`
- `BondstoneBuilder.RegisterMessagesFromAssemblyContaining<TMarker>()`

User application contract:

- `IMessage`
- `ICommand`
- `ICommand<TResult>`
- `IQuery<TResult>`
- `IDurableCommand`
- `IIntegrationEvent`
- `ICommandHandler<TCommand>`
- `ICommandHandler<TCommand, TResult>`
- `IQueryHandler<TQuery, TResult>`
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
- `DurableOperationWaitResult<TResult>`
- `DurableOperationExpirationResult`
- `DurableOperationFinalizationResult`
- `DurableOperationResultState`
- `DurablePayloadJsonOptions`
- `ModuleCommandExecutionResult`
- `ModuleCommandExecutionResult<TResult>`
- `ModuleEventSubscriberExecutionResult`

Normal default and advanced composition API:

- `MessageTypeRegistry`
- `SystemTextJsonDurablePayloadSerializer`

Advanced composition API:

- `IModuleCommandExecutor`
- `IModuleEventSubscriberExecutor`
- `IModuleCommandReceivePipeline`
- `IModuleEventReceivePipeline`
- `IBondstoneModuleRegistry`
- `IModuleCommandRouteRegistry`
- `IModuleQueryExecutor`
- `IModuleQueryRouteRegistry`
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
- `ModuleQueryRoute`
- `ModuleCommandReceiveContext`
- `ModuleEventSubscriberReceiveContext`
- `ModuleExecutionContext`
- `IModuleRuntimeExecutionContext`
- `IModuleTransactionRunner`
- `IModulePostHandlerAction`

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
- `DurableIncomingInboxFailureDecision`
- `DurableIncomingInboxFailureDecisionKind`
- `DurableIncomingInboxIngestionResult`
- `DurableIncomingInboxIngestionStatus`
- `DurableIncomingInboxKey`
- `DurableIncomingInboxProcessingResult`
- `DurableIncomingInboxRecord`
- `DurableIncomingInboxState`
- `DurableIncomingInboxStatus`
- `BondstoneSetupArgumentException`
- `BondstoneSetupCodes`
- `BondstoneSetupException`
- `IBondstoneSetupException`
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
- `SystemTextJsonDurableMessageEnvelopeSerializer`
- `DurableInboxHandlerExecutor`
- `DurableOutboxDispatcher`
- `DurableOutboxFailurePolicy`
- `DurableIncomingInboxDispatcher`
- `DurableIncomingInboxFailurePolicy`
- `DurableIncomingInboxProcessingOptions`
- `IDurableIncomingInboxDispatcher`
- `IDurableIncomingInboxFailurePolicy`
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
- `IDurableIncomingInboxClaimer`
- `IDurableIncomingInboxIngestionBoundaryResolver`
- `IDurableIncomingInboxIngestionPersistenceScope`
- `IDurableIncomingInboxIngestionStore`
- `IDurableIncomingInboxInspectionStore`
- `IDurableIncomingInboxLeaseRenewer`
- `IDurableIncomingInboxOutcomeRecorder`
- `DurableIncomingInboxIngestionBoundary`
- `DurableModulePersistenceRegistrationRegistry`
- `DurableModuleOutboxWriterRegistration`
- `DurableModuleInboxHandlerExecutorRegistration`
- `DurableModuleInboxInspectionStoreRegistration`
- `DurableModuleIncomingInboxDispatcherRegistration`
- `DurableModuleIncomingInboxIngestionBoundaryRegistration`
- `DurableModuleOperationStateStoreRegistration`
- `DurableModuleOutboxDispatcherRegistration`
- `DurableModuleOutboxInspectionStoreRegistration`

Provider/runtime concrete API:

- `DurableModuleOutboxDispatchAggregator`
- `DurableModuleIncomingInboxDispatcherAggregator`

## Bondstone.Persistence.EntityFrameworkCore

Normal setup API:

- `BondstoneEntityFrameworkCoreServiceCollectionExtensions`
- `BondstoneEntityFrameworkCoreModuleBuilderExtensions`
- `BondstoneModelBuilderExtensions`
- `BondstoneEntityFrameworkCoreDomainEventModuleBuilderExtensions`
- `BondstoneDomainEventModelBuilderExtensions`

Advanced composition API:

- `IEntityFrameworkCorePersistenceScope`
- `EntityFrameworkCorePersistenceScope<TDbContext>`

Provider/runtime contract:

- `EntityFrameworkCoreModulePersistence`
- `OutboxMessageEntity`
- `OutboxMessageEntityConfiguration`
- `OutboxMessageEntityConfiguration.Columns`
- `InboxMessageEntity`
- `InboxMessageEntityConfiguration`
- `InboxMessageEntityConfiguration.Columns`
- `IncomingInboxMessageEntity`
- `IncomingInboxMessageEntityConfiguration`
- `IncomingInboxMessageEntityConfiguration.Columns`
- `OperationStateEntity`
- `OperationStateEntityConfiguration`
- `DomainEventRecordEntity`
- `DomainEventRecordEntityConfiguration`
- `DomainEventRecordEntityConfiguration.Columns`

Provider/runtime concrete API:

- `EntityFrameworkCoreDurableInboxInspectionStore<TDbContext>`
- `EntityFrameworkCoreDurableIncomingInboxIngestionStore<TDbContext>`
- `EntityFrameworkCoreDurableIncomingInboxInspectionStore<TDbContext>`
- `EntityFrameworkCoreDurableOutboxWriter<TDbContext>`
- `EntityFrameworkCoreModuleDurableOutboxWriter<TDbContext>`
- `EntityFrameworkCoreDurableInboxStore<TDbContext>`
- `EntityFrameworkCoreDurableOperationStateStore<TDbContext>`
- `EntityFrameworkCoreModuleDurableOperationStateStore<TDbContext>`
- `EntityFrameworkCoreDurableOutboxInspectionStore<TDbContext>`

## Bondstone.Persistence.EntityFrameworkCore.Postgres

Normal setup API:

- `BondstonePostgreSqlBuilderExtensions`
- `BondstonePostgreSqlServiceCollectionExtensions`

Advanced composition API:

- `PostgreSqlPersistenceExceptionClassifier`

Provider implementation details hidden from public API:

- PostgreSQL concrete inbox registrar, module inbox executor, outbox claimer,
  lease renewer, dispatch recorder, and module outbox dispatcher classes are
  internal implementation details behind the public setup helpers and
  provider-neutral contracts.

## Bondstone.Hosting

Normal setup API:

- `BondstoneHostingBuilderExtensions`
- `BondstoneIncomingInboxHostingBuilderExtensions`
- `DurableIncomingInboxWorkerOptions`
- `DurableOutboxWorkerOptions`

Advanced composition API:

- `BondstoneHostingServiceCollectionExtensions`
- `BondstoneIncomingInboxHostingServiceCollectionExtensions`

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
