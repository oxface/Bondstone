# Module Architecture

Bondstone modules are service-shaped units that can run close together inside
a modular monolith or later move behind a transport boundary.

## Ownership Split

Modules own their durable capabilities:

- stable module name;
- module command handlers;
- module command validators;
- message identities for commands they handle;
- module persistence capability;
- durable messaging capability when the module sends or receives durable
  commands;
- future module transaction, inbox, outbox, and operation-state behavior.

Hosts own deployment topology and transport infrastructure:

- which modules are loaded in a process;
- which modules are local or remote;
- connection strings and environment-specific settings;
- transport adapters and target-module address maps;
- Rebus endpoint names, queue names, retry policy, and workers;
- exchange, topic, routing-key, subscription, and listener names for other
  transports;
- process-level hosted services and operational policy.

Module code should not need to know whether another module is local,
remote, or Rebus-backed. It can depend on stable module names and durable
message contracts. The host decides how commands reach the target module.

## Module Registration

`AddBondstone` is the host composition entrypoint. A host can register module
capabilities inline through `Module`, or a module can provide its own
`IBondstoneModule` registration object and be stitched into the host with
`AddModule`.

Handlers and validators can be discovered with `RegisterFromAssembly` or
registered explicitly with `RegisterHandler` and `RegisterValidator`. Keep
library-user setup examples in [../setup.md](../setup.md).

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
sends can record the executing module as the source module. Future pipeline
behavior will add operation-state updates, receive tracing, and deeper module
identity scopes.

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

The normal application-facing shape should be one module capability such as
`UseDurableMessaging`, not separate inbox/outbox toggles. Durable messaging
implies the module needs the persistence and pipeline pieces required for
durable send and receive: outbox writing, inbox registration/storage, module
transaction behavior, source-module scope for send, and durable receive
orchestration. Advanced APIs may later expose separate inbox, outbox, or
operation-state pieces, but they should not be the common path.

Current core registration records module metadata and durable messaging
capability through `IBondstoneModuleRegistry`. That metadata is groundwork for
later validation and pipeline behavior.

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

This is groundwork for the full durable boundary. Receive-side inbox handling
can now run inside the module command pipeline when a transport passes a
durable inbox record into `IModuleCommandExecutor`. Operation-state updates,
receive-side outbox coordination, actual listener binding, and receive
acknowledgement still need later slices before durable receive is fully
app-facing.

## Receive Inbox

Transport adapters can pass a durable inbox record when dispatching into
`IModuleCommandExecutor`. The inbox record is derived from the transport
envelope and module command route metadata.

When an inbox record is present, a Bondstone-owned inbox system behavior
wraps the rest of the module command pipeline with `IDurableInboxHandlerExecutor`.
The lower-level inbox executor still stages receive and processed markers, but
the module behavior supplies a no-op commit delegate so the outer module
transaction behavior can save handler state, outbox messages, and inbox
markers together. The executor returns `ModuleCommandExecutionResult`, which
allows transport adapters to inspect the inbox result without using ambient
receive state.

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

For Rebus, outgoing target-module-to-address mapping is configured through the
host-owned transport builder. The current implemented route shape maps target
modules to queue names or destination addresses.

The Rebus transport builder also records receive endpoint topology: an
endpoint name can accept one or more local modules, and configuring receive
topology registers the module command receive pipeline. The intended shape is
listener binding to local modules, not a generic route table. Actual Rebus
worker/listener binding to the configured endpoint metadata remains future
work.

Prefer one command receive endpoint per module when the module may need
independent deployment, scaling, retry/dead-letter policy, or operational
ownership. Group modules on one endpoint only when they are intentionally part
of the same host-level processing unit. A shared database inbox is fine; a
shared transport queue should be chosen deliberately because it couples
backlog, throughput, and failure recovery.

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
