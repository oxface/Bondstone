namespace Bondstone.Persistence.Postgres.Persistence;

internal static class PostgresDurableTableNames
{
    public const string OutboxMessages = "outbox_messages";
    public const string InboxMessages = "inbox_messages";
    public const string OperationStates = "operation_states";
}
