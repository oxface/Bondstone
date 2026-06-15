namespace Bondstone.Messaging;

public enum DurableOperationResultState
{
    Unknown = 0,
    Pending = 1,
    Running = 2,
    CompletedWithResult = 3,
    CompletedWithoutResult = 4,
    Failed = 5,
    Cancelled = 6,
    ResultDeserializationFailed = 7,
}
