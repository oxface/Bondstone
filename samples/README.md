# Samples

## Modular Monolith

[`ModularMonolith`](ModularMonolith) is the current Phase 4 adoption-proof
harness. It is intentionally closer to a verification app than a polished
consumer sample, and it proves the current durable command loop with:

- module registration for `ordering` and `fulfillment`;
- separate module-owned EF Core `DbContext` types and PostgreSQL schemas;
- outbox-backed durable command sending from ordering to fulfillment;
- Rebus in-memory transport and module receive endpoint binding;
- module command execution with receive inbox handling and handler state saved
  in the same EF transaction.

Run the app with a PostgreSQL connection string:

```bash
dotnet run --project samples/ModularMonolith -- "<connection-string>"
```

Run the infrastructure-backed smoke test with:

```bash
dotnet test tests/Bondstone.Samples.Tests/Bondstone.Samples.Tests.csproj --configuration Release --filter "Category=Integration"
```

See [docs/samples.md](../docs/samples.md) for the accepted sample direction.
