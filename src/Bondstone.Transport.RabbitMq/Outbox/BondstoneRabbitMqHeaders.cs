namespace Bondstone.Transport.RabbitMq.Outbox;

public static class BondstoneRabbitMqHeaders
{
    public const string MessageId = "bondstone-message-id";
    public const string MessageKind = "bondstone-message-kind";
    public const string MessageTypeName = "bondstone-message-type-name";
    public const string SourceModule = "bondstone-source-module";
    public const string TargetModule = "bondstone-target-module";
    public const string DurableOperationId = "bondstone-durable-operation-id";
    public const string CausationId = "bondstone-causation-id";
    public const string PartitionKey = "bondstone-partition-key";
    public const string TraceParent = "traceparent";
    public const string TraceState = "tracestate";
    public const string Baggage = "baggage";
}
