using Bondstone.Samples.ModularMonolith;

string? connectionString = args.FirstOrDefault(static arg =>
        !arg.StartsWith("--", StringComparison.Ordinal))
    ?? Environment.GetEnvironmentVariable("BONDSTONE_SAMPLE_POSTGRES");

if (string.IsNullOrWhiteSpace(connectionString))
{
    Console.Error.WriteLine(
        "Pass a PostgreSQL connection string as the first argument, or set BONDSTONE_SAMPLE_POSTGRES.");
    return 1;
}

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddModularMonolithSample(connectionString);

WebApplication app = builder.Build();
app.MapModularMonolithSample();

bool resetDatabase =
    args.Contains("--reset-database", StringComparer.Ordinal)
    || string.Equals(
        Environment.GetEnvironmentVariable("BONDSTONE_SAMPLE_RESET_DATABASE"),
        "true",
        StringComparison.OrdinalIgnoreCase);
bool prepareDatabase =
    resetDatabase
    || string.Equals(
        Environment.GetEnvironmentVariable("BONDSTONE_SAMPLE_PREPARE_DATABASE"),
        "true",
        StringComparison.OrdinalIgnoreCase);

if (prepareDatabase)
{
    await app.Services.EnsureModularMonolithDatabaseAsync(resetDatabase);
}

await app.RunAsync();

return 0;
