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

## Questions

- Which existing resolvers should move behind the runtime first:
  outbox writer, inbox executor, operation-state store, operation reader,
  transaction behavior, domain event persistence activation, or all of them?
- Should command routes and event subscriber registrations hold or resolve a
  module runtime descriptor at execution time?
- How should provider packages contribute module-owned runtime state without
  introducing a public generalized capability registry?
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
