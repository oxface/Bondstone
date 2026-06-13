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
- provider and transport adapter integration;
- eventual service extraction shape.

Future sample ideas are tracked in
[backlog/00-plans.md](backlog/00-plans.md).

## Modular Monolith Sample

The current `samples/ModularMonolith` project is an adoption-proof minimal API
sample. It composes `ordering`, `fulfillment`, and `billing` modules through
module-owned `IBondstoneModule` registration objects.

Ordering and fulfillment use separate module-owned EF Core `DbContext` types
and PostgreSQL schemas. Billing uses `Bondstone.Persistence.Postgres` with its
own schema. Ordering publishes `OrderPlacedEvent` and sends a durable command
to fulfillment. Fulfillment handles the command, records state, persists a
module-local domain event record through the EF domain-event capability, and
publishes `InventoryReservedEvent`. Ordering and billing subscribe to
integration events and record projections/invoices through their own module
persistence boundaries.

The sample uses `Bondstone.Transport.Local` through explicit local queue
routing by default. The local adapter dispatches claimed outbox records into
the provider-neutral `IModuleCommandReceivePipeline` and
`IModuleEventReceivePipeline`. This keeps the sample proving outbox claiming,
outbox dispatch recording, inbox handling, command/event execution, module
transactions, mixed persistence, and operation state without presenting local
transport as production broker guidance.

The focused smoke test lives in
[`tests/Bondstone.Samples.Tests`](../tests/Bondstone.Samples.Tests) and is an
`Integration` test because it uses Testcontainers PostgreSQL. Default fast
verification remains `Unit` and `Application` only; run sample smoke coverage
with the repository integration test entrypoint.

The sample is supplemented with one preferred direct provider path:
`AddModularMonolithSampleWithRabbitMq(...)`. The RabbitMQ path is covered by
an explicit Testcontainers RabbitMQ sample smoke test. Do not turn the first
sample into a broker matrix.

## Service-Split Readiness

The current service-split proof is documentation over the existing modular
monolith sample, not a separate sample application. The bounded extraction
story is:

- keep `ordering`, `fulfillment`, and `billing` as module-owned assemblies
  with explicit contract assemblies where messages cross module boundaries;
- use separate PostgreSQL schemas and module-owned persistence as the local
  extraction pressure;
- treat `fulfillment` as the first candidate for a future separate host
  because it already handles a durable command and publishes an integration
  event;
- use the RabbitMQ path as the preferred direct-provider proof when a broker
  boundary is needed;
- keep broker provisioning, deployment, authentication, and product UI outside
  the sample.

Do not add a second sample host until a concrete verification need justifies
the extra maintenance cost. A later service-split sample should be one bounded
host extraction, not a broker/provider matrix or product application.
