# Hosting Architecture

`Bondstone.Hosting` owns reusable .NET hosted workers that compose Bondstone
core abstractions.

The package depends on `Bondstone` and
`Microsoft.Extensions.Hosting`/options/logging abstractions. It does not own EF
Core mappings, PostgreSQL SQL, Rebus routing, transport envelopes, handler
discovery, or broker configuration.

## Outbox Worker

`DurableOutboxWorker` is a standard .NET hosted service over
`IDurableOutboxDispatcher`. It does not send directly through a transport and
does not own provider-specific SQL.

The worker uses `DurableOutboxWorkerOptions` for worker id, lease duration,
batch size, polling interval, and failure delay. It dispatches one batch per
call, immediately continues while rows are claimed, waits for the polling
interval when no rows are claimed, logs unexpected failures, waits for the
failure delay, and then continues. Host cancellation stops the loop.

The worker follows the same competitive-consumer model as provider claimers.
Multiple workers can run when the provider claim implementation supports
skip-locked or equivalent semantics.

`AddBondstone` is the preferred host registration path. Package-specific
extensions mark what they contribute, and the builder rejects hosted outbox
processing when persistence or transport capability is missing.

Typical host registration:

```csharp
services.AddBondstone(bondstone =>
{
    bondstone.UsePostgreSqlPersistence<AppDbContext>(connectionString);
    bondstone.Outbox.UseRebusTransport(
        new Dictionary<string, string>
        {
            ["fulfillment"] = "fulfillment-queue",
        });
    bondstone.Outbox.UseWorker(options =>
    {
        options.WorkerId = "orders-api-1";
        options.LeaseDuration = TimeSpan.FromMinutes(5);
        options.BatchSize = 100;
    });
});
```

Low-level registration methods remain available for tests and advanced
composition. For example, a consumer can register persistence and transport,
call `AddBondstoneDurableOutboxDispatcher`, and run a custom scheduler that
manually calls `IDurableOutboxDispatcher`. That is an advanced path; normal
host setup should prefer `AddBondstone`.

The worker resolves the dispatcher inside a service scope for each batch so
scoped persistence services are not captured by the singleton hosted service.
Startup validates that the dispatcher graph can be resolved and fails fast
when low-level registration omits required persistence or transport
dependencies.

## Future Workers

Future inbox and maintenance workers should live in `Bondstone.Hosting` when
their underlying core abstractions are stable. Examples include stale-claim
recovery, dead-letter retention, archiving, cleanup, and receive-side inbox
workers.

Provider SQL remains in provider packages. Transport-specific send, receive,
and envelope behavior remains in transport adapter packages.
