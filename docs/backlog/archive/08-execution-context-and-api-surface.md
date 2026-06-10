# Execution Context And API Surface

Archived: 2026-06-10

## Outcome

[ADR 0045](../../adr/0045-module-execution-context-semantics.md) accepted the
current ambient module execution context semantics.

Durable command sending and integration event publishing remain scoped to
module command and event subscriber execution. The source module comes from
the current module execution context pushed by Bondstone system pipeline
behaviors. The durable send/publish APIs do not currently expose a public
source-module override, module-scoped client, or general background-work
entrypoint.

[ADR 0046](../../adr/0046-public-api-surface-policy.md) accepted a
compatibility-first public API policy and left implementation cleanup to the
public API cleanup track. No public type was hidden, removed, or renamed in
this resolution.

## Review Summary

- `ModuleExecutionContextAccessor` remains an `AsyncLocal`-backed singleton
  owned by `AddBondstone`.
- Command and event subscriber execution context pipeline behaviors set the
  current module for handler execution and restore the previous context after
  the pipeline completes.
- `IDurableCommandSender` and `IDurableEventPublisher` fail fast without a
  current module execution context.
- HTTP endpoints and custom app-owned entrypoints should execute registered
  module commands through `IModuleCommandExecutor` when they need module
  handler semantics and handler-scoped durable send/publish.
- RabbitMQ and Service Bus receive dispatchers are the current lower-level
  receive processing services for hosted workers and app-owned consumers.
- RabbitMQ and Service Bus settlement handler helpers remain provider-native
  acknowledgement/completion ordering conveniences over those dispatchers.
- The public API surface is broad. Cleanup requires inventory and
  compatibility planning before public implementation types are hidden,
  renamed, or removed.

## Deferred Work

- Public API inventory, baseline/process design, and concrete type
  classification continue in
  [../10-public-api-and-composition-cleanup.md](../10-public-api-and-composition-cleanup.md).
- Receive helper naming or abstraction changes continue in
  [../12-transport-and-hosting-ergonomics.md](../12-transport-and-hosting-ergonomics.md)
  only if custom receive loops show the current dispatcher/handler split is
  awkward.
- HTTP/custom entrypoint adoption guidance continues in
  [../13-real-project-readiness.md](../13-real-project-readiness.md).

## Safe Cleanup

Removed an unused `Microsoft.Extensions.DependencyInjection` import from the
core command receive inbox pipeline behavior. This does not change public API
or durable behavior.

## Verification

Verified with:

- `pnpm format:check`
- `pnpm backend:build`
- `pnpm backend:test:fast`

`pnpm backend:pack` was not required because no public API or package surface
changed.
