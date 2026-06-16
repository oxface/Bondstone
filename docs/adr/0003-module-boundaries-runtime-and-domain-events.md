# 0003 Module Boundaries Runtime And Domain Events

Status: Accepted
Application: Applied
Date: 2026-06-16

## Context

Bondstone targets modular monoliths first. The core safety rule is that
modules own their data, handlers, durable contracts, and persistence
boundaries. Cross-module writes must not look like ordinary in-process method
calls when they can commit another module's state before the current module
has committed.

The earlier generalized module pipeline and capability machinery made the
runtime look like a mini framework. Post-MVP simplification removed that
machinery and made execution direct.

Domain events increased in value after transport simplification, but only as
module-local facts. Treating them as public transport messages would blur the
same boundary Bondstone is trying to make explicit.

## Decision

Modules should not reference other module implementation assemblies. Shared
contracts may live in explicit `.Contracts` assemblies where a boundary is
intentional.

App-owned entrypoints such as HTTP endpoints, schedulers, administrative jobs,
tests, and setup flows may execute one module command locally through
`IModuleCommandExecutor`.

A running module handler may execute another command in the same module, but
must not synchronously execute a different module's command. Cross-module
work uses durable commands, integration events, projections, explicit public
read APIs, or app-owned orchestration.

Module command and event subscriber execution use direct internal
orchestration. Bondstone does not expose an application middleware pipeline,
public runtime contribution records, named runtime slots, or capability
pipeline ordering.

The fixed command execution flow is:

1. provider transaction runners;
2. durable operation completion for receive execution when an operation id
   exists;
3. receive inbox handling when a receive context exists;
4. module execution context;
5. command validation;
6. registered handler;
7. provider post-handler actions before transaction save/commit.

The fixed event subscriber execution flow is:

1. provider transaction runners;
2. receive inbox handling when a receive context exists;
3. module execution context;
4. registered subscriber handler;
5. provider post-handler actions before transaction save/commit.

`ICommand<TResult>` remains the local result-command contract.
`IDurableCommand` remains the durable command marker. A durable result command
implements both `IDurableCommand` and `ICommand<TResult>`.

Domain event contracts live in `Bondstone.DomainEvents` in the core package.
Domain events are module-local facts. They are not integration events,
transport messages, durable command messages, or automatically dispatched
handler contracts. Mapping a domain event to an integration event is explicit
module code.

EF-backed domain event persistence is an explicit module opt-in in
`Bondstone.Persistence.EntityFrameworkCore`. It records module-local domain
events after a successful EF-backed module handler and clears sources only
after an observed commit.

## Consequences

The runtime is easier to understand and debug. Bondstone keeps the runtime
hooks it actually needs for EF transactions and EF domain event persistence,
but does not offer a general app pipeline.

Application concerns such as authorization, auditing, logging, metrics, and
policy should use ordinary handler code, DI decorators, endpoint filters, host
middleware, or frameworks chosen by the consumer app.

Domain events provide local recording pressure without becoming a second
message bus.

## Related Decisions

- Supersedes the active module-runtime and domain-event direction from the
  pre-restart ADR sequence summarized by
  [0001](0001-restart-adr-history-around-current-baseline.md) and pruned by
  [0009](0009-prune-pre-restart-archive-and-planning-notes.md).

## Application Notes

- Current contract: module execution and domain event behavior are documented
  in [docs/architecture/modules.md](../architecture/modules.md) and
  [docs/architecture/messaging.md](../architecture/messaging.md).
- Stable docs: EF domain event persistence is documented in
  [docs/architecture/persistence-ef-core.md](../architecture/persistence-ef-core.md).
- Agent guidance: root [AGENTS.md](../../AGENTS.md) requires ADR review before
  runtime, durable behavior, public API, or provider changes.
- Application evidence: public pipeline behavior/contribution APIs were
  removed; direct command/event subscriber executors and EF post-handler action
  support are applied; domain event contracts live in core. The generic
  runtime feature collection and transaction feature were removed; EF
  transaction/domain-event coordination now uses the narrow observed-transaction
  callback surface on `IModuleRuntimeExecutionContext`.
- Pending or deferred: none for the current runtime simplification baseline.

## Verification

Read current module, messaging, EF persistence, package discovery, and public
API docs. Runtime behavior is covered by unit, application, EF, public API,
and sample integration tests.
