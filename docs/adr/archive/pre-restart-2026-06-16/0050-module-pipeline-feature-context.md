# 0050 Module Pipeline Feature Context

Status: Archived
Application: Not Applicable
Date: 2026-06-11

## Context

Bondstone module command and integration-event subscriber execution are built
as ordered pipelines. System behaviors own durable runtime concerns such as
module transactions, inbox handling, operation-state updates, execution
context, and optional provider-owned capabilities such as EF Core domain event
persistence.

Some system behaviors need to coordinate inside one module execution without
creating global or scoped mutable state. EF Core domain event persistence made
that pressure visible: domain event collection must run after handler logic
and before transaction-owned `SaveChangesAsync`, but pending domain events
must be cleared only after Bondstone observes an owned transaction commit.
Using an ambient or scoped source bag couples unrelated executions in the same
service scope and makes concurrency and nested execution harder to reason
about.

Because provider packages live in separate runtime assemblies, production
package collaboration cannot rely on friend assemblies. Provider/runtime
coordination contracts that must cross package boundaries need explicit
contracts, even when they are advanced composition APIs rather than normal
application setup APIs.

## Decision

Bondstone module execution contexts will own a per-execution feature
collection. `ModuleCommandExecutionContext` and
`ModuleEventSubscriberExecutionContext` expose a
`ModuleRuntimeFeatureCollection` for advanced provider/runtime composition.
The collection supports scoped typed feature push/pop and nearest active
feature lookup. Features are stored under the exact pushed contract type, so
providers should push and read shared features through provider-neutral
interfaces such as `IModuleTransactionFeature`. Feature scopes must be
disposed in reverse order across the whole collection.

System behaviors should use the feature collection to coordinate state that
belongs to the current pipeline execution. They must not use scoped mutable
bags or service-locator lookups for workflow-local state when the state can be
owned by the current execution context.

Bondstone adds a provider-neutral `IModuleTransactionFeature` advanced
runtime contract. Transaction behaviors may push this feature while their
transaction boundary is active. Optional capability behaviors can inspect the
feature and register commit or rollback callbacks without depending on EF
Core, PostgreSQL, or another concrete provider implementation.

EF Core module transaction behavior pushes an internal implementation of
`IModuleTransactionFeature`. EF Core domain event persistence collects and
stages domain event records through the EF `DbContext.ChangeTracker`, then
registers source clearing through the active transaction feature only when
the feature observes commit. Domain event behavior owns domain-event clearing;
transaction behavior remains generic and does not know about domain events.
Commit and rollback callbacks are lightweight runtime coordination hooks.
They run after Bondstone observes transaction completion, so callback failures
may surface after the transaction has already committed and must not be treated
as a durable work boundary.

This decision does not expose provider-specific active transaction objects to
application handlers yet. Future public access to provider persistence
features, such as EF `DbContext` or relational connection/transaction access
for custom durability, requires a separate API decision.

## Amendment 2026-06-16: Fixed Runtime Sequence

ADR 0059 removes the generalized public module pipeline contribution model
that surrounded this feature-context decision. Module execution now uses a
fixed runtime sequence plus small hidden provider/runtime slot contracts.

The per-execution feature collection and `IModuleTransactionFeature` remain
active provider/runtime coordination contracts. EF transaction behavior still
pushes `IModuleTransactionFeature`, and EF domain event persistence still uses
that feature for clear-after-observed-commit behavior.

## Amendment 2026-06-16: Direct Runtime Execution

ADR 0060 supersedes ADR 0059's remaining application-pipeline and
provider-pipeline behavior model. Module execution now uses direct internal
orchestration plus hidden provider/runtime service contracts.

The per-execution feature collection and `IModuleTransactionFeature` remain
active provider/runtime coordination contracts. EF transaction runtime still
pushes `IModuleTransactionFeature`, and EF domain event persistence still uses
that feature for clear-after-observed-commit behavior.

## Consequences

Provider/runtime behavior coordination becomes explicit and per execution.
This improves concurrency reasoning and removes EF domain event dependence on
ambient transaction-frame state.

The feature collection and transaction feature are public advanced
composition APIs because provider packages need them across package
boundaries. They are not the normal application extension path.

Feature-based coordination avoids adding a new property to execution context
for each optional capability, but it also creates an advanced surface that
must be documented carefully so normal users do not treat it as the first
setup path.

Feature collections are per pipeline execution. A nested or recursive command
or event-subscriber execution creates its own execution context and does not
inherit active features from the caller's pipeline. Cross-execution transaction
feature inheritance and nested execution semantics remain deferred.

Changing handler contracts to pass richer public command or event contexts,
exposing active provider persistence features to user code, and replacing
ambient source-module lookup for durable send/publish remain separate
compatibility-sensitive decisions.

## Related Decisions

- [0028 Domain Event Persistence Capability](0028-domain-event-persistence-capability.md)
- [0032 Module-Owned Durable EF Persistence](0032-module-owned-durable-ef-persistence.md)
- [0042 Module Persistence Capability Metadata](0042-module-persistence-capability-metadata.md)
- [0045 Module Execution Context Semantics](0045-module-execution-context-semantics.md)
- [0046 Public API Surface Policy](0046-public-api-surface-policy.md)
- [0059 Fixed Module Runtime Sequence](0059-fixed-module-runtime-sequence.md)
- [0060 Direct Module Runtime Execution](0060-direct-module-runtime-execution.md)

## Application Notes

- Current contract: module command and event subscriber execution contexts own
  a per-execution feature collection for advanced provider/runtime
  coordination. Module runtime execution is direct internal orchestration
  fixed by ADR 0060. EF Core module transaction runtime publishes
  `IModuleTransactionFeature`; EF Core domain event persistence consumes that
  transaction feature to clear pending sources only after observed
  Bondstone-owned commit.
- Stable docs: applied to
  [docs/architecture/modules.md](../architecture/modules.md),
  [docs/architecture/persistence-core.md](../architecture/persistence-core.md),
  and
  [docs/architecture/persistence-ef-core.md](../architecture/persistence-ef-core.md).
- Agent guidance: root [AGENTS.md](../../AGENTS.md) already requires ADR
  review before broad module runtime, public API, durable behavior, or
  provider behavior changes. No new agent instruction is needed.
- Application evidence: core execution contexts expose a feature collection;
  EF Core transaction behavior pushes `IModuleTransactionFeature`; EF Core
  domain event behavior registers clear-on-commit callbacks through that
  feature; the older EF domain event transaction coordinator and transaction
  completion callback abstraction were removed. ADR 0059 later removed public
  contribution records while keeping this feature-context contract. ADR 0060
  later removed public application pipeline behavior and fixed-slot pipeline
  behavior contracts while keeping this feature-context contract.
- Pending or deferred: public handler-context redesign, public active
  provider persistence features, module-scoped user behaviors, and a broad
  public capability registry remain deferred. Cross-execution feature
  inheritance and nested module execution transaction semantics also remain
  deferred.

## Verification

Verified with focused tests for module runtime features, EF domain event
behavior, and PostgreSQL-backed external transaction behavior, plus repository
format/build/fast-test checks reported with the implementation.
