# Persistence Architecture

Persistence docs are split by ownership boundary:

- [persistence-core.md](persistence-core.md) describes provider-neutral
  contracts in `Bondstone.Persistence` and the module-aware core resolution
  that composes them from `Bondstone`.
- [persistence-ef-core.md](persistence-ef-core.md) describes provider-neutral
  EF Core mappings, stores, and the EF persistence scope.
- [persistence-postgresql.md](persistence-postgresql.md) describes
  PostgreSQL-specific registration, SQL behavior, and integration coverage.

Current package ownership is recorded in [../packaging.md](../packaging.md).

## Cross-Cutting Rules

Provider-neutral persistence contracts stay independent from EF Core,
PostgreSQL, transport adapters, SQL locking, schema migration, and background
dispatch mechanics.

EF Core components stage data and expose transaction/save boundaries, but they
do not own transport acknowledgement, retry policy, or a generic mediator.
Module command execution and module event subscriber execution own handler
registration; modules that opt into EF persistence get EF transaction
runtime pipeline contributions for command handlers and event subscribers.
Transport-backed receive orchestration belongs in direct provider adapters
that call the provider-neutral module receive pipelines.

`ApplyBondstonePersistence` remains the convenience entrypoint for the durable
EF Core mapping bundle: outbox, inbox, and operation state.
`ApplyBondstoneDomainEvents` is intentionally separate because domain event
persistence is optional and module-local. The granular
`ApplyBondstoneOutbox`, `ApplyBondstoneInbox`,
`ApplyBondstoneOperationState`, and `ApplyBondstoneDomainEvents` helpers let
hosts map only the persistence pieces a DbContext needs. The current EF module
runtime validates that modules using `UseDurableMessaging` with EF persistence
have outbox and inbox mappings in their DbContext model, and modules that opt
into EF domain event persistence have the explicit domain event mapping.

In the command loop, durable sends resolve outbox and pending operation-state
writes from the source module persistence context. Durable receives resolve
inbox handling, successful operation completion, handler state, and any
outgoing outbox writes from the target module persistence context. Existing
single-`DbContext` setups remain supported, but modular-monolith samples should
prefer one module-owned `DbContext` per module.

Already-received but unprocessed inbox rows remain operationally loud. Current
Bondstone persistence has no inbox lease, stale-row sweeper, failed receive
state, or provider-neutral recovery hook that can prove handler re-execution
is safe. Applications that need to recover those rows own the inspection,
mutation, audit, and provider/broker coordination.

Module persistence metadata remains on current module registration. The
provider name marks that the module declares persistence and lets provider
transaction behaviors decide whether they own command or event subscriber
execution. EF Core module persistence also records the module `DbContext` type
so EF transaction behavior can resolve the context and validate required
durable messaging mappings. Future non-EF providers should keep
provider-specific metadata such as schema, session, connection, and SQL
configuration in provider-owned services.

Provider module helpers contribute passive durable module runtime
registrations into `DurableModulePersistenceRegistrationRegistry` rather than
registering executable module services directly in DI. Those registrations
carry the module name plus factories for command and receive execution
services: outbox writer, inbox executor, and operation-state store. The
factory for a selected module runs inside the current DI scope only when that
module needs the service. Provider factories should return lightweight wrappers
around services owned by the current DI scope, not owned disposable resources
that the scope cannot dispose.

Fallback non-module persistence services are intentionally supported advanced
composition for the remaining low-level paths that use them. When no
module-owned durable runtime registrations are registered, core resolvers may
use root-level outbox writer, inbox handler executor, and operation-state store
services. This is useful for low-level single-store composition and
compatibility tests, but it is not the preferred module-boundary setup.
`IDurableOperationReader` does not use root-level fallback readers or stores;
it reads only from configured module-owned operation-state stores.

## Domain Event Persistence

ADR 0028 accepts optional module-local domain event persistence. Domain event
persistence records private module facts inside the owning module persistence
boundary; it does not write outgoing outbox messages, create inbox records,
publish transport events, or expose domain events as integration contracts.

The core shape is intentionally small and lives under
`Bondstone.Capabilities.DomainEvents` in the
`Bondstone.Capabilities.DomainEvents` package: module domain objects may
implement
`IDomainEventSource` to expose pending `IDomainEvent` instances through
`PendingDomainEvents` and clear them through `ClearPendingDomainEvents()`.
Capability bridge packages may collect, stage, and clear those events when the
module opts into the capability. Persisted records should carry a stable
record id, owning module, `DomainEventIdentityAttribute` name, timestamps,
serialized payload, payload metadata, and trace or causation metadata when
available.

Bondstone does not dispatch local domain events. Registering
`IDomainEventHandler<TDomainEvent>` services does not cause Bondstone to
dispatch pending `IDomainEvent` instances from the module command or event
subscriber pipelines.

EF Core is the first accepted capability bridge implementation. Non-EF
PostgreSQL domain event staging is application-owned.

Domain event persistence activation stays narrow. There is no public
capability-step registry, public named pipeline-slot API, or generic provider
metadata registry. Capability bridge packages can contribute ordered
capability pipeline records through their setup APIs. The
`Bondstone.Capabilities.DomainEvents` package contains the shared contracts; the
`Bondstone.Capabilities.DomainEvents.EntityFrameworkCore` bridge activates
from a module's EF persistence declaration, an explicit
`UseEntityFrameworkCoreDomainEventPersistence()` module opt-in, and
bridge-owned EF services.

The EF runtime behavior belongs inside module command and integration event
subscriber execution. It collects and stages pending domain events after
application behavior and handler logic, while the module execution context is
still active, and before the EF transaction owner saves and commits. Pending
events are cleared only after collection, staging, save, and commit succeed.

Capability bridge packages own their persisted shapes and provider-specific
tests. Provider-owned SQL should reuse table, column, and constraint names
from the bridge mappings when those mappings define the persisted shape.

EF Core with PostgreSQL durability semantics is the supported persistence path
for the post-MVP MVP. The previous direct non-EF
`Bondstone.Persistence.Postgres` proof was removed after MVP. Future non-EF
persistence work should come from real consumer need and ADR review.
