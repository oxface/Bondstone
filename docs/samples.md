# Samples

This document records the current Bondstone sample direction.

## Purpose

Samples exist to test and demonstrate Bondstone usage. They should apply
pressure across composition, persistence, transport, package references, local
tooling, and end-to-end workflows.

Samples are not product applications and must not drive product behavior into
library packages.

## Sample Shape

Samples should stay intentionally small and operationally boring:

- no authentication;
- no product-grade UI;
- no product-specific domain depth beyond what is needed to exercise
  Bondstone behavior;
- no deployment story beyond local verification.

Samples should demonstrate:

- modular monolith composition;
- module-owned persistence;
- durable message identity and registration;
- inbox/outbox behavior;
- provider integration and explicit local transport behavior;
- eventual service extraction shape.

## Modular Monolith Sample

The current `samples/ModularMonolith` project is an adoption-proof minimal API
sample. It composes `ordering`, `fulfillment`, and `billing` modules through
module-owned `IBondstoneModule` registration objects.

Ordering, fulfillment, and billing use separate module-owned EF Core
`DbContext` types and PostgreSQL schemas. Ordering publishes `OrderPlacedEvent` and sends a durable
result-returning command to fulfillment. Fulfillment handles the command,
records state, persists a module-local domain event record through the EF
domain-event persistence opt-in, publishes `InventoryReservedEvent`, and stores the
reservation result in durable operation state. Ordering and billing subscribe
to integration events and record projections/invoices through their own module
persistence boundaries.

The sample uses `Bondstone.Transport.Local` with
`UseModuleQueueConvention()` for local durable command routing and explicit
event subscriber bindings for integration events. The local adapter dispatches
claimed outbox records into the provider-neutral
`IModuleCommandReceivePipeline` and `IModuleEventReceivePipeline`. This keeps
the sample proving outbox claiming, outbox dispatch recording, inbox handling,
command/event execution, module transactions, EF/PostgreSQL persistence, operation
state, and result payload persistence without presenting local transport as
production broker guidance.

The sample does not check in generated migrations. Its smoke-test database
preparation uses EF-generated create scripts for module schemas. Consumer
applications should create normal module-owned migrations from the EF
`DbContext` types so application tables, Bondstone durable tables from
`ApplyBondstonePersistence(...)`, and optional domain-event tables from
`ApplyBondstoneDomainEvents(...)` are included in the module migration
history.

The focused smoke tests live in
[`tests/Bondstone.Samples.Tests`](../tests/Bondstone.Samples.Tests) and is an
`Integration` suite because they use Testcontainers PostgreSQL. They prove
durable command delivery, processed inbox idempotency, persisted operation
result reading, dispatched outbox evidence, the integration-event loop, and a
bounded service-split proof where ordering and fulfillment run in separate
service providers over the same PostgreSQL instance.
Default fast verification remains `Unit` and `Application` only; run sample
smoke coverage with the repository integration test entrypoint.

## Service-Split Readiness

The current service-split proof is a tested split of the existing modular
monolith sample, not a second product application. The bounded extraction
story is:

- keep `ordering`, `fulfillment`, and `billing` as module-owned assemblies
  with explicit contract assemblies where messages cross module boundaries;
- use separate PostgreSQL schemas and module-owned persistence as the local
  extraction pressure;
- treat `fulfillment` as the documented extraction candidate because it already
  handles a durable command and publishes an integration event;
- register remote outgoing contracts with `BondstoneBuilder.RegisterMessage<T>()`
  so the source service can serialize commands without referencing target
  implementation assemblies;
- use app-owned broker code around `IDurableEnvelopeDispatcher` and
  `IDurableEnvelopeReceiver` when a broker boundary is needed;
- observe durable operation results from the target service because the target
  module owns the completed result state;
- keep broker provisioning, deployment, authentication, and product UI outside
  the sample.

The integration proof uses a test-owned in-memory broker bridge that serializes
`DurableMessageEnvelope` values and calls the receiving service's
`IDurableEnvelopeReceiver`. It intentionally avoids a broker/provider matrix;
real hosts can replace that bridge with Rebus, RabbitMQ.Client, Azure Service
Bus, Kafka, or another app-owned transport runtime.
