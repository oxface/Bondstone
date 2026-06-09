# Samples

## Modular Monolith

[`ModularMonolith`](ModularMonolith) is the current Phase 4 adoption-proof
minimal API sample. It proves the current durable command loop with:

- module registration for `ordering` and `fulfillment`;
- module-owned assemblies with `IBondstoneModule` registration objects and
  assembly-scanned command handler registration;
- separate module-owned EF Core `DbContext` types and PostgreSQL schemas;
- outbox-backed durable command sending from ordering to fulfillment;
- durable outbox worker dispatch through Rebus in-memory transport;
- Rebus service-provider registration and module receive endpoint binding;
- module command execution with receive inbox handling and handler state saved
  in the same EF transaction.

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
