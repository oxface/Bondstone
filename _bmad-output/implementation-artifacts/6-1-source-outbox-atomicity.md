---
baseline_commit: 946886c
---

# Story 6.1: Source Outbox Atomicity

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a module owner,
I want source state and outgoing durable envelopes to commit atomically,
so that accepted durable work is not separated from the state change that caused it.

## Acceptance Criteria

1. Given a handler changes source module state and sends or publishes durable work, when the transaction commits, then state and outbox rows are both visible.
2. Given the transaction rolls back, when state is inspected, then no outgoing outbox row remains for the failed work.
3. Given an outbox dispatch fails terminally, when operations are inspected, then terminal outbox evidence is visible without relying on broker DLQ ownership.
4. Given concurrency or uniqueness behavior matters, when tests are written, then they use PostgreSQL-backed integration tests rather than EF InMemory.

## Tasks / Subtasks

- [x] Inventory current source transaction, sender/publisher, outbox, and inspector behavior before changing code. (AC: 1, 2, 3, 4)
  - [x] Confirm `EntityFrameworkCoreModuleTransactionRunner` still wraps handler execution plus `SaveChangesAsync` in the module `DbContext` transaction when no transaction is already active.
  - [x] Confirm `EntityFrameworkCoreDurableOutboxWriter` only adds `OutboxMessageEntity` to the active `DbContext` and does not call `SaveChangesAsync` or open its own transaction.
  - [x] Confirm `DurableCommandSender` and `DurableEventPublisher` resolve the current source module from `IModuleExecutionContextAccessor` and stage envelopes through the module outbox writer.
  - [x] Confirm `IDurableOutboxInspector` and `EntityFrameworkCoreDurableOutboxInspectionStore` expose terminal outbox failures by source module.
- [x] Add PostgreSQL integration coverage for committed source state plus durable command outbox rows. (AC: 1, 4)
  - [x] Add a source-state test entity and dedicated test `DbContext` in `tests/Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests`, or extend an existing Postgres fixture only if it stays readable.
  - [x] Configure a source module with `UseDurableMessaging()` and `UsePostgreSqlPersistence<TDbContext>()`.
  - [x] Register any remote durable command contract with `BondstoneBuilder.RegisterMessage<TCommand>()` when the target module route is not local to the test.
  - [x] Execute a source command handler through `IModuleCommandExecutor`; in the handler, persist source state and call `IDurableCommandSender.SendAsync`.
  - [x] Verify after commit that the source entity and exactly one `OutboxMessageEntity` are visible in PostgreSQL, with `MessageKind.Command`, the source module, the target module, stable message identity, and pending dispatch state.
- [x] Add PostgreSQL integration coverage for committed source state plus integration-event outbox rows. (AC: 1, 4)
  - [x] Register the source module's published event with `module.Events.RegisterPublishedEvent<TEvent>()`.
  - [x] Execute a handler that persists source state and calls `IDurableEventPublisher.PublishAsync`.
  - [x] Verify after commit that source state and an event `OutboxMessageEntity` are visible, with `MessageKind.Event` and `TargetModule == null`.
- [x] Add PostgreSQL rollback coverage proving state and outbox rows do not diverge. (AC: 2, 4)
  - [x] Execute a handler that adds source state, stages a durable command or event, then throws before the module transaction commits.
  - [x] Assert the exception is surfaced and a fresh `DbContext` sees no source-state row and no outbox row for the failed work.
  - [x] Preserve existing transaction callback behavior: domain-event sources must still clear only after an observed commit and must not clear for external transactions owned by application code.
- [x] Add PostgreSQL uniqueness or duplicate evidence where relational behavior matters. (AC: 4)
  - [x] Use a real PostgreSQL test to prove duplicate outbox identity or primary-key violation behavior, not EF InMemory.
  - [x] If the duplicate is staged in the same source transaction as app state, assert the transaction rolls back and leaves neither source state nor a partial outbox row.
  - [x] Reuse the existing `PostgreSqlFixture` and `postgres:17-alpine` Testcontainers setup.
- [x] Add or strengthen terminal outbox evidence inspection coverage. (AC: 3, 4)
  - [x] Use `DurableOutboxDispatcher` or a module outbox dispatcher with a throwing `IDurableEnvelopeDispatcher` and `DurableOutboxFailurePolicy(maxAttempts: 1)`.
  - [x] Verify the dispatch result records a terminal failure and the persisted outbox row has `DurableOutboxStatus.TerminalFailed`, `FailedAtUtc`, and `FailureReason`.
  - [x] Verify `IDurableOutboxInspector.FindTerminalFailedAsync(sourceModule, ...)` returns the failed record for the source module.
  - [x] Do not add broker DLQ, broker retry, topology management, or transport settlement behavior in this story.
- [x] Keep implementation scope narrow. (AC: 1, 2, 3, 4)
  - [x] Prefer tests-only if current runtime behavior already satisfies the acceptance criteria.
  - [x] If a runtime gap appears, make the smallest EF/Postgres persistence change that keeps module-owned durability and host-owned transport responsibilities intact.
  - [x] Do not introduce a generic bus abstraction, saga/process manager, provider-neutral broker runtime, automatic domain-event dispatch, or automatic schema rollout.
- [x] Verify the story outcome.
  - [x] Run targeted Postgres integration tests, for example `dotnet test tests/Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests/Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests.csproj --configuration Release --filter "FullyQualifiedName~SourceOutboxAtomicity|FullyQualifiedName~PostgreSqlOutboxDispatcherTests|FullyQualifiedName~PostgreSqlPersistenceTransactionTests"`.
  - [x] Run `pnpm backend:test:integration` when PostgreSQL-backed coverage is added.
  - [x] Run `pnpm backend:test` after runtime code changes.
  - [x] Run `pnpm backend:pack` and review public API baselines only if public/protected package surface changes.
  - [x] Run `pnpm check` as the final broad gate when code, public API, package metadata, samples, or broad docs change.

### Review Findings

- [x] [Review][Patch] Duplicate outbox identity test accepts any PostgreSQL error [tests/Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests/Persistence/PostgreSqlSourceOutboxAtomicityTests.cs:122]
- [x] [Review][Patch] Source atomicity success tests do not verify the committed durable payload [tests/Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests/Persistence/PostgreSqlSourceOutboxAtomicityTests.cs:37]

## Dev Notes

Story 6.1 is a proof slice for the source side of the transactional outbox. The likely successful implementation is narrow PostgreSQL integration coverage around existing behavior. Runtime changes should happen only if those tests expose a real atomicity gap.

### Current State Intelligence

The source transaction boundary already exists:

- `src/Bondstone.Persistence.EntityFrameworkCore/Persistence/EntityFrameworkCoreModuleTransactionRunner.cs` checks whether the module uses EF Core persistence, resolves the module `DbContext`, validates required durable outbox/inbox mappings for durable modules, and runs handler execution plus `scope.SaveChangesAsync()` inside `EntityFrameworkCorePersistenceScope`.
- `EntityFrameworkCorePersistenceScope<TDbContext>` opens an EF Core transaction when there is no current transaction, commits after the operation completes, and rolls back on exception. If a transaction is already active, it runs the operation without committing the application's transaction.
- `ModuleCommandRuntime` executes transaction runners around validation, handler execution, receive inbox handling where applicable, operation completion, and post-handler actions.
- `ModuleRuntimeTransactionCallbacks` plus `IModuleRuntimeExecutionContext` provide observed commit and rollback callbacks. EF domain-event persistence depends on this to clear pending domain events only after Bondstone observes a transaction commit.

The outbox writer participates in the active `DbContext`:

- `EntityFrameworkCoreDurableOutboxWriter<TDbContext>.WriteAsync` creates a `DurableOutboxRecord`, maps it to `OutboxMessageEntity`, and calls `context.Set<OutboxMessageEntity>().Add(...)`. It does not save changes, start a transaction, or dispatch to a broker.
- `EntityFrameworkCoreModuleDurableOutboxWriter<TDbContext>` wraps the generic writer for a named module.
- `OutboxMessageEntity` stores message id, message kind, stable message type name, source module, optional target module, operation id, trace/causation metadata, payload, stored time, dispatch status, attempt count, retry timestamps, terminal failure data, and claim lease fields.
- `OutboxMessageEntityConfiguration` maps `outbox_messages` with primary key `MessageId` plus indexes for status scheduling, claim lease, message type, and operation id.

The sender and publisher already stage through source-module outbox writers:

- `DurableCommandSender` requires a current module execution context, creates a command `DurableMessageEnvelope` with source module from the execution context and required target module, writes it to the source module outbox writer, and optionally stages pending operation state.
- `DurableEventPublisher` requires a current module execution context, verifies the source module registered the published event, creates an event envelope with no target module, and writes it to the source module outbox writer.
- Durable send and publish accept work and return metadata. They do not return target handler results and must remain distinct from immediate command execution.

Terminal outbox evidence already has most runtime pieces:

- `DurableOutboxDispatcher` claims due rows, dispatches them through the host-configured `IDurableEnvelopeDispatcher`, records retry or terminal failure through `IDurableOutboxDispatchRecorder`, and emits persisted outbox status counts.
- `PostgreSqlDurableOutboxClaimer` uses PostgreSQL `FOR UPDATE SKIP LOCKED` over due pending or expired processing rows and writes claim lease state.
- `PostgreSqlDurableOutboxDispatchRecorder` records dispatched, retry, or terminal failure state only when the row is still processing, still claimed by the worker, and still within the lease.
- `EntityFrameworkCoreDurableOutboxInspectionStore` exposes terminal failed rows, and `IDurableOutboxInspector` routes inspection to the module's registered source-module store.

Current coverage is close but not enough for this story:

- `PostgreSqlPersistenceTransactionTests` proves direct `EntityFrameworkCoreDurableOutboxWriter` writes commit and roll back with PostgreSQL transactions.
- `PostgreSqlOutboxDispatcherTests` proves dispatcher retry and terminal failure state, but terminal inspection through `IDurableOutboxInspector` should be added or strengthened for AC3.
- `PostgreSqlOutboxClaimTests` proves `SKIP LOCKED`, due-row claiming, active lease exclusion, and expired lease reclaiming.
- `PostgreSqlDomainEventTransactionTests` proves the existing pattern for module command execution inside real PostgreSQL transactions and protects transaction callback semantics.
- Missing proof: handler-level source state plus `IDurableCommandSender` or `IDurableEventPublisher` in the same source module transaction, with commit and rollback inspected through a fresh PostgreSQL `DbContext`.

### Files To Read Or Update

Read these UPDATE files before changing behavior:

- `src/Bondstone.Persistence.EntityFrameworkCore/Persistence/EntityFrameworkCoreModuleTransactionRunner.cs` - current module transaction runner, mapping validation, save/commit/rollback callback notifications.
- `src/Bondstone.Persistence.EntityFrameworkCore/Persistence/EntityFrameworkCorePersistenceScope.cs` - EF transaction ownership behavior for owned versus already active transactions.
- `src/Bondstone/Modules/Execution/ModuleCommandRuntime.cs` - transaction runner ordering around validation, handler, inbox, operations, and post-handler actions.
- `src/Bondstone/Modules/Execution/IModuleRuntimeExecutionContext.cs` and `ModuleRuntimeTransactionCallbacks.cs` - commit/rollback callback behavior that domain-event persistence depends on.
- `src/Bondstone.Persistence.EntityFrameworkCore/Outbox/EntityFrameworkCoreDurableOutboxWriter.cs` and `EntityFrameworkCoreModuleDurableOutboxWriter.cs` - outbox staging into the active `DbContext`.
- `src/Bondstone.Persistence.EntityFrameworkCore/Outbox/OutboxMessageEntity.cs` and `OutboxMessageEntityConfiguration.cs` - stored row shape, primary key, indexes, and dispatch evidence fields.
- `src/Bondstone/Messaging/Sending/DurableCommandSender.cs` - command envelope creation, current source module requirement, outbox writer resolution, and pending operation state.
- `src/Bondstone/Messaging/Publishing/DurableEventPublisher.cs` - integration event envelope creation, published-event registration validation, and no-target event rows.
- `src/Bondstone.Persistence/Persistence/Outbox/DurableOutboxDispatcher.cs` - dispatch retry and terminal failure handling.
- `src/Bondstone.Persistence.EntityFrameworkCore.Postgres/Outbox/PostgreSqlDurableOutboxClaimer.cs`, `PostgreSqlDurableOutboxDispatchRecorder.cs`, and `PostgreSqlModuleDurableOutboxDispatcher.cs` - provider-specific claim, lease, retry, and terminal mutation behavior.
- `src/Bondstone/Persistence/Outbox/DurableOutboxInspector.cs` and `src/Bondstone.Persistence.EntityFrameworkCore/Outbox/EntityFrameworkCoreDurableOutboxInspectionStore.cs` - terminal failure inspection path.

Likely test files:

- Add `tests/Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests/Persistence/PostgreSqlSourceOutboxAtomicityTests.cs` for the story-specific handler-level proof, or add a focused partial file beside the existing `PostgreSqlPersistenceTests` partials.
- Extend `tests/Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests/Persistence/PostgreSqlOutboxDispatcherTests.cs` only if terminal inspection evidence naturally belongs there.
- Reuse `tests/Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests/PostgreSqlFixture.cs` and the existing Postgres helper style.
- Reuse patterns from `tests/Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests/DomainEvents/PostgreSqlDomainEventTransactionTests.cs` for service-provider setup, command execution, `EnsureDeletedAsync`, and `EnsureCreatedAsync`.

### Architecture Compliance

Follow these non-negotiable rules:

- Bondstone remains a durable module-boundary library/framework, not a generic bus, workflow engine, saga/process-manager framework, broker topology manager, code generator, SaaS framework, application platform, or broker runtime owner.
- Source state and outgoing durable outbox rows must commit atomically in the source module transaction boundary.
- Module-owned EF persistence keeps source state, outbox rows, inbox markers, operation state, domain-event records, and incoming durable inbox rows in the owning module transaction boundary where applicable.
- Durable command send accepts work and returns metadata. Target handler results are observed through operation APIs, not returned from send.
- Integration events are durable cross-module facts with no single target module. Fanout and topology stay host-owned.
- Domain events are module-local facts. Do not make this story publish domain events automatically or stage domain events as integration-event outbox rows.
- Outbox terminal failure evidence is persisted outbox state. Do not depend on broker DLQ ownership for AC3.
- EF Core plus PostgreSQL is the production durable persistence path. EF InMemory is not proof for transaction, uniqueness, locking, claiming, retry, or terminal failure behavior.
- Consumers own EF migrations. Do not add package-shipped migrations or automatic schema rollout.
- Runtime package collaboration must use explicit contracts or package-local implementation. Do not add production `InternalsVisibleTo`.

### Testing Requirements

Use the repository testing policy from `docs/testing.md`:

- Mark all PostgreSQL-backed atomicity, rollback, duplicate, claim, retry, and terminal evidence tests with `[Trait("Category", "Integration")]`.
- Use EF InMemory only for fast mapping or change-tracker boundaries. It must not be used as proof for this story's acceptance criteria.
- Keep tests grouped under `tests/Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests` because this story depends on PostgreSQL transaction and constraint behavior.
- Prefer outcome assertions over interaction-only assertions: persisted source state, outbox rows, dispatch state, failed timestamps, failure reason, inspector rows, and absence of rows after rollback.
- Keep test fixtures neutral and in-repository. Do not depend on samples or a separate product module.

High-value test cases:

- `ModuleCommand_WhenHandlerSavesStateAndSendsDurableCommand_CommitsStateAndOutboxRow`.
- `ModuleCommand_WhenHandlerSavesStateAndPublishesIntegrationEvent_CommitsStateAndEventOutboxRow`.
- `ModuleCommand_WhenHandlerThrowsAfterDurableSend_RollsBackStateAndOutboxRow`.
- `OutboxWriter_WhenDuplicateMessageIdViolatesPrimaryKey_RollsBackSourceStateAndOutboxRows`.
- `OutboxDispatcher_WhenTerminalFailureRecorded_CanInspectTerminalOutboxEvidenceBySourceModule`.

### Previous Story Intelligence

Story 5.4 completed durable message-kind and domain-event boundary coverage in commit `871c479 fix: integration event tests`.

Carry these learnings forward:

- Commands, integration events, and domain events are intentionally distinct. Do not introduce a generic durable-message API for application contracts.
- Command outbox rows require `MessageKind.Command` and a target module. Event outbox rows require `MessageKind.Event` and no target module.
- Domain events are not outbox messages and are not automatically converted to integration events.
- Explicit domain-to-integration publication, where needed, is module code that maps local domain state to a separate `IIntegrationEvent` and calls `IDurableEventPublisher`.
- Stable durable identities and handler/subscriber identities must stay explicit and must not be derived from CLR type names.
- Public/protected API changes need public API classification and baseline review.

Story 5.3 completed immediate command and query boundaries:

- Query execution must remain read-oriented and must not become a durable write path.
- `IModuleCommandExecutor` is the immediate module command boundary, not a generic mediator.
- Cross-module restart-safe state changes use durable commands or integration events.

### Git Intelligence

Recent commits:

- `946886c fix: epic 5 done` updated sprint status after Epic 5 completion.
- `871c479 fix: integration event tests` added Story 5.4 file and strengthened EF domain-event/outbox separation tests.
- `4bbd7f6 fix: added query capability` added query contracts, docs, public API baselines, and query tests.
- `ecd150a fix: durable messages registration` added module-owned durable registration and architecture/product-boundary evidence.
- `9dd9da7 docs: readme and agents doc refine` refined source-of-truth routing docs.

Recent work favors focused tests and narrow runtime changes. Follow that pattern here.

### Latest Technical Information

No dependency upgrade is required for this story. Use the repository-pinned stack:

- .NET SDK `10.0.108` with `rollForward: latestFeature`.
- `net10.0`.
- EF Core packages `10.0.8`.
- `Npgsql` `10.0.3`.
- `Npgsql.EntityFrameworkCore.PostgreSQL` `10.0.2`.
- `Testcontainers.PostgreSql` `4.12.0`.
- xUnit with existing `[Trait("Category", "...")]` usage.

Current official docs align with the planned implementation:

- EF Core transaction docs state that `SaveChanges` participates in transactions and uses savepoints when a transaction is already active. This supports testing owned and already-active transaction behavior through EF Core rather than an ad hoc transaction abstraction.
- PostgreSQL current docs describe `FOR UPDATE SKIP LOCKED` for skipping locked rows, which matches the existing Postgres outbox claim implementation.
- Testcontainers for .NET documents the PostgreSQL module as the right way to start a PostgreSQL container from .NET tests.
- Npgsql EF Core provider 10.0 release notes confirm the provider line is current with EF 10 capabilities. Keep using repository-pinned versions unless a separate dependency update story changes central package management.

Sources:

- https://learn.microsoft.com/en-us/ef/core/saving/transactions
- https://www.postgresql.org/docs/current/sql-select.html
- https://dotnet.testcontainers.org/modules/postgres/
- https://www.npgsql.org/efcore/release-notes/10.0.html

### Project Context Reference

Project context says Bondstone is a .NET library/framework for durable module boundaries, durable command sending, EF Core backed inbox/outbox persistence, operation observation, and transport adapters. Module-owned EF persistence must keep source state, inbox markers, operation state, domain-event records, outgoing outbox rows, and durable inbox rows in the owning module transaction boundary where applicable. Transport adapters are thin native-driver envelope adapters. Cleanup, retention, replay, purge, stale-row recovery, broker dead-letter movement, topology management, and automatic schema rollout remain application-owned unless a future PRD and architecture add them.

### Open Questions

None blocking. The main implementation choice is whether this remains tests-only. Prefer tests-only if handler-level PostgreSQL integration coverage confirms current runtime behavior already satisfies the acceptance criteria.

### Story Completion Status

Ultimate context engine analysis completed - comprehensive developer guide created.

### References

- `_bmad-output/planning-artifacts/epics.md` - Epic 6 and Story 6.1 acceptance criteria
- `_bmad-output/planning-artifacts/prds/prd-Bondstone-2026-06-18/prd.md` - FR3.2, FR6.1, FR8.1, FR10.3
- `_bmad-output/planning-artifacts/architecture.md` - Durable Commands, Integration Events, Durable Outbox, Persistence Architecture, Hosting And Workers, Verification Strategy
- `_bmad-output/planning-artifacts/research/technical-epic-6-durable-persistence-and-receive-ledger-research-2026-06-19.md` - current technical research for Epic 6
- `_bmad-output/project-context.md` - runtime, code, testing, and workflow guardrails
- `docs/testing.md` - test categories, integration-test rules, and verification commands
- `docs/repository.md` - runtime implementation review evidence and repository layout
- `src/Bondstone.Persistence.EntityFrameworkCore/Persistence/EntityFrameworkCoreModuleTransactionRunner.cs` - current module EF transaction runner
- `src/Bondstone.Persistence.EntityFrameworkCore/Outbox/EntityFrameworkCoreDurableOutboxWriter.cs` - current EF outbox staging
- `src/Bondstone/Messaging/Sending/DurableCommandSender.cs` - current durable command send path
- `src/Bondstone/Messaging/Publishing/DurableEventPublisher.cs` - current durable event publish path
- `tests/Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests/Persistence/PostgreSqlPersistenceTransactionTests.cs` - existing Postgres transaction proof patterns
- `tests/Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests/DomainEvents/PostgreSqlDomainEventTransactionTests.cs` - existing module command transaction callback proof pattern
- `tests/Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests/Persistence/PostgreSqlOutboxDispatcherTests.cs` - existing terminal dispatch state proof pattern

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Implementation Plan

- Keep runtime behavior unchanged unless handler-level PostgreSQL tests expose an atomicity gap.
- Add focused Postgres integration coverage for command commit, event commit, rollback, duplicate outbox identity, and terminal outbox inspection.
- Verify with targeted Postgres tests, full integration tests, fast tests, pack, and repository `pnpm check`.

### Debug Log References

- 2026-06-19: Inventory confirmed EF module transaction runner wraps handler execution plus `SaveChangesAsync`, EF outbox writer stages rows without saving or opening transactions, command sender/event publisher use current source module outbox writers, and terminal outbox inspection filters by source module.
- 2026-06-19: Red run for source command atomicity test failed at compile time on `DurableCommandSendResult.MessageId`; corrected to the public `SendId` contract and reran the targeted Postgres test green.
- 2026-06-19: Red run for source event atomicity test failed because the command execution helper accepted only `PlaceOrderCommand`; generalized it to `ICommand` and reran the targeted Postgres test green.
- 2026-06-19: Rollback test plus existing Postgres domain-event transaction callback guard passed together, proving failed handler state/outbox staging rolls back and observed transaction callback behavior remains intact.
- 2026-06-19: Duplicate outbox id test used a seeded PostgreSQL row and verified a primary-key violation rolls back the new source-state row while leaving no partial duplicate outbox row.
- 2026-06-19: Terminal outbox evidence test used a module dispatcher with a throwing envelope dispatcher and `DurableOutboxFailurePolicy(maxAttempts: 1)`; persisted terminal state and inspector lookup by source module both passed.
- 2026-06-19: Runtime packages remained unchanged; implementation stayed tests-only with no generic bus, saga/process manager, provider-neutral broker runtime, automatic domain-event dispatch, or automatic schema rollout.
- 2026-06-19: Verification passed: targeted Postgres source outbox tests, `pnpm backend:test:integration`, `pnpm backend:test`, `pnpm backend:pack`, and final `pnpm check`. Initial `pnpm check` was blocked by Prettier on the untracked Epic 6 research markdown; applied mechanical Prettier formatting and reran successfully.

### Completion Notes List

- Added handler-level PostgreSQL integration coverage for source state plus durable command outbox atomic commit through `IModuleCommandExecutor` and `IDurableCommandSender`.
- Added handler-level PostgreSQL integration coverage for source state plus integration-event outbox atomic commit through `IModuleCommandExecutor` and `IDurableEventPublisher`.
- Added handler-level PostgreSQL rollback coverage proving source state and staged durable command outbox rows do not diverge when the handler throws.
- Added PostgreSQL primary-key violation coverage proving duplicate outbox identity behavior rolls back staged source state and prevents partial duplicate outbox persistence.
- Added PostgreSQL terminal outbox failure evidence coverage through module dispatch and `IDurableOutboxInspector.FindTerminalFailedAsync`.
- Kept the story implementation tests-only because current runtime behavior already satisfies the acceptance criteria.
- Verified all Story 6.1 acceptance criteria with PostgreSQL-backed integration coverage and no runtime package changes.

### File List

- \_bmad-output/implementation-artifacts/6-1-source-outbox-atomicity.md
- \_bmad-output/implementation-artifacts/sprint-status.yaml
- \_bmad-output/planning-artifacts/research/technical-epic-6-durable-persistence-and-receive-ledger-research-2026-06-19.md
- tests/Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests/Persistence/PostgreSqlSourceOutboxAtomicityTests.cs

### Change Log

- 2026-06-19: Added Story 6.1 PostgreSQL source outbox atomicity coverage and moved story to review.
