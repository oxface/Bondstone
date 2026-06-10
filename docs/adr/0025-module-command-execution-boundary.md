# 0025 Module Command Execution Boundary

Status: Amended
Application: Applied
Date: 2026-06-06

## Context

Bondstone started from modular-monolith infrastructure where persistence was
first-class: modules owned their DbContext, commands executed inside module
scope, validation and transaction behavior wrapped handlers, and outgoing
messages were staged in the same local commit. Recent extraction slices proved
the durable plumbing first: message identities, outbox/inbox persistence,
Rebus transport adapters, hosted dispatching, and low-level receive pipelines.

That plumbing is necessary but not sufficient. Without a module command
execution boundary, application code has to repeat the same ceremony around
every receive path: resolve command type, deserialize payload, validate,
enter module scope, open or join the persistence transaction, run a handler,
stage outgoing outbox messages, mark inbox processed, save, commit, and only
then let the transport acknowledge.

ADR 0004 rejected a generic mediator or message bus for ordinary in-process
module calls. That remains correct. The missing capability is narrower: a
durable module command pipeline that protects persistence, outbox, inbox,
validation, tracing, and service-extraction continuity. Reflection was also
treated cautiously during earlier slices, but the existing message registry
already supports startup assembly scanning for stable message identities.

Modules should look like nearby microservices. Some modules may be local to a
host process, some may be remote behind Rebus or another transport, and some
may not use a transport at all. Module code should describe the module's
capabilities. The host should decide which modules are loaded locally and how
remote delivery and receive endpoints are bound.

## Decision

Bondstone will introduce a module command execution boundary in `Bondstone`
core.

The boundary is command-first. It does not introduce event fan-out,
subscription ownership, or a generic mediator for arbitrary in-process calls.

Modules can be registered through the shared `AddBondstone` builder either
inline or through a module-provided abstraction:

- `IBondstoneModule` supplies a stable module name and configures a
  `BondstoneModuleBuilder`;
- `BondstoneModuleBuilder` exposes module-owned command registration;
- command registration supports explicit handler registration and startup
  assembly scanning for command handlers and validators.

Bondstone may use reflection during startup registration to discover:

- durable command handlers;
- durable command validators;
- durable command identity metadata.

Runtime execution must use prevalidated route metadata and cached closed
generic delegates rather than repeated reflection. Reflection is a
registration tool, not the hot-path dispatch model.

Command handlers implement `IDurableCommandHandler<TCommand>`. Validators
implement `IDurableCommandValidator<TCommand>`. Pipeline behavior is modeled
with `IModuleCommandPipelineBehavior<TCommand>` so validation, future module
transaction behavior, inbox handling, tracing, and other durable boundary
concerns can compose without a mediator package.

The first applied behavior is validation. Later EF/module receive behavior
will add transaction and inbox/outbox orchestration around the same executor.

Handler identity remains durable stable text. Registration may default it to
the command's stable message type name because that value is explicit
consumer-owned message identity, not a CLR-derived name. Consumers can still
override handler identity when they need a different stable inbox consumer
identity.

Host and module ownership are split:

- modules own module name, command handlers, validators, message identities,
  and module persistence capability;
- hosts own which modules are loaded in a process, which modules are local or
  remote, transport topology, Rebus endpoints, queue names, connection
  strings, workers, and operational policy.

Transport packages should bind host topology to the module command executor.
Rebus should remain a host-owned transport binding, not a thing every module
must reference or configure internally.

The existing low-level Rebus typed receive pipeline remains useful as a
transport primitive. The app-facing receive shape should eventually call the
module command executor instead of exposing handler and commit delegates to
application code.

## Amendment 2026-06-06: Command Pipeline Base Interface

The module command pipeline applies to module commands broadly, not only to
durable commands.

This amendment supersedes the original durable-only handler and validator
names in this ADR's decision text.

`Bondstone` adds `ICommand` as the base marker for commands executed through a
module command pipeline. `IDurableCommand` extends `ICommand` and keeps its
existing durable meaning: stable message identity, outbox delivery, inbox
deduplication, transport receive, and durable operation behavior.

Command handlers now implement `ICommandHandler<TCommand>`. Validators now
implement `ICommandValidator<TCommand>`. Pipeline behavior remains modeled by
`IModuleCommandPipelineBehavior<TCommand>`, now constrained to `ICommand`.

Startup reflection can discover handlers and validators for both regular
module commands and durable commands. Durable message identity registration
still applies only to `IDurableCommand`.

This keeps endpoint and module-boundary commands on the same validation,
logging, tracing, and future transaction pipeline without turning every
in-module method call into a mediator call.

## Amendment 2026-06-06: Durable Messaging And Transport Topology

Durable commands always use durable messaging semantics. If a module wants a
direct in-process call, it should use normal `.Contracts` references or a
plain `ICommand` through the local module command executor. `IDurableCommand`
send and receive should not silently degrade to direct in-process execution
because that would make service extraction change command semantics.

Bondstone should not introduce a first-class in-memory local durable queue as
part of the module command boundary. Local durable test or development
adapters can be considered later, but they are transport adapters, not the
default meaning of local module execution.

Modules that participate in durable messaging should opt into one clear
capability, such as `UseDurableMessaging`, instead of requiring normal
applications to toggle inbox and outbox separately. That capability implies
that the module needs durable send and receive infrastructure: outbox writer,
inbox registration/store, module transaction behavior, source-module scope,
and durable receive behavior. Advanced APIs may later expose separate inbox,
outbox, or operation-state pieces, but the default module shape should be one
durable-messaging capability.

Host route configuration should stay transport-topology oriented. Bondstone
can infer command handler routes from module registration and message
identity. Hosts and transport adapters provide the pieces Bondstone cannot
infer: broker connection strings, queue names, exchange/topic names, routing
keys, subscription names, listener endpoints, retry/dead-letter topology, and
which remote modules are reached through which transport.

Transport adapters should expose topology in their own vocabulary. For Rebus
this means mapping a target module to a Rebus destination address and binding
a Rebus receive endpoint to accepted local modules. For a topic-based
transport this may mean mapping a target module to an exchange/topic plus
routing key or subject. A generic module-to-module route table should remain
deferred until multiple transports or explicit policy requirements prove it
useful.

## Consequences

The durable command pipeline becomes reusable across local module execution,
transport receive adapters, tests, and future samples.

Application code can keep direct typed handlers without importing a mediator.
The call graph remains discoverable while Bondstone regains pipeline behavior
for validation, transaction wrapping, inbox/outbox commit, tracing, and
service-extraction continuity.

Startup scanning makes common registration concise. It also means Bondstone
must validate duplicate routes, missing message identities, ambiguous handlers,
and unsupported command shapes early.

Modules can stay transport-neutral. A module can run locally without Rebus, a
host can expose selected local modules through Rebus, and a host can route
commands to remote modules through Rebus or a later transport adapter.

The first applied slice does not yet provide:

- module-owned DbContext registration;
- public module unit-of-work abstraction;
- source-module ambient scope for durable command sending;
- EF transaction pipeline behavior;
- receive-side inbox behavior inside the module command pipeline;
- Rebus host topology binding to module command routes;
- event publish/subscribe or event handler fan-out.

Those pieces require later slices and, where they affect durable behavior,
additional ADR review or amendments.

## Related Decisions

- [0004 Positioning And Service Extraction Path](0004-positioning-and-service-extraction-path.md)
- [0015 Inbox Handle-Once Orchestration](0015-inbox-handle-once-orchestration.md)
- [0016 EF Core Persistence Scope](0016-ef-core-persistence-scope.md)
- [0021 Fluent Service Composition Guardrails](0021-fluent-service-composition-guardrails.md)
- [0023 Rebus Receive-Side Inbox Integration](0023-rebus-receive-side-inbox-integration.md)
- [0024 Rebus Typed Command Receive Pipeline](0024-rebus-typed-command-receive-pipeline.md)

## Application Notes

- Current contract: `Bondstone` core now has module registration, `ICommand`
  as the base module command marker, `IDurableCommand` as the durable command
  marker, command handler and validator abstractions, startup reflection
  registration for handlers and validators, cached module command routes, a
  scoped module command executor, module durable-messaging capability metadata,
  module durable sending execution context, module persistence capability
  metadata, source-module scoped durable command sending, ordered system
  pipeline behavior for Bondstone-owned runtime concerns, explicit receive
  inbox records on module command execution, execution results carrying inbox
  outcomes, receive-side inbox system behavior, and a validation pipeline
  behavior. `Bondstone.EntityFrameworkCore` now adds module-owned EF
  persistence opt-in and an EF transaction system pipeline behavior for
  modules that declare EF persistence. Rebus-specific transport application
  has been superseded by ADR 0036; current command receive dispatch is exposed
  through provider-neutral receive pipelines and the direct Local, RabbitMQ,
  and Azure Service Bus adapters.
- Stable docs: Current module command direction is described in
  [docs/architecture/modules.md](../architecture/modules.md), with supporting
  messaging, persistence, and direct transport notes in
  [docs/architecture/messaging.md](../architecture/messaging.md),
  [docs/architecture/persistence.md](../architecture/persistence.md),
  [docs/architecture/transport-local.md](../architecture/transport-local.md),
  [docs/architecture/transport-rabbitmq.md](../architecture/transport-rabbitmq.md),
  and [docs/architecture/transport-servicebus.md](../architecture/transport-servicebus.md).
- Agent guidance: Root [AGENTS.md](../../AGENTS.md) requires ADR review before
  public API, durable behavior, provider, transport, or module runtime changes.
- Application evidence: Core module command registration and executor
  implementation is applied with unit coverage for inline module command
  registration, module-provided registration, assembly scanning, validation,
  regular command execution, durable command execution, route lookup, stable
  handler identity defaulting, module execution context, source-module scoped
  durable command sending, ordered system pipeline behavior, module
  execution results, explicit receive inbox records, receive-side inbox system
  behavior, module persistence metadata, EF module persistence opt-in, EF
  command transaction/save behavior, and provider-neutral module command
  receive dispatch. Local, RabbitMQ, and Service Bus transports bind
  provider-native receive topology to the neutral command receive pipeline
  while leaving provider infrastructure configuration application-owned.
  Durable operation state is now integrated for caller-supplied operation ids
  in command send and successful module command receive. Core module
  registration now records module metadata,
  `UseDurableMessaging` capability state, and module persistence capability
  state through `IBondstoneModuleRegistry`.
- Pending or deferred: None for the module command execution boundary. Richer
  operation-state transition policy, receive retry state, stale receive
  recovery, and additional service-extraction examples remain separate future
  decisions.

## Verification

Read back affected architecture docs and ran:

- `dotnet test tests/Bondstone.Tests/Bondstone.Tests.csproj --configuration Release --filter "Category=Unit"`
- `dotnet test tests/Bondstone.EntityFrameworkCore.Tests/Bondstone.EntityFrameworkCore.Tests.csproj --configuration Release --filter "Category=Unit|Category=Application"`
- focused direct transport receive tests in later ADR slices
- `pnpm check`
