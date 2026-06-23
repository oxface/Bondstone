using Bondstone.Persistence.EntityFrameworkCore.IncomingInbox;
using Bondstone.Persistence.EntityFrameworkCore.Postgres.Persistence;

namespace Bondstone.Persistence.EntityFrameworkCore.Postgres.IncomingInbox;

internal static class PostgreSqlIncomingInboxTableIdentifier
{
    public static string BuildTableName(string? schema)
    {
        return PostgreSqlTableIdentifier.Build(
            IncomingInboxMessageEntityConfiguration.TableName,
            schema);
    }
}
