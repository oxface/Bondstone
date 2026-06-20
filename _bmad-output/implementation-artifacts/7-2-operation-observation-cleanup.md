---
baseline_commit: e85574a
---

# Story 7.2: Operation Observation Cleanup

Status: review

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As an operator,
I want operation APIs framed as observation,
so that waits/results are not confused with orchestration.

## Acceptance Criteria

1. Given durable work is accepted, when a caller receives metadata, then operation status and result are observed through operation APIs.
2. Given wait helpers are used, when a caller times out, then timeout does not write operation state or create orchestration semantics.
3. Given operation APIs are documented, when reviewed, then they do not imply saga, workflow, process-manager, or durable continuation state.
4. Given terminal outbox, receive, operation, or worker failures occur, when operators inspect state, then evidence is discoverable through documented surfaces.
5. Given accepted durable work completes, fails, or remains pending, when operation APIs are exercised in tests, then status reads, typed result reads, and timeout behavior are proven at the API boundary rather than only described in docs.
6. Given inventory finds that terminal outbox, receive, operation, or worker evidence is only available through ad hoc persistence queries, when the story closes, then it either adds an explicit non-orchestrating observation surface or records why the existing public API is sufficient.

## Tasks / Subtasks

- [x] Inventory current operation-observation behavior before changing code. (AC: 1, 2, 3, 4, 5, 6)
  - [x] Read every file listed in "Files To Read Or Update" before editing; summarize current state, intended change, and preserved behavior in the dev record.
  - [x] Confirm whether the existing operation APIs, docs, and tests already satisfy each AC. Do not rewrite working operation APIs for appearance.
  - [x] Treat operation observation as a read model over accepted durable work. Do not add orchestration, saga/process-manager state, durable continuations, automatic failure inference, or hidden workers.
- [x] Preserve accepted-work metadata and target-module result observation. (AC: 1, 5)
  - [x] Keep `DurableCommandSendResult.Operation` and `DurableOperationHandle` as metadata for source module, target module, and durable operation id.
  - [x] Preserve module-hinted and handle-based reads so callers can query the target module's operation-state store instead of relying only on aggregate operation-id scans.
  - [x] Ensure `GetResultAsync<TResult>()` continues to report unknown, pending, running, completed-with-result, completed-without-result, failed, cancelled, and result-deserialization-failed states without mutating operation state.
- [x] Prove timeout behavior remains caller patience only. (AC: 2, 5)
  - [x] Preserve `TryWaitForResultAsync<TResult>()` returning `CompletedWithinTimeout = false` with the latest observed result when no terminal state is reached.
  - [x] Preserve `WaitForResultAsync<TResult>()` throwing `TimeoutException` without writing `Failed`, `Cancelled`, or any other operation state.
  - [x] Add or tighten tests only if current tests fail to prove no store write/finalization occurs on timeout.
- [x] Tighten docs and public API wording where inventory finds ambiguity. (AC: 1, 2, 3, 4)
  - [x] Keep XML docs and consumer docs explicit that waits/results observe operation state and do not inspect broker retry, dead-letter state, source outbox retry state, or receive ambiguity unless application policy writes an explicit terminal outcome.
  - [x] Remove or revise wording that suggests request/response, workflow progression, saga/process-manager behavior, automatic continuations, or automatic terminal operation failure.
  - [x] Preserve the application-owned policy boundary for `IDurableOperationFinalizer` and `IDurableOperationExpirationProcessor`.
- [x] Make operational evidence discoverable without adding mutation semantics. (AC: 4, 6)
  - [x] Verify `IDurableOutboxInspector` documents and tests terminal source outbox failure discovery.
  - [x] Verify `IDurableInboxInspector` documents and tests unprocessed direct receive inbox discovery.
  - [x] Inventory durable incoming inbox evidence: `IDurableIncomingInboxInspectionStore` already supports status reads, stale processing claims, and terminal receive failures at the provider/runtime contract layer.
  - [x] If terminal durable incoming inbox failure remains only provider/runtime-facing or docs only, either add an explicit app-facing read-only inspector for durable incoming inbox evidence or record in the story completion notes why the existing public/provider surface is sufficient for this release.
  - [x] Preserve read-only semantics. Do not add reset, replay, purge, archival, broker dead-letter movement, stale-row mutation, or operation-failure inference.
- [x] Verify worker-failure evidence remains documented. (AC: 4, 6)
  - [x] Keep hosted outbox worker batch failures documented through log event id `1001` / `DispatchBatchFailed`.
  - [x] Keep hosted incoming inbox worker batch failures documented through log event id `2001` / `ProcessBatchFailed` and consecutive failure count.
  - [x] If tests are added, assert log/event behavior or option validation without depending on wall-clock sleeps where possible.
- [x] Validate and verify. (AC: 1, 2, 3, 4, 5, 6)
  - [x] Run targeted fast tests first, for example `dotnet test tests/Bondstone.Tests/Bondstone.Tests.csproj --configuration Release --filter "FullyQualifiedName~DurableOperationResultReaderTests|FullyQualifiedName~DurableOperationReaderTests|FullyQualifiedName~DurableOutboxInspectorTests|FullyQualifiedName~DurableInboxInspectorTests"`.
  - [x] Run EF Core inspection tests if EF inspection stores change: `dotnet test tests/Bondstone.Persistence.EntityFrameworkCore.Tests/Bondstone.Persistence.EntityFrameworkCore.Tests.csproj --configuration Release --filter "FullyQualifiedName~OperationStateStoreTests|FullyQualifiedName~IncomingInboxInspectionStoreTests|FullyQualifiedName~InboxInspectionStoreTests|FullyQualifiedName~OutboxInspectionStoreTests"`.
  - [x] Run `pnpm backend:test` after runtime, test, or public docs changes.
  - [x] Run `pnpm backend:pack` and review public API baseline diffs if public/protected APIs change.
  - [x] Run `pnpm check` as the final broad gate when code, public API, package metadata, samples, or broad docs change.

## Dev Notes

Story 7.2 is a boundary cleanup story, but it is not documentation-only by default. Epic 7 says each story starts with implementation inventory and may close as documentation-only only when the story records evidence that current code and tests already cover all criteria. If any operation evidence is available only through ad hoc persistence queries, add a read-only observation surface or record why the current public/provider contract is sufficient.

### Current State Intelligence

Operation result observation:

- `DurableCommandSendResult.Operation` carries a `DurableOperationHandle` with durable operation id, source module, and target module when the send includes an operation id.
- `IDurableOperationReader` reads module-owned operation state. Its operation-id-only read aggregates configured module stores and ranks terminal states above pending/running, then newest by `UpdatedAtUtc`; its module-hinted and handle overloads query one target module store.
- `IDurableOperationResultReader` reads typed operation result state. It does not run handlers, dispatch outbox rows, inspect broker state, or mutate operation state.
- `DurableOperationResult<TResult>` classifies unknown, pending, running, completed with result, completed without result, failed, cancelled, and result deserialization failure.
- Result deserialization failure does not change stored operation status; it reports caller-side inability to read a completed payload as the requested result type.

Wait behavior:

- `TryWaitForResultAsync<TResult>()` polls via the operation reader until a terminal state is observed or timeout expires. On timeout it returns `CompletedWithinTimeout = false` and the latest observed result.
- `WaitForResultAsync<TResult>()` wraps the non-throwing wait and throws `TimeoutException` on timeout.
- Timeout is caller patience. It must not write `Failed`, `Cancelled`, `Pending`, `Running`, or any other operation state. If application policy wants a terminal outcome, it must call `IDurableOperationFinalizer` or `IDurableOperationExpirationProcessor` explicitly.
- The current implementation uses `TimeProvider` and `Task.Delay(..., timeProvider, ct)` for polling delay. Cancellation should remain cancellation of the caller wait, not durable operation finalization.

Operation finalization and expiry:

- `IDurableOperationFinalizer` marks explicit application-owned `Failed` or `Cancelled` outcomes in one named module's operation-state store.
- `IDurableOperationExpirationProcessor` scans one module's operation-state store for stale pending/running candidates and finalizes them through the explicit finalizer. Bondstone does not register a hosted expiration worker.
- `IDurableOperationExpirationStore` is a provider/runtime query contract for stale pending/running operation states. It must not become a default cleanup or orchestration worker.

Operational evidence surfaces:

- `IDurableOutboxInspector` is the app-facing read-only inspector for terminal source outbox failures by module.
- `IDurableInboxInspector` is the app-facing read-only inspector for unprocessed direct receive inbox rows by module.
- `IDurableIncomingInboxInspectionStore` is a provider/runtime inspection contract for durable incoming inbox status reads, stale processing claims, and terminal receive failures. The EF Core implementation and tests already cover filtered `FindAsync`, `FindStaleProcessingAsync`, and `FindTerminalFailedAsync`.
- Durable incoming inbox terminal failure is operational evidence. It must not automatically write caller-visible operation `Failed` state.
- Hosted worker failures are currently surfaced through logs: outbox worker event id `1001` / `DispatchBatchFailed`; incoming inbox worker event id `2001` / `ProcessBatchFailed` with consecutive failure count.

### Files To Read Or Update

Read these UPDATE files completely before changing them:

- `src/Bondstone/Messaging/Contracts/IDurableOperationResultReader.cs` - public operation result read and wait contract wording.
- `src/Bondstone.Persistence/Messaging/Contracts/IDurableOperationReader.cs` - operation-state observation contract wording.
- `src/Bondstone.Persistence/Messaging/Operations/DurableOperationHandle.cs` - accepted-work metadata for source and target modules.
- `src/Bondstone.Persistence/Messaging/Operations/DurableOperationState.cs` and `DurableOperationStatus.cs` - operation-state shape and statuses.
- `src/Bondstone/Messaging/Sending/DurableOperationResult.cs`, `DurableOperationWaitResult.cs`, `DurableOperationResultState.cs`, and `DurableOperationResultDeserializationFailure.cs` - caller-facing observation result types.
- `src/Bondstone/Messaging/Sending/DurableOperationResultReader.cs` - wait/read implementation and timeout semantics.
- `src/Bondstone/Persistence/Operations/DurableModuleOperationReader.cs` - module-aware aggregate and hinted state reads.
- `src/Bondstone/Persistence/Operations/DurableOperationFinalizer.cs` and `DurableOperationExpirationProcessor.cs` - explicit application-owned finalization/expiry policy.
- `src/Bondstone.Persistence/Persistence/Contracts/IDurableOperationStateStore.cs` and `IDurableOperationExpirationStore.cs` - provider/runtime persistence contracts.
- `src/Bondstone.Persistence/Persistence/Contracts/IDurableOutboxInspector.cs`, `IDurableInboxInspector.cs`, `IDurableOutboxInspectionStore.cs`, `IDurableInboxInspectionStore.cs`, and `IDurableIncomingInboxInspectionStore.cs` - read-only inspection surfaces.
- `src/Bondstone/Persistence/Outbox/DurableOutboxInspector.cs` and `src/Bondstone/Persistence/Inbox/DurableInboxInspector.cs` - app-facing inspector implementations.
- `src/Bondstone.Persistence.EntityFrameworkCore/Operations/EntityFrameworkCoreDurableOperationStateStore.cs` and related operation entity/configuration files if EF operation state changes.
- `src/Bondstone.Persistence.EntityFrameworkCore/IncomingInbox/EntityFrameworkCoreDurableIncomingInboxInspectionStore.cs` if durable incoming inbox inspection is promoted or documented.
- `src/Bondstone.Hosting/Outbox/DurableOutboxWorker.cs`, `DurableOutboxWorkerLogEvents.cs`, `src/Bondstone.Hosting/IncomingInbox/DurableIncomingInboxWorker.cs`, and `DurableIncomingInboxWorkerLogEvents.cs` if worker evidence changes.
- `docs/operations.md`, `docs/observability.md`, and `docs/public-api.md` - consumer-facing operation observation and compatibility guidance.

Likely tests to inspect or extend:

- `tests/Bondstone.Tests/Messaging/DurableOperationReaderTests.cs`
- `tests/Bondstone.Tests/Messaging/DurableOperationResultReaderTests.cs`
- `tests/Bondstone.Tests/Messaging/DurableOperationFinalizerTests.cs`
- `tests/Bondstone.Tests/Messaging/DurableOperationExpirationProcessorTests.cs`
- `tests/Bondstone.Tests/Messaging/DurableOperationHandleTests.cs`
- `tests/Bondstone.Tests/Persistence/DurableOutboxInspectorTests.cs`
- `tests/Bondstone.Tests/Persistence/DurableInboxInspectorTests.cs`
- `tests/Bondstone.Persistence.EntityFrameworkCore.Tests/Operations/EntityFrameworkCoreDurableOperationStateStoreTests.cs`
- `tests/Bondstone.Persistence.EntityFrameworkCore.Tests/IncomingInbox/EntityFrameworkCoreDurableIncomingInboxInspectionStoreTests.cs`
- `tests/Bondstone.Persistence.EntityFrameworkCore.Tests/Inbox/EntityFrameworkCoreDurableInboxInspectionStoreTests.cs`
- `tests/Bondstone.Persistence.EntityFrameworkCore.Tests/Outbox/EntityFrameworkCoreDurableOutboxInspectionStoreTests.cs`
- `tests/Bondstone.Hosting.Tests/Outbox/DurableOutboxWorkerTests.cs` and `tests/Bondstone.Hosting.Tests/IncomingInbox/DurableIncomingInboxWorkerTests.cs` if worker evidence behavior changes.
- Public API baselines under `tests/Bondstone.PublicApi.Tests/Baselines/` if public/protected APIs change.

### Architecture Compliance

Follow these non-negotiable rules:

- Bondstone is a durable module-boundary library, not a generic bus, workflow engine, saga/process-manager framework, code generator, SaaS framework, application platform, broker runtime owner, or cleanup/retention engine.
- Operation observation answers "what is known about accepted durable work?" It is not orchestration, saga state, process-manager state, or durable continuation state.
- Durable command send accepts work. It does not return the target handler result directly. Results are observed through operation APIs when command and caller use an operation id.
- Terminal durable inbox failure is operational evidence; it does not automatically write caller-visible operation `Failed` state.
- Cleanup, retention, replay, purge, stale-row mutation, broker dead-letter movement, and topology management remain application-owned.
- Diagnostics and metrics must avoid high-cardinality labels such as message ids, operation ids, payloads, exception messages, broker delivery counts, topology details, and dead-letter state.
- Public/protected API changes are compatibility-sensitive. Additive operation observation APIs still require public API baseline review and docs updates.
- Do not add `InternalsVisibleTo` for runtime package collaboration.

### Testing Requirements

Use the repository testing policy from `docs/testing.md`:

- Use `Unit` tests for operation result state classification, wait timeout behavior, module-hinted reads, handle-based reads, finalizer validation, expiry policy validation, inspector routing, and worker option/log behavior with fakes.
- Use `Application` tests for EF Core mapping/change-tracker and in-process composition that avoids external infrastructure.
- Use `Integration` tests only for real PostgreSQL, RabbitMQ, Service Bus, or sample smoke behavior.
- Prefer outcome assertions over "called method" assertions: observed state, no state save on timeout, exact result state, diagnostic context, filtered inspection rows, log event id/name, and missing-registration error messages.
- If adding a public app-facing durable incoming inbox inspector, add unit tests for module/source transport filters and EF Core application tests for pending, processing, stale, processed, retry, and terminal failed rows as applicable.

### Previous Story Intelligence

Story 7.1 completed thin transport and fanout ergonomics and established:

- Start with an inventory; make runtime changes only for real gaps.
- Keep host ownership of topology, retry, dead-letter policy, credentials, monitoring, worker placement, and broker behavior explicit.
- Preserve operation finalization as Bondstone-owned durable semantics, while broker receive workers only ingest durable inbox rows and never run handlers or complete operation state.
- Use targeted tests first, then broader repo scripts.
- If a gap is documentation-only, fix docs narrowly and leave working runtime code alone.

Story 6.3 completed EF/PostgreSQL production persistence and established:

- EF Core plus PostgreSQL is the supported production durable persistence path.
- Consumers own EF migrations and schema rollout.
- Terminal outbox and durable receive evidence should be observable through intended inspection surfaces.

Story 6.2 completed the durable receive transaction boundary and established:

- Native broker deliveries must be durably ingested before native settlement.
- Durable incoming inbox terminal failure is operational evidence, not automatic operation failure.
- Duplicate ingestion can settle safely because durable receive evidence already exists.

Story 6.1 completed source outbox atomicity and established:

- Source state and outgoing outbox rows must commit atomically.
- Terminal outbox evidence should be observable through the intended inspection surface.

### Git Intelligence

Recent commits at story creation time:

- `e85574a docs: postgres durability`
- `dd17279 fix: sb durable worker`
- `6355dc4 fix: sb worker`
- `23bda7f fix: atomicity test`
- `946886c fix: epic 5 done`

The recent pattern is narrow runtime corrections driven by tests, followed by docs alignment. Continue that pattern; avoid broad operation API redesign unless the inventory proves a real observation gap.

### Latest Technical Information

No dependency upgrade is required for this story. Use repository-pinned versions from `Directory.Build.props`, `Directory.Packages.props`, `global.json`, and `package.json`:

- Target framework `net10.0`; SDK `10.0.108`; nullable enabled; warnings as errors; central package management.
- EF Core and Microsoft.Extensions packages pinned at `10.0.8`; NuGet currently lists `Microsoft.EntityFrameworkCore` `10.0.9`, but this story should not perform dependency upgrades.
- `Azure.Messaging.ServiceBus` pinned at `7.20.1`.
- `RabbitMQ.Client` pinned at `7.2.1`.
- xUnit pinned at `2.9.3`; `Microsoft.NET.Test.Sdk` pinned at `18.6.0`.

Official docs to keep in mind:

- `Task.Delay` with a cancellation token completes canceled when the token is signaled; cancellation remains caller wait cancellation, not operation finalization. Source: https://learn.microsoft.com/en-us/dotnet/api/system.threading.tasks.task.delay?view=net-10.0
- Microsoft Learn documents Azure Service Bus .NET client `7.20.1` for the repo-pinned package. Source: https://learn.microsoft.com/en-us/dotnet/api/overview/azure/messaging.servicebus-readme
- RabbitMQ documents the .NET client as the official AMQP 0-9-1 client. Source: https://www.rabbitmq.com/client-libraries/dotnet

### Project Structure Notes

- No UX files were found under `_bmad-output/planning-artifacts`; UX is not applicable for this library story.
- Keep operation APIs in core/persistence packages according to current package ownership. Do not move provider/runtime contracts into transport packages.
- Consumer-facing docs belong in `docs/operations.md`, `docs/observability.md`, and `docs/public-api.md`; internal runtime authority stays in BMAD architecture.
- Generated packages, coverage, temporary sample outputs, and build artifacts should stay out of source.

### Project Context Reference

Project context says Bondstone is a .NET library/framework for durable module boundaries, durable command sending, EF Core backed inbox/outbox persistence, operation observation, and transport adapters. Durable command send accepts work and returns metadata; target handler results are observed through operation APIs, not returned directly. Cleanup, retention, replay, purge, stale-row recovery, broker dead-letter movement, and topology management remain application-owned. EF Core InMemory is not proof of relational durability, uniqueness, transactions, locking, claiming, retries, or PostgreSQL behavior.

### Open Questions

None blocking. During implementation, the main design decision is whether durable incoming inbox terminal/stale evidence needs a new app-facing read-only inspector in this story, or whether the existing public/provider contract plus docs is sufficient for the current release. If adding public API, treat it as compatibility-sensitive and update public API docs and baselines intentionally.

## Dev Agent Record

### Implementation Plan

- Complete the required inventory first and preserve existing runtime behavior where tests and docs already prove the boundary.
- Tighten operation-observation wording only where the inventory found ambiguity.
- Add focused test assertions for timeout paths proving waits do not write operation state.
- Document the existing durable incoming inbox inspection store as the current read-only terminal receive evidence surface instead of adding a new app-facing mutation or orchestration API.

### Debug Log

- Read the story, sprint status, project context, repository/testing/docs indexes, BMAD architecture/PRD/epics excerpts, all listed operation/inspection/worker source files, and all listed tests before editing.
- Resolved workflow customization manually because the local Python resolver failed before loading its `json` import.
- Confirmed accepted-work metadata, handle-based/module-hinted reads, result state classification, finalizer/expiration policy, outbox/direct inbox inspectors, EF incoming inbox inspection, and worker log evidence already existed.
- Targeted core tests: `dotnet test tests/Bondstone.Tests/Bondstone.Tests.csproj --configuration Release --filter "FullyQualifiedName~DurableOperationResultReaderTests|FullyQualifiedName~DurableOperationReaderTests|FullyQualifiedName~DurableOperationFinalizerTests|FullyQualifiedName~DurableOperationExpirationProcessorTests|FullyQualifiedName~DurableOperationHandleTests|FullyQualifiedName~DurableOutboxInspectorTests|FullyQualifiedName~DurableInboxInspectorTests"` passed: 57/57.
- EF inspection tests: `dotnet test tests/Bondstone.Persistence.EntityFrameworkCore.Tests/Bondstone.Persistence.EntityFrameworkCore.Tests.csproj --configuration Release --filter "FullyQualifiedName~OperationStateStoreTests|FullyQualifiedName~IncomingInboxInspectionStoreTests|FullyQualifiedName~InboxInspectionStoreTests|FullyQualifiedName~OutboxInspectionStoreTests"` passed: 15/15.
- Worker evidence tests: `dotnet test tests/Bondstone.Hosting.Tests/Bondstone.Hosting.Tests.csproj --configuration Release --filter "FullyQualifiedName~DurableOutboxWorkerTests|FullyQualifiedName~DurableIncomingInboxWorkerTests"` passed: 11/11.
- `pnpm backend:test` passed the fast Unit/Application gate.
- `pnpm check` passed format, restore, Release build, fast tests, pack, and package tests.

### Completion Notes

- Current state: operation result APIs already observe module-owned operation state without running handlers, dispatching outbox rows, inspecting broker state, or mutating operation rows. Module-hinted and handle-based reads already query the target module store, and result classification covers unknown, pending, running, completed with/without result, failed, cancelled, and deserialization failure.
- Intended change: preserve runtime behavior and make the observation boundary clearer through XML/docs wording, documented durable incoming inbox inspection evidence, and explicit no-write timeout test assertions.
- Preserved behavior: no orchestration, saga/process-manager state, durable continuations, hidden workers, automatic operation failure inference, replay/reset/purge APIs, broker dead-letter movement, or stale-row mutation were added.
- `IDurableIncomingInboxInspectionStore` remains the sufficient current read-only durable receive evidence surface for this release because it is public, registered by EF Core persistence, filterable by receiver module and source transport, tested for broad status/stale/terminal-failed reads, and non-mutating. A separate app-facing incoming inbox inspector would be additive public API churn without a proven capability gap in this story.
- Public API shape did not change; `pnpm check` included Public API tests and package validation.

## File List

- `_bmad-output/implementation-artifacts/7-2-operation-observation-cleanup.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `docs/observability.md`
- `docs/operations.md`
- `docs/public-api.md`
- `src/Bondstone.Persistence/Messaging/Contracts/IDurableOperationReader.cs`
- `tests/Bondstone.Tests/Messaging/DurableOperationResultReaderTests.cs`

## Change Log

- 2026-06-20: Completed operation observation cleanup inventory, tightened observation wording/docs, documented durable incoming inbox inspection evidence, added no-write timeout assertions, and verified with targeted tests plus `pnpm check`.

### Story Completion Status

Ultimate context engine analysis completed - comprehensive developer guide created.

### References

- `_bmad-output/planning-artifacts/epics.md` - Epic 7 and Story 7.2 acceptance criteria.
- `_bmad-output/planning-artifacts/prds/prd-Bondstone-2026-06-18/prd.md` - FR3.2, FR5.3, FR8.1, FR8.3.
- `_bmad-output/planning-artifacts/architecture.md` - Operation Observation, Hosting And Workers, Diagnostics And Observability, Public API And Compatibility, Verification Strategy.
- `_bmad-output/project-context.md` - runtime, code, testing, and workflow guardrails.
- `_bmad-output/implementation-artifacts/7-1-thin-transport-and-fanout-ergonomics.md` - previous story implementation intelligence.
- `docs/operations.md` - operation result, timeout, finalization, expiry, and troubleshooting guidance.
- `docs/observability.md` - current diagnostic surfaces and non-current behavior.
- `docs/public-api.md` - compatibility notes for operation observation APIs and inspection surfaces.
- `docs/testing.md` - test categories and verification surface.
- `src/Bondstone/Messaging/Contracts/IDurableOperationResultReader.cs` - public operation result and wait contract.
- `src/Bondstone.Persistence/Messaging/Contracts/IDurableOperationReader.cs` - operation-state observation contract.
- `src/Bondstone/Messaging/Sending/DurableOperationResultReader.cs` - result and wait implementation.
- `src/Bondstone.Persistence/Persistence/Contracts/IDurableIncomingInboxInspectionStore.cs` - durable incoming inbox provider/runtime inspection surface.
- `src/Bondstone.Persistence.EntityFrameworkCore/IncomingInbox/EntityFrameworkCoreDurableIncomingInboxInspectionStore.cs` - EF Core incoming inbox inspection implementation.
