# 0037 PostgreSQL Persistence Package Identity

Status: Accepted
Application: Applied
Date: 2026-06-09

## Context

ADR 0035 introduced the first non-EF persistence proof as
`Bondstone.Persistence.Dapper.Postgres`. That name was useful while proving
that Dapper could exercise the persistence abstraction, but it exposed an
implementation library as the product-facing provider identity.

Phase 6.5 removed adapter-on-adapter transport design pressure. The same
principle applies to persistence: provider packages should present the durable
provider boundary Bondstone supports, not the helper library used internally.

The EF packages already describe their provider shape clearly:
`Bondstone.EntityFrameworkCore` is the provider-neutral EF package and
`Bondstone.EntityFrameworkCore.Postgres` is the EF PostgreSQL provider package.
Renaming those packages into a `Bondstone.Persistence.*` hierarchy would be a
larger package-family migration and is not required to hide an implementation
helper.

## Decision

Rename `Bondstone.Persistence.Dapper.Postgres` to
`Bondstone.Persistence.Postgres`.

The package remains PostgreSQL-specific and may keep using Dapper internally.
Dapper is not the public provider abstraction and should not appear in the
package ID, project name, namespace, or app-facing setup API.

Keep `Bondstone.EntityFrameworkCore` and
`Bondstone.EntityFrameworkCore.Postgres` under their current package identities
for this MVP. Reconsider a larger EF package-family rename only if a later ADR
accepts a broader persistence package taxonomy and migration plan.

## Consequences

The public non-EF PostgreSQL persistence provider now reads as a provider
adapter instead of a Dapper adapter.

Consumers use `Bondstone.Persistence.Postgres`, `UsePostgresPersistence(...)`,
`AddBondstonePostgresPersistence(...)`, `IPostgresModuleSession`, and
`PostgresSchema`.

Internally, the package can continue to depend on Dapper where that keeps SQL
execution simple and explicit. A future ADO.NET-only rewrite is not required
by this rename.

The old package identity is not preserved because Bondstone is not live yet.
No compatibility shim, type forwarder, or migration package is required for
the MVP repository.

## Related Decisions

- [0035 PostgreSQL Dapper Persistence Proof](0035-postgresql-dapper-persistence-proof.md)
- [0036 Direct Transport Adapters And Rebus Removal](0036-direct-transport-adapters-and-rebus-removal.md)

## Application Notes

- Current contract: The non-EF PostgreSQL persistence package is
  `Bondstone.Persistence.Postgres`. It is provider-specific, not a generic
  Dapper abstraction.
- Stable docs: Package identity and persistence docs are reflected in
  [docs/packaging.md](../packaging.md),
  [docs/architecture/persistence.md](../architecture/persistence.md),
  [docs/architecture/persistence-core.md](../architecture/persistence-core.md),
  [docs/architecture/persistence-postgres.md](../architecture/persistence-postgres.md),
  [docs/setup.md](../setup.md), [docs/testing.md](../testing.md),
  [docs/samples.md](../samples.md), and [docs/mvp-plan.md](../mvp-plan.md).
- Agent guidance: Root [AGENTS.md](../../AGENTS.md) names
  `Bondstone.Persistence.Postgres` as the current non-EF PostgreSQL proof
  package.
- Application evidence: Project, test project, namespaces, sample references,
  and app-facing setup APIs have been renamed to the PostgreSQL provider
  identity.
- Pending or deferred: A larger EF package-family rename is deferred to a
  later ADR if needed.

## Verification

- `dotnet restore Bondstone.slnx --disable-build-servers -p:NuGetAudit=false`
- `dotnet build Bondstone.slnx --configuration Release --no-restore --disable-build-servers`
