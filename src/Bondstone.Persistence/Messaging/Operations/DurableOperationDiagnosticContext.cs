namespace Bondstone.Messaging;

public sealed record DurableOperationDiagnosticContext
{
    public DurableOperationDiagnosticContext(
        string? moduleName = null,
        string? messageTypeName = null,
        string? handlerIdentity = null)
    {
        ModuleName = string.IsNullOrWhiteSpace(moduleName)
            ? null
            : moduleName;
        MessageTypeName = string.IsNullOrWhiteSpace(messageTypeName)
            ? null
            : messageTypeName;
        HandlerIdentity = string.IsNullOrWhiteSpace(handlerIdentity)
            ? null
            : handlerIdentity;

        if (ModuleName is null
            && MessageTypeName is null
            && HandlerIdentity is null)
        {
            throw new ArgumentException(
                "At least one durable operation diagnostic context value must be supplied.");
        }
    }

    public string? ModuleName { get; }

    public string? MessageTypeName { get; }

    public string? HandlerIdentity { get; }
}
