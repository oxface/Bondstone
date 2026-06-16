# 0015 Inbox Handle-Once Orchestration

Status: Archived
Application: Not Applicable
Date: 2026-06-05

## Context

ADR 0014 introduced an explicit inbox registration result so receive-side
durability does not depend on provider duplicate exceptions as public control
flow. Bondstone can now tell whether a message-handler pair was newly
registered, already received but not processed, or already processed.

The next receive-side boundary is handler execution. Historical source material
combined handler execution, inbox claim logic, EF Core unit-of-work behavior,
module lookup, and transport integration in one layer. Bondstone should keep
the first extracted orchestration smaller and more explicit.

Modern modular-monolith and microservice systems still need a handle-once
boundary, but Bondstone should not introduce a generic mediator or message bus
for in-process calls. Handler discovery, transport acknowledgement, retry
policy, and unit-of-work ownership need separate decisions.

## Decision

`Bondstone` defines a small inbox handle-once orchestration boundary that
composes:

- `IDurableInboxRegistrar` to register or classify the receive-side inbox row;
- a caller-supplied handler delegate for the actual user work;
- `IDurableInboxStore` to stage the processed marker after successful handler
  completion;
- a caller-supplied commit delegate for the transaction or unit of work that
  persists handler state and the processed marker atomically.

The executor runs the handler only when registration returns `Registered`.

When registration returns `AlreadyProcessed`, the executor returns a skipped
result and does not run the handler.

When registration returns `AlreadyReceived`, the executor returns a duplicate
in-progress or unresolved result and does not run the handler. Without an
inbox lease, retry state, or ownership model, Bondstone cannot prove that
running the handler again is safe.

If the handler throws, the executor does not mark the inbox row processed and
does not call the commit delegate. The exception flows to the caller so the
transport or caller-owned pipeline can decide retry and acknowledgement.

The commit delegate is intentionally explicit. Bondstone core does not start
database transactions, call EF Core `SaveChangesAsync`, acknowledge transport
messages, discover handlers, or wrap ordinary in-process calls in a mediator.
Provider or transport packages may later offer higher-level helpers around
this contract after their transaction behavior is proven with integration
tests.

## Amendment 2026-06-05

ADR 0016 applies the first EF-specific transaction companion:
`IEntityFrameworkCorePersistenceScope` executes operations inside an EF Core
transaction and exposes `SaveChangesAsync` as the explicit commit delegate for
the core inbox handler executor. This does not replace the ADR 0015 core
contract. Module identity scopes, domain-event capture, handler discovery,
transport acknowledgement, receive retry policy, and broader module-scoped
unit-of-work behavior remain future decisions.

## Amendment 2026-06-10: Commit Ownership Cleanup

ADR 0043 later removes the commit delegate from
`IDurableInboxHandlerExecutor`. The executor now composes inbox registration,
the caller-supplied handler delegate, and processed-marker staging only.
Transaction and save ownership lives outside the executor, normally in module
provider transaction behaviors. A `Handled` result means the handler ran and
the processed marker was staged in the current persistence context; the
surrounding transaction still determines durable commit or rollback.

## Consequences

The first handle-once primitive is usable by transport adapters and samples
without committing Bondstone to a generic message bus.

The API makes atomicity visible: callers must supply a commit operation that
persists handler state and the processed marker together. If registration,
handler work, processed marker, and commit are not in the same transactional
boundary, failures can leave an unprocessed inbox row that future attempts see
as `AlreadyReceived`.

`AlreadyReceived` remains deliberately conservative. Later ADRs may add inbox
leases, stale receive recovery, retry metadata, or dead-letter behavior that
allows safe re-processing of abandoned receive attempts.

Transport acknowledgement remains outside this contract. A transport adapter
should acknowledge only after handle-once execution and its commit boundary
have succeeded, but the exact adapter behavior requires transport-specific
decisions.

## Application Notes

- Current contract: `IDurableInboxHandlerExecutor` runs a caller-supplied
  handler once for newly registered inbox records, stages the processed marker,
  and returns an explicit `DurableInboxHandleResult`. It does not own the
  surrounding transaction or save boundary.
- Stable docs: Current messaging and persistence rules are described in
  [docs/architecture/messaging.md](../architecture/messaging.md) and
  [docs/architecture/persistence.md](../architecture/persistence.md), with
  current implementation state in [docs/archive/mvp-plan.md](../archive/mvp-plan.md).
- Agent guidance: Root [AGENTS.md](../../AGENTS.md) requires ADR review before
  broad durable behavior, transport strategy, provider support, migration
  policy, or public API changes.
- Application evidence: Core handle-once result and executor contracts are
  applied with neutral unit tests. Module command and event receive pipelines,
  EF and PostgreSQL composition, direct transport receive dispatchers, local
  transport, RabbitMQ and Service Bus settlement helpers, and sample smoke
  tests compose this boundary.
- Pending or deferred: None for the handle-once executor decision. Inbox lease
  or stale receive recovery, receive retry policy, and broker dead-letter
  behavior remain separate future decisions.

## Verification

Read back [docs/architecture/messaging.md](../architecture/messaging.md),
[docs/architecture/persistence.md](../architecture/persistence.md),
[docs/archive/mvp-plan.md](../archive/mvp-plan.md), and [docs/testing.md](../testing.md).
Verified the applied slice
with:

- `dotnet test tests/Bondstone.Tests/Bondstone.Tests.csproj --configuration Release --no-restore`
- `dotnet build Bondstone.slnx --configuration Release --no-restore`
- `dotnet test tests/Bondstone.EntityFrameworkCore.Postgres.Tests/Bondstone.EntityFrameworkCore.Postgres.Tests.csproj --configuration Release --no-build`
- `dotnet test Bondstone.slnx --configuration Release --no-build --filter "Category=Unit|Category=Application"`
- `pnpm backend:test:integration`
- `dotnet pack Bondstone.slnx --configuration Release --no-build --output artifacts/packages`

Later checkpoint verification restored the default `pnpm check` gate.

The 2026-06-10 commit ownership cleanup was verified with:

- `git diff --check`
- `pnpm format:check`
- `pnpm backend:build`
- `pnpm backend:test:fast`
- `pnpm backend:test:integration`
- `dotnet test tests/Bondstone.EntityFrameworkCore.Postgres.Tests/Bondstone.EntityFrameworkCore.Postgres.Tests.csproj --configuration Release --no-build --filter "Category=Integration"`
