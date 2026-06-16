# 0058 Domain Events In Core And EF Persistence

Status: Accepted
Application: Applied
Date: 2026-06-16

## Context

Post-MVP transport and runtime simplification removed the need to prove a
general capability package and capability pipeline model. Domain events are no
longer useful as a separate capability package boundary: the contracts are
small module-domain abstractions, and the only accepted runtime behavior is
EF Core collection and persistence.

ADR 0052 placed domain event contracts in
`Bondstone.Capabilities.DomainEvents` and EF-backed persistence in
`Bondstone.Capabilities.DomainEvents.EntityFrameworkCore`. That split helped
prove optional package composition, but it now preserves the old capability
pipeline framing we are deliberately removing.

The `IDomainEventHandler<TDomainEvent>` contract also remained misleading. It
suggested Bondstone might dispatch a local domain event bus even though the
current runtime does not invoke those handlers and intentionally keeps mapping
domain events to integration events as explicit module code.

## Decision

Supersede ADR 0052.

Move the domain event contracts into the core `Bondstone` package under the
`Bondstone.DomainEvents` namespace:

- `IDomainEvent`;
- `DomainEventIdentityAttribute`;
- `IDomainEventSource`.

Remove `IDomainEventHandler<TDomainEvent>` from the active public API until a
future ADR accepts local domain event dispatch semantics.

Move EF Core domain event persistence into
`Bondstone.Persistence.EntityFrameworkCore`:

- `UseEntityFrameworkCoreDomainEventPersistence()`;
- `ApplyBondstoneDomainEvents(...)`;
- `DomainEventRecordEntity`;
- `DomainEventRecordEntityConfiguration`;
- EF domain event collection, staging, and clear-after-observed-commit
  behavior.

Remove `Bondstone.Capabilities.DomainEvents` and
`Bondstone.Capabilities.DomainEvents.EntityFrameworkCore` from the active
package set.

Domain event persistence remains explicitly opt-in per EF-backed module. It
does not become a hidden behavior of every EF module, does not dispatch local
domain event handlers, and does not map domain events to integration events.

## Consequences

The package set is smaller and the runtime story is easier to explain:
Bondstone core owns module-domain event contracts, and the EF package owns the
EF persistence bridge.

The next runtime pipeline simplification can remove the capability package
proof and replace generic capability contributions with an internal fixed
sequence.

Applications using the removed capability packages must update package
references and namespaces:

- `Bondstone.Capabilities.DomainEvents` becomes `Bondstone.DomainEvents`.
- `Bondstone.Capabilities.DomainEvents.EntityFrameworkCore.Persistence`
  becomes `Bondstone.Persistence.EntityFrameworkCore.Persistence`.

Removing `IDomainEventHandler<TDomainEvent>` is a breaking API cleanup. It is
intentional because the active runtime does not implement local domain event
dispatch.

## Related Decisions

- Supersedes
  [0052 Domain Event Capability Package Boundary](0052-domain-event-capability-package-boundary.md).
- Amends [0051 Package Boundary Split](0051-package-boundary-split.md).
- Amends
  [0028 Domain Event Persistence Capability](0028-domain-event-persistence-capability.md).
- Relates to
  [0056 Post-MVP Communication And Transport Simplification](0056-post-mvp-communication-and-transport-simplification.md).
- Relates to
  [0046 Public API Surface Policy](0046-public-api-surface-policy.md).

## Application Notes

- Current contract: domain event contracts live in `Bondstone.DomainEvents`
  inside the core `Bondstone` package. EF Core domain event persistence lives
  in `Bondstone.Persistence.EntityFrameworkCore.Persistence` and remains an
  explicit module opt-in.
- Stable docs: package inventory and discovery are updated in
  [docs/packaging.md](../packaging.md) and
  [docs/package-discovery.md](../package-discovery.md). Runtime behavior is
  updated in [docs/architecture/messaging.md](../architecture/messaging.md),
  [docs/architecture/persistence.md](../architecture/persistence.md),
  [docs/architecture/persistence-ef-core.md](../architecture/persistence-ef-core.md),
  and [docs/architecture/modules.md](../architecture/modules.md).
- Agent guidance: root [AGENTS.md](../../AGENTS.md) already requires ADR
  review for package-boundary, public API, provider, transport, durable
  behavior, and runtime changes.
- Application evidence: the capability packages and their test projects were
  removed from `Bondstone.slnx`; source moved into `Bondstone` and
  `Bondstone.Persistence.EntityFrameworkCore`; tests moved into the owning
  core, EF, and EF/PostgreSQL test projects; public API baselines were
  refreshed for the active package set.
- Pending or deferred: fixed runtime pipeline simplification remains next.
  Automatic local domain event dispatch remains absent and requires a future
  ADR.

## Verification

- `pnpm format:check`
- `dotnet restore Bondstone.slnx --disable-build-servers`
- `dotnet build Bondstone.slnx --configuration Release --no-restore --disable-build-servers`
- `BONDSTONE_UPDATE_PUBLIC_API_BASELINE=1 dotnet test tests/Bondstone.PublicApi.Tests/Bondstone.PublicApi.Tests.csproj --configuration Release --no-build --disable-build-servers`
- `dotnet test Bondstone.slnx --configuration Release --no-build --disable-build-servers --filter "Category=Unit|Category=Application"`
- `dotnet test tests/Bondstone.PublicApi.Tests/Bondstone.PublicApi.Tests.csproj --configuration Release --no-build --disable-build-servers`
- `pnpm backend:pack`
- `dotnet test tests/Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests/Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests.csproj --configuration Release --no-build --disable-build-servers --filter "FullyQualifiedName~PostgreSqlDomainEventTransactionTests"`
