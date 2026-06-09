# Module Architecture

Bondstone modules are service-shaped units that can run close together inside
a modular monolith or later move behind a transport boundary.

## Ownership Split

Modules own their durable capabilities:

- stable module name;
- module command handlers;
- module command validators;
- message identities for commands they handle;
- integration event identities they publish;
- integration event subscriber handlers and stable subscriber identity
  metadata;
- module persistence capability;
- durable messaging capability when the module sends or receives durable
  commands or integration events;
- future module transaction, inbox, outbox, subscriber, and operation-state
  behavior.

Hosts own deployment topology and transport infrastructure:

- which modules are loaded in a process;
- which modules are local or remote;
- connection strings and environment-specific settings;
- transport adapters and target-module address maps;
- Rebus endpoint names, queue names, retry policy, and workers;
- exchange, topic, routing-key, subscription, and listener names;
- process-level hosted services and operational policy.

Module code should not need to know whether another module is local,
remote, or Rebus-backed. It can depend on stable module names and durable
message contracts. The host decides how commands reach the target module.

## Module Registration

`AddBondstone` is the host composition entrypoint. Inline `Module(...)`
registration is useful for small examples, tests, or host-owned composition.
For a real module assembly, prefer a module-owned `IBondstoneModule`
registration object and stitch it into the host with `AddModule(...)` or a
small module-specific host extension that delegates to `AddModule(...)`.

Module-owned registration keeps command handlers, validators, durable
messaging capability, provider-specific persistence opt-ins, and handler
assembly scanning close to the module implementation. The host should still
own environment-specific values such as connection strings and transport
topology, so module registration objects can accept those values through their
constructor or a thin host extension.

Handlers and validators can be discovered with `RegisterFromAssembly` or
registered explicitly with `RegisterHandler` and `RegisterValidator`. In a
module-owned assembly, prefer `RegisterFromAssemblyContaining<TMarker>()` so
the host does not need to list every handler. Keep library-user setup examples
in [../setup.md](../setup.md).

## Command Handlers

Module command handlers are direct typed `ICommandHandler<TCommand>`
implementations. Plain `ICommand` handlers and durable `IDurableCommand`
handlers use the same command pipeline; durable commands add stable durable
message identity. Validators are typed `ICommandValidator<TCommand>`
implementations and are optional.

`RegisterFromAssembly` scans at startup for command handlers, validators, and
durable message identity metadata. Runtime dispatch uses cached route metadata
and closed generic delegates.

## Command Execution

`IModuleCommandExecutor` executes an `ICommand` for a named module. The
current applied pipeline runs registered validators before the direct handler.
Bondstone-owned runtime concerns use ordered system pipeline behaviors, which
always wrap normal application pipeline behaviors. For modules that opt into
EF persistence, an EF-specific system behavior wraps validation and handler
execution in the EF persistence scope and saves changes before commit. A core
system behavior also sets the current module execution context so durable
sends can record the executing module as the source module. Another core
system behavior records durable operation completion for caller-supplied
operation ids after successful command execution. Future pipeline behavior
will add receive tracing and deeper module identity scopes.

`ICommand` should be used for module endpoint and boundary operations that
benefit from the module pipeline. Ordinary helper methods and internal domain
collaboration inside a module should remain direct method calls.

Transport adapters should enter the same command executor rather than owning
module command behavior themselves.

## Durable Messaging Capability

Plain `ICommand` execution is direct module command pipeline execution. It is
appropriate for module-owned endpoints and local application use cases that
need validation, logging, transaction behavior, or other module pipeline
features.

`IDurableCommand` represents asynchronous durable messaging. Durable command
send and receive should use inbox/outbox-backed infrastructure even when the
source and target modules happen to be deployed in the same process. Direct
in-process collaboration can use `.Contracts` references or plain `ICommand`.
When an operation changes state across two or more module-owned persistence
boundaries, prefer a durable command or event choreography over a direct call.
The modules do not share a transaction, so the durable boundary is what
preserves retry, deduplication, and service-extraction behavior.

Integration events are the durable event half of that boundary. A module may
publish a durable cross-module fact after its own state changes, and zero or
more subscriber modules may react independently. Publication should remain an
explicit module action. Module-local domain events are private facts and are
not automatically integration events.

The normal application-facing shape should be one module capability such as
`UseDurableMessaging`, not separate inbox/outbox toggles. Durable messaging
implies the module needs the persistence and pipeline pieces required for
durable send, publish, receive, and subscribe: outbox writing, inbox
registration/storage, module transaction behavior, source-module scope,
subscriber identity metadata, and durable receive orchestration. The current
implementation can stage durable event publish envelopes and record event
subscriber metadata, publish event outbox records through configured Rebus
topics, and execute registered event subscribers through a core module
subscriber executor. Rebus event receive, subscription binding, and EF
transaction composition for event handlers are applied for the current MVP
event loop. Other transport receive implementations remain follow-up slices.
Advanced APIs may later expose separate inbox, outbox, subscriber, or
operation-state pieces, but they should not be the common path.

Current core registration records module metadata and durable messaging
capability through `IBondstoneModuleRegistry`. When a module combines
`UseDurableMessaging` with EF persistence, the EF module behavior validates
that the module DbContext model includes outbox and inbox mappings before
running the command pipeline. This keeps persistence-only EF modules free to
omit durable messaging tables while durable messaging modules fail with a
specific mapping error. Operation-state mapping is required when operation
tracking is used; EF operation-state persistence fails with a clear
`ApplyBondstoneOperationState()` mapping error if the store is used without
that mapping.

`AddBondstone` also validates durable-messaging capability declarations after
host configuration runs. A module that calls `UseDurableMessaging` must
declare persistence through `UsePersistence` or a provider-specific opt-in
such as `UseEntityFrameworkCorePersistence<TDbContext>`. A durable command
handler or durable event subscriber can be registered only on a module that
uses durable messaging. These checks fail during composition with module,
handler, subscriber, and message identity details so incomplete durable
message loops do not reach runtime dispatch.

Module event registration lives under `module.Events`. Published integration
events can be registered with `RegisterPublishedEvent`; event subscribers can
be registered with `RegisterSubscriber<TEvent, THandler>` where handlers
implement `IIntegrationEventHandler<TEvent>`. Registration records stable
message identity, subscriber module, stable subscriber identity, and handler
type.

First-class subscriber execution is accepted by
[ADR 0033](../adr/0033-first-class-event-publish-subscribe-topology.md). Core
provides `IModuleEventSubscriberExecutor`, which resolves subscribers by
module, stable event identity, and stable subscriber identity, then executes
typed `IIntegrationEventHandler<TEvent>` handlers through an event-specific
subscriber pipeline. System pipeline behaviors set the module execution
context for the subscriber module and can wrap handler execution with a
per-subscriber receive inbox record. Modules that opt into EF persistence get
an EF event subscriber transaction behavior that commits event handler state,
inbox markers, and outgoing durable messages inside the subscriber module
boundary. Transport subscription binding is separate from event publish
dispatch and core subscriber execution.

## Persistence Capability

Persistence is a module capability. Core records neutral module persistence
metadata through `BondstoneModuleRegistration`, including whether a module uses
persistence, the provider capability name, and an optional provider context
type. Provider packages use that metadata to attach provider-specific behavior
without moving provider dependencies into `Bondstone`.

`Bondstone.EntityFrameworkCore` exposes
`UseEntityFrameworkCorePersistence<TDbContext>` on `BondstoneModuleBuilder`.
That module-owned opt-in records the module's DbContext type, registers the
provider-neutral EF durable stores and persistence scope, and attaches the EF
module transaction behavior.

For modules that opt into EF persistence, module command execution runs inside
`IEntityFrameworkCorePersistenceScope`. The EF behavior wraps validation and
handler execution through the command pipeline, then calls `SaveChangesAsync`
before the scope commits a transaction it owns. Modules without EF persistence
continue through the command executor without EF transaction wrapping.

For durable messaging, module-owned EF persistence is resolved by module name
when provider-specific module bindings are configured. A source module sends
through its own outbox. A target module receives through its own inbox and EF
transaction, saving handler state, inbox markers, operation completion, and
any outgoing outbox messages together.

Receive-side inbox handling can run inside the module command pipeline when a
transport passes a durable inbox record into `IModuleCommandExecutor`.
Receive failure state, retry state, stale receive recovery, and receive
acknowledgement policy remain later durable-boundary pieces.

## Receive Inbox

Transport adapters can pass a durable inbox record when dispatching into
`IModuleCommandExecutor`. The inbox record is derived from the transport
envelope and module command route metadata.

When an inbox record is present, a Bondstone-owned inbox system behavior
wraps the rest of the module command or event subscriber pipeline with
`IDurableInboxHandlerExecutor`. The lower-level inbox executor still stages
receive and processed markers, but the module behavior supplies a no-op
commit delegate so the outer module transaction behavior can save handler
state, outbox messages, and inbox markers together. Command execution returns
`ModuleCommandExecutionResult`; event subscriber execution returns
`ModuleEventSubscriberExecutionResult`. Transport adapters can inspect the
inbox result without using ambient receive state.

## Source-Module Scope

`IModuleCommandExecutor` sets a current module execution context while a
command pipeline is running. `IDurableCommandSender` uses that context to stage
outgoing durable command envelopes with the executing module as
`SourceModule`.

Durable command sending is therefore tied to module command execution. Sending
outside a module command context fails instead of guessing the source module.
HTTP endpoints that need module command behavior should call
`IModuleCommandExecutor`; direct services that do not need the module boundary
should avoid durable send APIs unless they are explicitly inside a module
command handler.

## Transport Binding

Transport is host topology. Modules declare durable messaging capability and
command handlers; transport adapters configure infrastructure Bondstone cannot
infer, such as broker connection strings, queue names, endpoint names,
exchange/topic names, routing keys, and subscriptions.

For Rebus, module queue conventions and explicit target-module-to-address
overrides are configured through the host-owned transport builder. Explicit
routes are overrides for legacy, extracted, or otherwise non-conventional
topology; applications should not need a pairwise module route matrix.

The Rebus transport builder also records receive endpoint topology: an
endpoint name can accept one or more local modules, and configuring receive
topology registers the module command receive pipeline and endpoint
dispatcher. The dispatcher accepts a Rebus endpoint name plus a Bondstone wire
envelope, validates that the endpoint exists and accepts the envelope target
module, then delegates to the module command receive pipeline. The Rebus
module command endpoint handler binds a configured endpoint name to that
dispatcher so a normal Rebus receive loop can dispatch command envelopes into
`IModuleCommandExecutor`. Receive bindings also derive outgoing command
destinations for the accepted modules unless an explicit route overrides them.
When a module queue convention is configured, outgoing commands can route to
target modules by convention even if this host does not receive that module
locally. The intended shape is listener binding to local modules, not a
generic route table. Rebus broker, worker, retry, dead-letter, serializer, and
input queue setup remains application-owned Rebus configuration.

Rebus receive topology is validated at `AddBondstone` composition time. Every
accepted module on a Rebus receive endpoint must be registered in the host,
must use durable messaging, and must have at least one durable command handler.
This validates local listener bindings only; remote outbound destinations can
still be supplied through explicit routes or module queue conventions.

Prefer one command receive endpoint per module when the module may need
independent deployment, scaling, retry/dead-letter policy, or operational
ownership. Group modules on one endpoint only when they are intentionally part
of the same host-level processing unit. A shared database inbox is fine; a
shared transport queue should be chosen deliberately because it couples
backlog, throughput, and failure recovery.

Event topology uses topic and subscription language instead of command route
language. A durable event topic represents the publish side of an integration
event identity, while a subscriber endpoint or subscription binding represents
one module subscriber's copy and inbox identity. Hosts own the concrete topic,
exchange, queue, subscription storage, listener, retry, and dead-letter
configuration through the transport provider.

Topic-based transports may expose different topology while keeping the same
module concepts: route outgoing durable commands by stable target module, bind
listener endpoints to locally accepted modules, and keep transport-native
exchange, topic, routing-key, and subscription details in the host.

Wolverine provides a useful comparison point: it discovers handlers, applies
local routing automatically for known local handlers, lets explicit routing
override conventions, and configures listening endpoints separately from
handler registration. Bondstone should borrow that separation of concerns,
while keeping routing by stable module and durable message identity rather
than CLR type names.
