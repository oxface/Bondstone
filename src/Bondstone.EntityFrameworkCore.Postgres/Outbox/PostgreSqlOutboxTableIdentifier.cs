using Bondstone.EntityFrameworkCore.Outbox;

namespace Bondstone.EntityFrameworkCore.Postgres.Outbox;

internal static class PostgreSqlOutboxTableIdentifier
{
    public static string BuildTableName(string? schema)
    {
        return string.IsNullOrWhiteSpace(schema)
            ? QuoteIdentifier(OutboxMessageEntityConfiguration.TableName)
            : $"{QuoteIdentifier(schema.Trim())}.{QuoteIdentifier(OutboxMessageEntityConfiguration.TableName)}";
    }

    private static string QuoteIdentifier(string value)
    {
        return "\"" + value.Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";
    }
}
