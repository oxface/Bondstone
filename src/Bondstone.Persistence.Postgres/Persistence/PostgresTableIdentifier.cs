namespace Bondstone.Persistence.Postgres.Persistence;

internal static class PostgresTableIdentifier
{
    public static string Build(string tableName, string? schema)
    {
        return string.IsNullOrWhiteSpace(schema)
            ? QuoteIdentifier(tableName)
            : $"{QuoteIdentifier(schema.Trim())}.{QuoteIdentifier(tableName)}";
    }

    public static string QuoteIdentifier(string value)
    {
        return "\"" + value.Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";
    }
}
