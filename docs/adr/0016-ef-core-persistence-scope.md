# 0016 EF Core Persistence Scope

Status: Accepted
Application: Partially Applied
Date: 2026-06-05

## Context

ADR 0015 introduced a narrow inbox handle-once executor with an explicit commit
delegate. That keeps Bondstone core independent from EF Core, transactions,
transport acknowledgement, and handler discovery. It also leaves a deliberate
gap: consumers and future transport adapters need a clear way to run handler
state changes, outbox writes, inbox processed markers, and operation-state
updates in one EF Core transaction.

The historical source repository provided module-scoped transaction handling,
but it also mixed transaction ownership with domain events, module runtime
lookup, handler execution, outbox mapping, and transport concerns. Bondstone
should not bulk-copy that layer. The first extracted step should be a small EF
Core persistence scope that proves the transaction boundary before wider
module-scoped behavior is added.

## Decision

`Bondstone.EntityFrameworkCore` defines an EF-specific
`IEntityFrameworkCorePersistenceScope` contract.

The scope:

- executes a caller-supplied operation inside an EF Core transaction when no
  transaction is already active;
- participates in the current DbContext transaction when one already exists;
- exposes `SaveChangesAsync` so callers can pass it as the explicit commit
  delegate to lower-level primitives such as `IDurableInboxHandlerExecutor`;
- commits only transactions it started;
- rolls back only transactions it started;
- does not discover handlers, publish messages, acknowledge transports,
  calculate retries, process domain events, or introduce a generic mediator.

The scope belongs to `Bondstone.EntityFrameworkCore`, not `Bondstone`, because
it is tied to `DbContext`, EF Core transactions, and `SaveChangesAsync`.
Bondstone core remains persistence- and provider-neutral.

Future module-scoped helpers may wrap this EF-specific scope, but they require
separate decisions if they introduce module identity, handler discovery,
domain-event capture, outbox mapping, transport acknowledgement, retry policy,
or a public unit-of-work abstraction.

## Amendment 2026-06-05

The EF persistence scope is not a required pattern for non-EF providers. A
future Dapper or direct ADO.NET provider should implement the core `Bondstone`
persistence contracts directly and own any connection, transaction, or commit
scope in its own package. Such providers should reuse core records and
orchestration contracts, but should not depend on EF entity mappings,
`DbContext`, or `IEntityFrameworkCorePersistenceScope`.

## Consequences

The inbox handle-once executor now has a clear EF Core transaction companion
without making core depend on EF Core.

Transport adapters and samples can compose:

1. EF persistence scope;
2. inbox handler executor;
3. handler delegate;
4. `scope.SaveChangesAsync` as the commit delegate.

The API makes transaction ownership visible. Consumers can still use their own
transactions; the scope joins them and leaves commit or rollback to the owner.

This does not yet recreate the historical module unit of work. Domain-event
capture, source-state-plus-outbox mapping, module identity scopes, retry
policy, and transport acknowledgement remain future work.

## Application Notes

- Current contract: `IEntityFrameworkCorePersistenceScope` executes operations
  inside an EF Core transaction and exposes explicit `SaveChangesAsync` for
  lower-level durable primitives. `Bondstone.EntityFrameworkCore` also uses
  that scope in an EF-specific module command system pipeline behavior for
  modules that opt into `UseEntityFrameworkCorePersistence<TDbContext>`. The
  scope is EF-specific and not a core provider abstraction for future non-EF
  integrations.
- Stable docs: Current persistence rules are described in
  [docs/architecture/persistence.md](../architecture/persistence.md), with
  extraction state in [docs/extraction.md](../extraction.md) and
  [docs/extraction-plan.md](../extraction-plan.md).
- Agent guidance: Root [AGENTS.md](../../AGENTS.md) requires ADR review before
  broad durable behavior, provider support, migration policy, transport
  strategy, or public API changes.
- Application evidence: EF Core persistence scope contract, implementation,
  service registration, fast registration tests, module-owned EF persistence
  opt-in, module command transaction/save behavior, receive-side inbox
  behavior inside the module command pipeline, and PostgreSQL transaction tests
  are applied.
- Pending or deferred: Module identity scopes, domain-event capture,
  source-state-plus-outbox mapping helpers, inbox markers, operation-state
  integration, transport acknowledgement, receive retry policy, and public
  unit-of-work abstractions remain future work.

## Verification

Read back [docs/architecture/persistence.md](../architecture/persistence.md),
[docs/extraction.md](../extraction.md), and
[docs/extraction-plan.md](../extraction-plan.md). Verified the applied slice
with:

- `dotnet build Bondstone.slnx --configuration Release --no-restore`
- `dotnet test tests/Bondstone.EntityFrameworkCore.Tests/Bondstone.EntityFrameworkCore.Tests.csproj --configuration Release --no-build`
- `dotnet test tests/Bondstone.EntityFrameworkCore.Postgres.Tests/Bondstone.EntityFrameworkCore.Postgres.Tests.csproj --configuration Release --no-build`
- `dotnet test Bondstone.slnx --configuration Release --no-build --filter "Category=Unit|Category=Application"`
- `pnpm backend:test:integration`
- `dotnet pack Bondstone.slnx --configuration Release --no-build --output artifacts/packages`
- `pnpm format:check`
- `git diff --check`

Later checkpoint verification restored the default `pnpm check` gate.

Module transaction checkpoint verification ran:

- `dotnet test tests/Bondstone.Tests/Bondstone.Tests.csproj --configuration Release --filter "Category=Unit"`
- `dotnet test tests/Bondstone.EntityFrameworkCore.Tests/Bondstone.EntityFrameworkCore.Tests.csproj --configuration Release --filter "Category=Unit|Category=Application"`
- `pnpm check`
