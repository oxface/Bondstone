using Bondstone.Persistence.EntityFrameworkCore.Inbox;
using Bondstone.Persistence.EntityFrameworkCore.IncomingInbox;
using Bondstone.Persistence.EntityFrameworkCore.Operations;
using Bondstone.Persistence.EntityFrameworkCore.Outbox;
using Bondstone.Persistence.EntityFrameworkCore.Persistence;
using Bondstone.Persistence.EntityFrameworkCore.Postgres.Inbox;
using Bondstone.Persistence.EntityFrameworkCore.Postgres.Outbox;
using Bondstone.Persistence.EntityFrameworkCore.Postgres.Persistence;
using Bondstone.Messaging;
using Bondstone.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests.Persistence;

public sealed partial class PostgreSqlPersistenceTests
{
    [Fact]
    [Trait("Category", "Integration")]
    public async Task ApplyBondstonePersistence_WhenDatabaseIsCreated_CreatesBondstoneTables()
    {
        await ResetDatabaseAsync();
        await using PostgreSqlTestDbContext context = CreateContext();

        string[] tableNames = await context
            .Database
            .SqlQueryRaw<string>(
                """
                SELECT table_name AS "Value"
                FROM information_schema.tables
                WHERE table_schema = 'public'
                AND table_name IN ('outbox_messages', 'inbox_messages', 'incoming_inbox_messages', 'operation_states')
                ORDER BY table_name
                """)
            .ToArrayAsync();

        Assert.Equal(
            ["inbox_messages", "incoming_inbox_messages", "operation_states", "outbox_messages"],
            tableNames);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task ApplyBondstonePersistence_WhenDatabaseIsCreated_CreatesExpectedPrimaryKeys()
    {
        await ResetDatabaseAsync();
        await using PostgreSqlTestDbContext context = CreateContext();

        string[] primaryKeys = await context
            .Database
            .SqlQueryRaw<string>(
                """
                SELECT rel.relname || ':' || con.conname AS "Value"
                FROM pg_constraint con
                JOIN pg_class rel ON rel.oid = con.conrelid
                JOIN pg_namespace nsp ON nsp.oid = rel.relnamespace
                WHERE con.contype = 'p'
                AND nsp.nspname = 'public'
                AND rel.relname IN ('outbox_messages', 'inbox_messages', 'incoming_inbox_messages', 'operation_states')
                ORDER BY rel.relname
                """)
            .ToArrayAsync();

        Assert.Equal(
            [
                $"inbox_messages:{InboxMessageEntityConfiguration.PrimaryKeyName}",
                $"incoming_inbox_messages:{IncomingInboxMessageEntityConfiguration.PrimaryKeyName}",
                "operation_states:PK_operation_states",
                "outbox_messages:PK_outbox_messages",
            ],
            primaryKeys);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task ApplyBondstonePersistence_WhenDatabaseIsCreated_CreatesOutboxClaimLeaseColumns()
    {
        await ResetDatabaseAsync();
        await using PostgreSqlTestDbContext context = CreateContext();

        string[] columnNames = await context
            .Database
            .SqlQueryRaw<string>(
                """
                SELECT column_name AS "Value"
                FROM information_schema.columns
                WHERE table_schema = 'public'
                AND table_name = 'outbox_messages'
                AND column_name IN ('ClaimedBy', 'ClaimedUntilUtc')
                ORDER BY column_name
                """)
            .ToArrayAsync();

        Assert.Equal(["ClaimedBy", "ClaimedUntilUtc"], columnNames);
    }
}
