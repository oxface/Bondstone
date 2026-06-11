# 0052 Domain Event Capability Package Boundary

Status: Accepted
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
- Pending or deferred: Automatic local domain-event dispatch,
  integration-event mapping, non-EF provider bridges, and old-package
  compatibility shims remain separate decisions.

## Verification

- `dotnet restore Bondstone.slnx`
- `dotnet build Bondstone.slnx --configuration Release --no-restore`
