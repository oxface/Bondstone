namespace Bondstone.Persistence.Dapper.Postgres.Persistence;

internal static class PostgresDapperDurableTableNames
{
    public const string OutboxMessages = "outbox_messages";
    public const string InboxMessages = "inbox_messages";
    public const string OperationStates = "operation_states";
}
