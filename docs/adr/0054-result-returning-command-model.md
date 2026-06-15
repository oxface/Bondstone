# 0054 Result-Returning Command Model

Status: Amended
Application: Applied
Date: 2026-06-13

## Context

Bondstone currently has a module command execution boundary for commands that
do not return a typed application result. The public command model is:

- `ICommand` marks commands executed through a module command pipeline.
- `IDurableCommand` extends `ICommand` for asynchronous outbox delivery and
  transport receive.
- `ICommandHandler<TCommand>` handles a command and returns `ValueTask`.
- `IModuleCommandPipelineBehavior<TCommand>` and
  `ModuleCommandPipelineNext` are void-shaped.
- `IModuleCommandExecutor` returns `ModuleCommandExecutionResult`, which
  currently carries receive inbox metadata and not an application result.
- Durable send APIs return `DurableCommandSendResult` with a send id, optional
  durable operation id, and accepted status.
- The neutral command receive pipeline deserializes durable envelopes, executes
  the registered module command route, and returns a `DurableInboxHandleResult`.
- Durable operation state has an optional `ResultPayload`, but ADR 0031
  intentionally deferred operation result payload semantics, polling APIs,
  result deserialization, retry state, failure state, and cancellation policy.

This leaves a gap for module application workflows that need Bondstone's module
pipeline, module execution context, validation, persistence behavior, and
durable send/publish ergonomics while also returning an immediate value to the
caller. Examples include synchronizing a current user and returning context,
granting initial admin access and returning a domain outcome, and create/update
flows that return identifiers or accepted-operation metadata.

Keeping those workflows in plain application services avoids public API churn,
but it also bypasses the module command pipeline and pushes applications toward
a parallel in-process command framework. Turning durable commands into direct
request/response calls would solve the wrong problem: durable execution crosses
outbox, transport, inbox, retry, and persistence boundaries where direct RPC
semantics are misleading.

The design must therefore keep one command abstraction while separating the
way a result is fulfilled:

- local in-process execution returns the result directly; and
- durable execution returns accepted operation metadata, then exposes the
  committed result through explicit operation-state observation.

The change affects public API shape, durable command semantics, package
boundaries, compatibility, and public API baselines.

## Decision

Bondstone will add first-class result-returning module command contracts
without splitting the application command abstraction into separate
in-process and durable result models.

The public command shape is:

- `ICommand<TResult>` extends `ICommand` and marks a command that produces a
  `TResult`.
- `ICommandHandler<TCommand, TResult>` handles a result command and returns
  `ValueTask<TResult>`.
- Module command registration supports result-returning handlers beside the
  existing void handlers.
- The module command executor exposes an explicit typed result execution API
  that returns both `TResult` and any execution metadata that remains relevant
  to the command path.

Durable result commands use the same result command abstraction. A command can
be durable by implementing `IDurableCommand` and result-producing by
implementing `ICommand<TResult>`. Bondstone does not need a separate
`IDurableCommand<TResult>` marker in the first slice.

Result-returning commands must execute through the same module boundary
semantics as existing module commands:

- route lookup is module-owned;
- validators registered for the executing module run before the handler;
- selected system and capability pipeline contributions wrap application
  behavior and handler execution;
- application command pipeline behavior remains the supported extension point;
- the current module execution context is available during behavior and
  handler execution, so durable sends and event publishes still use the
  executing module as the source module;
- provider transaction behavior can commit handler state, outbox rows,
  operation state, inbox markers when applicable, and other selected runtime
  work around the result handler.

The first implementation keeps application pipeline behavior void-shaped:
existing `IModuleCommandPipelineBehavior<TCommand>` instances wrap the result
handler and preserve ordering, execution context, validation, transaction,
inbox, operation-state, and capability behavior. Result-specific pipeline
behavior remains deferred until concrete application behavior needs to inspect
or transform `TResult`.

Local in-process execution of `ICommand<TResult>` returns `TResult` directly.

Durable execution must not return `TResult` from send or transport receive.
Durable sends continue to return an accepted command result/handle. When a
durable result command is sent with a durable operation id, target-module
receive executes the same result handler, serializes the committed `TResult`
with Bondstone's durable payload JSON options, and stores it on the completed
operation state result payload. Callers observe durable results by reading or
polling operation state through `IDurableOperationReader` and deserializing the
payload when the state is `Completed`.

The initial durable result observation model is intentionally small:

- callers create or supply the durable operation id for result observation;
- the sender still stages `Pending` in the source module store when an
  operation id is supplied;
- the target module receive transaction stores `Completed` plus the serialized
  result payload only after the handler and surrounding pipeline succeed;
- `IDurableOperationReader` remains the polling/read primitive; and
- callers own polling cadence, timeout policy, and typed deserialization for
  now.

Generated operation ids, typed operation result readers, operation-specific
result content types, result payload versioning beyond the command/result
contract, failure/cancellation policy, timeout policy, and provider-specific
operation concurrency remain later decisions. None of those future APIs may
imply send-and-wait durable RPC.

## Amendment 2026-06-13: Typed Durable Operation Result Reader

The initial implementation also includes a typed operation result reader so
callers do not have to hand-roll result payload deserialization and polling.

Wolverine demonstrates a unified handler abstraction with
`InvokeAsync<TResult>()`, including remote request/reply with timeout behavior
when enabled. Brighter takes a stricter command-query-separation posture:
commands do not return values, and outcomes are communicated through events,
queries, or command fields used like out parameters.

Bondstone keeps the unified command/result handler abstraction, but it must
not hide durable transport execution behind direct request/reply semantics.
The compatible Bondstone shape is:

- local `ExecuteResultAsync` returns `TResult` directly;
- durable send still returns accepted operation metadata;
- `IDurableOperationResultReader.GetResultAsync<TResult>()` reads current
  operation state once and deserializes a completed result payload when
  available;
- `IDurableOperationResultReader.WaitForResultAsync<TResult>()` performs
  explicit timeout-bounded polling until the operation reaches a terminal
  state; and
- callers still own operation id creation, timeout choice, polling interval,
  and endpoint/API policy.

This narrows the earlier deferral for typed operation result readers. Generated
operation ids, result content-type/version metadata, failure/cancellation
policy beyond stored terminal state, default timeout policy, and
provider-specific operation concurrency remain deferred.

## Amendment 2026-06-15: Durable Result Diagnostic Context

The typed durable operation result reader also needs enough stored context to
produce useful diagnostics when a durable result is unavailable or a completed
result payload cannot be deserialized as the requested type.

Bondstone will carry optional diagnostic context on durable operation state:

- module name;
- durable message type name; and
- handler identity.

The target module receive path stores this context when a durable result
command completes with a durable operation id. The context is diagnostic only:
it does not create durable request/reply semantics, does not make send wait
for a result, and does not change the caller-owned polling, timeout, or
endpoint policy.

Operation states created before this amendment, manually-created operation
states, and provider rows that have not been migrated may not include this
context. Result diagnostics must therefore remain useful with only the
operation id and requested result type.

## Consequences

Module application workflows can use Bondstone's existing module command
boundary and return typed values without requiring a separate mediator or
application command framework for ordinary same-process work.

The public API grows in a compatibility-sensitive area. Baseline changes,
stable docs, and release notes will be required when implementation starts.

The module command runtime needs a result-aware execution path. Existing void
handlers and behaviors must continue to work, and ordering must remain stable
for validation, execution context, operation state, receive inbox, persistence,
domain-event capability behavior, and application behavior.

Durable commands keep asynchronous accepted-work semantics. Consumers that need
a durable outcome get an operation handle/status model rather than a direct
RPC result. This preserves service-extraction continuity and avoids implying
that transport dispatch, broker retry, remote handler execution, and target
module commit are part of one synchronous call.

The first implementation slice is narrow:

- add and test unified result command contracts and executor behavior;
- serialize result payloads into completed durable operation state for
  operation-tracked durable result commands;
- document polling through `IDurableOperationReader` as the initial durable
  result observation strategy;
- update public API baselines for intentional public additions.

Generated operation ids, richer failure/cancellation policy, default timeout
policy, and provider-specific operation concurrency remain separate follow-up
decisions.

## Related Decisions

- [0004 Positioning And Service Extraction Path](0004-positioning-and-service-extraction-path.md)
- [0025 Module Command Execution Boundary](0025-module-command-execution-boundary.md)
- [0031 Durable Operation State Integration](0031-durable-operation-state-integration.md)
- [0032 Module-Owned Durable EF Persistence](0032-module-owned-durable-ef-persistence.md)
- [0045 Module Execution Context Semantics](0045-module-execution-context-semantics.md)
- [0046 Public API Surface Policy](0046-public-api-surface-policy.md)

## Application Notes

- Current contract: Bondstone supports a unified result-returning command
  model through `ICommand<TResult>` and
  `ICommandHandler<TCommand, TResult>`. Local module execution returns typed
  results through `IModuleCommandExecutor.ExecuteResultAsync`. Durable result
  commands keep accepted-work send semantics and expose committed result
  payloads through operation state when an operation id is supplied.
  `IDurableOperationResultReader` provides current-state reads and
  timeout-bounded polling over those operation result payloads. Completed
  durable result command receives also store optional operation diagnostic
  context: module name, durable message type name, and handler identity.
- Stable docs: Current behavior is described in
  [docs/architecture/modules.md](../architecture/modules.md),
  [docs/architecture/messaging.md](../architecture/messaging.md),
  [docs/public-api.md](../public-api.md), and
  [docs/packaging.md](../packaging.md).
- Agent guidance: Root [AGENTS.md](../../AGENTS.md) already requires ADR
  review before public API, durable behavior, package-boundary, or
  compatibility-sensitive changes. No additional agent instruction is needed
  for this applied slice.
- Application evidence: Core command contracts, module command registration,
  assembly scanning, route metadata, executor behavior, result payload
  serialization, durable receive operation-state completion with diagnostic
  context, typed durable operation result reading, timeout-bounded polling,
  stable docs, focused core and persistence tests, and the public API baseline
  are updated.
- Pending or deferred: Generated operation ids, richer failure/cancellation
  policy, default timeout policy, result content-type/version metadata,
  result-specific pipeline behavior, and provider-specific operation
  concurrency remain deferred.

## Verification

Read back the issue, project status, repository docs, relevant architecture
docs, public API docs, packaging docs, current command/executor source,
receive pipeline source, durable sender source, operation-state contracts,
public API baselines, and core command tests. No executable behavior changed in
this proposed ADR slice.

Verified the unchanged core command surface with:

- `dotnet test tests/Bondstone.Tests/Bondstone.Tests.csproj --configuration Release --filter "Category=Unit"`
- `git diff --check`

Applied the accepted result-command model and verified with:

- `dotnet test tests/Bondstone.Tests/Bondstone.Tests.csproj --configuration Release --filter "Category=Unit"`
- `BONDSTONE_UPDATE_PUBLIC_API_BASELINE=1 dotnet test tests/Bondstone.PublicApi.Tests/Bondstone.PublicApi.Tests.csproj --configuration Release`
- `dotnet test tests/Bondstone.PublicApi.Tests/Bondstone.PublicApi.Tests.csproj --configuration Release`
- `git diff --check`
- `pnpm check`

Reviewed prior-art docs:

- [Wolverine message bus request/reply](https://wolverinefx.net/guide/messaging/message-bus.html)
- [Brighter returning results from a handler](https://brightercommand.gitbook.io/paramore-brighter-documentation/brighter-request-handlers-and-middleware-pipelines/returningresultsfromahandler)
- [Brighter basic concepts](https://brightercommand.gitbook.io/paramore-brighter-documentation/overview/basicconcepts)

Applied the durable result diagnostic context amendment and verified with:

- `dotnet test tests/Bondstone.Tests/Bondstone.Tests.csproj --configuration Release --filter "FullyQualifiedName~DurableOperationResultReaderTests|FullyQualifiedName~ModuleReceivePipelineTests|FullyQualifiedName~DurableOperationStateTests"`
- `dotnet test tests/Bondstone.Persistence.EntityFrameworkCore.Tests/Bondstone.Persistence.EntityFrameworkCore.Tests.csproj --configuration Release --filter "FullyQualifiedName~OperationStateEntityTests|FullyQualifiedName~EntityFrameworkCoreDurableOperationStateStoreTests|FullyQualifiedName~BondstoneModelBuilderExtensionsTests"`
- `dotnet test tests/Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests/Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests.csproj --configuration Release --filter "FullyQualifiedName~OperationStateStore_WhenStateIsSavedAgain_UpdatesExistingState"`
- `dotnet test tests/Bondstone.Persistence.Postgres.Tests/Bondstone.Persistence.Postgres.Tests.csproj --configuration Release --filter "FullyQualifiedName~PostgresPersistenceTests"`
- `BONDSTONE_UPDATE_PUBLIC_API_BASELINE=1 dotnet test tests/Bondstone.PublicApi.Tests/Bondstone.PublicApi.Tests.csproj --configuration Release`
- `dotnet test tests/Bondstone.PublicApi.Tests/Bondstone.PublicApi.Tests.csproj --configuration Release`
- `git diff --check`
- `pnpm backend:build`
- `pnpm check`
