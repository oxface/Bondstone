# Samples

## Modular Monolith

[`ModularMonolith`](ModularMonolith) is the current adoption-proof minimal API
sample. It proves the current durable command and integration event loop with:

- module registration for `ordering`, `fulfillment`, and `billing`;
- mixed persistence with EF-backed ordering/fulfillment modules and a
  `Bondstone.Persistence.Postgres` billing module;
- module-owned assemblies with `IBondstoneModule` registration objects,
  assembly-scanned command handler registration, and explicit event
  registration;
- ordering and fulfillment contract assemblies that publish
  `OrderPlacedEvent` and `InventoryReservedEvent`;
- separate module-owned EF Core `DbContext` types and PostgreSQL schemas;
- a billing schema using `Bondstone.Persistence.Postgres`;
- EF-backed module-local domain event persistence in fulfillment;
- outbox-backed durable command sending from ordering to fulfillment;
- outbox-backed durable event publishing from ordering and fulfillment;
- durable outbox worker dispatch through explicit
  `Bondstone.Transport.Local` queue routing that calls the provider-neutral
  module receive pipelines;
- module command and event subscriber execution with receive inbox handling
  and handler state saved in the subscriber module transaction boundary.

The local transport is not a production broker adapter or hidden fallback. The
sample also exposes an explicit RabbitMQ registration path that keeps broker
connection and topology setup app-owned.

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
