---
baseline_commit: ecd150a
---

# Story 5.3: Immediate Command And Query Boundaries

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a consumer,
I want immediate commands and queries to have explicit semantics,
so that callers do not confuse local execution, durable send, and read-only module access.

## Acceptance Criteria

1. Given `IModuleCommandExecutor` executes a command, when the handler runs, then it uses Bondstone's module command pipeline and is not exposed as a generic mediator.
2. Given cross-module state-changing work must survive restart or extraction, when it is invoked from a handler, then durable commands or integration events are used unless immediate same-process execution is explicitly accepted.
3. Given a durable command is sent, when the sender completes, then it returns accepted-work metadata rather than the target handler result.
4. Given a module query executes, when it completes, then it writes no inbox, outbox, operation, domain-event, or integration-event state.
5. Given a command or query route is missing, when execution is attempted, then diagnostics identify the missing route without deriving durable identities from CLR names.

## Tasks / Subtasks

- [x] Inventory the current immediate command and durable-send behavior before changing APIs. (AC: 1, 2, 3, 5)
  - [x] Review `IModuleCommandExecutor`, `ModuleCommandExecutor`, `ModuleCommandRuntime`, `ModuleCommandRoute`, and `ModuleCommandRouteRegistry`.
  - [x] Review `IDurableCommandSender`, `DurableCommandSender`, `DurableCommandSendResult`, operation result readers, and send tests.
  - [x] Review `ModuleCommandRegistrationTests`, `DurableCommandSenderTests`, and `ModuleReceivePipelineTests` for current pipeline, same-module nesting, cross-module guard, operation metadata, and route diagnostic behavior.
  - [x] Record in completion notes how command execution remains a Bondstone module boundary and not a generic mediator.
- [x] Preserve and tighten immediate command semantics. (AC: 1, 2, 5)
  - [x] Keep `IModuleCommandExecutor` as the command boundary for registered module commands only; do not rename, broaden, or document it as a mediator/request bus.
  - [x] Preserve command pipeline sequence through route lookup, module validation, module execution context, handler execution, receive inbox behavior when present, operation completion when present, and post-handler actions.
  - [x] Preserve same-module nested immediate execution when explicitly routed through the command executor.
  - [x] Preserve or tighten the current cross-module immediate execution guard so handlers cannot silently execute a different module's state-changing command; diagnostics should point callers to durable commands or integration events.
  - [x] Missing command-route diagnostics must name the normalized module and command type or durable message identity, and must not invent or derive durable identities from CLR names.
- [x] Preserve durable send as accepted-work metadata, not RPC. (AC: 3)
  - [x] Keep `IDurableCommandSender.SendAsync` returning `DurableCommandSendResult` with `SendId`, `Status`, optional `DurableOperationId`, and optional `DurableOperationHandle`.
  - [x] Keep durable send requiring a current command/subscriber module execution context and using that module as the source module.
  - [x] Do not add a `SendAsync<TResult>` path that waits for or returns the target handler result.
  - [x] If result observation guidance changes, route it through `IDurableOperationResultReader` and operation handles rather than durable send returning handler values.
- [x] Add a separate immediate query boundary. (AC: 4, 5)
  - [x] Introduce explicit query contracts and registration/execution surfaces in `src/Bondstone`, for example `IQuery<TResult>`, `IQueryHandler<TQuery, TResult>`, `IModuleQueryExecutor`, `BondstoneModuleQueryBuilder`, `ModuleQueryRoute`, and a query route registry.
  - [x] Add query registration under module-owned configuration, likely `BondstoneModuleBuilder.Queries`, without reusing command routes, durable message identities, command validators, inbox receive contexts, or operation completion behavior.
  - [x] Keep query execution immediate and read-only: it may resolve the registered handler and return a typed result, but it must not write durable inbox rows, outgoing outbox rows, operation state, domain-event records, or integration-event records.
  - [x] Prevent query handlers from accidentally gaining durable write capability through the same current module execution context used by commands and subscribers. If query execution needs module identity, represent it so `IDurableCommandSender` and `IDurableEventPublisher` still reject durable send/publish from query execution.
  - [x] Missing query-route diagnostics must name the normalized module and query type; do not use CLR type names as durable message identities.
  - [x] Keep query APIs in the core `Bondstone` package unless architecture is updated to split a contracts package.
- [x] Update docs and public API evidence only where behavior changes. (AC: 1, 3, 4, 5)
  - [x] Update `docs/setup.md` to distinguish local command execution, durable command send plus operation observation, and immediate read-only query execution.
  - [x] Update `docs/package-discovery.md`, `src/Bondstone/README.md`, or `docs/public-api.md` only if new public query contracts or setup APIs are introduced.
  - [x] If public/protected APIs are added or changed, classify the surface and update public API baselines intentionally.
- [x] Add focused tests. (AC: 1, 2, 3, 4, 5)
  - [x] Use `tests/Bondstone.Tests` `Unit` coverage for command executor guardrails, durable send metadata, query registration/execution, query no-write behavior, and missing route diagnostics.
  - [x] Use `tests/Bondstone.Composition.Tests` `Application` coverage only if the query boundary crosses package composition or `AddBondstone` service-provider setup.
  - [x] Verify query execution does not call outbox writers, inbox handlers, operation stores, domain-event persistence post-handler actions, or event publishers as side effects.
  - [x] Preserve existing tests that prove same-module command nesting, cross-module command rejection, operation-state pending creation, result command operation completion, and identity-only remote registration.
- [x] Verify the story outcome.
  - [x] Run targeted core tests first, for example `dotnet test tests/Bondstone.Tests/Bondstone.Tests.csproj --configuration Release --filter "FullyQualifiedName~ModuleCommandRegistrationTests|FullyQualifiedName~DurableCommandSenderTests"`.
  - [x] Add and run the new query-focused test class with `Category=Unit`.
  - [x] Run `pnpm backend:test` after runtime changes.
  - [x] Run `pnpm backend:pack` and public API baseline verification if public/protected package surface changes.
  - [x] Run `pnpm check` as the final broad gate when code, public API, package metadata, samples, or broad docs change.

### Review Findings

- [x] [Review][Patch] Document query read-only as pipeline-level enforcement and keep low-level durable mutation services as handler/application policy [docs/setup.md:690]
- [x] [Review][Patch] Suppress inherited command/subscriber execution context while running query handlers [src/Bondstone/Modules/Execution/ModuleQueryExecutor.cs:32]
- [x] [Review][Patch] Exclude open generic query handlers during assembly scanning [src/Bondstone/Modules/Registration/BondstoneModuleQueryBuilder.cs:51]
- [x] [Review][Patch] Preserve duplicate query-route diagnostics thrown through reflection-based assembly scanning [src/Bondstone/Modules/Registration/BondstoneModuleQueryBuilder.cs:82]
- [x] [Review][Patch] Clarify docs wording that visible handler identity applies to command handlers, not query handlers [docs/setup.md:262]

## Dev Notes

Story 5.3 is a runtime/API story. The command side already has substantial implementation and tests; the query side appears to be a deliberate v2 gap. Implement narrowly: preserve the existing command pipeline and durable send semantics, add a separate query boundary, and avoid turning Bondstone into a generic mediator, request bus, workflow engine, or application pipeline framework.

### Current State Intelligence

Command execution already has the intended basic shape:

- `src/Bondstone/Modules/Contracts/IModuleCommandExecutor.cs` exposes immediate module command execution overloads for `ICommand`, `ICommand<TResult>`, object commands, and durable receive-context execution.
- `src/Bondstone/Modules/Execution/ModuleCommandExecutor.cs` resolves a command route by module plus command type, validates the local execution boundary, invokes the route, and returns `ModuleCommandExecutionResult` or `ModuleCommandExecutionResult<TResult>`.
- `ModuleCommandExecutor.ValidateLocalExecutionBoundary(...)` currently allows same-module nested execution and rejects cross-module immediate command execution from inside a running module handler with guidance to send a durable command or publish an integration event.
- `src/Bondstone/Modules/Execution/ModuleCommandRuntime.cs` owns the module command pipeline: provider transaction runners, durable operation completion for receive commands with operation ids, durable inbox handler execution when a receive context is present, current module execution context, command validators, handler execution, and post-handler actions.
- `src/Bondstone/Modules/Routing/ModuleCommandRouteRegistry.cs` records command routes by normalized module plus command type and durable command routes by normalized module plus stable durable message identity.
- `src/Bondstone/Modules/Routing/ModuleCommandRoute.cs` requires `ICommand`, preserves optional durable command identity and stable handler identity, invokes handlers through `IModuleCommandRuntime`, and serializes result payloads for durable receive operation completion.

Durable send already separates accepted work from handler results:

- `src/Bondstone/Messaging/Contracts/IDurableCommandSender.cs` documents that send stages a durable command in the current module's outbox and returns accepted send metadata.
- `src/Bondstone/Messaging/Sending/DurableCommandSender.cs` requires a current module execution context, uses it as source module, serializes the command, writes a `DurableMessageEnvelope` to the source module outbox, optionally writes `Pending` operation state, and returns `DurableCommandSendResult`.
- `src/Bondstone/Messaging/Sending/DurableCommandSendResult.cs` carries `SendId`, optional `DurableOperationId`, optional `DurableOperationHandle`, `SourceModule`, `TargetModule`, and `Status`. It does not carry target handler results.
- `docs/setup.md` already states that `ExecuteResultAsync` is local and in-process, while durable send is not RPC and results are observed through `IDurableOperationResultReader`.
- `docs/operations.md` already frames operation results as the read model for accepted durable work, not as durable send return values.

Query execution is not currently implemented in the core package:

- No `IQuery`, `IQueryHandler`, `IModuleQueryExecutor`, query route registry, or `BondstoneModuleBuilder.Queries` surface was found under `src/Bondstone`.
- No query-focused test class was found under `tests/Bondstone.Tests` or `tests/Bondstone.Composition.Tests`.
- The architecture explicitly calls for a separate module query boundary for immediate same-process reads that respect module registration and persistence ownership while writing no durable inbox, outbox, operation, domain-event, or integration-event state.

### Architecture Compliance

Follow these non-negotiable rules:

- Bondstone remains a durable module-boundary library/framework, not a generic mediator, generic bus, workflow engine, saga/process-manager framework, code generator, SaaS framework, application platform, or broker runtime owner.
- `IModuleCommandExecutor` is the immediate same-process command boundary. It must execute registered typed command handlers through Bondstone's module command pipeline.
- Cross-module state-changing work that must survive restart or service extraction uses durable commands or integration events. Same-process immediate execution is only for explicit local module-command use and must not be disguised as restart-safe orchestration.
- Durable command send accepts work and returns metadata. It does not return target handler results; operation APIs observe eventual status and result payloads.
- Query execution is a separate immediate read boundary. It must not write durable inbox rows, outbox rows, operation state, domain-event records, or integration events.
- Durable message identities, handler identities, subscriber identities, and domain-event identities are stable and explicit. Do not derive durable identities from CLR type names.
- Public/protected API changes are compatibility-sensitive. New query public contracts should be intentional user application contracts or advanced composition APIs and should be reflected in public API baselines.
- Do not add production `InternalsVisibleTo`; runtime packages collaborate through explicit contracts or package-local implementation.

### Files To Read Or Update

Already-read UPDATE files whose behavior must be preserved:

- `src/Bondstone/Modules/Contracts/IModuleCommandExecutor.cs` - public immediate command executor contract.
- `src/Bondstone/Modules/Execution/ModuleCommandExecutor.cs` - route lookup, local execution boundary validation, typed result execution, and command route invocation.
- `src/Bondstone/Modules/Execution/ModuleCommandRuntime.cs` - command transaction, validation, receive inbox, operation completion, execution context, and post-handler sequence.
- `src/Bondstone/Modules/Routing/ModuleCommandRoute.cs` - route metadata, durable identity validation, handler invocation, result payload capture.
- `src/Bondstone/Modules/Routing/ModuleCommandRouteRegistry.cs` - missing-route and duplicate-route diagnostics.
- `src/Bondstone/Modules/Registration/BondstoneModuleBuilder.cs` - module-owned registration surface; likely gains a `Queries` builder.
- `src/Bondstone/Modules/Registration/BondstoneModuleCommandBuilder.cs` - command registration pattern to mirror where appropriate without reusing command-specific durable identity behavior for queries.
- `src/Bondstone/Configuration/BondstoneServiceCollectionExtensions.cs` - default service registration and `AddBondstone` composition; likely registers query executor/registry/runtime.
- `src/Bondstone/Configuration/BondstoneBuilder.cs` - module builder construction and validation context.
- `src/Bondstone/Messaging/Contracts/IDurableCommandSender.cs`, `src/Bondstone/Messaging/Sending/DurableCommandSender.cs`, and `src/Bondstone/Messaging/Sending/DurableCommandSendResult.cs` - durable send metadata contract to preserve.
- `src/Bondstone/Messaging/Publishing/DurableEventPublisher.cs` - current module execution context use; make sure query execution does not accidentally enable durable publish.
- `src/Bondstone/Modules/Execution/ModuleExecutionContext.cs` and `ModuleExecutionContextAccessor.cs` - current module context is broad today; do not let query execution turn this into durable write permission by accident.

Likely NEW files if adding the query boundary:

- `src/Bondstone/Messaging/Contracts/IQuery.cs`
- `src/Bondstone/Modules/Contracts/IQueryHandler.cs`
- `src/Bondstone/Modules/Contracts/IModuleQueryExecutor.cs`
- `src/Bondstone/Modules/Contracts/IModuleQueryRouteRegistry.cs`
- `src/Bondstone/Modules/Registration/BondstoneModuleQueryBuilder.cs`
- `src/Bondstone/Modules/Routing/ModuleQueryRoute.cs`
- `src/Bondstone/Modules/Routing/ModuleQueryRouteRegistry.cs`
- `src/Bondstone/Modules/Execution/ModuleQueryExecutor.cs`
- `src/Bondstone/Modules/Execution/ModuleQueryExecutionResult.cs` only if a result wrapper is needed; otherwise prefer returning `TResult` directly if that best fits the API.
- `tests/Bondstone.Tests/Modules/ModuleQueryRegistrationTests.cs`

Likely docs/public API files:

- `docs/setup.md`
- `docs/package-discovery.md`
- `docs/public-api.md`
- `src/Bondstone/README.md`
- Public API baseline files under `tests/Bondstone.PublicApi.Tests` if new public/protected APIs are introduced.

Do not edit unrelated transport, hosting, EF provider, sample, package metadata, or generated artifact files unless the implementation proves they are directly affected.

### Testing Requirements

Use the repository testing policy from `docs/testing.md`:

- `Unit` tests for core command/query route registration, route lookup, diagnostics, durable send metadata, query no-write behavior, and public contract behavior with no external infrastructure.
- `Application` tests only when service-provider composition across package boundaries is the behavior under test.
- `Integration` only for PostgreSQL, RabbitMQ, Azure Service Bus, durable inbox/outbox provider semantics, broker settlement, retry, or sample smoke behavior.
- EF Core InMemory is not proof of relational durability or transaction semantics. Query no-write tests can use in-repository fakes or spies for outbox writers, inbox executors, operation stores, event publishers, and post-handler actions.

High-value tests to add or preserve:

- Command executor still runs validators before handlers and sets/restores command module execution context.
- Same-module nested immediate command execution still succeeds.
- Cross-module immediate command execution from inside a handler still fails with diagnostics that name source module, target module, and durable command/integration event guidance.
- Durable send still returns `DurableCommandSendResult` metadata and never target handler result.
- Missing command routes still diagnose normalized module plus command type or durable message identity.
- Query registration records module-owned query routes without durable message identities.
- Query execution returns the handler result and does not invoke command validators, receive inbox handling, operation state writes, outbox writes, domain-event post-handler actions, or integration-event publish/staging.
- Query handler attempts to use `IDurableCommandSender` or `IDurableEventPublisher` fail with a clear diagnostic because query execution is read-only.
- Missing query route diagnostics name normalized module plus query type and do not mention generated durable identities.

### Previous Story Intelligence

Story 5.2 completed module registration and host composition guardrails. Carry these learnings forward:

- The existing registration model already preserves module-owned `IBondstoneModule.Configure(BondstoneModuleBuilder module)`, module-scoped `UseDurableMessaging()`, persistence binding, command handlers, validators, published events, and subscriber handlers.
- Remote contracts remain identity-only through `BondstoneBuilder.RegisterMessage<TMessage>()` and assembly registration. Identity-only registration must not imply a local route, handler, subscriber, module binding, or receive binding.
- Stable diagnostic assertions should use durable substrings rather than brittle full exception messages.
- The review fix split metadata and explicit-identity durable command registration helpers in `BondstoneModuleCommandBuilder`; do not regress the explicit null identity path.
- No public API baselines were updated in 5.2. If 5.3 adds query public contracts, baseline review is expected.

Story 5.1 also established that runtime stories must record product-boundary evidence: preserve durable module boundaries, service-extraction continuity, stable message/handler identities, handler patterns, inbox/outbox semantics, operation observation, module-owned durability, and host-owned broker topology.

### Git Intelligence

Recent commits:

- `ecd150a fix: durable messages registration`
- `9dd9da7 docs: readme and agents doc refine`
- `9bc4667 docs: metadata fix`
- `bb2c102 docs: bmad native docs`
- `783b260 docs: more bmad documents refactoring`

The latest commit aligns with Story 5.2's registration diagnostic fix. Keep this story narrow and compatible with that direction: focused runtime/API changes, focused tests, no broad package churn, no hidden transport/runtime ownership.

### Latest Technical Information

External research does not require a dependency upgrade for this story. Use the repository-pinned stack: `net10.0`, .NET SDK `10.0.108`, EF Core `10.0.8`, Npgsql `10.0.3`, and `Npgsql.EntityFrameworkCore.PostgreSQL` `10.0.2`.

Relevant current guidance from official Microsoft docs:

- The .NET DI model registers services in `IServiceCollection` during startup and builds an `IServiceProvider` after registration. Keep query services in the existing `AddBondstone` registration/composition model; do not build an internal provider during setup.
- ASP.NET Core DI guidance continues to recommend grouped `Add{GroupName}` extension methods for related framework features. Keep the public composition style consistent with `AddBondstone` and module-owned builder extensions.
- EF Core 10 is the repo's pinned EF line and Microsoft documents EF Core 10 as aligned with .NET 10. Query no-write behavior should not require provider upgrades.

Sources:

- https://learn.microsoft.com/en-us/dotnet/core/extensions/dependency-injection/overview
- https://learn.microsoft.com/en-us/aspnet/core/fundamentals/dependency-injection?view=aspnetcore-10.0
- https://learn.microsoft.com/en-us/ef/core/what-is-new/ef-core-10.0/whatsnew

### Project Context Reference

Project context says Bondstone is a library/framework for durable module boundaries, not a product app, SaaS framework, workflow engine, or general-purpose bus. It also says `IModuleCommandExecutor` is the immediate module command boundary, durable command send returns metadata while results are observed through operation APIs, commands/integration events/domain events remain distinct, durable identities are stable and explicit, transport adapters are thin native-driver envelope adapters, and runtime packages must collaborate through explicit contracts or package-local implementation rather than production `InternalsVisibleTo`.

### Open Questions

None blocking. One design choice should be made explicitly during implementation: whether query execution exposes module identity to handlers at all. If it does, avoid using the existing broad `ModuleExecutionContext` in a way that makes durable send/publish possible from query handlers.

### Story Completion Status

Ultimate context engine analysis completed - comprehensive developer guide created.

### References

- `_bmad-output/planning-artifacts/epics.md` - Epic 5 and Story 5.3 acceptance criteria
- `_bmad-output/planning-artifacts/prds/prd-Bondstone-2026-06-18/prd.md` - FR4.3, FR4.4, FR4.5, FR5.2, FR8.3, FR9, and FR10
- `_bmad-output/planning-artifacts/architecture.md` - Module Command Pipeline, Module Query Pipeline, Durable Commands, Operation Observation, Public API And Compatibility, Verification Strategy
- `_bmad-output/project-context.md` - runtime, code, testing, and workflow guardrails
- `docs/setup.md` - result-returning module commands, durable send metadata, operation result observation, and sending command guidance
- `docs/operations.md` - operation results and pending operation troubleshooting
- `docs/public-api.md` - current public API classification
- `docs/testing.md` - test categories and verification surface
- `_bmad-output/implementation-artifacts/5-1-product-boundary-and-extraction-guardrail.md` - product-boundary evidence pattern
- `_bmad-output/implementation-artifacts/5-2-module-registration-and-host-composition.md` - previous same-epic story learnings and registration diagnostics

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- `dotnet test tests/Bondstone.Tests/Bondstone.Tests.csproj --configuration Release --filter "FullyQualifiedName~ModuleQueryRegistrationTests"` failed red before query contracts existed, then passed after implementation.
- `dotnet test tests/Bondstone.Tests/Bondstone.Tests.csproj --configuration Release --filter "FullyQualifiedName~ModuleCommandRegistrationTests|FullyQualifiedName~DurableCommandSenderTests"` passed.
- `dotnet test tests/Bondstone.PublicApi.Tests/Bondstone.PublicApi.Tests.csproj --configuration Release --filter "FullyQualifiedName~PublicApiBaselineTests"` failed before baseline refresh, then passed with `BONDSTONE_UPDATE_PUBLIC_API_BASELINE=1`.
- `pnpm format:check` passed after formatting `docs/package-discovery.md`.
- `pnpm backend:build` passed.
- `pnpm backend:test` passed.
- `pnpm backend:pack` passed.
- `pnpm check` passed.

### Implementation Plan

- Preserve the command executor and durable sender pipeline without broadening command execution into a mediator or adding RPC-style durable send result paths.
- Add query contracts and module-owned registration in the core package, but keep query routes separate from command routes and durable message identities.
- Execute queries by resolving the registered handler directly without pushing `ModuleExecutionContext`, so durable send/publish remain unavailable from query handlers.
- Update consumer docs and public API evidence for the additive query surface.

### Completion Notes List

- Inventory confirmed `IModuleCommandExecutor` remains Bondstone's immediate module command boundary: route lookup, local boundary validation, module execution context, validators, handler execution, receive inbox handling, operation completion, and post-handler actions remain in the existing command pipeline.
- Durable command send remains accepted-work metadata only. `IDurableCommandSender.SendAsync` still returns `DurableCommandSendResult`/operation metadata and no target handler result path was added.
- Added immediate read-only query boundary in `Bondstone`: `IQuery<TResult>`, `IQueryHandler<TQuery, TResult>`, `IModuleQueryExecutor`, `IModuleQueryRouteRegistry`, `BondstoneModuleQueryBuilder`, `ModuleQueryRoute`, and `ModuleQueryRouteRegistry`.
- Added `BondstoneModuleBuilder.Queries` and default `AddBondstone` registration for `IModuleQueryExecutor`; query route registration does not reuse command routes, command validators, receive contexts, operation completion, or durable message identities.
- Query execution does not set `ModuleExecutionContext`; tests verify query handlers attempting `IDurableCommandSender` or `IDurableEventPublisher` fail before writing outbox rows.
- Updated setup/package discovery/core README/public API docs and refreshed the Bondstone public API baseline for additive query contracts.

### File List

- `_bmad-output/implementation-artifacts/5-3-immediate-command-and-query-boundaries.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `docs/package-discovery.md`
- `docs/public-api.md`
- `docs/setup.md`
- `src/Bondstone/Configuration/BondstoneBuilder.cs`
- `src/Bondstone/Configuration/BondstoneServiceCollectionExtensions.cs`
- `src/Bondstone/Messaging/Contracts/IQuery.cs`
- `src/Bondstone/Modules/Contracts/IModuleQueryExecutor.cs`
- `src/Bondstone/Modules/Contracts/IModuleQueryRouteRegistry.cs`
- `src/Bondstone/Modules/Contracts/IQueryHandler.cs`
- `src/Bondstone/Modules/Execution/ModuleQueryExecutor.cs`
- `src/Bondstone/Modules/Registration/BondstoneModuleBuilder.cs`
- `src/Bondstone/Modules/Registration/BondstoneModuleQueryBuilder.cs`
- `src/Bondstone/Modules/Routing/ModuleQueryRoute.cs`
- `src/Bondstone/Modules/Routing/ModuleQueryRouteRegistry.cs`
- `src/Bondstone/README.md`
- `tests/Bondstone.PublicApi.Tests/Baselines/Bondstone.txt`
- `tests/Bondstone.Tests/Modules/ModuleQueryRegistrationTests.cs`

## Change Log

- 2026-06-19: Implemented immediate read-only query boundary, updated docs and public API baseline, and verified command/durable-send semantics remained intact.
