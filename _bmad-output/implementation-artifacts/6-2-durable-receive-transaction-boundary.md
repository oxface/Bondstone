---
baseline_commit: 23bda7f
---

# Story 6.2: Durable Receive Transaction Boundary

Status: review

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As an operator,
I want one durable receive ledger,
so that ingestion, retry, terminal failure, stale state, and processed state are visible in one place.

## Acceptance Criteria

1. Given a native broker delivery arrives, when the envelope is valid, then it is durably ingested before native settlement.
2. Given ingestion fails, when the adapter handles the delivery, then native broker settlement does not acknowledge or complete the delivery as processed.
3. Given receive processing succeeds, when the module transaction commits, then receive markers, module state, outgoing rows, operation state, domain-event records, and durable inbox state commit in the owning boundary where applicable.
4. Given receive processing fails, when retry policy allows more attempts, then the durable inbox row records retry state.
5. Given retry is exhausted or failure is terminal, when inspected, then the durable inbox row exposes terminal evidence.
6. Given the transitional direct `inbox_messages` marker remains, when runtime work proceeds, then it is hidden as an implementation detail and not the operator-facing ledger.

## Tasks / Subtasks

- [x] Inventory the receive ingestion and processing paths before changing code. (AC: 1, 2, 3, 4, 5, 6)
  - [x] Confirm `RabbitMqReceiveWorker` durable incoming inbox mode deserializes the envelope, resolves command/event receiver identity, calls `IDurableIncomingInboxIngestionBoundaryResolver`, saves the ingestion transaction, and only then calls `BasicAckAsync`.
  - [x] Confirm `ServiceBusReceiveWorker` currently uses direct `IDurableEnvelopeReceiver.ReceiveAsync` and `CompleteMessageAsync`, then decide whether this story must add durable incoming inbox ingestion parity for Service Bus.
  - [x] Confirm `DurableIncomingInboxDispatcher` claims `incoming_inbox_messages`, invokes command/event receive pipelines, and records processed, retry, terminal failure, or stale outcomes through `IDurableIncomingInboxOutcomeRecorder`.
  - [x] Confirm `DurableInboxHandlerExecutor` and `PostgreSqlDurableInboxRegistrar` still use `inbox_messages` only as the direct handler idempotency marker inside module processing.
  - [x] Confirm `EntityFrameworkCoreModuleTransactionRunner` keeps handler state, direct inbox marker, outgoing outbox rows, operation state, and domain-event persistence in the module EF transaction where applicable.
- [x] Make native broker ingestion settle only after durable incoming inbox ingestion succeeds. (AC: 1, 2)
  - [x] Preserve RabbitMQ behavior: `BasicConsumeAsync(autoAck: false)`, durable ingestion before `BasicAckAsync`, and `BasicNackAsync` on ingestion failure using `RequeueOnFailure`.
  - [x] Add Service Bus durable incoming inbox ingestion mode or replace the current direct receive worker path if that is the narrowest compatible change.
  - [x] Keep `ServiceBusReceiveWorkerOptions.ProcessorOptions.AutoCompleteMessages = false`; continue rejecting options that enable auto-complete.
  - [x] For Service Bus ingestion failure, do not call `CompleteMessageAsync`; allow the exception path/processor settlement behavior to leave the message unsettled or abandoned according to Azure SDK semantics.
  - [x] For duplicate durable ingestion (`AlreadyIngested`), acknowledge/complete the native delivery without executing the handler, because the durable ledger already owns further processing.
- [x] Prove receive processing commits all module-owned effects atomically. (AC: 3, 6)
  - [x] Add or extend PostgreSQL integration coverage where a durable incoming inbox command row is processed by `IDurableIncomingInboxDispatcher`.
  - [x] In the handler, persist module state and send/publish durable outgoing work; include a durable operation id when proving operation completion state.
  - [x] Verify after commit that `incoming_inbox_messages` is `Processed`, `inbox_messages` has the processed handler marker, module state exists, outbox rows exist, operation state is completed where applicable, and domain-event records are persisted when the handler uses EF domain-event persistence.
  - [x] Use fresh `DbContext` instances for assertions so the test proves persisted PostgreSQL state, not tracked entities.
  - [x] Keep `incoming_inbox_messages` as the operator-facing ledger in assertions and docs; only mention `inbox_messages` as the implementation detail that prevents direct handler re-execution.
- [x] Prove rollback and retry behavior with PostgreSQL-backed receive processing. (AC: 3, 4)
  - [x] Add a handler failure test where retry remains available; assert `incoming_inbox_messages.Status == RetryScheduled`, `AttemptCount` increments, `NextAttemptAtUtc`, `FailedAtUtc`, and `FailureReason` are populated, and no module state, direct inbox marker, operation completion, or outgoing outbox row remains from the failed handler transaction.
  - [x] Reprocess after retry becomes due and verify the claimer moves the row back to `Processing`, clears previous failure evidence while claimed, then records the final outcome.
  - [x] Preserve the existing policy that handler failures do not make broker DLQ state the Bondstone receive ledger.
- [x] Prove terminal failure and stale-state inspection. (AC: 5)
  - [x] Add or strengthen PostgreSQL coverage for terminal receive failure with `DurableIncomingInboxProcessingOptions(maxAttempts: 1)`.
  - [x] Verify terminal rows expose `TerminalFailed`, `FailedAtUtc`, `FailureReason`, attempt count, receiver module, handler identity, message kind, message type, source transport name, and envelope metadata needed for operator inspection.
  - [x] Use `IDurableIncomingInboxInspectionStore` or the module inspection path where available to find terminal and stale processing rows rather than querying only raw EF sets.
  - [x] Preserve stale behavior: stale rows are operational evidence/reclaim candidates, not automatic destructive cleanup or replay.
- [x] Cover duplicate ingestion and receiver identity boundaries. (AC: 1, 6)
  - [x] Prove duplicate native delivery maps to the same `DurableIncomingInboxKey` and returns `AlreadyIngested` without creating competing rows or running the handler.
  - [x] Cover command receiver identity from target module plus stable handler identity.
  - [x] Cover event receiver identity from subscriber module plus stable subscriber identity.
  - [x] Keep durable identities explicit; do not derive receive keys from CLR type names or handler class names.
- [x] Keep implementation scope narrow. (AC: 1, 2, 3, 4, 5, 6)
  - [x] Prefer focused tests if current runtime behavior already satisfies an acceptance criterion.
  - [x] If runtime changes are needed, modify the existing incoming inbox, receive worker, or EF/PostgreSQL components instead of adding a second receive ledger.
  - [x] Do not add broker topology management, broker retry policy, broker DLQ movement, cleanup/retention/replay workers, saga/process-manager behavior, automatic schema rollout, or automatic domain-event publication.
- [x] Verify the story outcome.
  - [x] Run targeted tests, for example `dotnet test tests/Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests/Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests.csproj --configuration Release --filter "FullyQualifiedName~IncomingInbox"`.
  - [x] Run targeted transport tests, for example `dotnet test tests/Bondstone.Transport.RabbitMq.Tests/Bondstone.Transport.RabbitMq.Tests.csproj --configuration Release --filter "FullyQualifiedName~ReceiveWorker"` and the equivalent Service Bus receive-worker filter when Service Bus changes.
  - [x] Run `pnpm backend:test:integration` when PostgreSQL or provider-backed transport coverage changes.
  - [x] Run `pnpm backend:test` after runtime code changes.
  - [x] Run `pnpm backend:pack` and review public API baselines only if public/protected package surface changes.
  - [x] Run `pnpm check` as the final broad gate when code, public API, package metadata, samples, or broad docs change.

## Dev Notes

Story 6.2 should make the receive side as provable as Story 6.1 made the source outbox side. The likely successful shape is a mix of tests and a narrow Service Bus receive-worker parity change: RabbitMQ already has durable incoming inbox ingestion before ack; Service Bus currently completes after direct receive processing and does not yet show the same incoming-ledger handoff in its worker.

### Current State Intelligence

Existing durable receive pieces already cover most of the ledger:

- `IncomingInboxMessageEntity` maps `incoming_inbox_messages` with primary key `(ReceiverModule, MessageId, HandlerIdentity)`. It stores envelope fields, receiver module, handler identity, source transport name, ingestion timestamp, status, attempt count, retry timestamp, processed timestamp, failed timestamp, failure reason, claim owner, and claim lease.
- `EntityFrameworkCoreDurableIncomingInboxIngestionStore<TDbContext>` stages a pending incoming inbox row and returns `AlreadyIngested` for duplicates. It does not execute handlers.
- `EntityFrameworkCoreDurableIncomingInboxIngestionPersistenceScope` wraps ingestion plus `SaveChangesAsync` in the module EF persistence scope.
- `DurableIncomingInboxDispatcher` claims incoming rows, invokes `IModuleCommandReceivePipeline` or `IModuleEventReceivePipeline`, and records processed, retry scheduled, terminal failed, or stale outcomes.
- `PostgreSqlDurableIncomingInboxClaimer` uses `FOR UPDATE SKIP LOCKED` over pending, due retry, and stale processing rows. It increments `AttemptCount`, clears previous failure fields while claimed, and sets `Processing`, `ClaimedBy`, and `ClaimedUntilUtc`.
- `PostgreSqlDurableIncomingInboxOutcomeRecorder` uses compare-and-update semantics for `Processing` rows still owned by the current worker and still inside the lease. It records `Processed`, `RetryScheduled`, or `TerminalFailed` and clears claim fields.
- `EntityFrameworkCoreDurableIncomingInboxInspectionStore` can find rows by status, stale processing lease, terminal failure, receiver module, source transport, and cutoff timestamps.

The direct receive marker remains separate:

- `DurableInboxHandlerExecutor` registers a `DurableInboxRecord` before handler execution and marks it processed after the handler succeeds.
- `PostgreSqlDurableInboxRegistrar` inserts into `inbox_messages` with `ON CONFLICT DO NOTHING` and returns `AlreadyReceived` or `AlreadyProcessed`.
- `ModuleCommandRuntime` and `ModuleEventSubscriberRuntime` call the direct inbox executor only when a receive context exists. This marker prevents duplicate handler execution inside module processing but is not the operator-facing receive ledger for ingestion/retry/terminal/stale state.

Transport adapter state:

- `RabbitMqReceiveWorker` already supports `RabbitMqReceiveWorkerMode.DurableIncomingInboxIngestion`. In that mode it deserializes the body, resolves command/event receiver keys, calls the incoming inbox ingestion boundary, and only then calls `BasicAckAsync`. Failure logs and calls `BasicNackAsync`.
- `RabbitMqReceiveWorkerOptions.ReceiveCommand()` and `ReceiveEvent(...)` currently select durable incoming inbox ingestion mode; old direct receive mode still exists internally for tests/backward compatibility.
- `ServiceBusReceiveWorker` currently creates processors with manual completion options, calls `IDurableEnvelopeReceiver.ReceiveAsync(...)`, then calls `CompleteMessageAsync(...)`. This proves completion-after-receive, but not durable incoming inbox ingestion-before-complete.
- `ServiceBusReceiveWorkerOptions` defaults `AutoCompleteMessages = false` and throws if a caller enables auto-complete. Preserve this rule.

Existing coverage:

- `PostgreSqlIncomingInboxProcessingTests` already proves pending command rows process successfully, duplicate ingestion does not rerun handlers, module dispatchers claim only their receiver module rows, retry is scheduled, and terminal failure is recorded.
- `PostgreSqlIncomingInboxMutationTests` covers claim ordering, due retry claims, stale processing reclaim, active lease exclusion, lease renewal, processed outcome, retry outcome, and terminal outcome.
- `EntityFrameworkCoreDurableIncomingInboxIngestionStoreTests` covers staging, duplicate detection, non-pending rejection, and missing mapping errors.
- `EntityFrameworkCoreDurableIncomingInboxInspectionStoreTests` covers filtered pending/stale/terminal inspection.
- `RabbitMqReceiveWorkerTests` already proves durable ingestion commits before ack, duplicate ingestion acks without handler execution, module boundary resolution, and ingestion failure nacks.
- `ServiceBusReceiveWorkerTests` currently proves only direct receive completion ordering and error logging.

### Files To Read Or Update

Read these UPDATE files completely before changing behavior:

- `src/Bondstone/Messaging/Receiving/DurableEnvelopeReceiver.cs` - current direct receive path over command/event pipelines.
- `src/Bondstone/Messaging/Contracts/IDurableEnvelopeReceiver.cs` - public direct receive contract; avoid public API churn unless required.
- `src/Bondstone/Modules/Execution/ModuleCommandReceivePipeline.cs` and `ModuleEventReceivePipeline.cs` - envelope validation, stable identity resolution, receive context creation, and direct inbox result handling.
- `src/Bondstone/Modules/Execution/ModuleCommandRuntime.cs` and `ModuleEventSubscriberRuntime.cs` - transaction runner ordering, direct inbox marker execution, operation completion, current module context, and post-handler actions.
- `src/Bondstone.Persistence/Persistence/Inbox/DurableInboxHandlerExecutor.cs` - direct idempotency marker semantics.
- `src/Bondstone.Persistence.EntityFrameworkCore.Postgres/Inbox/PostgreSqlDurableInboxRegistrar.cs` - `inbox_messages` duplicate behavior.
- `src/Bondstone.Persistence.EntityFrameworkCore/IncomingInbox/EntityFrameworkCoreDurableIncomingInboxIngestionStore.cs` and `EntityFrameworkCoreDurableIncomingInboxIngestionPersistenceScope.cs` - durable ingestion staging and save boundary.
- `src/Bondstone.Persistence.EntityFrameworkCore/IncomingInbox/IncomingInboxMessageEntity.cs` and `IncomingInboxMessageEntityConfiguration.cs` - operator-facing ledger schema.
- `src/Bondstone.Persistence/Persistence/IncomingInbox/*.cs` and `src/Bondstone/Persistence/IncomingInbox/*.cs` - provider-neutral incoming inbox records, status, failure policy, dispatcher, diagnostics, and processing options.
- `src/Bondstone.Persistence.EntityFrameworkCore.Postgres/IncomingInbox/PostgreSqlDurableIncomingInboxClaimer.cs`, `PostgreSqlDurableIncomingInboxOutcomeRecorder.cs`, `PostgreSqlDurableIncomingInboxLeaseRenewer.cs`, and `PostgreSqlModuleDurableIncomingInboxDispatcher.cs` - PostgreSQL claim, retry, terminal, stale, and module-scoped dispatch behavior.
- `src/Bondstone.Transport.RabbitMq/RabbitMqReceiveWorker.cs` and `RabbitMqReceiveWorkerOptions.cs` - existing durable ingestion-before-ack pattern to preserve and mirror.
- `src/Bondstone.Transport.ServiceBus/ServiceBusReceiveWorker.cs` and `ServiceBusReceiveWorkerOptions.cs` - likely update path for durable ingestion-before-complete parity.

Likely test files:

- Extend `tests/Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests/Persistence/PostgreSqlIncomingInboxProcessingTests.cs` for atomic receive commit/rollback proof, retry with no partial handler effects, terminal inspection, and event subscriber processing if missing.
- Extend `tests/Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests/Persistence/PostgreSqlIncomingInboxMutationTests.cs` only for low-level claim/outcome gaps.
- Extend `tests/Bondstone.Transport.RabbitMq.Tests/RabbitMqReceiveWorkerTests.cs` only if RabbitMQ ingestion behavior changes or needs an additional regression.
- Extend `tests/Bondstone.Transport.ServiceBus.Tests/ServiceBusReceiveWorkerTests.cs` for durable incoming inbox ingestion-before-complete, ingestion failure not completing, duplicate ingestion completing without handler execution, and receiver module boundary resolution.
- Reuse `tests/Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests/PostgreSqlFixture.cs`, `PostgreSqlIncomingInboxTestDbContext.cs`, and existing fixed-time/test-entity patterns.

### Architecture Compliance

Follow these non-negotiable rules:

- Bondstone remains a durable module-boundary library/framework, not a generic bus, workflow engine, saga/process-manager framework, broker topology manager, code generator, SaaS framework, application platform, or broker runtime owner.
- Native broker delivery must not be acknowledged or completed before durable incoming inbox ingestion succeeds.
- `incoming_inbox_messages` is the v2 durable receive ledger for ingestion, claim, retry, processed, stale, and terminal failure state.
- `inbox_messages` is a transitional direct idempotency marker during module processing. Do not present it as the operator-facing receive ledger.
- Receive processing success must keep module state, direct receive markers, outgoing outbox rows, operation state, domain-event records, and incoming durable inbox state in the owning module transaction boundary where applicable.
- Terminal durable inbox failure is operational evidence; it does not automatically write caller-visible operation `Failed` state.
- Broker topology, provisioning, credentials, prefetch/concurrency, native retry, dead-letter policy, and broker monitoring remain host-owned.
- Cleanup, retention, replay, purge, stale-row mutation, and broker dead-letter movement remain application-owned unless a future BMAD PRD and architecture add them.
- Commands, integration events, and domain events stay distinct. Do not add automatic domain-event dispatch or automatic domain-to-integration publication.
- EF Core plus PostgreSQL is the supported production durable persistence path. EF InMemory is not proof for transaction, uniqueness, locking, claiming, retry, stale, or terminal behavior.
- Consumers own EF migrations. Do not add package-shipped migrations or automatic schema rollout.
- Production package collaboration must use explicit contracts or package-local implementation. Do not add runtime `InternalsVisibleTo`.

### Testing Requirements

Use the repository testing policy from `docs/testing.md`:

- Mark PostgreSQL-backed ingestion, receive transaction, retry, terminal, stale, duplicate, and provider-backed transport tests with `[Trait("Category", "Integration")]` when they require real infrastructure.
- Mark pure receive-worker unit tests with `[Trait("Category", "Unit")]` when they use recording doubles and no external broker.
- Use EF InMemory only for mapping or change-tracker behavior. It must not be used as proof for this story's durable receive semantics.
- Prefer persisted outcome assertions over interaction-only assertions: incoming inbox status, attempt count, retry timestamp, terminal failure data, direct inbox marker, module state, outbox rows, operation state, domain-event records, native settlement counts, and absence of partial rows after failure.
- Keep tests grouped by package boundary: EF/PostgreSQL durable behavior in `tests/Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests`, RabbitMQ behavior in `tests/Bondstone.Transport.RabbitMq.Tests`, Service Bus behavior in `tests/Bondstone.Transport.ServiceBus.Tests`.

High-value test cases:

- `ServiceBusReceiveWorker_WhenDurableIncomingInboxIngestionSucceeds_CompletesOnlyAfterSave`.
- `ServiceBusReceiveWorker_WhenDurableIncomingInboxIngestionFails_DoesNotCompleteMessage`.
- `ServiceBusReceiveWorker_WhenDurableIncomingInboxAlreadyIngested_CompletesWithoutHandlerExecution`.
- `IncomingInboxDispatcher_WhenCommandHandlerCommits_CommitsIncomingLedgerDirectInboxStateOutboxAndOperationState`.
- `IncomingInboxDispatcher_WhenEventSubscriberCommits_CommitsIncomingLedgerDirectInboxStateAndOutboxRows`.
- `IncomingInboxDispatcher_WhenHandlerFailsBeforeMaxAttempts_RollsBackHandlerEffectsAndRecordsRetry`.
- `IncomingInboxDispatcher_WhenHandlerFailsAtMaxAttempts_RecordsTerminalEvidenceForInspection`.
- `IncomingInboxDispatcher_WhenProcessingClaimIsStale_CountsStaleAndLeavesEvidenceInspectable`.

### Previous Story Intelligence

Story 6.1 completed as a tests-only source outbox atomicity proof. Carry these learnings forward:

- Prefer PostgreSQL integration coverage for transaction, uniqueness, retry, terminal, and persisted-state proof.
- Keep runtime changes out unless tests expose a real gap.
- Use fresh `DbContext` verification and assert durable payload/state details, not only row counts.
- Duplicate durable identity tests should assert the specific PostgreSQL duplicate/primary-key behavior where relevant, not accept any exception.
- Terminal evidence should be exposed through the intended inspector path, not only raw row queries.
- Runtime scope stayed narrow: no generic bus, saga/process manager, provider-neutral broker runtime, automatic domain-event dispatch, or automatic schema rollout.
- Story 6.1 verification passed targeted Postgres tests, `pnpm backend:test:integration`, `pnpm backend:test`, `pnpm backend:pack`, and `pnpm check`; use the same staged verification style here.

### Git Intelligence

Recent commits:

- `23bda7f fix: atomicity test` completed Story 6.1 source outbox atomicity proof.
- `946886c fix: epic 5 done` updated sprint status after Epic 5 completion.
- `871c479 fix: integration event tests` strengthened durable message kind and domain-event boundary tests.
- `4bbd7f6 fix: added query capability` added query contracts, docs, public API baselines, and query tests.
- `ecd150a fix: durable messages registration` added module-owned durable registration and architecture/product-boundary evidence.

Recent work favors focused tests, explicit durable identities, and compatibility-sensitive public API changes. Follow that pattern.

### Latest Technical Information

No dependency upgrade is required for this story. Use the repository-pinned stack in `Directory.Packages.props`:

- .NET SDK `10.0.108` with `rollForward: latestFeature`; target framework `net10.0`.
- EF Core packages `10.0.8`.
- `Npgsql` `10.0.3`.
- `Npgsql.EntityFrameworkCore.PostgreSQL` `10.0.2`.
- `RabbitMQ.Client` `7.2.1`.
- `Azure.Messaging.ServiceBus` `7.20.1`.
- `Testcontainers.PostgreSql`, `Testcontainers.RabbitMq`, and `Testcontainers.ServiceBus` `4.12.0`.
- xUnit with existing `[Trait("Category", "...")]` usage.

Current official docs align with the implementation:

- EF Core transaction docs state that `SaveChanges` is transactional and uses savepoints when a transaction is already active; keep receive handler effects inside the module EF transaction rather than saving in separate ad hoc boundaries.
- PostgreSQL docs describe `FOR UPDATE SKIP LOCKED`, which matches the existing queue-like incoming inbox claimer pattern.
- RabbitMQ docs define manual positive and negative acknowledgements (`basic.ack`, `basic.nack`); keep ack/nack after durable ingestion success/failure respectively.
- Azure Service Bus docs describe locks and settlement, and `ServiceBusProcessorOptions.AutoCompleteMessages` controls automatic completion; keep auto-complete disabled so Bondstone controls completion after durable ingestion.
- Testcontainers for .NET documents PostgreSQL containers as a normal .NET test dependency; use existing Testcontainers-backed integration fixtures for relational proof.

Sources:

- https://learn.microsoft.com/en-us/ef/core/saving/transactions
- https://www.postgresql.org/docs/current/sql-select.html
- https://www.rabbitmq.com/docs/confirms
- https://learn.microsoft.com/en-us/azure/service-bus-messaging/message-transfers-locks-settlement
- https://learn.microsoft.com/en-us/dotnet/api/azure.messaging.servicebus.servicebusprocessoroptions.autocompletemessages
- https://dotnet.testcontainers.org/modules/postgres/
- https://www.npgsql.org/efcore/release-notes/10.0.html

### Project Context Reference

Project context says Bondstone is a .NET library/framework for durable module boundaries, durable command sending, EF Core backed inbox/outbox persistence, operation observation, and transport adapters. Module-owned EF persistence must keep source state, inbox markers, operation state, domain-event records, outgoing outbox rows, and durable inbox rows in the owning module transaction boundary where applicable. Native broker delivery must not be acknowledged or completed before durable inbox ingestion succeeds. Transport adapters are thin native-driver envelope adapters, while cleanup, retention, replay, purge, stale-row recovery, broker dead-letter movement, topology management, and automatic schema rollout remain application-owned unless a future PRD and architecture add them.

### Open Questions

None blocking. The main implementation decision is whether Service Bus receives a new durable incoming inbox ingestion mode beside direct receive, or whether the existing receive worker moves fully to durable incoming inbox ingestion semantics. Prefer the smallest compatibility-sensitive change that satisfies ingestion-before-complete and keeps public API churn low.

### Story Completion Status

Ultimate context engine analysis completed - comprehensive developer guide created.

### References

- `_bmad-output/planning-artifacts/epics.md` - Epic 6 and Story 6.2 acceptance criteria
- `_bmad-output/planning-artifacts/prds/prd-Bondstone-2026-06-18/prd.md` - FR3.2, FR6.2, FR6.5, FR7.4, FR8.1, FR10.3
- `_bmad-output/planning-artifacts/architecture.md` - Durable Inbox, Receive Pipeline, Transport Boundary, Hosting And Workers, Persistence Architecture, Operation Observation, Verification Strategy
- `_bmad-output/planning-artifacts/research/technical-epic-6-durable-persistence-and-receive-ledger-research-2026-06-19.md` - current technical research for Epic 6
- `_bmad-output/project-context.md` - runtime, code, testing, and workflow guardrails
- `docs/testing.md` - test categories, integration-test rules, and verification commands
- `docs/repository.md` - runtime implementation review evidence and repository layout
- `src/Bondstone.Transport.RabbitMq/RabbitMqReceiveWorker.cs` - current durable incoming inbox ingestion-before-ack pattern
- `src/Bondstone.Transport.ServiceBus/ServiceBusReceiveWorker.cs` - current direct receive then complete path
- `src/Bondstone.Persistence.EntityFrameworkCore/IncomingInbox/IncomingInboxMessageEntity.cs` - durable receive ledger row shape
- `src/Bondstone.Persistence.EntityFrameworkCore.Postgres/IncomingInbox/PostgreSqlDurableIncomingInboxClaimer.cs` - PostgreSQL claim/retry/stale behavior
- `src/Bondstone.Persistence.EntityFrameworkCore.Postgres/IncomingInbox/PostgreSqlDurableIncomingInboxOutcomeRecorder.cs` - PostgreSQL processed/retry/terminal outcome behavior
- `tests/Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests/Persistence/PostgreSqlIncomingInboxProcessingTests.cs` - existing receive processing integration proof
- `tests/Bondstone.Transport.RabbitMq.Tests/RabbitMqReceiveWorkerTests.cs` - existing RabbitMQ settlement timing proof
- `tests/Bondstone.Transport.ServiceBus.Tests/ServiceBusReceiveWorkerTests.cs` - current Service Bus direct receive completion proof

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- 2026-06-19: Resolved workflow customization manually after `python3 _bmad/scripts/resolve_customization.py --skill .agents/skills/bmad-dev-story --key workflow` failed because the environment could not import Python's `json` module.
- 2026-06-19: Inventory confirmed RabbitMQ already ingests to `incoming_inbox_messages` before ack; Service Bus used direct `IDurableEnvelopeReceiver` before completion and required parity work.
- 2026-06-19: Red Service Bus receive-worker test failed before implementation because the worker and registration lacked durable incoming inbox ingestion support.

### Completion Notes List

- Replaced Service Bus receive-worker direct handler execution with durable incoming inbox ingestion before `CompleteMessageAsync`.
- Added Service Bus receive-worker coverage for ingestion-before-complete ordering, ingestion failure without completion, duplicate ingestion completion without handler execution, and event subscriber receiver identity.
- Updated Service Bus broker-backed integration receive tests to assert durable incoming inbox ingestion records instead of direct receiver callbacks.
- Strengthened PostgreSQL incoming inbox processing coverage for committed module state, direct inbox marker, outbox row, operation state, EF domain-event record, rollback of staged effects on retry, terminal evidence, and stale processing inspection.
- Verified RabbitMQ receive-worker settlement behavior remained intact.
- Verification passed: targeted PostgreSQL incoming inbox processing tests, targeted RabbitMQ receive-worker tests, full Service Bus tests, `pnpm backend:test:integration`, `pnpm backend:test`, and `pnpm check`.

### File List

- `_bmad-output/implementation-artifacts/6-2-durable-receive-transaction-boundary.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `src/Bondstone.Transport.ServiceBus/ServiceBusReceiveWorker.cs`
- `src/Bondstone.Transport.ServiceBus/ServiceBusReceiveWorkerOptions.cs`
- `src/Bondstone.Transport.ServiceBus/ServiceBusReceiveWorkerRegistration.cs`
- `tests/Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests/Persistence/PostgreSqlIncomingInboxProcessingTests.cs`
- `tests/Bondstone.Transport.ServiceBus.Tests/ServiceBusIntegrationTests.cs`
- `tests/Bondstone.Transport.ServiceBus.Tests/ServiceBusReceiveWorkerTests.cs`

### Change Log

- 2026-06-19: Implemented Service Bus durable incoming inbox ingestion before native completion and expanded receive-ledger PostgreSQL/transport proof coverage.
