# Module Pipeline And Capability Runtime

Priority: High

Goal: replace implicit system pipeline composition with an explicit module
runtime plan before adding Domain Events as a Bondstone capability.

## Why Now

Bondstone command and event subscriber execution already uses middleware-like
pipeline behaviors. That approach is conceptually sound: transactions, inbox
idempotency, operation state, execution context, application behaviors, and
handlers are sequential runtime concerns.

The current implementation relies on resolving system behaviors from DI,
sorting them by integer order, and appending application behaviors. That works
for the current surface, but new capabilities such as Domain Events need more
intentional placement. Domain event collection should run inside the module
transaction and execution context, after application behavior and handler
logic, and before transaction commit. That ordering should be a runtime plan,
not a convention hidden in scattered order values.

## Scope

- Review command and event subscriber pipeline assembly.
- Decide whether Bondstone should build internal pipeline plans from module
  registration, provider registration, and enabled capabilities.
- Decide how providers contribute transaction and persistence-related system
  steps for only the modules they own.
- Decide how optional capabilities such as Domain Events contribute system
  steps without requiring broad package-boundary churn.
- Clarify normal user pipeline behaviors versus advanced system/provider
  pipeline behaviors.
- Preserve current command and event subscriber behavior while making the
  composition model explicit and testable.

## Related ADRs

- [0025 Module Command Execution Boundary](../adr/0025-module-command-execution-boundary.md)
- [0028 Domain Event Persistence Capability](../adr/0028-domain-event-persistence-capability.md)
- [0032 Module-Owned Durable EF Persistence](../adr/0032-module-owned-durable-ef-persistence.md)
- [0036 Direct Transport Adapters And Rebus Removal](../adr/0036-direct-transport-adapters-and-rebus-removal.md)
- [0042 Module Persistence Capability Metadata](../adr/0042-module-persistence-capability-metadata.md)
- [0045 Module Execution Context Semantics](../adr/0045-module-execution-context-semantics.md)
- [0046 Public API Surface Policy](../adr/0046-public-api-surface-policy.md)

## Design Questions

- Should pipeline plans be built per module route/subscriber from module
  registration metadata and DI descriptors, or should the current DI
  enumeration remain the source of truth with stronger validation?
- Should system steps use named slots such as transaction, operation state,
  receive inbox, execution context, domain events, application behaviors, and
  handler instead of raw integer order?
- How should a provider package contribute a transaction step? For example,
  EF Core and direct PostgreSQL both currently register transaction behaviors
  and no-op for modules they do not own.
- How should a capability package contribute a step? Domain Events may need a
  core capability step plus provider-specific collection/persistence support.
- Should pipeline plans cache descriptors and factories only, while resolving
  scoped behavior instances per execution?
- Should user application behaviors stay DI-registered, become module-scoped
  registration APIs, or support both?
- Should `IModuleCommandSystemPipelineBehavior<T>` and
  `IModuleEventSubscriberSystemPipelineBehavior<T>` remain public advanced
  composition contracts, or become implementation details behind provider and
  capability registration APIs?
- Does Domain Events belong in `Bondstone` core, a
  `Bondstone.DomainEvents` capability package, and/or provider-specific
  integration packages?
- Should broader persistence or transport abstractions be extracted from core,
  or should core continue owning provider-neutral contracts until there is a
  concrete package-boundary reason to split them?

## Candidate Direction

The likely direction is an internal module pipeline planner that builds an
execution plan from:

- core system steps;
- provider-owned steps enabled by the target module's persistence provider;
- optional capability steps enabled by module or package registration;
- user application behaviors;
- the final handler.

The planner should preserve the current behavior while making order and
activation explicit. It should cache plans or descriptors, not scoped behavior
instances.

Normal users should continue to use application pipeline behaviors for
validation, logging, authorization, auditing, and similar concerns. System
pipeline behaviors should be treated as advanced provider/runtime/capability
composition until the cleanup track decides whether they remain public.

Domain Events should wait for this runtime model before implementing DE-02 and
DE-03 in [10-domain-events.md](10-domain-events.md).

## Implementation Backlog

### MPC-01: Runtime Design And ADR

Priority: P0

Explore the current command and event subscriber pipeline code, provider
transaction behavior registration, Domain Events ADR 0028, and public API
policy ADR 0046. Produce the smallest ADR or ADR amendment needed before
rewriting pipeline assembly.

The decision should explicitly cover whether the rewrite is internal only, how
providers/capabilities contribute steps, and whether any public API or package
boundary changes are required before implementation.

Verification:

- `pnpm format:check`

### MPC-02: Command Pipeline Planner

Priority: P0 after MPC-01.

Move command route execution from ad hoc DI enumeration into an internal plan
or planner while preserving current runtime behavior.

Important files:

- `src/Bondstone/Modules/Routing/ModuleCommandRoute.cs`
- `src/Bondstone/Modules/Execution`
- `src/Bondstone/Configuration/BondstoneServiceCollectionExtensions.cs`
- `tests/Bondstone.Tests/Modules`
- `tests/Bondstone.Composition.Tests`

Verification:

- `pnpm backend:build`
- `pnpm backend:test:fast`

### MPC-03: Event Subscriber Pipeline Planner

Priority: P0 after MPC-01.

Move event subscriber execution from ad hoc DI enumeration into the same
planning model while preserving current receive, transaction, execution
context, and application behavior semantics.

Important files:

- `src/Bondstone/Modules/Events/ModuleEventSubscriberRegistration.cs`
- `src/Bondstone/Modules/Execution`
- `tests/Bondstone.Tests/Modules`

Verification:

- `pnpm backend:build`
- `pnpm backend:test:fast`

### MPC-04: User Behavior Extension Guidance

Priority: P1.

Document and test the supported way for users to add command and event
subscriber application behaviors. Decide whether module-scoped behavior
registration is needed now or should remain future work.

Important files:

- `docs/architecture/modules.md`
- `docs/setup.md`
- `tests/Bondstone.Tests/Modules`

Verification:

- `pnpm format:check`
- `pnpm backend:test:fast`

### MPC-05: Capability Contribution Model

Priority: P1 before Domain Events implementation.

Define how optional capabilities contribute system pipeline steps and how
those steps are activated by module registration. Use Domain Events as the
first concrete capability pressure test, but avoid overfitting the model to
one feature.

Important files:

- `docs/backlog/10-domain-events.md`
- `docs/adr/0028-domain-event-persistence-capability.md`
- future Domain Events package or core registration files, if accepted

Verification:

- `pnpm format:check`
- `pnpm backend:test:fast` if code changes.

### MPC-06: Capability Package Boundary Decision

Priority: P1.

Decide whether Domain Events should live in core, a
`Bondstone.DomainEvents` capability package, provider-specific integration
packages, or a combination. Defer broader persistence and transport package
extraction unless this work exposes a concrete package-boundary problem.

Verification:

- `pnpm format:check`
- `pnpm backend:pack` if package files change.

## Verification

- `pnpm format:check`
- `pnpm backend:build`
- `pnpm backend:test:fast`
