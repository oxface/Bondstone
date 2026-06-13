using Bondstone.Persistence.EntityFrameworkCore.Outbox;
using Bondstone.Persistence.EntityFrameworkCore.Postgres.Persistence;

namespace Bondstone.Persistence.EntityFrameworkCore.Postgres.Outbox;

internal static class PostgreSqlOutboxTableIdentifier
{
    public static string BuildTableName(string? schema)
    {
        return PostgreSqlTableIdentifier.Build(
            OutboxMessageEntityConfiguration.TableName,
            schema);
    }
}
