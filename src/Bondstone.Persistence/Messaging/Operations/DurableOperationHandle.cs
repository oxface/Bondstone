using Bondstone.Utility;

namespace Bondstone.Messaging;

/// <summary>
/// Identifies a durable operation and the module boundaries that own its
/// acceptance receipt and eventual result state.
/// </summary>
public sealed record DurableOperationHandle
{
    public DurableOperationHandle(
        Guid durableOperationId,
        string sourceModule,
        string targetModule)
    {
        if (durableOperationId == Guid.Empty)
        {
            throw new ArgumentException(
                "Durable operation id must not be empty.",
                nameof(durableOperationId));
        }

        DurableOperationId = durableOperationId;
        SourceModule = sourceModule.NormalizeRequired(
            nameof(sourceModule),
            "Source module");
        TargetModule = targetModule.NormalizeRequired(
            nameof(targetModule),
            "Target module");
    }

    public Guid DurableOperationId { get; }

    public string SourceModule { get; }

    public string TargetModule { get; }
}
