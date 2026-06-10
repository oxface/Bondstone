# 0042 Module Persistence Capability Metadata

Status: Proposed
Application: Not Applicable
Date: 2026-06-10

## Context

Bondstone module registration currently records persistence with
`PersistenceProviderName` and optional `PersistenceContextType`.
`PersistenceProviderName` marks persistence capability, lets provider
transaction behaviors decide whether they own a module execution, and improves
missing-service diagnostics. `PersistenceContextType` is primarily EF Core
metadata, because EF transaction behavior needs a module DbContext type.

The current shape works, but EF-specific metadata lives on the provider-neutral
module registration. Non-EF providers such as `Bondstone.Persistence.Postgres`
do not need equivalent CLR context metadata. Several module persistence
resolvers also repeat similar logic: normalize the module name, validate the
module exists, find the module-owned service by module name, and throw a
module-aware diagnostic when it is missing.

The current fallback non-module persistence services are functional through
root-level persistence registration paths such as
`AddBondstoneEntityFrameworkCorePersistence<TDbContext>` and
`AddBondstonePostgreSqlPersistence<TDbContext>`. They support single-store or
low-level advanced composition when no module-owned implementations are
registered. Current module-boundary samples and preferred setup use
module-owned persistence instead, so the fallback path may be compatibility
residue rather than a desired long-term user path.

## Decision

Decide whether module persistence metadata should move from general module
registration into provider-owned module capability metadata keyed by module
name.

The candidate direction is:

- Keep module-owned persistence as the preferred durable messaging setup.
- Introduce a shared module capability or module persistence metadata registry
  that provider packages contribute to.
- Keep provider-specific details, such as EF DbContext type or PostgreSQL
  schema/session details, in provider-owned metadata.
- Let resolvers use segregated interfaces over shared module capability data
  rather than copying resolver logic for every persistence service.
- Decide whether fallback non-module persistence services remain supported
  advanced composition or become test-only/retired compatibility residue.

## Consequences

Provider-owned metadata would reduce leakage from EF Core into core module
registration and make future provider metadata more natural.

A shared registry could simplify diagnostics and resolver implementation.

Changing the public module registration shape, `UsePersistence(...)` behavior,
or provider extension contracts is a compatibility-sensitive package API
change.

## Related Decisions

- [0003 Package Boundaries And Target Framework](0003-package-boundaries-and-target-framework.md)
- [0016 EF Core Persistence Scope](0016-ef-core-persistence-scope.md)
- [0021 Fluent Service Composition Guardrails](0021-fluent-service-composition-guardrails.md)
- [0032 Module-Owned Durable EF Persistence](0032-module-owned-durable-ef-persistence.md)
- [0035 PostgreSQL Dapper Persistence Proof](0035-postgresql-dapper-persistence-proof.md)
- [0037 PostgreSQL Persistence Package Identity](0037-postgresql-persistence-package-identity.md)

## Application Notes

- Current contract: proposed only; no binding change yet.
- Stable docs: if accepted, update module architecture, persistence docs, and
  setup examples that describe module persistence registration.
- Agent guidance: if accepted, update root AGENTS architecture/package
  guidance if module metadata ownership changes.
- Application evidence: current code stores provider name and optional context
  type in `BondstoneModuleRegistration` and uses module-owned service
  resolvers for writers, inbox executors, dispatchers, and operation stores.
- Pending or deferred: decide registry shape, compatibility treatment for
  existing public metadata, and migration path for provider extensions.

## Verification

No executable verification yet; this is a proposed decision draft.
