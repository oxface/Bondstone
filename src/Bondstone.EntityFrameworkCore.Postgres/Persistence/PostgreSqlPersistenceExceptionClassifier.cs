using Bondstone.EntityFrameworkCore.Inbox;
using Npgsql;

namespace Bondstone.EntityFrameworkCore.Postgres.Persistence;

public static class PostgreSqlPersistenceExceptionClassifier
{
    public static bool IsUniqueViolation(
        Exception exception,
        string? constraintName = null)
    {
        ArgumentNullException.ThrowIfNull(exception);

        PostgresException? postgresException = FindPostgresException(exception);
        if (postgresException?.SqlState != PostgresErrorCodes.UniqueViolation)
        {
            return false;
        }

        string? normalizedConstraintName = string.IsNullOrWhiteSpace(constraintName)
            ? null
            : constraintName.Trim();

        return normalizedConstraintName is null
            || StringComparer.Ordinal.Equals(postgresException.ConstraintName, normalizedConstraintName);
    }

    public static bool IsInboxMessageDuplicate(Exception exception)
    {
        return IsUniqueViolation(
            exception,
            InboxMessageEntityConfiguration.PrimaryKeyName);
    }

    public static PostgresException? FindPostgresException(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            if (current is PostgresException postgresException)
            {
                return postgresException;
            }
        }

        return null;
    }
}
