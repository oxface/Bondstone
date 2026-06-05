using Bondstone.EntityFrameworkCore.Outbox;
using Bondstone.EntityFrameworkCore.Postgres.Persistence;

namespace Bondstone.EntityFrameworkCore.Postgres.Outbox;

internal static class PostgreSqlOutboxTableIdentifier
{
    public static string BuildTableName(string? schema)
    {
        return PostgreSqlTableIdentifier.Build(
            OutboxMessageEntityConfiguration.TableName,
            schema);
    }
}
