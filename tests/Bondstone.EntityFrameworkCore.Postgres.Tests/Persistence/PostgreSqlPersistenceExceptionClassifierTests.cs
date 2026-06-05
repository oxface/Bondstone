using Bondstone.EntityFrameworkCore.Inbox;
using Bondstone.EntityFrameworkCore.Postgres.Persistence;
using Npgsql;
using Xunit;

namespace Bondstone.EntityFrameworkCore.Postgres.Tests.Persistence;

public sealed class PostgreSqlPersistenceExceptionClassifierTests
{
    [Fact]
    [Trait("Category", "Unit")]
    public void IsUniqueViolation_WhenExceptionIsNull_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            PostgreSqlPersistenceExceptionClassifier.IsUniqueViolation(null!));
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void IsUniqueViolation_WhenInnerPostgresExceptionIsUniqueViolation_ReturnsTrue()
    {
        var exception = new InvalidOperationException(
            "Wrapper",
            CreatePostgresException(
                PostgresErrorCodes.UniqueViolation,
                InboxMessageEntityConfiguration.PrimaryKeyName));

        Assert.True(PostgreSqlPersistenceExceptionClassifier.IsUniqueViolation(exception));
        Assert.True(PostgreSqlPersistenceExceptionClassifier.IsUniqueViolation(
            exception,
            InboxMessageEntityConfiguration.PrimaryKeyName));
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void IsUniqueViolation_WhenConstraintDoesNotMatch_ReturnsFalse()
    {
        var exception = new InvalidOperationException(
            "Wrapper",
            CreatePostgresException(
                PostgresErrorCodes.UniqueViolation,
                InboxMessageEntityConfiguration.PrimaryKeyName));

        Assert.False(PostgreSqlPersistenceExceptionClassifier.IsUniqueViolation(exception, "other_constraint"));
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void IsUniqueViolation_WhenPostgresExceptionIsDifferentSqlState_ReturnsFalse()
    {
        var exception = CreatePostgresException(PostgresErrorCodes.ForeignKeyViolation, "fk_messages");

        Assert.False(PostgreSqlPersistenceExceptionClassifier.IsUniqueViolation(exception));
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void FindPostgresException_WhenNestedPostgresExceptionExists_ReturnsIt()
    {
        PostgresException postgresException =
            CreatePostgresException(
                PostgresErrorCodes.UniqueViolation,
                InboxMessageEntityConfiguration.PrimaryKeyName);
        var exception = new InvalidOperationException("Wrapper", postgresException);

        Assert.Same(
            postgresException,
            PostgreSqlPersistenceExceptionClassifier.FindPostgresException(exception));
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void IsInboxMessageDuplicate_WhenConstraintMatchesInboxPrimaryKey_ReturnsTrue()
    {
        var exception = new InvalidOperationException(
            "Wrapper",
            CreatePostgresException(
                PostgresErrorCodes.UniqueViolation,
                InboxMessageEntityConfiguration.PrimaryKeyName));

        Assert.True(PostgreSqlPersistenceExceptionClassifier.IsInboxMessageDuplicate(exception));
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void IsInboxMessageDuplicate_WhenConstraintDoesNotMatchInboxPrimaryKey_ReturnsFalse()
    {
        var exception = new InvalidOperationException(
            "Wrapper",
            CreatePostgresException(
                PostgresErrorCodes.UniqueViolation,
                "PK_outbox_messages"));

        Assert.False(PostgreSqlPersistenceExceptionClassifier.IsInboxMessageDuplicate(exception));
    }

    private static PostgresException CreatePostgresException(
        string sqlState,
        string constraintName)
    {
        return new PostgresException(
            "message",
            "ERROR",
            "ERROR",
            sqlState,
            constraintName: constraintName);
    }
}
