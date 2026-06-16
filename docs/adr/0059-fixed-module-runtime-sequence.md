# 0059 Fixed Module Runtime Sequence

Status: Superseded
Application: Not Applicable
Date: 2026-06-16

## Context

Post-MVP transport simplification removed most of the pressure that originally
justified a generalized module runtime contribution system. The public
`ModuleCommandPipelineContribution`,
`ModuleEventSubscriberPipelineContribution`, step-kind enum, and system order
constants made Bondstone look like a small framework for arbitrary runtime
pipeline composition.

The actual active runtime needs are narrower:

- core must establish module execution context;
- EF Core must own the module transaction when configured;
- receive execution must use inbox idempotency when a receive context exists;
- command receive with an operation id must stage completion after successful
  handling;
- command validation and application pipeline behavior must remain supported;
- EF-backed domain event persistence must run after handler/application logic
  and before EF `SaveChangesAsync`;
- EF domain event source clearing still needs observed-commit coordination.

Provider packages still live in separate assemblies, so package collaboration
cannot rely on friend assemblies or internal access. Any cross-package runtime
slot that remains must be explicit, small, and documented as provider/runtime
surface rather than normal application setup.

## Decision

Remove the generalized public module pipeline contribution API:

- `ModuleCommandPipelineContribution`;
- `ModuleEventSubscriberPipelineContribution`;
- `ModulePipelineContributionRegistry`;
- `ModulePipelineStepKind`;
- `ModuleCommandSystemPipelineOrder`;
- `ModuleEventSubscriberSystemPipelineOrder`;
- `BondstoneModuleBuilder.AddCommandPipelineContribution(...)`;
- `BondstoneModuleBuilder.AddEventSubscriberPipelineContribution(...)`.

Module command execution uses a fixed internal sequence:

1. provider transaction runtime behaviors;
2. durable operation completion behavior;
3. receive inbox behavior;
4. module execution context behavior;
5. provider post-handler runtime behaviors;
6. command validation behavior;
7. application `IModuleCommandPipelineBehavior<TCommand>` behaviors;
8. handler.

Module event subscriber execution uses a fixed internal sequence:

1. provider transaction runtime behaviors;
2. receive inbox behavior;
3. module execution context behavior;
4. provider post-handler runtime behaviors;
5. application `IModuleEventSubscriberPipelineBehavior<TEvent>` behaviors;
6. handler.

Keep application pipeline behavior contracts as the normal user extension
point. They run after Bondstone/provider runtime concerns have established the
module execution boundary, inbox boundary, transaction feature, and optional
post-handler hooks.

Add small hidden provider/runtime slot contracts in `Bondstone`:

- `IModuleCommandTransactionPipelineBehavior<TCommand>`;
- `IModuleEventSubscriberTransactionPipelineBehavior<TEvent>`;
- `IModuleCommandPostHandlerPipelineBehavior<TCommand>`;
- `IModuleEventSubscriberPostHandlerPipelineBehavior<TEvent>`;
- marker `IModuleRuntimePipelineBehavior`.

Provider runtime behaviors register in DI under those fixed slot contracts.
The core planner excludes `IModuleRuntimePipelineBehavior` services from the
application behavior list.

Add simple module runtime-option metadata for explicit package opt-ins.
Provider packages may set and read string runtime options through hidden
module builder/registration APIs when a feature needs module-specific opt-in
without a generalized capability registry.

EF Core persistence registers transaction runtime behaviors in the transaction
slot. EF-backed domain event persistence registers post-handler behaviors and
uses explicit runtime-option metadata to activate only for modules that call
`UseEntityFrameworkCoreDomainEventPersistence()`. That opt-in now requires the
module to declare EF persistence first.

Keep `ModuleRuntimeFeatureCollection`, `IModuleRuntimeExecutionContext`, and
`IModuleTransactionFeature` as hidden provider/runtime coordination contracts
for now. EF transaction behavior still publishes `IModuleTransactionFeature`,
and EF domain event behavior still consumes it for clear-after-observed-commit.

## Consequences

Normal consumers see a smaller story: register modules, choose persistence,
optionally add EF domain event persistence, and add application pipeline
behaviors when needed.

Bondstone no longer exposes named runtime slots, contribution ordering, or a
module-level API for arbitrary runtime contribution injection. This reduces
the framework-like surface and removes tests/docs dedicated to contribution
ambiguity.

Provider collaboration remains possible through fixed runtime slots. This is
still public API because packages are separate assemblies, but the surface is
smaller and less open-ended than the removed contribution model.

The fixed sequence preserves current EF/domain-event transaction semantics:
domain event records are staged before EF `SaveChangesAsync`, and sources are
cleared only after an observed commit.

Provider runtime slot proliferation is a risk. New slots require ADR review;
normal application behavior should stay on the existing application pipeline
contracts.

## Related Decisions

- Superseded by
  [0060 Direct Module Runtime Execution](0060-direct-module-runtime-execution.md).
- Amends
  [0050 Module Pipeline Feature Context](0050-module-pipeline-feature-context.md).
- Applies the runtime simplification direction from
  [0056 Post-MVP Communication And Transport Simplification](0056-post-mvp-communication-and-transport-simplification.md).
- Follows
  [0058 Domain Events In Core And EF Persistence](0058-domain-events-in-core-and-ef-persistence.md).
- Relates to
  [0046 Public API Surface Policy](0046-public-api-surface-policy.md).

## Application Notes

- Current contract: module execution uses fixed internal command and event
  subscriber runtime sequences. Application pipeline behavior contracts remain
  the user extension point. Provider packages use hidden fixed-slot runtime
  behavior contracts and hidden module runtime-option metadata only where
  cross-package runtime collaboration is required.
- Stable docs: current behavior is described in
  [docs/architecture/modules.md](../architecture/modules.md),
  [docs/architecture/persistence-ef-core.md](../architecture/persistence-ef-core.md),
  and [docs/public-api.md](../public-api.md).
- Agent guidance: root [AGENTS.md](../../AGENTS.md) already requires ADR
  review for public API, provider, durable behavior, and runtime changes.
- Application evidence: contribution records, registry, order constants,
  configuration validator, module builder contribution methods, and
  contribution-specific tests were removed. EF transaction/domain-event
  behavior now registers through fixed runtime slots. Public API baselines were
  refreshed.
- Pending or deferred: `ModuleRuntimeFeatureCollection`,
  `IModuleRuntimeExecutionContext`, and `IModuleTransactionFeature` remain
  public hidden provider/runtime contracts. Further reduction would require a
  package-collaboration alternative.

## Verification

- `dotnet build Bondstone.slnx --configuration Release --no-restore --disable-build-servers`
- `dotnet test tests/Bondstone.Persistence.EntityFrameworkCore.Tests/Bondstone.Persistence.EntityFrameworkCore.Tests.csproj --configuration Release --no-build --disable-build-servers --filter "FullyQualifiedName~EntityFrameworkCoreModuleTransactionBehaviorTests|FullyQualifiedName~EntityFrameworkCoreDomainEventPersistenceTests"`
- `dotnet test tests/Bondstone.Tests/Bondstone.Tests.csproj --configuration Release --no-build --disable-build-servers --filter "FullyQualifiedName~ModuleCommandRegistrationTests|FullyQualifiedName~ModuleEventSubscriberExecutionTests"`
- `dotnet test tests/Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests/Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests.csproj --configuration Release --no-build --disable-build-servers --filter "FullyQualifiedName~PostgreSqlDomainEventTransactionTests"`
- `BONDSTONE_UPDATE_PUBLIC_API_BASELINE=1 dotnet test tests/Bondstone.PublicApi.Tests/Bondstone.PublicApi.Tests.csproj --configuration Release --no-build --disable-build-servers`
- `dotnet test tests/Bondstone.PublicApi.Tests/Bondstone.PublicApi.Tests.csproj --configuration Release --no-restore --disable-build-servers`
- `dotnet test Bondstone.slnx --configuration Release --no-build --disable-build-servers --filter "Category=Unit|Category=Application"`
- `pnpm format:check`
- `pnpm backend:pack`
- `rg -n "ModuleCommandPipelineContribution|ModuleEventSubscriberPipelineContribution|ModulePipelineContributionRegistry|ModulePipelineStepKind|ModuleCommandSystemPipelineOrder|ModuleEventSubscriberSystemPipelineOrder|AddCommandPipelineContribution|AddEventSubscriberPipelineContribution|ModuleRuntimePipelineConfigurationValidator|EntityFrameworkCoreDomainEventModulePipelineOrder" src tests docs/architecture docs/setup.md docs/packaging.md docs/public-api.md docs/todos/post-mvp-architecture-and-consumer-feedback-plan.md -g '*.cs' -g '*.md'`
