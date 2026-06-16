# Samples

## Modular Monolith

[`ModularMonolith`](ModularMonolith) is the current adoption-proof minimal API
sample. It proves the current durable command and integration event loop with:

- module registration for `ordering`, `fulfillment`, and `billing`;
- EF-backed ordering, fulfillment, and billing modules;
- module-owned assemblies with `IBondstoneModule` registration objects,
  assembly-scanned command handler registration, and explicit event
  registration;
- ordering and fulfillment contract assemblies that publish
  `OrderPlacedEvent` and `InventoryReservedEvent`;
- separate module-owned EF Core `DbContext` types and PostgreSQL schemas;
- a billing schema using EF Core/PostgreSQL persistence;
- EF-backed module-local domain event persistence in fulfillment;
- outbox-backed durable command sending from ordering to fulfillment;
- outbox-backed durable event publishing from ordering and fulfillment;
- durable outbox worker dispatch through explicit
  `Bondstone.Transport.Local` command queue convention and explicit event
  subscriber bindings that call the provider-neutral module receive pipelines;
- module command and event subscriber execution with receive inbox handling
  and handler state saved in the subscriber module transaction boundary.

Start with these files when copying the setup into a modular monolith:

- [`ModularMonolith/ModularMonolithApplication.cs`](ModularMonolith/ModularMonolithApplication.cs)
  for host composition, `UseModuleQueueConvention()`, explicit event
  subscribers, the hosted outbox worker, database preparation, and status
  reads.
- [`ModularMonolith.Ordering/OrderingBondstoneModule.cs`](ModularMonolith.Ordering/OrderingBondstoneModule.cs)
  and [`ModularMonolith.Fulfillment/FulfillmentBondstoneModule.cs`](ModularMonolith.Fulfillment/FulfillmentBondstoneModule.cs)
  for module-owned `IBondstoneModule` registration.
- [`ModularMonolith.Ordering/OrderingDbContext.cs`](ModularMonolith.Ordering/OrderingDbContext.cs)
  and [`ModularMonolith.Fulfillment/FulfillmentDbContext.cs`](ModularMonolith.Fulfillment/FulfillmentDbContext.cs)
  for module-owned EF Core schemas, `ApplyBondstonePersistence`, and optional
  `ApplyBondstoneDomainEvents`.
- [`ModularMonolith.Fulfillment.Contracts/ReserveInventoryCommand.cs`](ModularMonolith.Fulfillment.Contracts/ReserveInventoryCommand.cs)
  and [`ModularMonolith.Fulfillment/ReserveInventoryHandler.cs`](ModularMonolith.Fulfillment/ReserveInventoryHandler.cs)
  for a durable command that returns a persisted operation result.
- [`../tests/Bondstone.Samples.Tests/ModularMonolithSampleTests.cs`](../tests/Bondstone.Samples.Tests/ModularMonolithSampleTests.cs)
  for the smoke test that proves durable delivery, processed inbox
  idempotency, persisted operation results, and dispatched outbox evidence.

The sample uses `EnsureCreated`/explicit SQL only for local smoke-test setup.
Consumer applications should generate normal EF Core migrations from their
module-owned `DbContext` types so each module migration includes both the
application tables and the Bondstone tables added by
`ApplyBondstonePersistence(...)`. Modules using EF-backed domain event
persistence should also include the tables added by
`ApplyBondstoneDomainEvents(...)`.

The local transport is not a production broker adapter or hidden fallback.
`UseModuleQueueConvention()` is complete local durable command topology for
module-to-module command dispatch. Event routes still bind subscribers
explicitly because subscriber module and subscriber identity are part of
durable receive identity. The sample also exposes an explicit RabbitMQ
registration path that keeps broker connection and topology setup app-owned.

## Service-Split Path

The current service-split proof is the modular-monolith sample shape, not a
second sample host. The bounded extraction path is to keep the contract
assemblies stable, keep module persistence isolated by schema, and treat
`fulfillment` as the documented extraction candidate because it receives a
durable command and publishes an integration event.

Use the RabbitMQ registration path when proving a broker boundary. Broker
provisioning, deployment, authentication, product UI, and a provider matrix
stay out of this sample.

Run the app with a PostgreSQL connection string:

```bash
BONDSTONE_SAMPLE_PREPARE_DATABASE=true \
  dotnet run --project samples/ModularMonolith -- "<connection-string>"
```

Create an order:

```bash
curl -X POST http://localhost:5000/orders \
  -H "Content-Type: application/json" \
  -d '{"sku":"coffee-mug","quantity":2}'
```

Read durable state with the returned order and operation ids:

```bash
curl "http://localhost:5000/orders/<order-id>?operationId=<operation-id>"
```

Run the infrastructure-backed smoke test with:

```bash
dotnet test tests/Bondstone.Samples.Tests/Bondstone.Samples.Tests.csproj --configuration Release --filter "Category=Integration"
```

See [docs/samples.md](../docs/samples.md) for the accepted sample direction.
