# 0051 Package Boundary Split

Status: Archived
Application: Not Applicable
Date: 2026-06-11

## Context

Bondstone's core package had accumulated several different kinds of public
surface: module execution, provider-neutral durable persistence contracts,
provider-neutral transport topology diagnostics, module-local domain event
contracts, and provider-specific EF Core and PostgreSQL persistence
implementations.

That made package boundaries harder to test because a provider could
accidentally reach through `Bondstone` for contracts that should be neutral
abstraction packages. It also made naming inconsistent: the non-EF PostgreSQL
provider already used `Bondstone.Persistence.Postgres`, while EF packages used
`Bondstone.EntityFrameworkCore` and `Bondstone.EntityFrameworkCore.Postgres`.

ADR 0037 intentionally deferred the EF package-family rename. The current
architecture work is now explicitly testing abstraction boundaries, so keeping
the old EF package names would preserve a known inconsistency at the moment
when the boundary split is cheapest to validate.

ADR 0028 also kept domain event contracts inside `Bondstone` while EF domain
event persistence was still being shaped. Those contracts are useful without
core module execution, durable messaging, EF Core, or PostgreSQL, so they now
fit better as a small independent package.

## Decision

Split the current package family into smaller abstraction and provider
packages:

- `Bondstone` keeps core module registration, module execution, command and
  integration-event execution, module-aware runtime resolution, and the
  `AddBondstone` composition builder.
- `Bondstone.Persistence` owns provider-neutral durable persistence contracts,
  durable envelopes, message trace context, operation state, inbox/outbox
  records, dispatch primitives, and passive durable module runtime
  registrations.
- `Bondstone.Transport` owns provider-neutral transport topology diagnostic
  contracts and shapes.
- `Bondstone.DomainEvents` owns the small module-local domain event contracts.
- `Bondstone.Persistence.EntityFrameworkCore` replaces
  `Bondstone.EntityFrameworkCore`.
- `Bondstone.Persistence.EntityFrameworkCore.Postgres` replaces
  `Bondstone.EntityFrameworkCore.Postgres`.
- `Bondstone.Persistence.Postgres` remains the non-EF PostgreSQL persistence
  provider.
- Concrete transport adapters remain `Bondstone.Transport.Local`,
  `Bondstone.Transport.RabbitMq`, and `Bondstone.Transport.ServiceBus`.

Use full package-chain names for specific providers. The provider-neutral EF
package is `Bondstone.Persistence.EntityFrameworkCore`, and the PostgreSQL EF
provider package is `Bondstone.Persistence.EntityFrameworkCore.Postgres`.

The dependency direction is:

- `Bondstone.DomainEvents` is independent.
- `Bondstone.Persistence` is provider-neutral and independent of `Bondstone`.
- `Bondstone.Transport` depends on `Bondstone.Persistence`.
- `Bondstone` may depend on `Bondstone.Persistence` and
  `Bondstone.Transport`, but not on provider, adapter, or hosting packages.
- `Bondstone.Persistence.EntityFrameworkCore` depends on `Bondstone`,
  `Bondstone.Persistence`, and `Bondstone.DomainEvents`.
- `Bondstone.Persistence.EntityFrameworkCore.Postgres` depends on
  `Bondstone`, `Bondstone.Persistence`,
  `Bondstone.Persistence.EntityFrameworkCore`, and PostgreSQL EF packages.
- `Bondstone.Persistence.Postgres` depends on `Bondstone`,
  `Bondstone.Persistence`, Dapper, and Npgsql.
- Transport adapters depend on `Bondstone`, `Bondstone.Persistence`,
  `Bondstone.Transport`, and their provider SDKs.
- `Bondstone.Hosting` depends on `Bondstone` and `Bondstone.Persistence`.

No compatibility shim or type-forwarding package is added for the old EF
package IDs in this slice. The split is a package-family migration accepted
while Bondstone is still deliberately tightening boundaries.

## Consequences

The abstraction boundaries are easier to test. Provider packages now compile
against the same neutral packages a custom provider would need.

The core package is smaller conceptually, but it still coordinates module
runtime behavior over neutral persistence and transport contracts. This is not
a pure leaf package split.

Domain events are easier to adopt independently, but
`Bondstone.DomainEvents` must remain intentionally small. Runtime collection,
dispatch, persistence, and provider integration stay out of that package.

The EF package rename is compatibility-sensitive. Existing consumers of the
old package IDs must update package references and namespaces to the
`Bondstone.Persistence.EntityFrameworkCore` chain.

Public API cleanup still requires inventory. This split moves contracts into
better packages; it does not decide which public types should later become
internal, hidden behind builders, or documented as advanced composition API.

## Amendment 2026-06-11: Domain Event Capability Package

ADR 0052 narrows the domain event package placement from the short-lived
`Bondstone.DomainEvents` package name to capability packages:
`Bondstone.Capabilities.DomainEvents` for domain event capability contracts
and `Bondstone.Capabilities.DomainEvents.EntityFrameworkCore` for the EF Core
bridge.

The broader package split remains accepted and applied. `Bondstone.Persistence`,
`Bondstone.Transport`, the renamed EF persistence package chain, hosting,
concrete transports, and non-EF PostgreSQL persistence remain current.

## Amendment 2026-06-16: Domain Events Returned To Core

ADR 0058 supersedes ADR 0052's capability-package placement. The active domain
event contracts now live in the core `Bondstone` package under the
`Bondstone.DomainEvents` namespace. EF-backed domain event collection, record
mapping, and explicit module opt-in now live in
`Bondstone.Persistence.EntityFrameworkCore`.

The old `Bondstone.Capabilities.DomainEvents` and
`Bondstone.Capabilities.DomainEvents.EntityFrameworkCore` package projects are
removed from the active package set.

## Related Decisions

- Supersedes the EF package identity deferral in
  [0037 PostgreSQL Persistence Package Identity](0037-postgresql-persistence-package-identity.md).
- Amends the domain event package placement in
  [0028 Domain Event Persistence Capability](0028-domain-event-persistence-capability.md).
- Relates to
  [0046 Public API Surface Policy](0046-public-api-surface-policy.md).
- Amended by
  [0052 Domain Event Capability Package Boundary](0052-domain-event-capability-package-boundary.md).
- Amended by
  [0058 Domain Events In Core And EF Persistence](0058-domain-events-in-core-and-ef-persistence.md).

## Application Notes

- Current contract: Package identities and dependency direction are documented
  in [docs/packaging.md](../packaging.md). Source package navigation is
  documented in [src/README.md](../../src/README.md).
- Stable docs: Architecture docs now describe `Bondstone.Persistence`,
  active transport adapters, `Bondstone.DomainEvents`, and the renamed EF
  package chain.
- Agent guidance: Root [AGENTS.md](../../AGENTS.md) still requires ADR review
  before package-boundary, public API, provider, transport, durable behavior,
  or runtime changes.
- Application evidence: Source projects, test projects, solution entries,
  samples, namespaces, project references, and stable docs have been updated
  for the split.
- Pending or deferred: Public API inventory, old-package compatibility policy,
  and any future type-forwarding packages remain separate decisions.
- Amendment: ADR 0058 keeps the broader package split but replaces the
  capability package placement with core `Bondstone.DomainEvents` contracts
  and EF package-owned domain event persistence.

## Verification

- `dotnet restore Bondstone.slnx`
- `dotnet build Bondstone.slnx --configuration Release --no-restore`
