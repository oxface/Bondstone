# 0014 Inbox Registration Contract

Status: Amended
Application: Applied
Date: 2026-06-05

## Context

Bondstone currently has a provider-neutral inbox record and store. The EF Core
store can stage reads, inserts, and processed markers, but duplicate detection
is still exposed as provider-specific database exceptions when `SaveChanges`
flushes a duplicate inbox row.

Receive-side durable behavior needs a clearer boundary before building a full
inbox handler pipeline. A transport adapter or future inbox orchestrator needs
to know whether a message-handler pair was newly registered, was already seen,
or was already processed. That decision affects public persistence behavior,
provider conflict handling, and handler orchestration, so it needs an ADR.

The registration contract must stay smaller than an inbox handler pipeline. It
should record or classify the receive-side inbox row, but it should not invoke
user handlers, mark the row processed, own domain unit-of-work boundaries, or
decide retry policy.

## Decision

`Bondstone` defines a provider-neutral `IDurableInboxRegistrar` contract for
idempotently registering a received inbox record.

The registrar returns a `DurableInboxRegistrationResult` with status:

- `Registered`: the inbox record was inserted by this call;
- `AlreadyReceived`: an inbox record already exists but is not processed;
- `AlreadyProcessed`: an inbox record already exists and is processed.

The result carries the effective inbox record. For duplicate results this is
the existing persisted record.

Provider implementations may use provider-specific conflict primitives.
PostgreSQL uses `INSERT ... ON CONFLICT DO NOTHING` against the inbox primary
key and reads the existing row when the insert is skipped. The PostgreSQL
implementation participates in the current DbContext connection and
transaction when present, but it does not call `SaveChangesAsync`.

`IDurableInboxStore` remains the lower-level store for reading, staging an
inbox row, and marking it processed. Handler execution, processed-marker
orchestration, receive-side retry behavior, transport acknowledgement, and
domain unit-of-work policy remain future decisions.

## Amendment 2026-06-05

ADR 0015 applies the first narrow handler-execution decision:
`IDurableInboxHandlerExecutor` composes registration, a caller-supplied handler
delegate, processed-marker staging, and a caller-supplied commit delegate. The
registration contract in this ADR remains registration-only. Handler discovery,
receive-side retry behavior, transport acknowledgement, stale receive recovery,
module identity scopes, and higher-level transaction helper APIs remain future
decisions.

## Consequences

Duplicate inbox detection becomes an explicit public result instead of an
exception-only provider detail.

The PostgreSQL implementation can avoid poisoning the surrounding transaction
on duplicate registration because it uses `ON CONFLICT DO NOTHING` instead of
failing inserts.

The contract is intentionally registration-only. ADR 0015 defines the first
narrow delegate-based handle-once executor. A future inbox or transport
pipeline must still decide how handlers are discovered, how failures are
retried, how stale unprocessed rows are recovered, and how transport
acknowledgement is coordinated.

## Application Notes

- Current contract: `IDurableInboxRegistrar` idempotently registers an inbox
  record and returns whether it was newly registered, already received, or
  already processed.
- Stable docs: Current persistence rules are described in
  [docs/architecture/persistence.md](../architecture/persistence.md), with
  extraction state in [docs/extraction.md](../extraction.md) and
  [docs/extraction-plan.md](../extraction-plan.md).
- Agent guidance: Root [AGENTS.md](../../AGENTS.md) requires ADR review before
  broad durable behavior, provider support, or migration policy changes.
- Application evidence: Core inbox registration contract, PostgreSQL
  implementation, PostgreSQL service registration, and Testcontainers-backed
  registration tests are applied.
- Pending or deferred: Inbox handler discovery, receive-side retry policy,
  stale receive recovery, transport acknowledgement, module identity scopes,
  higher-level transaction helper APIs, provider implementations beyond
  PostgreSQL, and migration helpers remain future work.

## Verification

Read back [docs/architecture/persistence.md](../architecture/persistence.md),
[docs/extraction.md](../extraction.md), and
[docs/extraction-plan.md](../extraction-plan.md). Verified with targeted
PostgreSQL tests for newly registered, already received, already processed,
transaction-safe duplicate registration, and schema-aware registration.

Also verified the applied slice with:

- `dotnet build Bondstone.slnx --configuration Release --no-restore`
- `dotnet test tests/Bondstone.EntityFrameworkCore.Postgres.Tests/Bondstone.EntityFrameworkCore.Postgres.Tests.csproj --configuration Release --no-restore`
- `dotnet test Bondstone.slnx --configuration Release --no-build --filter "Category=Unit|Category=Application"`
- `pnpm backend:test:integration`
- `dotnet pack Bondstone.slnx --configuration Release --no-build --output artifacts/packages`
- `pnpm format:check`
- `git diff --check`

Fresh `dotnet restore` / `pnpm check` remains deferred because restore is
currently timing out around the PostgreSQL project in this environment; the
slice was verified against already restored assets.
