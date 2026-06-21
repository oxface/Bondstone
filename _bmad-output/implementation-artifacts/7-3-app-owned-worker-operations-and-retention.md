---
baseline_commit: 8a7090d
---

# Story 7.3: App-Owned Worker Operations And Retention

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As an operator,
I want explicit inspection and cleanup guidance,
so that durable evidence is not destroyed by defaults.

## Acceptance Criteria

1. Given cleanup, retention, replay, purge, stale-row recovery, broker dead-letter movement, or topology management is needed, when documentation is reviewed, then it is application-owned unless a future BMAD PRD and architecture add native support.
2. Given worker health guidance is reviewed, when terminal outbox failures, stale inbox rows, terminal receive failures, worker failures, or operation expiration backlog exist, then inspection recipes describe the evidence.
3. Given helper APIs are added, when reviewed, then they are explicit and opt in rather than automatic destructive defaults.
4. Given inspection recipes depend on durable evidence, when feasible, then tests exercise the evidence source or helper API so the story does not close on prose alone.
5. Given a cleanup, replay, purge, stale-row, or DLQ action would be destructive or provider-owned, when no helper is added, then the story records that as a deliberate non-feature and leaves only non-destructive inspection support.

## Tasks / Subtasks

- [x] Inventory worker-operation and retention surfaces before changing code. (AC: 1, 2, 3, 4, 5)
  - [x] Read every file listed in "Files To Read Or Update" before editing; summarize current state, intended change, and preserved behavior in the dev record.
  - [x] Map each evidence category to an existing read-only surface or documented non-feature: terminal outbox failures, direct inbox ambiguity, durable incoming inbox stale/terminal evidence, worker batch failures, operation expiration backlog, broker DLQ/topology state, and retention cleanup.
  - [x] Close documentation-only only if the inventory records that current tests already prove every inspection recipe used by docs.
- [x] Keep destructive operations application-owned. (AC: 1, 3, 5)
  - [x] Do not add default hosted cleanup, retention, replay, purge, archival, stale-row mutation, broker DLQ movement, broker topology management, or operation-failure inference.
  - [x] If adding any helper API, make it explicit, opt in, narrowly named, non-default, and compatibility-reviewed; prefer read-only inspection unless the story proves a real current gap.
  - [x] If no helper API is added, record the deliberate non-feature decision in completion notes.
- [x] Tighten operations guidance only where the inventory finds gaps. (AC: 1, 2, 5)
  - [x] Keep `docs/operations.md` clear that retention and cleanup are app-owned because durable rows may be business evidence.
  - [x] Ensure health/readiness recipes mention concrete evidence for terminal outbox failures, stale direct inbox rows, terminal durable incoming inbox failures, stale durable incoming inbox processing claims, repeated hosted worker failures, and operation expiration backlog.
  - [x] Keep broker retry, delivery counts, DLQ movement, queue/topic/exchange topology, and provider monitoring under native broker or application ownership.
- [x] Prove inspection recipes through tests or record existing proof. (AC: 2, 4)
  - [x] Verify terminal outbox inspection tests cover module filtering, UTC cutoff, and read-only store routing.
  - [x] Verify direct inbox inspection tests cover unprocessed row filtering, UTC cutoff, and read-only store routing.
  - [x] Verify durable incoming inbox inspection tests cover broad status reads, stale processing claim reads, terminal failure reads, receiver-module filters, source-transport filters, and setup errors.
  - [x] Verify operation expiry tests cover candidate reads, explicit finalization by app-owned policy, max-count behavior, invalid terminal statuses, and metrics.
  - [x] Verify hosted outbox and incoming inbox worker tests cover failure log event ids and continue behavior.
  - [x] Add focused tests only for uncovered inspection evidence used by docs; do not add tests for broker-owned DLQ or topology behavior unless an accepted Bondstone-owned behavior is introduced.
- [x] Validate and verify. (AC: 1, 2, 3, 4, 5)
  - [x] Run targeted core evidence tests first, for example `dotnet test tests/Bondstone.Tests/Bondstone.Tests.csproj --configuration Release --filter "FullyQualifiedName~DurableOutboxInspectorTests|FullyQualifiedName~DurableInboxInspectorTests|FullyQualifiedName~DurableOperationExpirationProcessorTests|FullyQualifiedName~DurableIncomingInboxDispatcherTests"`.
  - [x] Run EF inspection tests if EF inspection docs or stores change: `dotnet test tests/Bondstone.Persistence.EntityFrameworkCore.Tests/Bondstone.Persistence.EntityFrameworkCore.Tests.csproj --configuration Release --filter "FullyQualifiedName~IncomingInboxInspectionStoreTests|FullyQualifiedName~InboxInspectionStoreTests|FullyQualifiedName~OutboxInspectionStoreTests|FullyQualifiedName~OperationStateStoreTests"`.
  - [x] Run worker tests if worker failure guidance or code changes: `dotnet test tests/Bondstone.Hosting.Tests/Bondstone.Hosting.Tests.csproj --configuration Release --filter "FullyQualifiedName~DurableOutboxWorkerTests|FullyQualifiedName~DurableIncomingInboxWorkerTests"`.
  - [x] Run PostgreSQL incoming inbox tests if provider-backed durable incoming inbox evidence changes: `dotnet test tests/Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests/Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests.csproj --configuration Release --filter "FullyQualifiedName~PostgreSqlIncomingInboxProcessingTests"`.
  - [x] Run `pnpm backend:test` after runtime, test, or public docs changes.
  - [x] Run `pnpm backend:pack` and review public API baseline diffs if public/protected APIs change.
  - [x] Run `pnpm check` as the final broad gate when code, public API, package metadata, samples, or broad docs change.

### Review Findings

- [x] [Review][Patch] Incoming inbox diagnostics are documented under the wrong activity source and meter sections [docs/observability.md:38]

## Dev Notes

Story 7.3 is an operations-boundary story. It should not become a cleanup engine story unless a current gap is proven and the resulting API is explicit, opt in, and non-default. Epic 7 says runtime stories start with implementation inventory and may close documentation-only only when evidence shows current code and tests satisfy every acceptance criterion.

### Current State Intelligence

Operations ownership:

- `docs/operations.md` already says Bondstone owns durable envelopes, EF mappings, incoming inbox ledger, outbox retry/terminal state, hosted outbox and incoming inbox workers, read-only inspection contracts, and explicit operation finalization/expiration APIs.
- The application owns EF migrations, schema rollout, table retention, broker topology/provisioning/subscriptions/native consumers/retry/delivery counts/DLQ/prefetch/concurrency/monitoring, replay/reset/purge/archive/stale-inbox recovery/broker movement, endpoint status policy, and DTO compatibility.
- `docs/operations.md` has health/readiness recipes for terminal outbox failures, stale direct inbox rows, repeated outbox worker failures, repeated incoming inbox worker failures, and operation expiration backlog.

Existing read-only inspection surfaces:

- `IDurableOutboxInspector` reads terminal failed outbox rows for one module through the module's registered `IDurableOutboxInspectionStore`.
- `IDurableInboxInspector` reads received-but-unprocessed direct inbox rows for one module through the module's registered `IDurableInboxInspectionStore`.
- `IDurableIncomingInboxInspectionStore` is the current provider/runtime durable incoming inbox evidence surface. It supports broad status reads, stale processing claim reads, and terminal receive-failure reads filtered by receiver module and source transport. It must remain read-only.
- `IDurableOperationExpirationStore` finds stale pending/running operation-state rows. `IDurableOperationExpirationProcessor` is an explicit app-owned processor that finalizes candidates through `IDurableOperationFinalizer`; Bondstone does not schedule it as a hosted worker.

Worker and diagnostics evidence:

- `DurableOutboxWorker` logs unexpected batch failures with event id `1001` / `DispatchBatchFailed`, waits `FailureDelay`, and continues.
- `DurableIncomingInboxWorker` logs unexpected process-batch failures with event id `2001` / `ProcessBatchFailed`, includes consecutive failure count, waits `FailureDelay`, and continues.
- `DurableIncomingInboxDispatcher` records processed, retry scheduled, terminal failed, and stale outcomes for claimed durable incoming rows. Terminal receive failure is operational evidence and must not automatically write caller-visible operation `Failed`.
- `IncomingInboxProcessingDiagnostics` currently emits incoming inbox processing activities and counters for claimed, processed, retry scheduled, terminal failed, and stale transitions. `docs/observability.md` still says finalized durable inbox worker metrics are not current behavior; verify and align wording if this story touches observability docs.

Deliberate non-features to preserve:

- No default cleanup or retention worker.
- No replay/reset/purge/archive/stale-row mutation API unless explicitly justified as a narrow opt-in helper.
- No provider-neutral broker DLQ movement, queue/topic/exchange provisioning, retry policy ownership, subscription storage, or broker monitoring.
- No automatic inference that terminal outbox failure, terminal durable inbox failure, broker retry exhaustion, or DLQ presence means an operation should be marked failed.
- No hidden local transport fallback for missing production broker configuration.

### Files To Read Or Update

Read these UPDATE files completely before changing them:

- `docs/operations.md` - primary consumer-facing ownership, health/readiness, operation expiry, and retention guidance.
- `docs/observability.md` - current diagnostic surfaces and not-current behavior.
- `docs/public-api.md` - compatibility classification for inspection, operation expiry, and any new helper API.
- `docs/package-discovery.md` and `docs/setup.md` - package/setup references that mention workers, incoming inbox processing, retention, or app-owned transport behavior.
- `src/Bondstone.Persistence/Persistence/Contracts/IDurableOutboxInspector.cs` and `IDurableOutboxInspectionStore.cs` - read-only terminal outbox evidence contracts.
- `src/Bondstone.Persistence/Persistence/Contracts/IDurableInboxInspector.cs` and `IDurableInboxInspectionStore.cs` - read-only direct inbox ambiguity evidence contracts.
- `src/Bondstone.Persistence/Persistence/Contracts/IDurableIncomingInboxInspectionStore.cs` - provider/runtime durable incoming inbox evidence contract.
- `src/Bondstone.Persistence/Persistence/Contracts/IDurableOperationExpirationStore.cs` - provider/runtime stale operation candidate query contract.
- `src/Bondstone/Persistence/Outbox/DurableOutboxInspector.cs` and `src/Bondstone/Persistence/Inbox/DurableInboxInspector.cs` - app-facing inspector implementations.
- `src/Bondstone/Messaging/Contracts/IDurableOperationExpirationProcessor.cs`, `src/Bondstone/Messaging/Sending/DurableOperationExpirationResult.cs`, and `src/Bondstone/Persistence/Operations/DurableOperationExpirationProcessor.cs` - explicit app-owned operation expiry processor.
- `src/Bondstone.Persistence.EntityFrameworkCore/IncomingInbox/EntityFrameworkCoreDurableIncomingInboxInspectionStore.cs`, `src/Bondstone.Persistence.EntityFrameworkCore/Outbox/EntityFrameworkCoreDurableOutboxInspectionStore.cs`, `src/Bondstone.Persistence.EntityFrameworkCore/Inbox/EntityFrameworkCoreDurableInboxInspectionStore.cs`, and `src/Bondstone.Persistence.EntityFrameworkCore/Operations/EntityFrameworkCoreDurableOperationStateStore.cs` - EF-backed evidence source implementations.
- `src/Bondstone/Persistence/IncomingInbox/DurableIncomingInboxDispatcher.cs`, `IncomingInboxProcessingDiagnostics.cs`, and `src/Bondstone.Persistence/Persistence/IncomingInbox/DurableIncomingInboxProcessingResult.cs` - durable incoming inbox processing evidence and diagnostics.
- `src/Bondstone.Hosting/Outbox/DurableOutboxWorker.cs`, `DurableOutboxWorkerLogEvents.cs`, `src/Bondstone.Hosting/IncomingInbox/DurableIncomingInboxWorker.cs`, and `DurableIncomingInboxWorkerLogEvents.cs` - hosted worker failure evidence.

Likely tests to inspect or extend:

- `tests/Bondstone.Tests/Persistence/DurableOutboxInspectorTests.cs`
- `tests/Bondstone.Tests/Persistence/DurableInboxInspectorTests.cs`
- `tests/Bondstone.Tests/Persistence/DurableIncomingInboxDispatcherTests.cs`
- `tests/Bondstone.Tests/Messaging/DurableOperationExpirationProcessorTests.cs`
- `tests/Bondstone.Persistence.EntityFrameworkCore.Tests/IncomingInbox/EntityFrameworkCoreDurableIncomingInboxInspectionStoreTests.cs`
- `tests/Bondstone.Persistence.EntityFrameworkCore.Tests/Outbox/EntityFrameworkCoreDurableOutboxInspectionStoreTests.cs`
- `tests/Bondstone.Persistence.EntityFrameworkCore.Tests/Inbox/EntityFrameworkCoreDurableInboxInspectionStoreTests.cs`
- `tests/Bondstone.Persistence.EntityFrameworkCore.Tests/Operations/EntityFrameworkCoreDurableOperationStateStoreTests.cs`
- `tests/Bondstone.Hosting.Tests/Outbox/DurableOutboxWorkerTests.cs`
- `tests/Bondstone.Hosting.Tests/IncomingInbox/DurableIncomingInboxWorkerTests.cs`
- `tests/Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests/Persistence/PostgreSqlIncomingInboxProcessingTests.cs` if provider-backed durable incoming inbox evidence changes.
- Public API baselines under `tests/Bondstone.PublicApi.Tests/Baselines/` if public/protected APIs change.

### Architecture Compliance

Follow these non-negotiable rules:

- Bondstone is a durable module-boundary library, not a generic bus, broker runtime owner, workflow engine, saga/process-manager framework, application platform, or cleanup/retention engine.
- The target worker topology has three roles only: source outbox dispatch worker, transport ingestion listener/worker, and durable inbox processing worker.
- Cleanup, retention, replay, purge, stale-row mutation, and broker dead-letter movement remain application-owned without a future BMAD PRD and architecture decision.
- Durable incoming inbox terminal failure is operational evidence; it does not automatically write caller-visible operation `Failed` state.
- Operation expiry is explicit application policy through `IDurableOperationExpirationProcessor`; Bondstone must not register a hosted operation expiry worker by default.
- Diagnostics should remain low-cardinality. Do not add message ids, operation ids, payloads, exception messages, broker delivery counts, topology details, or dead-letter state as metric dimensions.
- Public/protected API changes are compatibility-sensitive. Additive helper APIs still need docs, public API baseline review, and migration/release-note consideration.
- Do not add `InternalsVisibleTo` for runtime package collaboration.

### Testing Requirements

Use the repository testing policy from `docs/testing.md`:

- Use `Unit` tests for inspector routing, option validation, worker log/continue behavior, operation expiry validation, and result-count semantics without external infrastructure.
- Use `Application` tests for EF Core mapping/change-tracker inspection queries that avoid real external IO.
- Use `Integration` tests for PostgreSQL durability, real provider behavior, locking, claiming, retry, terminal state transitions, and receive/outbox semantics.
- EF Core InMemory is acceptable for mapping/query shape checks only; it is not proof of PostgreSQL locking, uniqueness, transactions, claiming, or retry behavior.
- Prefer outcome assertions over interaction-only assertions: returned records, filters, UTC validation, status, failure reason, claim owner, metric/log event id, and absence of mutation.

Existing test evidence to preserve or cite in completion notes:

- `DurableOutboxInspectorTests` proves module-routed terminal outbox inspection and clear missing-store errors.
- `DurableInboxInspectorTests` proves module-routed unprocessed direct inbox inspection and clear missing-store errors.
- `EntityFrameworkCoreDurableIncomingInboxInspectionStoreTests` proves status, stale processing, terminal failed, receiver module/source transport filters, UTC validation, and missing mapping errors.
- `EntityFrameworkCoreDurableOperationStateStoreTests` proves EF expiration candidate reads for stale pending/running operations.
- `DurableOperationExpirationProcessorTests` proves explicit app-owned finalization, metrics, max-count behavior, unsupported store errors, and invalid terminal status rejection.
- `DurableOutboxWorkerTests` and `DurableIncomingInboxWorkerTests` prove worker failure logs and continue behavior.
- `PostgreSqlIncomingInboxProcessingTests` proves provider-backed terminal durable incoming inbox evidence and stale processing inspection in PostgreSQL.

### Previous Story Intelligence

Story 7.2 finished operation observation cleanup and established:

- Inventory first; preserve runtime behavior when code and tests already prove the boundary.
- `IDurableIncomingInboxInspectionStore` is the sufficient current read-only durable receive evidence surface for this release because it is public, registered by EF Core persistence, filterable by receiver module and source transport, tested, and non-mutating.
- Operation waits and reads observe state only; timeout does not write terminal operation state.
- Public API shape did not change in 7.2; docs and tests carried the clarification.
- Do not add replay/reset/purge APIs, broker dead-letter movement, stale-row mutation, automatic operation failure inference, hidden workers, or orchestration semantics.

Story 7.1 established:

- Transport adapters are thin native-driver envelope adapters.
- Host code owns topology, retry, dead-letter policy, credentials, monitoring, worker placement, and broker behavior.
- Broker receive workers ingest native deliveries into durable inbox rows before native settlement and do not run handlers or complete operation state.

Story 6.3 established:

- EF Core plus PostgreSQL is the supported production durable persistence path.
- Consumers own migrations and schema rollout.
- Durable table retention remains app-owned.

Story 6.2 established:

- Durable incoming inbox terminal failure is receive evidence, not automatic operation failure.
- Duplicate ingestion can settle safely because durable receive evidence already exists.

Story 6.1 established:

- Terminal outbox evidence should stay observable through the intended inspection surface.

### Git Intelligence

Recent commits at story creation time:

- `8a7090d fix: more observation` - completed 7.2 observation docs/tests and updated 7.1/7.2 artifacts.
- `e85574a docs: postgres durability` - documented PostgreSQL durability and package guidance.
- `dd17279 fix: sb durable worker` - tightened Service Bus worker setup and tests.
- `6355dc4 fix: sb worker` - prior Service Bus worker correction.
- `23bda7f fix: atomicity test` - narrowed source outbox atomicity test behavior.

The recent pattern is narrow runtime or documentation correction backed by targeted tests. Continue that pattern; do not redesign operations APIs unless the inventory proves a current inspection gap.

### Latest Technical Information

No dependency upgrade is required for this story. Use repository-pinned versions from `Directory.Build.props`, `Directory.Packages.props`, `global.json`, and `package.json`:

- Target framework `net10.0`; package version prefix currently `1.1.0`.
- EF Core packages are pinned at `10.0.8`.
- `Azure.Messaging.ServiceBus` is pinned at `7.20.1`.
- `RabbitMQ.Client` is pinned at `7.2.1`.
- xUnit is pinned at `2.9.3`; `Microsoft.NET.Test.Sdk` is pinned at `18.6.0`.

Official external docs checked during story creation:

- Microsoft Learn documents Azure Service Bus .NET client version `7.20.1`; use native Service Bus client behavior for broker retry/dead-letter/topology, not Bondstone provider-neutral ownership. Source: https://learn.microsoft.com/en-us/dotnet/api/overview/azure/messaging.servicebus-readme?view=azure-dotnet
- RabbitMQ docs describe manual acknowledgements and negative acknowledgements as broker/client behavior; Bondstone's RabbitMQ adapter should keep durable ingestion before ack/nack and leave queue/DLQ policy to the host. Sources: https://www.rabbitmq.com/docs/confirms and https://www.rabbitmq.com/client-libraries/dotnet-api-guide

### Project Structure Notes

- No UX files were found under `_bmad-output/planning-artifacts`; UX is not applicable for this library story.
- Consumer-facing operations guidance belongs in `docs/operations.md`; diagnostic surface inventory belongs in `docs/observability.md`; compatibility notes belong in `docs/public-api.md`.
- Internal durable architecture remains in `_bmad-output/planning-artifacts/architecture.md`; do not duplicate architecture-book content into docs beyond useful consumer guidance.
- Keep package boundaries aligned with `docs/packaging.md`: core contracts in `Bondstone`/`Bondstone.Persistence`, EF implementations in `Bondstone.Persistence.EntityFrameworkCore`, PostgreSQL proof in `Bondstone.Persistence.EntityFrameworkCore.Postgres`, and hosted workers in `Bondstone.Hosting`.
- Generated packages, coverage, temporary sample outputs, and build artifacts should stay out of source.

### Project Context Reference

Project context says Bondstone is a .NET library/framework for durable module boundaries, durable command sending, EF Core backed inbox/outbox persistence, operation observation, and transport adapters. Cleanup, retention, replay, purge, stale-row recovery, broker dead-letter movement, and topology management remain application-owned. EF Core InMemory is not proof of relational durability, uniqueness, transactions, locking, claiming, retries, or PostgreSQL behavior. Public/protected API changes are compatibility-sensitive.

### Open Questions

None blocking. During implementation, the main decision is whether the current operations docs and tests already satisfy every AC, or whether 7.3 should add a narrow read-only health/inspection helper. A helper should be added only if the inventory proves existing `IDurableOutboxInspector`, `IDurableInboxInspector`, `IDurableIncomingInboxInspectionStore`, worker logs, and operation expiration APIs are insufficient.

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- 2026-06-21: Activation resolver failed because local `python3` could not import stdlib `json`; resolved workflow customization manually from the documented base/team/user TOML fallback. No prepend or append steps configured.
- 2026-06-21: Inventory read completed for required docs, contracts, EF stores, incoming inbox dispatcher/diagnostics/result, hosted workers, scoped AGENTS guidance, and listed tests before editing.
- 2026-06-21: Targeted core evidence tests passed: `dotnet test tests/Bondstone.Tests/Bondstone.Tests.csproj --configuration Release --filter "FullyQualifiedName~DurableOutboxInspectorTests|FullyQualifiedName~DurableInboxInspectorTests|FullyQualifiedName~DurableOperationExpirationProcessorTests|FullyQualifiedName~DurableIncomingInboxDispatcherTests"` (14 passed).
- 2026-06-21: EF inspection evidence tests passed: `dotnet test tests/Bondstone.Persistence.EntityFrameworkCore.Tests/Bondstone.Persistence.EntityFrameworkCore.Tests.csproj --configuration Release --filter "FullyQualifiedName~EntityFrameworkCoreDurableIncomingInboxInspectionStoreTests|FullyQualifiedName~EntityFrameworkCoreDurableInboxInspectionStoreTests|FullyQualifiedName~EntityFrameworkCoreDurableOutboxInspectionStoreTests|FullyQualifiedName~EntityFrameworkCoreDurableOperationStateStoreTests"` (15 passed).
- 2026-06-21: Hosted worker evidence tests passed: `dotnet test tests/Bondstone.Hosting.Tests/Bondstone.Hosting.Tests.csproj --configuration Release --filter "FullyQualifiedName~DurableOutboxWorkerTests|FullyQualifiedName~DurableIncomingInboxWorkerTests"` (11 passed).
- 2026-06-21: `pnpm backend:test` passed for the fast Unit/Application suite.
- 2026-06-21: `pnpm check` passed, including format check, restore, Release build, fast tests, pack, and package tests.

### Implementation Plan

- Preserve runtime and public API behavior; add no cleanup, replay, purge, stale-row mutation, DLQ movement, topology management, expiry worker, or operation-failure inference.
- Close the story as documentation-only because existing read-only inspection surfaces and tests prove the recipes used by docs.
- Tighten `docs/operations.md` with concrete durable incoming inbox terminal-failure and stale-processing-claim inspection recipes.
- Align `docs/observability.md` with current incoming inbox processing activities and metrics emitted by `IncomingInboxProcessingDiagnostics`.

### Completion Notes List

- Inventory found terminal outbox failures covered by `IDurableOutboxInspector` and EF outbox inspection store tests; direct inbox ambiguity covered by `IDurableInboxInspector` and EF inbox inspection store tests; durable incoming terminal/stale evidence covered by `IDurableIncomingInboxInspectionStore`, EF inspection tests, and PostgreSQL integration proof; worker batch failures covered by hosted worker log/continue tests; operation expiration backlog covered by explicit `IDurableOperationExpirationProcessor` tests and EF candidate reads.
- Deliberate non-feature decision: no helper API was added. Cleanup, retention, replay, reset, purge, archival, stale-row mutation, broker DLQ movement, broker topology management, provider monitoring, and automatic operation-failure inference remain application-owned or provider-native.
- `docs/operations.md` now has concrete read-only recipes for terminal durable incoming inbox failures and stale durable incoming inbox processing claims, including evidence to inspect and destructive actions to avoid.
- `docs/observability.md` now documents current incoming inbox processing activities, metric instruments, low-cardinality metric attributes, and the boundary that metrics are not worker health, cleanup, or readiness policy.
- PostgreSQL incoming inbox tests were inspected as existing provider-backed proof but not re-run separately because no provider-backed durable incoming inbox behavior changed; `pnpm check` ran the fast PostgreSQL Unit/Application tests and package gate.

### File List

- `_bmad-output/implementation-artifacts/7-3-app-owned-worker-operations-and-retention.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `docs/operations.md`
- `docs/observability.md`

### Change Log

- 2026-06-21: Added durable incoming inbox terminal-failure and stale-processing-claim operations recipes; aligned observability docs with current incoming inbox processing diagnostics; recorded no-helper/non-destructive ownership decision.

### Story Completion Status

Complete; ready for review.

### References

- `_bmad-output/planning-artifacts/epics.md` - Epic 7 and Story 7.3 acceptance criteria.
- `_bmad-output/planning-artifacts/prds/prd-Bondstone-2026-06-18/prd.md` - FR8.1 and FR8.2.
- `_bmad-output/planning-artifacts/architecture.md` - Hosting And Workers, Operation Observation, Transport Boundary, Diagnostics And Observability, Public API And Compatibility, Verification Strategy, Explicit Deferred Work.
- `_bmad-output/project-context.md` - runtime, code, testing, and workflow guardrails.
- `_bmad-output/implementation-artifacts/7-2-operation-observation-cleanup.md` - previous story implementation intelligence.
- `docs/operations.md` - ownership, worker health, operation expiry, and retention guidance.
- `docs/observability.md` - current diagnostic surfaces and not-current behavior.
- `docs/public-api.md` - inspection and operation expiry public API classification.
- `docs/testing.md` - test categories and verification surface.
- `src/Bondstone.Persistence/Persistence/Contracts/IDurableIncomingInboxInspectionStore.cs` - durable incoming inbox provider/runtime inspection surface.
- `src/Bondstone/Messaging/Contracts/IDurableOperationExpirationProcessor.cs` - explicit app-owned operation expiry processor.
- `src/Bondstone.Hosting/Outbox/DurableOutboxWorker.cs` and `src/Bondstone.Hosting/IncomingInbox/DurableIncomingInboxWorker.cs` - hosted worker failure log evidence.
- `tests/Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests/Persistence/PostgreSqlIncomingInboxProcessingTests.cs` - provider-backed terminal/stale durable incoming inbox evidence.
