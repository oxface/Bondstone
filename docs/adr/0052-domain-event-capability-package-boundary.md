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

### 2026-06-11: EF Bridge Owns Local Dispatch Timing

This amendment supersedes the same-day provider-neutral dispatch pipeline
without rewriting the historical amendment above.
`Bondstone.Capabilities.DomainEvents` returns to an abstractions-only package:
it owns domain event contracts and handler contracts, but no module pipeline
behavior and no `UseDomainEventDispatch()` setup API.

`Bondstone.Capabilities.DomainEvents.EntityFrameworkCore` owns the EF-backed
runtime behavior end to end. Modules opt into EF domain event persistence with
`UseEntityFrameworkCoreDomainEventPersistence()`. They may enable local handler
dispatch on that EF bridge with
`UseEntityFrameworkCoreDomainEventPersistence(options =>
options.DispatchLocalHandlers = true)`.

The EF bridge resolves pending domain event sources from the EF change
tracker, optionally dispatches local `IDomainEventHandler<TDomainEvent>`
handlers after application pipeline behavior and handler logic, then collects
and stages all still-pending domain events before `SaveChangesAsync`. Events
raised by local domain event handlers are therefore persisted in the same EF
transaction. Dispatch does not clear sources; EF persistence remains the
clear-on-observed-commit owner.

No provider-neutral source feature or standalone domain event dispatch system
behavior is introduced in this slice. A future non-EF provider bridge should
own its own source discovery and dispatch timing instead of relying on global
capability ordering.

### 2026-06-11: Local Dispatch Is Deferred Again

This amendment supersedes the same-day EF-owned local dispatch amendment
without rewriting the historical amendment above. The runtime slice keeps the
smallest applied behavior: `Bondstone.Capabilities.DomainEvents` owns domain
event contracts only, and
`Bondstone.Capabilities.DomainEvents.EntityFrameworkCore` owns EF-backed
collection, staging, and clear-on-observed-commit only.

`UseEntityFrameworkCoreDomainEventPersistence()` has no
`DispatchLocalHandlers` option. Registering
`IDomainEventHandler<TDomainEvent>` services does not cause Bondstone to
invoke them from module command or event subscriber execution. The handler
contract remains future-facing for now because removing it is a separate
public API cleanup decision, and because local dispatch still needs a clear
answer for provider-neutral source discovery, recursive handling, failure
semantics, handler scope, and mapping selected domain events to integration
events.

EF domain event persistence must not become a hidden domain-event bus. It may
discover pending `IDomainEventSource` instances through EF Core's change
tracker, stage module-local `DomainEventRecordEntity` rows after application
pipeline behavior and handler logic, and clear pending events only after the
observed EF commit succeeds. Mapping persisted or pending domain events to
public integration events remains explicit module code and is deferred from
this runtime slice.

### 2026-06-11: Capability Pipeline Step Kind

Capability bridge packages now contribute ordered module pipeline behavior
through capability behavior interfaces rather than registering as core system
behavior. The planner keeps capability steps distinct from Bondstone system
steps for diagnostics and future runtime composition, while still ordering
system and capability steps together by numeric `Order` before normal
application behavior.

This remains narrower than a broad capability registry or public named pipeline
slot model. Capability bridge setup APIs still own activation and module
metadata, and normal user extension remains application pipeline behavior in DI
registration order.

### 2026-06-11: Capability Runtime Uses Passive Pipeline Contributions

This amendment supersedes the capability behavior interface detail in the
same-day amendment above without rewriting the historical text. Capability
bridge packages now contribute passive module pipeline contribution records,
not executable behavior implementations discovered from global DI enumerable
resolution. Those records are stored in Bondstone module/runtime registration
metadata.

The EF domain event bridge registers command and event subscriber capability
contributions for each opted-in module. The contribution applies only when the
executing module is the opted-in module and declares EF Core persistence. The
planner selects that passive contribution before creating the executable EF
domain event behavior. This keeps capability activation module-scoped without
adding a broad public capability registry or relying on behavior
self-filtering as the primary boundary.

Normal user extension remains application pipeline behavior in DI registration
order. Module-scoped or per-command user behavior registration remains
deferred to the broader pipeline contribution model accepted in ADR 0025.

## Related Decisions

- Amends [0051 Package Boundary Split](0051-package-boundary-split.md).
- Amends [0028 Domain Event Persistence Capability](0028-domain-event-persistence-capability.md).
- Relates to
  [0050 Module Pipeline Feature Context](0050-module-pipeline-feature-context.md).
- Relates to
  [0025 Module Command Execution Boundary](0025-module-command-execution-boundary.md).
- Relates to
  [0046 Public API Surface Policy](0046-public-api-surface-policy.md).

## Application Notes

- Current contract: Package identities and dependency direction are documented
  in [docs/packaging.md](../packaging.md). Source package navigation is
  documented in [src/README.md](../../src/README.md), and test project
  boundaries are documented in [tests/README.md](../../tests/README.md).
  `IDomainEventHandler<TDomainEvent>` remains a public, future-facing local
  handler contract, but Bondstone does not dispatch it automatically.
- Stable docs: Domain event capability runtime is described in
  [docs/architecture/messaging.md](../architecture/messaging.md),
  [docs/architecture/persistence.md](../architecture/persistence.md), and
  [docs/architecture/persistence-ef-core.md](../architecture/persistence-ef-core.md).
- Agent guidance: Root [AGENTS.md](../../AGENTS.md) requires ADR review before
  package-boundary, public API, provider, transport, durable behavior, or
  runtime changes.
- Application evidence: Source projects, solution entries, tests, namespaces,
  project references, and stable docs have been updated for
  `Bondstone.Capabilities.DomainEvents` and
  `Bondstone.Capabilities.DomainEvents.EntityFrameworkCore`. The EF bridge
  registers module-specific passive capability pipeline contributions through
  `UseEntityFrameworkCoreDomainEventPersistence()`.
- Pending or deferred: Broad capability registries, named public pipeline
  slots, non-EF provider bridges, integration-event mapping, old-package
  compatibility shims, automatic local domain-event handler dispatch,
  reusable provider-neutral dispatch services, and richer handler scoping
  remain separate decisions.

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
- 2026-06-11 EF-owned dispatch amendment: removed the provider-neutral
  dispatch pipeline behavior and moved optional local handler dispatch into
  the EF bridge behavior. Ran focused domain-event tests, `pnpm format:check`,
  `pnpm backend:build`, and `pnpm backend:test:fast`.
- 2026-06-11 local-dispatch deferral amendment: removed the EF
  `DispatchLocalHandlers` option, kept EF behavior persistence-only, and
  updated stable architecture docs and package READMEs. Ran focused domain
  event tests, `pnpm format:check`, `pnpm backend:build`, and
  `pnpm backend:test:fast`.
- 2026-06-11 passive pipeline contribution amendment: updated the EF bridge to
  register module-specific capability pipeline contributions instead of global
  executable behavior interfaces. A follow-up tightened the implementation so
  those contributions live in Bondstone module/runtime metadata rather than
  the service collection. Updated stable docs and ran `pnpm format:check`,
  `pnpm backend:build`, and `pnpm backend:test:fast`.
- `dotnet restore Bondstone.slnx`
- `dotnet build Bondstone.slnx --configuration Release --no-restore`
