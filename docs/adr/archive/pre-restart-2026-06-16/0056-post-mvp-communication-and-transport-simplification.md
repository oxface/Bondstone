# 0056 Post-MVP Communication And Transport Simplification

Status: Archived
Application: Not Applicable
Date: 2026-06-16

## Context

Bondstone passed MVP and has been published as a public library, but current
usage is still limited to the maintainer's own template and learning projects.
That means compatibility pressure is real but bounded enough to cut
undercooked surface instead of preserving it indefinitely.

The MVP proved durable module execution, EF Core/PostgreSQL outbox and inbox
persistence, local transport, RabbitMQ and Service Bus direct adapters,
operation results, and EF-backed module-local domain event persistence. The
review after publication showed a mismatch in product shape:

- the persistence model is the strongest and clearest part of the library;
- transport packages have grown toward broker runtime ownership through
  topology DSLs, receive workers, route diagnostics, and validation matrices;
- local module command execution can be mistaken for cross-module
  communication, which violates modular-monolith boundaries when each module
  owns its own persistence transaction;
- result-returning commands are useful, but durable result observation should
  be tied to module command lifecycle rather than transport delivery details;
- the module pipeline has generalized runtime/capability extension machinery
  before there is enough package diversity to justify it.

Modular monolith guidance points toward strict module boundaries: modules own
their data and internal implementation, expose intentional contracts, and
communicate across module write boundaries through durable messages or
explicit public read models. A module should not synchronously call another
module and commit that other module's state inside the caller's local control
flow.

The decision affects runtime architecture, transport strategy, package
boundaries, public API, samples, and compatibility. It therefore needs an ADR
before broad implementation.

## Decision

Bondstone will simplify around a smaller durable module-boundary library
model.

The communication model is:

- App-owned entrypoints such as HTTP endpoints, schedulers, setup flows, and
  administrative jobs may execute one module command locally through
  `IModuleCommandExecutor`.
- Module handlers must not synchronously execute another module's command
  through local command execution. Cross-module writes use durable commands or
  integration events.
- Modules should not reference other module implementation assemblies.
- Domain events remain module-local facts. Mapping a domain event to a public
  integration event remains explicit module code.
- Result-returning commands stay in the public model. `ICommand<TResult>`
  means the command has a semantic result. Local execution may return the
  result directly to an app entrypoint. Durable send returns acceptance and
  operation metadata, and committed results are observed later through
  operation state.

The transport model is:

- Bondstone is not a broker runtime.
- Bondstone owns durable envelope creation, durable outbox claiming and
  leases, dispatch outcome recording, receive pipelines, inbox idempotency,
  operation-state updates, and optional adapter helpers that map native
  provider messages to `DurableMessageEnvelope`.
- Applications own broker or bus runtime infrastructure: queues, exchanges,
  topics, subscriptions, rules, bindings, consumers, processors, retry,
  dead-letter policy, prefetch, concurrency, lifecycle, and native settlement.
- The core abstraction should move toward envelope dispatch and receive
  helpers, not general transport topology.
- Local transport may keep local-specific routing configuration because there
  is no broker subscription system to provide dispatch intent. That local
  configuration must not define a general provider-neutral topology model.
- Real transport adapters should be thin: outbound envelope sending,
  native-message envelope mapping, and receive helper methods that dispatch an
  envelope with explicit command target or event subscriber intent.

The persistence model for the next MVP is:

- EF Core with PostgreSQL durability semantics is the supported durable
  persistence path.
- Provider-specific concurrency remains valuable and stays in the persistence
  layer: outbox claiming, claim owner/lease checks, lease renewal, retry and
  terminal outcome recording, inbox unique-key idempotency, and transaction
  boundaries.
- The direct Dapper-backed `Bondstone.Persistence.Postgres` package should be
  removed or parked from the public product surface until a real non-EF
  consumer need appears.
- Provider-neutral persistence contracts may remain only where they keep the
  EF/PostgreSQL implementation honest without pretending multiple production
  providers are imminent.

The operation-state model should become easier to reason about:

- source module operation state is an acceptance receipt for durable command
  send;
- target module operation state owns completed, failed, or cancelled command
  outcome and result payload;
- durable operation reads should gain a future module-aware handle or hint so
  callers do not have to scan every configured module store;
- global operation-store scans may remain compatibility behavior for small
  hosts and tests.

The module pipeline should shrink toward a fixed internal runtime sequence.
Application pipeline behaviors remain the small extension point for
application concerns. Generalized public capability pipeline contributions,
named runtime slots, and feature-collection coordination should be reduced or
hidden unless a concrete package need proves they are required.

The first implementation slice is the communication guardrail: local command
execution from inside a running module may execute the same module, but may
not execute a different module unless it is durable receive execution driven
by a receive pipeline.

## Consequences

Bondstone becomes smaller and more library-like. The durable persistence,
outbox, inbox, operation-state, and receive-pipeline pieces become the center
of the product instead of the transport packages.

Transport diagnostics and startup validation should become much smaller after
old transport topology ownership is removed. Diagnostics should focus on
persistence, outbox dispatch, receive pipeline execution, inbox idempotency,
and operation results.

Existing direct provider transport and receive-worker APIs are expected to be
removed, parked, or replaced by thinner envelope adapter APIs in follow-up
work. Because current external usage is bounded, the project accepts this
compatibility churn.

The communication guardrail changes `IModuleCommandExecutor` behavior for
code that attempts cross-module local execution from inside a module handler.
That code should use durable command sending, integration events, projections,
or explicit app-owned orchestration instead.

The direct PostgreSQL package removal will reduce test and documentation
surface but also removes the current non-EF abstraction pressure. Future
provider work should be added from real project need rather than speculative
surface.

Domain event persistence becomes more valuable as transport shrinks, because
it is the module-local mechanism for recording internal facts without making
them public integration events. The implementation should remain explicit and
narrow.

## Related Decisions

- [0004 Positioning And Service Extraction Path](0004-positioning-and-service-extraction-path.md)
- [0025 Module Command Execution Boundary](0025-module-command-execution-boundary.md)
- [0036 Direct Transport Adapters And Rebus Removal](0036-direct-transport-adapters-and-rebus-removal.md)
- [0038 Provider Retry Recovery And Settlement Boundaries](0038-provider-retry-recovery-and-settlement-boundaries.md)
- [0039 Startup Transport Topology Validation](0039-startup-transport-topology-validation.md)
  is superseded by this post-MVP simplification.
- [0045 Module Execution Context Semantics](0045-module-execution-context-semantics.md)
- [0051 Package Boundary Split](0051-package-boundary-split.md)
- [0052 Domain Event Capability Package Boundary](0052-domain-event-capability-package-boundary.md)
- [0054 Result-Returning Command Model](0054-result-returning-command-model.md)

## Application Notes

- Current contract: Partially applied. The accepted direction is recorded here
  and in the post-MVP planning note. Applied code slices prevent cross-module
  local command execution from inside a running module while preserving app
  entrypoint execution and durable receive execution, and remove the direct
  non-EF PostgreSQL and Service Bus packages from the active public product
  surface.
- Stable docs: Partially applied. Architecture, packaging, setup, package
  discovery, public API, testing, sample, and package README docs now describe
  EF Core/PostgreSQL as the supported persistence path, local transport as a
  dev/test/sample path, and RabbitMQ as the remaining direct broker adapter.
- Agent guidance: Current root guidance already points agents to the stable
  docs and ADR review before package-boundary, persistence, or transport
  changes.
- Application evidence: The second implementation slice removes
  `Bondstone.Persistence.Postgres`, `Bondstone.Transport.ServiceBus`, and
  their tests from the repository and `Bondstone.slnx`; removes them from the
  public API baseline matrix; converts the modular monolith billing sample to
  EF/PostgreSQL persistence; and removes Service Bus from active composition
  tests. The third implementation slice renames the neutral outbox transport
  handoff to durable envelope dispatch contracts and refreshes public API
  baselines plus stable docs. The fourth implementation slice removes the
  separate `Bondstone.Transport` package, removes the core aggregate startup
  topology diagnostics layer, removes RabbitMQ topology diagnostics from the
  public/service surface, and makes Local/RabbitMQ route validation a
  dispatch/receive-time concern. The fifth implementation slice replaces the
  RabbitMQ outbound topology DSL with command/event destination functions over
  durable envelopes while keeping receive queue bindings as adapter-local
  helper metadata.
- Pending or deferred: Module-aware operation handles, operation failure
  policy, pipeline simplification, local transport correctness fixes, and
  broader public API cleanup remain follow-up work.

## Verification

For ADR creation:

- Read root and docs ADR guidance through the ADR create skill.
- Read related architecture and runtime docs.
- Read related ADRs.

For the first implementation slice:

- Focused module command executor tests should cover app entrypoint execution,
  same-module nested execution, cross-module local execution rejection, and
  durable receive execution.

For the public package surface cut:

- `Bondstone.Persistence.Postgres` and `Bondstone.Transport.ServiceBus` should
  be absent from the repository, `Bondstone.slnx`, default public API baseline
  inputs, current package discovery, and current setup guidance.
- The modular monolith sample should use EF/PostgreSQL persistence for all
  module-owned durable persistence.
- Focused composition and public API tests should run before full restore,
  build, fast tests, formatter checks for changed docs, and `git diff --check`.

For the transport diagnostics package removal:

- `Bondstone.Transport` should be absent from the repository, `Bondstone.slnx`,
  package discovery, setup guidance, package dependency docs, and public API
  baseline inputs.
- ADR 0039 should be marked superseded because startup transport topology
  validation is no longer the current operating contract.
- Build, fast tests, public API baseline refresh, formatter checks for changed
  docs, and `git diff --check` should run.
