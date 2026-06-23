using System.ComponentModel;

namespace Bondstone.Modules;

[EditorBrowsable(EditorBrowsableState.Never)]
public interface IModuleTransactionRunner
{
    ValueTask ExecuteAsync(
        IModuleRuntimeExecutionContext context,
        Func<CancellationToken, ValueTask> operation,
        CancellationToken ct = default);
}

[EditorBrowsable(EditorBrowsableState.Never)]
public interface IModulePostHandlerAction
{
    ValueTask RunAsync(
        IModuleRuntimeExecutionContext context,
        CancellationToken ct = default);
}
