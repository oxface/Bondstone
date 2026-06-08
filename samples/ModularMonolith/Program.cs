using Bondstone.Samples.ModularMonolith;

string? connectionString = args.FirstOrDefault()
    ?? Environment.GetEnvironmentVariable("BONDSTONE_SAMPLE_POSTGRES");

if (string.IsNullOrWhiteSpace(connectionString))
{
    Console.Error.WriteLine(
        "Pass a PostgreSQL connection string as the first argument, or set BONDSTONE_SAMPLE_POSTGRES.");
    return 1;
}

SampleRunResult result = await ModularMonolithSample.RunAsync(
    connectionString,
    resetDatabase: true);

Console.WriteLine(
    $"Order {result.OrderId} reserved. "
    + $"Orders={result.OrderCount}, Reservations={result.ReservationCount}, "
    + $"InboxProcessed={result.ProcessedInboxCount}, OutboxDispatched={result.DispatchedOutboxCount}, "
    + $"Operation={result.OperationStatus}.");

return 0;
