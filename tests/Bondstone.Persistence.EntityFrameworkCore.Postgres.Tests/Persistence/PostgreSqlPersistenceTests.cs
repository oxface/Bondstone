using Bondstone.Persistence.EntityFrameworkCore.Outbox;
using Bondstone.Persistence.EntityFrameworkCore.Persistence;
using Bondstone.Messaging;
using Bondstone.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests.Persistence;

public sealed partial class PostgreSqlPersistenceTests : IClassFixture<PostgreSqlFixture>
{
    private readonly PostgreSqlFixture _fixture;

    public PostgreSqlPersistenceTests(PostgreSqlFixture fixture)
    {
        _fixture = fixture;
    }

    private async Task ResetDatabaseAsync()
    {
        await using PostgreSqlTestDbContext context = CreateContext();
        await context.Database.EnsureDeletedAsync();
        await context.Database.EnsureCreatedAsync();
    }

    private PostgreSqlTestDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<PostgreSqlTestDbContext>()
            .UseNpgsql(_fixture.ConnectionString)
            .Options;

        return new PostgreSqlTestDbContext(options);
    }

    private async Task ResetSchemaDatabaseAsync()
    {
        await using PostgreSqlSchemaTestDbContext context = CreateSchemaContext();
        await context.Database.EnsureDeletedAsync();
        await context.Database.EnsureCreatedAsync();
    }

    private PostgreSqlSchemaTestDbContext CreateSchemaContext()
    {
        var options = new DbContextOptionsBuilder<PostgreSqlSchemaTestDbContext>()
            .UseNpgsql(_fixture.ConnectionString)
            .Options;

        return new PostgreSqlSchemaTestDbContext(options);
    }

    private static async Task<Guid> SelectNextPendingOutboxMessageForUpdateAsync(
        PostgreSqlTestDbContext context)
    {
        return await context
            .Database
            .SqlQueryRaw<Guid>(
                """
                SELECT "MessageId" AS "Value"
                FROM outbox_messages
                WHERE "Status" = 'Pending'
                ORDER BY "StoredAtUtc", "MessageId"
                FOR UPDATE SKIP LOCKED
                LIMIT 1
                """)
            .SingleAsync();
    }

    private async Task WriteOutboxMessagesAsync(
        params (Guid MessageId, DateTimeOffset StoredAtUtc)[] messages)
    {
        await using PostgreSqlTestDbContext context = CreateContext();

        foreach ((Guid messageId, DateTimeOffset storedAtUtc) in messages)
        {
            var writer = new EntityFrameworkCoreDurableOutboxWriter<PostgreSqlTestDbContext>(
                context,
                new FixedTimeProvider(storedAtUtc));

            await writer.WriteAsync(CreateEnvelope(messageId));
        }

        await context.SaveChangesAsync();
    }

    private async Task MarkOutboxMessageProcessingAsync(
        Guid messageId,
        string claimedBy,
        DateTimeOffset claimedUntilUtc)
    {
        await using PostgreSqlTestDbContext context = CreateContext();

        await context.Database.ExecuteSqlInterpolatedAsync(
            $"""
            UPDATE outbox_messages
            SET
                "Status" = 'Processing',
                "AttemptCount" = 1,
                "ClaimedBy" = {claimedBy},
                "ClaimedUntilUtc" = {claimedUntilUtc}
            WHERE "MessageId" = {messageId}
            """);
    }

    private async Task MarkOutboxMessageNextAttemptAsync(
        Guid messageId,
        DateTimeOffset nextAttemptAtUtc)
    {
        await using PostgreSqlTestDbContext context = CreateContext();

        await context.Database.ExecuteSqlInterpolatedAsync(
            $"""
            UPDATE outbox_messages
            SET "NextAttemptAtUtc" = {nextAttemptAtUtc}
            WHERE "MessageId" = {messageId}
            """);
    }

    private async Task WriteClaimedOutboxMessageAsync(
        Guid messageId,
        string claimedBy,
        DateTimeOffset? claimedUntilUtc = null)
    {
        await WriteOutboxMessagesAsync(
            (messageId, DateTimeOffset.Parse("2026-06-04T00:00:01+00:00")));

        await MarkOutboxMessageProcessingAsync(
            messageId,
            claimedBy,
            claimedUntilUtc ?? DateTimeOffset.Parse("2026-06-04T00:05:00+00:00"));
    }

    private static DurableMessageEnvelope CreateEnvelope(Guid? messageId = null)
    {
        return new DurableMessageEnvelope(
            messageId ?? Guid.Parse("48cb19e0-3689-4ec7-b629-8f8e19916d43"),
            MessageKind.Command,
            "orders.submit.v1",
            "sales",
            "fulfillment",
            """{"orderId":"A-100"}""",
            DateTimeOffset.Parse("2026-06-04T00:00:00+00:00"),
            durableOperationId: Guid.Parse("a0e7c46f-2699-40ec-888a-267b9323a164"),
            traceContext: new MessageTraceContext(
                "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-00"),
            causationId: Guid.Parse("e01d0600-18dd-4573-9947-5c6a72eca8ab"),
            partitionKey: "orders/A-100");
    }

    private static DurableInboxRecord CreateInboxRecord(Guid? messageId = null)
    {
        return new DurableInboxRecord(
            new DurableInboxMessageKey(
                messageId ?? Guid.Parse("4d2fa8ff-3375-4cde-a751-b3cc73da171e"),
                "fulfillment",
                "fulfillment.submit-order.v1"),
            DateTimeOffset.Parse("2026-06-04T00:00:02+00:00"));
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow()
        {
            return utcNow;
        }
    }
}
