---
baseline_commit: dd17279
---

# Story 6.3: EF/PostgreSQL Production Persistence And Migrations

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a library consumer,
I want EF Core plus PostgreSQL to be the supported production durability path,
so that persistence behavior and migration ownership are clear.

## Acceptance Criteria

1. Given production durable persistence is documented, when consumers read setup and operations guidance, then EF Core plus PostgreSQL is named as the supported production path.
2. Given schema changes are required, when package guidance is reviewed, then consumers own EF migrations and Bondstone does not ship automatic schema rollout.
3. Given provider-specific semantics matter, when tests cover persistence, then PostgreSQL integration tests prove uniqueness, transactions, locking, claiming, retry, and terminal state where applicable.
4. Given EF InMemory tests exist, when reviewed, then they are limited to fast mapping or change-tracker boundaries and not used as relational proof.

## Tasks / Subtasks

- [x] Inventory current EF/PostgreSQL production-persistence guidance before changing docs. (AC: 1, 2)
  - [x] Read `docs/setup.md`, `docs/operations.md`, `docs/package-discovery.md`, `docs/packaging.md`, `docs/testing.md`, `docs/samples.md`, `src/Bondstone.Persistence.EntityFrameworkCore/README.md`, and `src/Bondstone.Persistence.EntityFrameworkCore.Postgres/README.md`.
  - [x] Confirm the docs consistently name `Bondstone.Persistence.EntityFrameworkCore` plus `Bondstone.Persistence.EntityFrameworkCore.Postgres` as the supported production durable persistence path.
  - [x] Confirm the docs consistently say applications own EF migrations, migration history, schema deployment, production rollout, retention, and operational recovery.
  - [x] Fix stale or contradictory receive/persistence wording encountered during review. In particular, re-check current `Bondstone.Transport.ServiceBus` receive-worker behavior before preserving statements in `docs/setup.md` or `docs/operations.md` that say the built-in Service Bus worker does not ingest into the durable incoming inbox.
- [x] Make migration ownership unambiguous without adding package-shipped migrations. (AC: 1, 2)
  - [x] Document that Bondstone packages provide EF mappings and PostgreSQL provider helpers only; consumers generate and apply module-owned migrations from their own `DbContext` models.
  - [x] Ensure setup guidance says migrator/design-time composition should include provider options, module schemas, application entities, and the same `ApplyBondstone...` mappings, but omit transport setup and hosted workers.
  - [x] Ensure docs do not imply `EnsureCreated`, runtime auto-migration, package migrations, or Bondstone-owned schema rollout is a production deployment path.
  - [x] Preserve the current no-checked-in-migrations posture for samples unless `docs/samples.md` intentionally says generated migrations are part of a future sample direction.
- [x] Inventory provider-backed persistence proof before adding tests. (AC: 3, 4)
  - [x] Read existing PostgreSQL tests under `tests/Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests/Persistence/` and domain-event transaction tests.
  - [x] Map current coverage to the required semantics: schema/table creation, primary keys and indexes, unique-violation handling, transactions and rollback, `FOR UPDATE SKIP LOCKED` claiming, retry scheduling, terminal outbox evidence, terminal incoming inbox evidence, stale claim inspection, module-scoped incoming claims, and duplicate ingestion.
  - [x] Read EF InMemory tests under `tests/Bondstone.Persistence.EntityFrameworkCore.Tests/` and verify they are fast mapping/change-tracker or package-local transaction-behavior checks, not accepted as PostgreSQL relational proof.
  - [x] Add only targeted PostgreSQL integration tests for real gaps found by the inventory. Do not duplicate Story 6.1 or 6.2 coverage just to satisfy the story mechanically.
- [x] If test gaps exist, fill them in the provider-owned test project. (AC: 3, 4)
  - [x] Put PostgreSQL relational-proof tests in `tests/Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests` with `[Trait("Category", "Integration")]`.
  - [x] Use existing `PostgreSqlFixture`, `PostgreSqlTestDbContext`, `PostgreSqlIncomingInboxTestDbContext`, and focused test patterns where possible.
  - [x] Use fresh `DbContext` instances for persisted-state assertions.
  - [x] Assert outcomes, rows, status fields, timestamps, claim owner/lease fields, operation state, outbox state, and exception classifier behavior rather than interaction-only behavior.
  - [x] Do not use EF InMemory for uniqueness, locking, transaction, claiming, retry, stale, terminal, or SQL-generation proof.
- [x] Keep package and public API scope narrow. (AC: 1, 2, 3, 4)
  - [x] Avoid public/protected API changes unless an inventory proves they are required for the story.
  - [x] Do not add `InternalsVisibleTo` for production package collaboration.
  - [x] Do not add package-shipped migrations, runtime migration application, migration hosted services, schema rollout automation, cleanup/retention workers, replay/purge APIs, broker DLQ movement, or non-EF production persistence providers.
  - [x] Do not move broker topology, retry, dead-letter policy, credentials, native monitoring, or worker placement into Bondstone.
- [x] Verify the story outcome.
  - [x] For docs-only changes, run `pnpm format:check`.
  - [x] If PostgreSQL tests change, run targeted tests first, for example `dotnet test tests/Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests/Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests.csproj --configuration Release --filter "FullyQualifiedName~PostgreSqlPersistence|FullyQualifiedName~IncomingInbox|FullyQualifiedName~Outbox"`.
  - [x] Run `pnpm backend:test:integration` when provider-backed persistence tests change.
  - [x] Run `pnpm backend:test` after runtime or test changes.
  - [x] Run `pnpm backend:pack` and review public API baselines only if public/protected package surface or package docs materially change.
  - [x] Run `pnpm check` as the final broad gate when code, public API, package metadata, samples, or broad docs change.

### Review Findings

- [x] [Review][Patch] Operations guidance still does not name EF/PostgreSQL as the supported production durable persistence path [docs/operations.md:432]
- [x] [Review][Patch] Setup namespace list omits the granular durable incoming inbox mapping helper [docs/setup.md:112]

## Dev Notes

Story 6.3 is mostly an alignment and proof story. The expected successful shape is docs cleanup plus a test inventory; runtime code changes are unlikely unless the inventory finds a real gap in EF/PostgreSQL mapping or provider behavior.

### Current State Intelligence

Core persistence mapping:

- `ApplyBondstonePersistence(...)` currently maps `outbox_messages`, `inbox_messages`, `incoming_inbox_messages`, and `operation_states`.
- Granular helpers exist for `ApplyBondstoneOutbox(...)`, `ApplyBondstoneInbox(...)`, `ApplyBondstoneIncomingInbox(...)`, and `ApplyBondstoneOperationState(...)`.
- Optional domain-event persistence is separate through `ApplyBondstoneDomainEvents(...)`; domain event records are module-local records, not durable transport or outbox messages.
- `Bondstone.Persistence.EntityFrameworkCore.Postgres` composes PostgreSQL EF module infrastructure and provider-specific stores through `UsePostgreSqlPersistence<TDbContext>(...)`.

Existing PostgreSQL proof is already substantial:

- `PostgreSqlPersistenceTests` covers table creation, primary keys, outbox claim lease columns, outbox transaction commit/rollback, persistence-scope rollback, existing-transaction behavior, and service registration.
- `PostgreSqlSourceOutboxAtomicityTests` covers module source state plus command/event outbox commit, rollback after send/publish, duplicate outbox primary-key rollback, and terminal outbox inspection.
- `PostgreSqlOutboxClaimTests`, `PostgreSqlDurableOutboxDispatchRecorderTests`, and `PostgreSqlDurableOutboxLeaseRenewerTests` cover `FOR UPDATE SKIP LOCKED`, claim ordering, due rows, active/stale leases, dispatched/retry/terminal outcomes, and lease renewal.
- `PostgreSqlIncomingInboxMutationTests` covers pending/due retry/stale processing claim, active lease exclusion, lease renewal, processed outcome, retry outcome, terminal outcome, and stale claim compare-and-update protection.
- `PostgreSqlIncomingInboxProcessingTests` covers durable incoming inbox processing through module receive, duplicate ingestion, module-scoped claims, retry with rollback of handler effects, terminal failure inspection, stale evidence inspection, operation completion, outgoing outbox rows, and optional domain-event records.
- `PostgreSqlPersistenceExceptionClassifierTests` covers provider unique-violation classification.
- `PostgreSqlDomainEventTransactionTests` covers provider-backed domain-event transaction behavior.

Known documentation drift to inspect:

- `docs/setup.md` and `docs/operations.md` already describe application-owned migrations, but they should be reviewed for consistency after the Story 6.2 Service Bus durable receive changes.
- Current docs include statements that the built-in Azure Service Bus receive worker does not ingest into the durable incoming inbox. The dev must check the actual `src/Bondstone.Transport.ServiceBus` implementation and update docs if that statement is now stale.
- `docs/samples.md` already says the sample smoke-test database is created from EF mappings and generated migrations are not checked in. Preserve that if still true.

### Files To Read Or Update

Read these UPDATE files completely before changing them:

- `docs/setup.md` - primary consumer setup path, EF mapping examples, migrator/design-time guidance, package setup, receive direction.
- `docs/operations.md` - production ownership summary, receive semantics, broker settlement, migration and retention ownership.
- `docs/package-discovery.md` - package/capability matrix and PostgreSQL helper guidance.
- `docs/packaging.md` - active package IDs and replacement/migration policy.
- `docs/testing.md` - EF InMemory and PostgreSQL integration-test policy.
- `docs/samples.md` - sample persistence and migration posture.
- `src/Bondstone.Persistence.EntityFrameworkCore/README.md` and `src/Bondstone.Persistence.EntityFrameworkCore.Postgres/README.md` - package-facing quick path and references.
- `src/Bondstone.Persistence.EntityFrameworkCore/Persistence/BondstoneModelBuilderExtensions.cs` - canonical EF mapping helper surface.
- `src/Bondstone.Persistence.EntityFrameworkCore/*/*Configuration.cs` - table names, primary keys, columns, indexes, and max lengths that consumers will see in migrations.
- `src/Bondstone.Persistence.EntityFrameworkCore.Postgres/Persistence/BondstonePostgreSqlBuilderExtensions.cs` and `BondstonePostgreSqlServiceCollectionExtensions.cs` - PostgreSQL setup path.
- `src/Bondstone.Persistence.EntityFrameworkCore.Postgres/Outbox/*.cs`, `Inbox/*.cs`, and `IncomingInbox/*.cs` - provider-specific SQL for claiming, retry, terminal, stale, duplicate, and lease behavior.
- `src/Bondstone.Transport.ServiceBus/ServiceBusReceiveWorker.cs` and `ServiceBusReceiveWorkerOptions.cs` only if documentation text about Service Bus durable inbox behavior is being corrected.

Likely docs to update:

- `docs/setup.md`
- `docs/operations.md`
- `docs/package-discovery.md`
- `src/Bondstone.Persistence.EntityFrameworkCore/README.md`
- `src/Bondstone.Persistence.EntityFrameworkCore.Postgres/README.md`

Likely tests to inspect or extend:

- `tests/Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests/Persistence/PostgreSqlPersistenceSchemaTests.cs`
- `tests/Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests/Persistence/PostgreSqlPersistenceTransactionTests.cs`
- `tests/Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests/Persistence/PostgreSqlOutboxClaimTests.cs`
- `tests/Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests/Persistence/PostgreSqlIncomingInboxMutationTests.cs`
- `tests/Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests/Persistence/PostgreSqlIncomingInboxProcessingTests.cs`
- `tests/Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests/Persistence/PostgreSqlSourceOutboxAtomicityTests.cs`
- `tests/Bondstone.Persistence.EntityFrameworkCore.Tests/Persistence/BondstoneModelBuilderExtensionsTests.cs`
- `tests/Bondstone.Persistence.EntityFrameworkCore.Tests/Persistence/EntityFrameworkCoreModuleTransactionBehaviorTests.cs`
- `tests/Bondstone.Persistence.EntityFrameworkCore.Tests/DomainEvents/EntityFrameworkCoreDomainEventPersistenceTests.cs`

### Architecture Compliance

Follow these non-negotiable rules:

- EF Core plus PostgreSQL is the supported production durable persistence path.
- Consumers own EF migrations, migration history, schema deployment, rollout policy, and rollback planning.
- Bondstone packages provide mappings and provider helpers, not package-shipped migrations or automatic schema rollout.
- Module-owned EF persistence must keep source state, outbox rows, direct inbox markers, operation state, domain-event records, and incoming durable inbox rows in the owning module transaction boundary where applicable.
- Generic EF mappings own canonical table, column, index, and constraint names; PostgreSQL adapts provider-specific behavior.
- EF Core InMemory is acceptable only for fast mapping/change-tracker boundaries. It is not relational proof for uniqueness, transactions, savepoints, locking, SQL generation, claiming, deduplication races, retry, stale, or terminal state transitions.
- Cleanup, retention, replay, purge, stale-row mutation, and broker dead-letter movement remain application-owned unless future BMAD artifacts add them.
- Transport adapters stay thin native-driver envelope adapters. Broker topology, provisioning, credentials, prefetch/concurrency, retry, dead-letter policy, worker placement, and monitoring remain host-owned.
- Bondstone remains a durable module-boundary library/framework, not a generic bus, workflow engine, saga/process-manager framework, broker topology manager, code generator, SaaS framework, application platform, or broker runtime owner.
- Public/protected API changes are compatibility-sensitive. If any public surface changes, inventory affected setup APIs and update public API baselines only after review.

### Testing Requirements

Use the repository testing policy from `docs/testing.md`:

- PostgreSQL-backed relational behavior belongs in `[Trait("Category", "Integration")]` tests backed by Testcontainers or equivalent real provider infrastructure.
- EF InMemory tests belong in `[Trait("Category", "Unit")]` or `[Trait("Category", "Application")]` only when they are testing package-local mapping, change-tracker behavior, or non-relational transaction-runner orchestration.
- Any new test proving uniqueness, transaction rollback, row locking, `SKIP LOCKED`, claim owner mutation, retry, terminal state, or stale state must use PostgreSQL.
- Prefer durable state assertions over interaction assertions: table rows, status, attempt count, timestamps, claim fields, failure reason, operation state, outbox state, and inspector results.
- Use fresh `DbContext` instances for persisted-state assertions.

High-value gap-fill candidates if the inventory finds a miss:

- `ApplyBondstonePersistence_WhenDatabaseIsCreated_CreatesIncomingInboxIndexes` to prove incoming inbox claim indexes from the mapped model exist in PostgreSQL.
- `ApplyBondstonePersistence_WhenDatabaseIsCreated_CreatesOperationDiagnosticColumns` if operation diagnostic columns are not already covered in schema proof.
- A documentation-facing test or assertion that no checked-in migrations exist is probably not worth adding; prefer docs clarity unless the repo later adopts migration artifacts.

### Previous Story Intelligence

Story 6.2 completed the durable receive transaction boundary and established these patterns:

- Prefer focused PostgreSQL integration coverage for transaction, uniqueness, retry, terminal, and persisted-state proof.
- Keep runtime changes out unless tests expose a real gap.
- Use fresh `DbContext` verification and assert durable payload/state details, not only row counts.
- Terminal receive evidence should be exposed through the intended inspection path, not only raw EF queries.
- Keep `incoming_inbox_messages` as the operator-facing receive ledger and `inbox_messages` as a direct module-processing idempotency detail.
- Do not add generic bus behavior, saga/process-manager behavior, broker topology ownership, automatic schema rollout, automatic domain-event publication, cleanup workers, or provider-neutral broker runtime ownership.
- Story 6.2 changed Service Bus receive behavior and tests; docs mentioning Service Bus durable incoming inbox behavior must be rechecked against current code.

Story 6.1 completed source outbox atomicity and established:

- PostgreSQL integration tests are the right proof for source state/outbox transaction behavior.
- Duplicate durable identity tests should assert the specific PostgreSQL duplicate/primary-key behavior where relevant.
- Terminal outbox evidence should be observable through `IDurableOutboxInspector`.
- Staged verification used targeted PostgreSQL tests, `pnpm backend:test:integration`, `pnpm backend:test`, `pnpm backend:pack`, and `pnpm check`.

### Git Intelligence

Recent commits at story creation time:

- `dd17279 fix: sb durable worker`
- `6355dc4 fix: sb worker`
- `23bda7f fix: atomicity test`
- `946886c fix: epic 5 done`
- `871c479 fix: integration event tests`

The recent pattern is narrow runtime patches only when tests expose a real gap, plus provider-backed integration tests for durability claims. Follow that pattern.

### Latest Technical Information

No dependency upgrade is required for this story. Use the repository-pinned stack in `Directory.Packages.props` and `global.json`:

- .NET SDK `10.0.108` with `rollForward: latestFeature`; target framework `net10.0`.
- EF Core packages `10.0.8`.
- `Npgsql` `10.0.3`.
- `Npgsql.EntityFrameworkCore.PostgreSQL` `10.0.2`.
- `Testcontainers.PostgreSql` `4.12.0`.
- xUnit `2.9.3` with existing `[Trait("Category", "...")]` usage.

Current official docs align with the story direction:

- Microsoft EF Core migration guidance recommends production deployment via reviewed SQL scripts; use that as external support for app-owned migration rollout, not as a reason for Bondstone to ship migrations. Source: https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/applying
- Npgsql EF Core provider 10.0 is the current provider line for EF Core 10-era PostgreSQL integration. Source: https://www.npgsql.org/efcore/release-notes/10.0.html
- PostgreSQL documents `SKIP LOCKED` as useful for avoiding lock contention with queue-like tables while warning that it gives an inconsistent view for general reads. That supports current claim-table usage, not arbitrary business reads. Source: https://www.postgresql.org/docs/current/sql-select.html
- Testcontainers for .NET documents PostgreSQL containers as a normal .NET integration-test dependency. Source: https://dotnet.testcontainers.org/modules/postgres/

### Project Context Reference

Project context says Bondstone is a .NET library/framework for durable module boundaries, durable command sending, EF Core backed inbox/outbox persistence, operation observation, and transport adapters. EF Core plus PostgreSQL is the supported production durable persistence path. Consumers own EF migrations. Native broker delivery must not be acknowledged or completed before durable inbox ingestion succeeds. EF InMemory is not proof of relational durability, uniqueness, transactions, locking, claiming, retries, or PostgreSQL behavior. Prefer repository scripts: `pnpm check`, `pnpm backend:test`, `pnpm backend:test:integration`, and `pnpm backend:pack`.

### Open Questions

None blocking. The dev should verify current Service Bus durable receive behavior from code before fixing docs because Story 6.2 completed after some consumer docs were written.

### Story Completion Status

Ultimate context engine analysis completed - comprehensive developer guide created.

### References

- `_bmad-output/planning-artifacts/epics.md` - Epic 6 and Story 6.3 acceptance criteria
- `_bmad-output/planning-artifacts/prds/prd-Bondstone-2026-06-18/prd.md` - FR6.3, FR6.4, FR10.3
- `_bmad-output/planning-artifacts/architecture.md` - Persistence Architecture, Verification Strategy, Transport Boundary, Documentation Ownership
- `_bmad-output/planning-artifacts/research/technical-epic-6-durable-persistence-and-receive-ledger-research-2026-06-19.md` - current Epic 6 technology research
- `_bmad-output/project-context.md` - runtime, code, testing, and workflow guardrails
- `_bmad-output/implementation-artifacts/6-1-source-outbox-atomicity.md` - previous source outbox proof patterns
- `_bmad-output/implementation-artifacts/6-2-durable-receive-transaction-boundary.md` - previous durable receive proof patterns and Service Bus docs warning
- `docs/setup.md` - consumer setup and migration guidance
- `docs/operations.md` - production ownership and runbook guidance
- `docs/package-discovery.md` - package/capability matrix
- `docs/packaging.md` - package IDs and migration policy
- `docs/testing.md` - EF InMemory and PostgreSQL integration-test rules
- `docs/samples.md` - sample migration posture
- `src/Bondstone.Persistence.EntityFrameworkCore/Persistence/BondstoneModelBuilderExtensions.cs` - canonical EF mapping helpers
- `src/Bondstone.Persistence.EntityFrameworkCore.Postgres/Persistence/BondstonePostgreSqlBuilderExtensions.cs` - PostgreSQL setup helpers
- `tests/Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests/Persistence/PostgreSqlPersistenceSchemaTests.cs` - mapped schema proof
- `tests/Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests/Persistence/PostgreSqlPersistenceTransactionTests.cs` - PostgreSQL transaction proof
- `tests/Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests/Persistence/PostgreSqlSourceOutboxAtomicityTests.cs` - source state plus outbox proof
- `tests/Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests/Persistence/PostgreSqlIncomingInboxMutationTests.cs` - incoming inbox claim/retry/terminal/stale proof
- `tests/Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests/Persistence/PostgreSqlIncomingInboxProcessingTests.cs` - incoming receive transaction proof
- `tests/Bondstone.Persistence.EntityFrameworkCore.Tests/Persistence/BondstoneModelBuilderExtensionsTests.cs` - EF mapping/change-tracker boundary proof

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- 2026-06-20: Resolved workflow customization manually after `python3 _bmad/scripts/resolve_customization.py --skill .agents/skills/bmad-dev-story --key workflow` failed because the environment could not import Python's `json` module.
- 2026-06-20: Inventory confirmed `ServiceBusReceiveWorker` now resolves the durable incoming inbox ingestion boundary and completes native messages only after `IngestAndSaveAsync` succeeds.
- 2026-06-20: Documentation sweep found stale Service Bus direct receive wording in setup, operations, package discovery, and public API notes.
- 2026-06-20: PostgreSQL proof inventory found existing integration coverage for table creation, primary keys, duplicate/unique violations, transaction rollback, `FOR UPDATE SKIP LOCKED`, claim/lease mutation, retry, terminal outbox and incoming inbox evidence, stale claim inspection, module-scoped incoming claims, and duplicate ingestion.
- 2026-06-20: EF InMemory inventory found tests limited to mapping, entity round-trips, change-tracker/store behavior, package-local transaction orchestration, and domain-event behavior; relational proof remains in PostgreSQL integration tests.

### Completion Notes List

- Updated consumer setup guidance to name EF/PostgreSQL as the supported production durable persistence path and to describe Service Bus durable incoming inbox ingestion before native completion.
- Updated operations guidance to clarify Service Bus settlement after durable ingestion and to state that Bondstone does not ship package-owned migrations or automatic schema rollout.
- Updated package discovery and package READMEs to include durable incoming inbox mappings, PostgreSQL provider helper scope, and application-owned EF migrations/schema rollout.
- Updated public API notes so Service Bus durable incoming inbox handoff is documented consistently with RabbitMQ.
- Preserved sample no-checked-in-migrations posture and did not add package migrations, runtime migration execution, schema rollout automation, cleanup/retention workers, broker ownership, runtime code, public API changes, or new dependencies.
- No PostgreSQL tests were added because the inventory found the required provider-backed semantics already covered by existing integration tests.
- Verification passed: `pnpm format:check`, `pnpm backend:test`, and final `pnpm check`.

### File List

- `_bmad-output/implementation-artifacts/6-3-ef-postgresql-production-persistence-and-migrations.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `docs/operations.md`
- `docs/package-discovery.md`
- `docs/public-api.md`
- `docs/setup.md`
- `src/Bondstone.Persistence.EntityFrameworkCore/README.md`
- `src/Bondstone.Persistence.EntityFrameworkCore.Postgres/README.md`

### Change Log

- 2026-06-20: Aligned EF/PostgreSQL production persistence, migration ownership, and Service Bus durable receive documentation; moved story to review.
