# Module Runtime Isolation

Goal: reduce same-host module leakage by consolidating module-owned provider
and capability resolution behind an internal module runtime descriptor.

## Why Now

Bondstone modules run in the same host and usually share one DI container.
That is normal for a modular monolith, but it means every provider and
capability behavior must be careful not to act on services, registrations, or
state that belong to another module.

Recent EF Core Domain Events work surfaced the risk. A capability can be
correctly scoped by intent yet still rely on scattered `moduleName` checks,
global service enumeration, or scoped state that needs module ownership to be
part of the state itself.

Bondstone does not need module-specific DI containers yet. It does need a
stronger internal runtime handle that represents the target module and exposes
only the provider/capability state owned by that module.

## Direction To Explore

- Introduce an internal module runtime descriptor/handle rather than passing
  only raw module names through provider and capability code.
- Keep normal .NET DI scopes. Do not introduce child containers or
  module-specific `IServiceProvider` instances in this slice.
- Let the runtime expose module-owned provider/capability state through
  properties or focused `Provide...` methods.
- Cache descriptors/factories where useful, but continue resolving scoped
  service instances from the current execution scope.
- Move repeated module lookup, persistence-provider checks, module-owned
  service selection, and capability activation checks behind the runtime where
  that reduces leakage risk.
- Keep public API unchanged unless a concrete provider/runtime contract needs
  ADR review.

## Completed Slice

2026-06-11:

- Added a core internal scoped module runtime registry and descriptor built
  from `IBondstoneModuleRegistry` plus existing module-owned durable
  persistence registrations.
- Moved core durable outbox writer, inbox handler executor, operation-state
  store, and operation reader resolution behind the runtime registry while
  preserving existing single-root fallback behavior and duplicate/missing
  registration diagnostics.
- Tightened the core runtime descriptor so it carries lazy module-owned
  persistence state instead of routing resolver calls back through
  module-name keyed lookups.
- Added provider-local EF Core and non-EF PostgreSQL runtime descriptors for
  transaction activation. EF domain event persistence activation now uses the
  EF provider runtime descriptor instead of scanning EF opt-in registrations
  ad hoc from behavior code.
- Split module-owned durable command/receive runtime metadata from executable
  persistence services. Core now builds module maps from passive
  `DurableModule*Registration` records and invokes only the selected module's
  scoped factory for outbox writer, inbox executor, and operation-state store
  services.
- Added focused unit coverage proving that a module does not use another
  module's outbox writer, inbox executor, or operation-state store when both
  modules share the same DI scope. Added EF domain event coverage proving one
  EF module's domain event opt-in does not activate collection for another EF
  module in the same host.
- Added non-EF PostgreSQL runtime coverage proving Postgres transaction
  behavior wraps Postgres-backed module execution, preserves ordering, and
  does not resolve `IPostgresModuleSession` for EF-provider, other-provider,
  or non-persistence modules in the same host.
- Added runtime factory coverage proving unrelated non-EF PostgreSQL sessions
  and EF PostgreSQL `DbContext` instances are not resolved while another
  module performs durable sends in the same host.
- Left global operation-state reads as the intentional fan-out path: because
  `IDurableOperationReader.GetStateAsync(...)` has no module identity, it may
  create and query every configured module operation-state store until a later
  module-targeted read contract is accepted.
- Did not add child containers, module-specific `IServiceProvider` instances,
  or a generalized capability registry. Added the small provider-facing
  durable module runtime registration records accepted by the ADR 0042
  amendment.

ADR 0042 was amended for the provider-facing durable module runtime
registration pattern. Provider-specific runtime descriptors remain
package-local bridges because exposing the core runtime descriptor across
package boundaries would require compatibility review.

## Questions

- Which provider package path should move behind the runtime next, if real
  leakage appears: EF transaction completion state, additional provider-owned
  capability metadata, or outbound dispatcher ownership?
- Should command routes and event subscriber registrations hold or resolve a
  module runtime descriptor at execution time?
- Should the new durable module runtime registration pattern eventually expand
  to a public generalized capability registry, or remain specific to durable
  command/receive persistence?
- Can EF Core domain event activation and transaction completion state become
  module-owned through this runtime instead of relying on global service
  enumeration plus module-name filtering?
- What diagnostics should the runtime own for missing or duplicate
  module-owned provider services?

## Deliverables

- ADR amendment or new ADR if the runtime descriptor changes durable module
  behavior, provider contracts, or public API.
- The smallest implementation slice that introduces internal module runtime
  resolution and migrates one or more high-risk provider/capability paths.
- Tests proving module-owned services or capability state do not leak across
  modules in the same host scope.
- Stable docs updates for any accepted current runtime behavior.

## Verification

- `pnpm format:check`
- `pnpm backend:build`
- `pnpm backend:test:fast`
