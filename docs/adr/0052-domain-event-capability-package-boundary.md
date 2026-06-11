# 0052 Domain Event Capability Package Boundary

Status: Amended
Application: Applied
Date: 2026-06-11

## Context

ADR 0051 split durable persistence, transport diagnostics, and domain event
contracts out of the core `Bondstone` package. That improved the package
shape, but `Bondstone.DomainEvents` still looked like a foundational
Bondstone abstraction package.

That framing is not quite honest. Domain events are not part of Bondstone's
core durable module boundary in the same way as commands, integration events,
outbox, inbox, operation state, or topology diagnostics. The domain event
feature exists as an optional utility capability: it lets applications use
Bondstone's module pipeline and provider transaction hooks to collect,
persist, and clear module-local domain facts without implementing their own
`SaveChangesAsync` interception or recursive domain-event capture.

The EF implementation also made the base
`Bondstone.Persistence.EntityFrameworkCore` package depend on domain event
contracts. That dependency was backwards for an optional capability. Durable
EF persistence should be usable without knowing that the domain event
capability exists.

## Decision

Replace `Bondstone.DomainEvents` with capability packages:

- `Bondstone.Capabilities.DomainEvents` owns the domain event contracts:
  `IDomainEvent`, `DomainEventIdentityAttribute`, `IDomainEventSource`, and
  `IDomainEventHandler<TDomainEvent>`.
- `Bondstone.Capabilities.DomainEvents` may depend on `Bondstone` and no
  provider, persistence, transport, or hosting packages.
- `Bondstone.Capabilities.DomainEvents` is contracts-only for now. It is not a
  domain event bus, does not dispatch `IDomainEventHandler<TDomainEvent>`
  automatically, and does not persist events by itself.
- `Bondstone.Capabilities.DomainEvents.EntityFrameworkCore` owns the EF Core
  bridge: EF change-tracker collection, `DomainEventRecordEntity`, EF mapping
  helper `ApplyBondstoneDomainEvents()`, module opt-in
  `UseEntityFrameworkCoreDomainEventPersistence()`, and the system pipeline
  behavior that stages records and clears sources only after observed EF
  commit.
- `Bondstone.Persistence.EntityFrameworkCore` must not depend on domain event
  capability packages. It owns durable EF outbox, inbox, operation state,
  transaction, and persistence-scope behavior only.
- Non-EF PostgreSQL domain event staging remains application-owned until a
  later ADR accepts a concrete capability bridge.

The broader package split from ADR 0051 remains accepted:
`Bondstone.Persistence`, `Bondstone.Transport`, the full EF persistence
package chain, hosting, concrete transports, and non-EF PostgreSQL persistence
remain current.

## Consequences

The package graph better communicates optionality. Applications that do not
want Bondstone's domain event utility do not see it through persistence
dependencies.

Capability packages can implement Bondstone system pipeline behaviors because
the behavior contracts live in `Bondstone`. They should treat those behaviors
as optional module-runtime integrations rather than core Bondstone semantics.

The EF bridge package becomes the first proof that capabilities can compose
over Bondstone and a provider package without pulling capability concepts into
the provider itself.

Consumers that used the short-lived `Bondstone.DomainEvents` or EF-package
domain event APIs must move to `Bondstone.Capabilities.DomainEvents` and
`Bondstone.Capabilities.DomainEvents.EntityFrameworkCore`.

Public API inventory still needs to classify the capability contracts and EF
bridge types honestly, especially `IDomainEventHandler<TDomainEvent>`, which
is not automatically dispatched yet.

## Amendments

### 2026-06-11: Domain Event Handler Dispatch Remains Deferred

`Bondstone.Capabilities.DomainEvents` remains contracts-only for this runtime
slice. Bondstone does not automatically discover pending domain events or
dispatch `IDomainEventHandler<TDomainEvent>` from the module command or event
subscriber pipelines.

Keep `IDomainEventHandler<TDomainEvent>` as a future-facing module-local
handler contract rather than removing it before release. Removing it would
churn the accepted contract surface without solving the harder runtime
questions: provider-neutral pending-source discovery, dispatch opt-in, handler
registration scope, recursive event handling, failure semantics, and ordering
relative to persistence.

Automatic local dispatch requires a later ADR before implementation. That ADR
must introduce an explicit provider-neutral source discovery or collector shape
that does not couple the capability package to EF Core. A plausible shape is a
per-execution module pipeline feature or collector contract that exposes
pending `IDomainEventSource` instances and clear-on-success ownership, but no
such public contract is added now.

The EF bridge remains persistence behavior, not a hidden domain-event bus. It
may collect pending sources from the EF `ChangeTracker`, stage
`DomainEventRecordEntity` rows, and clear sources after observed commit, but it
must not resolve or invoke `IDomainEventHandler<TDomainEvent>`, recursively
dispatch domain events, or map them to integration events.

### 2026-06-11: Opt-In Local Domain Event Dispatch

This amendment supersedes the same-day dispatch deferral above without
rewriting it. `Bondstone.Capabilities.DomainEvents` now owns the smallest
provider-neutral runtime behavior for explicit local dispatch:

- modules opt in with `UseDomainEventDispatch()`;
- provider or application behaviors expose pending sources through the
  per-execution `IDomainEventSourceFeature`;
- the dispatch behavior resolves and invokes
  `IDomainEventHandler<TDomainEvent>` for pending domain events;
- dispatch does not clear `IDomainEventSource` instances.

The dispatch behavior is ordered after module execution context and after EF
domain-event persistence has pushed its source feature. In the current EF
composition, transaction behavior remains outermost, EF domain-event
persistence runs at `ExecutionContext + 10`, local dispatch runs at
`ExecutionContext + 20`, and application behaviors plus the handler remain
inside both. On the way out, local dispatch runs after application behavior
and handler logic, then EF persistence collects and stages any still-pending
events, then EF transaction behavior calls `SaveChangesAsync` and commits.

This ordering lets local handlers run while the module execution context and
EF transaction are still active, and lets EF persistence stage events raised
by local domain-event handlers in the same transaction. Source clearing remains
owned by the provider behavior that stages events. EF clears sources only
after collection, staging, `SaveChangesAsync`, and observed commit succeed.

The EF bridge remains a persistence bridge, not a hidden bus. Calling
`UseEntityFrameworkCoreDomainEventPersistence()` alone does not dispatch
handlers. EF only contributes `IDomainEventSourceFeature` from its change
tracker while its persistence behavior is active. Local dispatch requires the
separate `UseDomainEventDispatch()` opt-in.

## Related Decisions

- Amends [0051 Package Boundary Split](0051-package-boundary-split.md).
- Amends [0028 Domain Event Persistence Capability](0028-domain-event-persistence-capability.md).
- Relates to
  [0050 Module Pipeline Feature Context](0050-module-pipeline-feature-context.md).
- Relates to
  [0046 Public API Surface Policy](0046-public-api-surface-policy.md).

## Application Notes

- Current contract: Package identities and dependency direction are documented
  in [docs/packaging.md](../packaging.md). Source package navigation is
  documented in [src/README.md](../../src/README.md), and test project
  boundaries are documented in [tests/README.md](../../tests/README.md).
  `IDomainEventHandler<TDomainEvent>` is an implemented local handler
  contract when a module opts into `UseDomainEventDispatch()` and an active
  provider or application behavior exposes `IDomainEventSourceFeature`.
- Stable docs: Domain event capability behavior is described in
  [docs/architecture/messaging.md](../architecture/messaging.md),
  [docs/architecture/persistence.md](../architecture/persistence.md), and
  [docs/architecture/persistence-ef-core.md](../architecture/persistence-ef-core.md).
- Agent guidance: Root [AGENTS.md](../../AGENTS.md) requires ADR review before
  package-boundary, public API, provider, transport, durable behavior, or
  runtime changes.
- Application evidence: Source projects, solution entries, tests, namespaces,
  project references, and stable docs have been updated for
  `Bondstone.Capabilities.DomainEvents` and
  `Bondstone.Capabilities.DomainEvents.EntityFrameworkCore`.
- Pending or deferred: Broad capability registries, named public pipeline
  slots, non-EF provider bridges, integration-event mapping, old-package
  compatibility shims, and richer handler scoping remain separate decisions.

## Verification

- 2026-06-11 handler-dispatch amendment: read back this ADR and affected
  stable docs. Added a focused EF command pipeline test proving registered
  `IDomainEventHandler<TDomainEvent>` services are not invoked automatically
  while EF domain event persistence still stages before `SaveChangesAsync` and
  clears after successful save/commit. Ran `pnpm format:check`,
  `pnpm backend:build`, and `pnpm backend:test:fast`.
- 2026-06-11 opt-in dispatch amendment: added provider-neutral dispatch
  behavior, EF source feature composition, provider-neutral dispatch tests,
  and EF ordering tests. Ran focused domain-event tests, `pnpm format:check`,
  `pnpm backend:build`, and `pnpm backend:test:fast`.
- `dotnet restore Bondstone.slnx`
- `dotnet build Bondstone.slnx --configuration Release --no-restore`
