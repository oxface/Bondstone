# 0060 Direct Module Runtime Execution

Status: Archived
Application: Not Applicable
Date: 2026-06-16

## Context

ADR 0059 removed generalized module pipeline contributions but kept public
application pipeline behavior contracts and hidden provider/runtime pipeline
behavior contracts. After transport simplification, that still leaves
Bondstone with framework-shaped execution machinery: planners, behavior
chains, runtime slots, and app middleware semantics.

Bondstone's current product lane is narrower. It should provide durable module
boundaries, result-returning command execution, EF-backed transaction
participation, receive inbox idempotency, operation completion, validation,
and EF-backed domain event persistence. It should not be the application
middleware framework for logging, authorization, metrics, or auditing.

Provider packages still live in separate assemblies, so EF transaction and
domain event persistence still need explicit cross-package runtime contracts.
Those contracts can be smaller than generic pipeline behavior interfaces.

## Decision

Supersede ADR 0059's application-pipeline and provider-pipeline behavior model.

Remove the public application pipeline behavior contracts:

- `IModuleCommandPipelineBehavior<TCommand>`;
- `IModuleEventSubscriberPipelineBehavior<TEvent>`;
- `ModuleCommandPipelineNext`;
- `ModuleEventSubscriberPipelineNext`.

Remove the hidden provider/runtime pipeline behavior contracts:

- `IModuleRuntimePipelineBehavior`;
- `IModuleCommandTransactionPipelineBehavior<TCommand>`;
- `IModuleEventSubscriberTransactionPipelineBehavior<TEvent>`;
- `IModuleCommandPostHandlerPipelineBehavior<TCommand>`;
- `IModuleEventSubscriberPostHandlerPipelineBehavior<TEvent>`.

Remove internal pipeline planners and plan/step records. Module command and
event subscriber execution should be direct orchestration code.

Module command execution uses this fixed runtime flow:

1. provider transaction runners wrap execution;
2. durable operation completion wraps receive execution when an operation id
   exists;
3. receive inbox handling wraps handler execution when a receive context
   exists;
4. module execution context is pushed;
5. command validators run;
6. the registered command handler runs;
7. provider post-handler actions run before the transaction owner saves and
   commits.

Module event subscriber execution uses this fixed runtime flow:

1. provider transaction runners wrap execution;
2. receive inbox handling wraps handler execution when a receive context
   exists;
3. module execution context is pushed;
4. the registered subscriber handler runs;
5. provider post-handler actions run before the transaction owner saves and
   commits.

Keep only two hidden provider/runtime extension contracts in `Bondstone`:

- `IModuleTransactionRunner`, which may wrap an execution context and
  operation when the provider owns that module's transaction;
- `IModulePostHandlerAction`, which may run after a successful handler and
  before transaction save/commit.

Provider implementations must no-op when they do not apply to the executing
module. Normal application code should not implement these contracts.

Command validation remains a first-class module command contract through
`ICommandValidator<TCommand>`. Other application concerns should use ordinary
handler code, DI decorators, endpoint filters, host-specific middleware, or
application frameworks chosen by the consumer app. Bondstone does not provide
an app pipeline middleware model.

Keep `ModuleRuntimeFeatureCollection`, `IModuleRuntimeExecutionContext`, and
`IModuleTransactionFeature` as hidden provider/runtime coordination contracts
for now. EF transaction behavior still publishes `IModuleTransactionFeature`,
and EF domain event persistence still consumes it for clear-after-observed
commit.

## Consequences

Normal consumers see a simpler library: register modules, handlers,
validators, durable messaging, persistence, and optional EF domain event
persistence.

Bondstone loses a generic app behavior extension point. This is intentional:
the extension point was more framework-shaped than the library's current
scope justifies.

Provider collaboration becomes smaller and more semantic. EF transaction
support is a transaction runner, not a command/event pipeline behavior. EF
domain event persistence is a post-handler action, not a wrapper with `next`.

The runtime remains internally wrapper-like where semantics require it:
transactions and inbox idempotency must control the boundary around handler
execution. That control is not exposed as a public application middleware
surface.

Result-returning command behavior remains first-class. Durable receive can
still serialize a result payload into operation state before provider
post-handler actions and transaction commit.

## Related Decisions

- Supersedes
  [0059 Fixed Module Runtime Sequence](0059-fixed-module-runtime-sequence.md).
- Narrows
  [0025 Module Command Execution Boundary](0025-module-command-execution-boundary.md).
- Amends
  [0050 Module Pipeline Feature Context](0050-module-pipeline-feature-context.md).
- Applies the simplification direction from
  [0056 Post-MVP Communication And Transport Simplification](0056-post-mvp-communication-and-transport-simplification.md).
- Follows
  [0058 Domain Events In Core And EF Persistence](0058-domain-events-in-core-and-ef-persistence.md).
- Relates to
  [0046 Public API Surface Policy](0046-public-api-surface-policy.md).

## Application Notes

- Current contract: module command and event subscriber execution use direct
  internal orchestration. Application pipeline behavior contracts are removed.
  Provider packages use hidden transaction-runner and post-handler-action
  contracts only where cross-package runtime collaboration is required.
- Stable docs: current behavior is described in
  [docs/architecture/modules.md](../architecture/modules.md),
  [docs/architecture/messaging.md](../architecture/messaging.md),
  [docs/architecture/persistence-ef-core.md](../architecture/persistence-ef-core.md),
  [docs/setup.md](../setup.md), [docs/package-discovery.md](../package-discovery.md),
  and [docs/public-api.md](../public-api.md).
- Agent guidance: root [AGENTS.md](../../AGENTS.md) already requires ADR
  review for public API, provider, durable behavior, and runtime changes.
- Application evidence: public application pipeline behavior contracts,
  hidden fixed-slot provider pipeline behavior contracts, pipeline planners,
  and pipeline behavior implementation wrappers were removed. Command and
  event subscriber routes now invoke direct internal runtime services. EF Core
  persistence registers `IModuleTransactionRunner`; EF-backed domain event
  persistence registers `IModulePostHandlerAction`. Core string runtime-option
  metadata was removed; EF-backed domain event persistence now uses EF-owned
  module opt-in metadata. Concrete command and event subscriber execution
  contexts were internalized. Public API baselines were refreshed.
- Pending or deferred: RabbitMQ receive topology simplification remains a
  separate transport-adapter slice.

## Verification

- `dotnet build Bondstone.slnx --configuration Release --no-restore --disable-build-servers`
- `dotnet test tests/Bondstone.Tests/Bondstone.Tests.csproj --configuration Release --no-build --disable-build-servers --filter "FullyQualifiedName~ModuleCommandRegistrationTests|FullyQualifiedName~ModuleEventSubscriberExecutionTests"`
- `dotnet test tests/Bondstone.Persistence.EntityFrameworkCore.Tests/Bondstone.Persistence.EntityFrameworkCore.Tests.csproj --configuration Release --no-build --disable-build-servers --filter "FullyQualifiedName~EntityFrameworkCoreModuleTransactionBehaviorTests|FullyQualifiedName~EntityFrameworkCoreDomainEventPersistenceTests"`
- `BONDSTONE_UPDATE_PUBLIC_API_BASELINE=1 dotnet test tests/Bondstone.PublicApi.Tests/Bondstone.PublicApi.Tests.csproj --configuration Release --no-build --disable-build-servers`
- `dotnet test tests/Bondstone.PublicApi.Tests/Bondstone.PublicApi.Tests.csproj --configuration Release --no-restore --disable-build-servers`
- `dotnet test Bondstone.slnx --configuration Release --no-build --disable-build-servers --filter "Category=Unit|Category=Application"`
- `pnpm format:check`
- `pnpm backend:pack`
- `rg -n "IModuleCommandPipelineBehavior|IModuleEventSubscriberPipelineBehavior|ModuleCommandPipelineNext|ModuleEventSubscriberPipelineNext|IModuleRuntimePipelineBehavior|IModuleCommandTransactionPipelineBehavior|IModuleEventSubscriberTransactionPipelineBehavior|IModuleCommandPostHandlerPipelineBehavior|IModuleEventSubscriberPostHandlerPipelineBehavior|ModuleCommandPipelinePlanner|ModuleEventSubscriberPipelinePlanner" src tests -g '*.cs'`
